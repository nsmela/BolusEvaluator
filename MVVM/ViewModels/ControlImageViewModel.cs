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
    private readonly IDicomService _dicom;
    private DicomSet _data => _dicom.Control;

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
    partial void OnUpperWindowValueChanged(double value) => SetUpperWindowLevel(value);
    partial void OnLowerWindowValueChanged(double value) => SetLowerWindowLevel(value);

    //current frame slider
    [ObservableProperty] private bool _showFramesSlider = false;
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private int _maxFrames;

    partial void OnCurrentFrameChanged(int value) => SetFrame(value);

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
            _dicom.LoadControlDicomSet(dataset);

            _dicom.Control.OnNewFrame += NewFrame;
            _dicom.Control.OnImageUpdated += ImageUpdated;

            ShowFramesSlider = _data.FrameCount > 1;
            ShowWindowSlider = true;
            

        } catch (OperationCanceledException e) {
            MessageBox.Show("Load Control Dicom File failed: " + e.Message);
        }

    }

    public ControlImageViewModel() {
        _dicom = App.AppHost.Services.GetService<IDicomService>();
    }

    private void SetUpperWindowLevel(double? value) {
        if (_data is null) return;

        _dicom.Control.UpperWindowValue = (double)value;
    }

    private void SetLowerWindowLevel(double? value) {
        if (_data is null) return;

        _dicom.Control.LowerWindowValue = (double)value;
    }

    private void SetFrame(int value) {
        if (_data is null) return;

        
    }

    private void NewFrame() {
        //frame slider
        CurrentFrame = _data.CurrentFrame;
        MaxFrames = _data.FrameCount;

        //window level slider
        MinWindowValue = _data.CurrentSlice.MinValue;
        MaxWindowValue = _data.CurrentSlice.MaxValue;

        //text
        LayerText = _data.CurrentSlice.FrameText;
    }

    private void ImageUpdated() {
        //window level slider
        LowerWindowValue = _data.LowerWindowValue;
        UpperWindowValue = _data.UpperWindowValue;

        DisplayImage = _data.CurrentSlice.GetWindowedImage(LowerWindowValue, UpperWindowValue);
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

