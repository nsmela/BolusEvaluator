using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FellowOakDicom;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Windows;
using BolusEvaluator.Messages;
using BolusEvaluator.ImageTools;
using BolusEvaluator.Services.DicomService;
using Microsoft.Extensions.DependencyInjection;
using BolusEvaluator.Services.ImageOverlayService;
using FellowOakDicom.Imaging.Render;
using FellowOakDicom.Imaging;
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
    private MouseHUValue _mouseTool;

    [ObservableProperty] private string? _currentTool = string.Empty; //placeholder for more advanced tools    

    private void IsBusy (bool value) => WeakReferenceMessenger.Default.Send(new IsBusyMessage(value));

    public ToolbarViewModel() {
        _dicom = App.AppHost.Services.GetService<IDicomService>();
        _imageService = App.AppHost.Services.GetService<IImageOverlayService>();
        _imageTools = new();

        _mouseTool = new();
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
            _dicom.LoadDataset(dataset);

            //dump DICOM tags
            WeakReferenceMessenger.Default.Send(new DicomDetailsMessage( await GetAllTags(file)));

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
        _dicomService.OnDicomImageUpdated += OnWindowLevelChanged;
        OnWindowLevelChanged();
    }

    public void OnEnd() {
        _dicomService.OnDicomImageUpdated -= OnWindowLevelChanged;
        _imageService.ClearImage();
    }

    private void OnWindowLevelChanged() {
        var pixels  = _dicomService.GetHUs(); //double[row, column] or double[height, width]
        if (pixels is null) return;

        int height = pixels.GetLength(0);
        int width = pixels.GetLength(1);
        double lowerWindow = _dicomService.LowerWindowValue;
        double upperWindow = _dicomService.UpperWindowValue;

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

public class MouseHUValue : IImageTools {
    private readonly IDicomService _dicom;
    private readonly IInputService _inputService;
    private bool _isActive;

    public MouseHUValue() {
        _inputService = App.AppHost.Services.GetService<IInputService>();

        _inputService.OnImageLeftMouseDown += StartReadings;
        _inputService.OnImageLeftMouseUp += StopReadings;
        _inputService.OnImageMouseMove += GiveReading;

        _isActive = false;
        _dicom = App.AppHost.Services.GetService<IDicomService>();

        //clear text
        WeakReferenceMessenger.Default.Send(new InfoMessage(string.Empty));
    }

    public void OnBegin() {
        
    }

    public void OnEnd() {
        _inputService.OnImageLeftMouseDown -= StartReadings;
        _inputService.OnImageLeftMouseUp -= StopReadings;
        _inputService.OnImageMouseMove -= GiveReading;
    }

    private void StartReadings(Point point) {
        _isActive = true;
        GiveReading(point);
    }

    private void StopReadings(Point point) {
        _isActive = false;
        
    }

    private void GiveReading(Point point) {
        if (!_isActive) return;

        string text = $"X: {point.X}\r\nY: {point.Y}\r\nHU: {_dicom.GetHU(point)}";
        WeakReferenceMessenger.Default.Send(new InfoMessage(text));
    }
}


