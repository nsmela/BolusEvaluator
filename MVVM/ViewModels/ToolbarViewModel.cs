using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FellowOakDicom;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Windows;
using BolusEvaluator.Messages;
using BolusEvaluator.Services.DicomService;
using Microsoft.Extensions.DependencyInjection;
using BolusEvaluator.Services.ImageOverlayService;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Linq;
using BolusEvaluator.Services.InputService;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
public partial class ToolbarViewModel {
    private readonly IDicomService _dicom;
    private readonly IImageOverlayService _imageService;
    private List<IImageTools> _imageTools;

    [ObservableProperty] private string? _currentTool = string.Empty; //placeholder for more advanced tools    

    private void IsBusy (bool value) => WeakReferenceMessenger.Default.Send(new IsBusyMessage(value));

    public ToolbarViewModel() {
        _dicom = App.AppHost.Services.GetService<IDicomService>();
        _imageService = App.AppHost.Services.GetService<IImageOverlayService>();
        _imageTools = new();
    }


    [RelayCommand]
    private async Task ShowHighlight() {
        if (_imageTools is null) return;
        if (_imageTools.Count < 1 
            || _imageTools.OfType<HighlightWindowTool>().First() is null) {
            var highlight = new HighlightWindowTool(_imageService, _dicom);
            highlight.OnBegin();
            _imageTools.Add(highlight);

        } else {
            var highlightTool = _imageTools.OfType<HighlightWindowTool>().First();
            highlightTool.OnEnd();
            _imageTools.Remove(highlightTool);
        }
    }

    [RelayCommand]
    private async Task LoadDicomFile(CancellationToken token) {
        try {
            //file explorer
            OpenFileDialog openFile = new() {
                Filter = "DICOM Files (*.dcm)|*.dcm|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFile.ShowDialog() != true)
                return;

            IsBusy(true);

            //generating an image
            var file = await DicomFile.OpenAsync(openFile.FileName);
            var dataset = await GetData(openFile);
            await _dicom.LoadDicomSet(dataset);

            //dump DICOM tags
            WeakReferenceMessenger.Default.Send(new DicomHeadersMessage( await GetAllTags(file)));

            await _dicom.LoadDicomSet(dataset);
        } catch (OperationCanceledException e) {
            MessageBox.Show("Load Dicom File failed: " + e.Message);
        }
            IsBusy(false);
    }


    private async Task<string> GetAllTags(DicomFile file) {
        var task = await Task.Run(() => {
            string result = "Results: \r\n";
            foreach (var tag in file.Dataset) {
                if (file.Dataset.TryGetString(tag.Tag, out string value)) {
                    result += $"\r\n {tag.Tag.DictionaryEntry.Name} : {value}";
                }
            }
            return result;
        });

        return task;
    }

    private async Task<List<DicomDataset>> GetData(OpenFileDialog fileDialog) {
        if (fileDialog.FileNames.Length == 1)
            return new List<DicomDataset>() {
                (await DicomFile.OpenAsync(fileDialog.FileName)).Dataset
            };


        var task = await Task.Run(async () => {
            List<DicomDataset> data = new();
            for (int i = 0; i < fileDialog.FileNames.Length; i++) {
                var dataset = await DicomFile.OpenAsync(fileDialog.FileNames[i]);
                data.Add((await DicomFile.OpenAsync(fileDialog.FileNames[i])).Dataset);
            }

            return data;
        });

        return task;
    }

}

public interface IImageTools {
    void OnBegin();
    void OnEnd();
}

public class HighlightWindowTool : IImageTools {
    private IImageOverlayService _imageService;
    private readonly IDicomService _dicomService;
    private Color color;

    public HighlightWindowTool(IImageOverlayService imageService, IDicomService dicomService) {
        _imageService = imageService;
        _dicomService = dicomService;
        color = Colors.MediumPurple;
    }

    public void OnBegin() {
        _dicomService.Control.OnImageUpdated += OnWindowLevelChanged;
        OnWindowLevelChanged();
    }

    public void OnEnd() {
        _dicomService.Control.OnImageUpdated -= OnWindowLevelChanged;
        _imageService.ClearImage();
    }

    private void OnWindowLevelChanged() {
        var pixels  = _dicomService.Control.CurrentSlice.GetHUs(); //double[row, column] or double[height, width]
        if (pixels is null) return;

        int height = pixels.GetLength(0);
        int width = pixels.GetLength(1);
        double lowerWindow = _dicomService.Control.LowerWindowValue;
        double upperWindow = _dicomService.Control.UpperWindowValue;

        WriteableBitmap bitmap = BitmapFactory.New(width, height);
        for (int row = 0; row < height; row++) {
            for (int col = 0; col < width; col++) {
                if (pixels[row, col] >= lowerWindow && pixels[row, col] <= upperWindow) {
                    bitmap.SetPixel(row, col, color);
                }
            }
        }

        _imageService.SetImage(bitmap);
    }
}




