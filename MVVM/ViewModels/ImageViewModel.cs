using BolusEvaluator.ImageTools;
using BolusEvaluator.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
[ObservableRecipient]
public partial class ImageViewModel {
    //images
    //images
    [ObservableProperty] private ImageSource? _displayImage;
    [ObservableProperty] private ImageSource? _layerImage;

    //window levels slider
    [ObservableProperty] private double? _maxWindowValue = 40;
    [ObservableProperty] private double? _minWindowValue = -40;
    [ObservableProperty] private double? _lowerWindowValue;
    [ObservableProperty] private double? _upperWindowValue;
    partial void OnUpperWindowValueChanged(double? value) => UpdateDisplayImage();
    partial void OnLowerWindowValueChanged(double? value) => UpdateDisplayImage();

    //current frame slider
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private int _maxFrames;

    partial void OnCurrentFrameChanged(int value) => UpdateImageFrame();

    //test image
    [ObservableProperty] private bool _showTestImage = false;
    //point on image 
    [ObservableProperty] private string? _mousePointText;

    //frame data
    private List<DicomDataset>? _dicomData;
    private double _rescaleSlope, _rescaleIntercept; //for calculating HU
    private int _imageWidth = 512, _imageHeight = 512; //image pixel sizes
    private List<Bitmap>? _bitmapData;

    //image tools
    private List<IImageTool> _tools;

    private bool _isBusy = false;
    private void IsBusy(bool value) {
        _isBusy = value;
        WeakReferenceMessenger.Default.Send(new IsBusyMessage(value));
    }

    public ImageViewModel() {
        _tools = new();

        UpdateDisplayImage();

        WeakReferenceMessenger.Default.Register<DicomDatasetMessage>(this, (r, m) => {
            LoadDataset(m.Value);
        });

        WeakReferenceMessenger.Default.Register<AddImageTool>(this, (r, m) => {
            if (_tools.Contains(m.Value)) return;

            _tools.Add(m.Value);

            UpdateDisplayImage();
        });
    }

    public DicomDataset GetCurrentFrameData() => _dicomData[CurrentFrame];

    private void LoadDataset(List<DicomDataset> datasets) {
        if (datasets == null || datasets.Count < 1) {
            DisplayImage = null;
            _dicomData = null;
            return;
        }

        _dicomData = datasets;

        try {
            IsBusy(true);//starts loading bar
            CurrentFrame = 0;
            MaxFrames = datasets.Count - 1;

            var range = _dicomData[0].GetValue<double>(DicomTag.WindowWidth, 0) / 2;
            var center = _dicomData[0].GetValue<double>(DicomTag.WindowCenter, 0);

            LowerWindowValue = center - range;
            UpperWindowValue = center + range;

            _rescaleSlope = _dicomData[0].GetSingleValue<double>(DicomTag.RescaleSlope);
            _rescaleIntercept = _dicomData[0].GetSingleValue<double>(DicomTag.RescaleIntercept);

            _imageWidth = _dicomData[0].GetSingleValue<int>(DicomTag.Columns);
            _imageHeight = _dicomData[0].GetSingleValue<int>(DicomTag.Rows);

            //creating blank bitmaps for futhur use
            _bitmapData = new();
            for (int i = 0; i < datasets.Count; i++)
                _bitmapData.Add(new Bitmap(_imageWidth, _imageHeight));

        } catch (Exception ex) {
            MessageBox.Show("Error: " + ex.Message);
        }

        IsBusy(false);
        UpdateImageFrame();
    }

    //new image frame
    private void UpdateImageFrame() {
        if (_dicomData is null) return;

        var image = new DicomImage(_dicomData[CurrentFrame]);

        var header = DicomPixelData.Create(_dicomData[CurrentFrame]);
        var pixelMap = PixelDataFactory.Create(header, 0);
        var range = pixelMap.GetMinMax();

        MinWindowValue = range.Minimum;
        MaxWindowValue = range.Maximum;

        UpdateDisplayImage();
    }

    //changed the Display so new image needed
    private void UpdateDisplayImage() {
        if (_isBusy) return;
        if (_dicomData is null) return;
        if (_dicomData.Count < 1) return;

        var window = (UpperWindowValue - LowerWindowValue);
        var center = (LowerWindowValue + (window / 2));

        var image = new DicomImage(_dicomData[CurrentFrame]);

        image.WindowWidth = window is null ? 100.0f : (double)window;
        image.WindowCenter = center is null ? 0.0f : (double)center;

        DisplayImage = image.RenderImage().AsWriteableBitmap();
        UpdateImageTools();
    }

    public void UpdateMousePoint(Point mousePoint) {
        MousePointText = $"X: {mousePoint.X}\r\nY: {mousePoint.Y}\r\nHU: {GetHU(mousePoint)}";
        var huValue = GetHU(mousePoint);
    }

    //https://stackoverflow.com/questions/22991009/how-to-get-hounsfield-units-in-dicom-file-using-fellow-oak-dicom-library-in-c-sh
    //https://www.sciencedirect.com/topics/medicine-and-dentistry/hounsfield-scale
    //Hounsfield units = (Rescale Slope * Pixel Value) + Rescale Intercept
    private double GetHU(Point point) {
        if (_dicomData is null) return 0.0f;
        var frame = _dicomData[CurrentFrame];
        var header = DicomPixelData.Create(frame);
        var pixelMap = (PixelDataFactory.Create(header, 0));
        return GetHUValue(pixelMap, (int)point.X, (int)point.Y);
    }

    public double GetHUValue(IPixelData pixelMap, int iX, int iY) {
        if (pixelMap is null) return -2000;

        int index = (int)(iX + pixelMap.Width * iY);
        switch (pixelMap) {
            case GrayscalePixelDataU16:
                return ((GrayscalePixelDataU16)pixelMap).Data[index] * _rescaleSlope + _rescaleIntercept;
            case GrayscalePixelDataS16:
                return ((GrayscalePixelDataS16)pixelMap).Data[index] * _rescaleSlope + _rescaleIntercept;
            default:
                return 0.0f;
        }
    }

    private void UpdateImageTools() {
        foreach (var tool in _tools) {
            tool.Execute(this);
        }
    }
}


