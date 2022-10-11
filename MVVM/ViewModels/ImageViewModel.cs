using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
[ObservableRecipient]
public partial class ImageViewModel {

    [ObservableProperty] private ImageSource? _displayImage;

    //window levels slider
    [ObservableProperty] private double? _lowerWindowValue;
    [ObservableProperty] private double? _upperWindowValue;
    partial void OnUpperWindowValueChanged(double? value) => RecalculateImageWindow();
    partial void OnLowerWindowValueChanged(double? value) => RecalculateImageWindow();

    //currentImage slider
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private int _maxFrames;

    partial void OnCurrentFrameChanged(int value) => RecalculateImageWindow();

    //point on image 
    [ObservableProperty] private string? _mousePointText;

    //housfield units calculation
    private double _rescaleSlope, _rescaleIntercept;
    private List<DicomDataset>? _dicomData;

    private bool _isBusy = false;

    private void RecalculateImageWindow() {
        if (_isBusy) return;
        if(_dicomData is null) return;
        if (_dicomData.Count < 1) return;

        UpdateDisplayImage();

    }

    public ImageViewModel() {
        WeakReferenceMessenger.Default.Register<DicomDatasetMessage>(this, (r, m) => {
            LoadDataset(m.Value);
        });
    }

    private void LoadDataset(List<DicomDataset> datasets) {
        if (datasets == null || datasets.Count < 1) {
            DisplayImage = null;
            _dicomData = null;
            return;
        }

        _dicomData = datasets;

        try {
            _isBusy = true;
            CurrentFrame = 0;
            MaxFrames = datasets.Count - 1;

            var range = _dicomData[0].GetValue<double>(DicomTag.WindowWidth, 0) / 2;
            var center = _dicomData[0].GetValue<double>(DicomTag.WindowCenter, 0);

            LowerWindowValue = center - range;
            UpperWindowValue = center + range;

            _rescaleSlope = _dicomData[0].GetSingleValue<double>(DicomTag.RescaleSlope);
            _rescaleIntercept = _dicomData[0].GetSingleValue<double>(DicomTag.RescaleIntercept);

            UpdateDisplayImage();

            _isBusy = false;
        } catch (Exception ex) {
            MessageBox.Show("Error: " + ex.Message);
        }
    }

    private void UpdateDisplayImage() {
        if (_dicomData is null) return;

        var image = new DicomImage(_dicomData[CurrentFrame]);

        var window = (UpperWindowValue - LowerWindowValue);
        var center = (LowerWindowValue + (window / 2));

        image.WindowWidth = window is null ? 100.0f : (double)window;
        image.WindowCenter = center is null ? 0.0f : (double)center;

        DisplayImage = image.RenderImage().AsWriteableBitmap();
    }

    public void UpdateMousePoint(Point mousePoint) {
        MousePointText = $"X: {mousePoint.X}\r\nY: {mousePoint.Y}\r\nHU: {GetHU(mousePoint)}";
        var huValue = GetHU(mousePoint);
    }

    //https://stackoverflow.com/questions/22991009/how-to-get-hounsfield-units-in-dicom-file-using-fellow-oak-dicom-library-in-c-sh
    //https://www.sciencedirect.com/topics/medicine-and-dentistry/hounsfield-scale
    //Hounsfield units = (Rescale Slope * Pixel Value) + Rescale Intercept
    private double GetHU(Point point) {
        if(_dicomData is null) return 0.0f;

        var frame = _dicomData[CurrentFrame];
        
        var header = DicomPixelData.Create(frame);
        var pixelData = ((GrayscalePixelDataS16)PixelDataFactory.Create(header, 0)).Data;

        //index calculation
        int column =  ((int)point.X);
        int row = ((int)point.Y);
        int indexOffset = ((column + 1) * row) + column;

        var pixel = pixelData[indexOffset];
        var slope = _rescaleSlope;
        var intercept = _rescaleIntercept;

        return (pixel * slope) + intercept; 
    }
}

