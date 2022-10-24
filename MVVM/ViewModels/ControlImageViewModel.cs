using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FellowOakDicom;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using BolusEvaluator.Services.DicomService;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Color = System.Windows.Media.Color;
using BolusEvaluator.MVVM.Models;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
public partial class ControlImageViewModel {
    private readonly IDicomService _dicomService;
    private DicomSet _data => _dicomService.Control;

    [ObservableProperty] private Color _controlColor;

    //images
    [ObservableProperty] private ImageSource? _displayImage;
    [ObservableProperty] private ImageSource? _layerImage;

    //text on image
    [ObservableProperty] private string _layerText;

    //window levels slider
    [ObservableProperty] private bool _showWindowSlider = false;
    [ObservableProperty] private double _maxWindowValue = 40;
    [ObservableProperty] private double _minWindowValue = -40;
    [ObservableProperty] private double _lowerWindowValue;
    [ObservableProperty] private double _upperWindowValue;
    partial void OnUpperWindowValueChanged(double value) => _dicomService.Control.UpperWindowValue = (double)value;
    partial void OnLowerWindowValueChanged(double value) => _dicomService.Control.LowerWindowValue = (double)value;

    //current frame slider
    [ObservableProperty] private bool _showFrameSlider = false;
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private int _maxFrames;

    partial void OnCurrentFrameChanged(int value) => _dicomService.Control.CurrentFrame = value;

    //test image
    [ObservableProperty] private bool _showTestImage = false;

    //info text bottom right
    [ObservableProperty] private string? _infoText;


    //frame data
    [ObservableProperty] private int _imageWidth = 512, _imageHeight = 512; //image pixel sizes

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

            //generating an image
            var file = await DicomFile.OpenAsync(openFile.FileName);
            var dataset = await GetData(openFile);
            await _dicomService.LoadControlDicomSet(dataset);

            _dicomService.Control.OnNewFrame += NewFrame;
            _dicomService.Control.OnImageUpdated += ImageUpdated;

            ShowFrameSlider = _data.FrameCount > 1;
            ShowWindowSlider = true;
            

        } catch (OperationCanceledException e) {
            MessageBox.Show("Load Control Dicom File failed: " + e.Message);
        }

    }

    public ControlImageViewModel() {
        _dicomService = App.AppHost.Services.GetService<IDicomService>();
        _dicomService.OnControlLoaded += DatasetLoaded;
    }

    private void DatasetLoaded() {

        _dicomService.Control.OnNewFrame += NewFrame;
        _dicomService.Control.OnImageUpdated += ImageUpdated;

        NewFrame();
        ImageUpdated();
    }

    private void NewFrame() {
        ShowFrameSlider = _dicomService.Data.FrameCount > 1;

        //frame slider
        CurrentFrame = _dicomService.Data.CurrentFrame;
        MaxFrames = _dicomService.Data.FrameCount - 1;

        //window level slider
        MinWindowValue = _dicomService.Data.CurrentSlice.MinValue;
        MaxWindowValue = _dicomService.Data.CurrentSlice.MaxValue;

        LowerWindowValue = _dicomService.Data.LowerWindowValue;
        UpperWindowValue = _dicomService.Data.UpperWindowValue;

        //text
        LayerText = _dicomService.Data.CurrentSlice.FrameText;
    }

    private void ImageUpdated() {
        //window level slider
        LowerWindowValue = _dicomService.Data.LowerWindowValue;
        UpperWindowValue = _dicomService.Data.UpperWindowValue;

        DisplayImage = _dicomService.Data.CurrentSlice.GetWindowedImage(LowerWindowValue, UpperWindowValue);
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

