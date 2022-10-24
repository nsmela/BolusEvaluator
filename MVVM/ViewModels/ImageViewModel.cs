﻿using BolusEvaluator.Messages;
using BolusEvaluator.Services.DicomService;
using BolusEvaluator.Services.ImageOverlayService;
using BolusEvaluator.Services.InputService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
[ObservableRecipient]
public partial class ImageViewModel {
    //Services
    private readonly IDicomService _dicom;
    private readonly IImageOverlayService _imageService;
    private readonly IInputService? _inputService;

    //images
    [ObservableProperty] private ImageSource? _displayImage;
    [ObservableProperty] private ImageSource? _layerImage;

    //text on image
    [ObservableProperty] private string _layerText;

    //window levels slider
    [ObservableProperty] private double _maxWindowValue = 40;
    [ObservableProperty] private double _minWindowValue = -40;
    [ObservableProperty] private double _lowerWindowValue;
    [ObservableProperty] private double _upperWindowValue;
    partial void OnUpperWindowValueChanged(double value) => _dicom.Data.UpperWindowValue = (double)value;

    partial void OnLowerWindowValueChanged(double value) => _dicom.Data.LowerWindowValue = (double)value;

    //current frame slider
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private int _maxFrames;

    partial void OnCurrentFrameChanged(int value) => _dicom.Data.CurrentFrame = value;

    //test image
    [ObservableProperty] private bool _showTestImage = false;

    //info text bottom right
    [ObservableProperty] private string? _infoText;
    

    //frame data
    [ObservableProperty] private int _imageWidth = 512, _imageHeight = 512; //image pixel sizes
    [ObservableProperty] private bool _showFrameSlider = false;
    private Dictionary<int, WriteableBitmap> _filledImages; //frames and their bitmaps
    private Dictionary<int, double> _volumes; //frames and their volumes

    //mouse inputs
    private IMouseTool _mouseTool;

    public ImageViewModel() {
        _dicom = App.AppHost.Services.GetService<IDicomService>();
        _dicom.OnDatasetLoaded += DatasetLoaded;

        _imageService = App.AppHost.Services.GetService<IImageOverlayService>();
        _imageService.OnImageUpdated += OverlayImageUpdated;

        WeakReferenceMessenger.Default.Register<InfoMessage>(this, (r, m) => {
            InfoText = m.Value;
        });

        LayerText = string.Empty;

        _inputService = App.AppHost.Services.GetService<IInputService>();
        _inputService.OnImageLeftMouseDown += OnLeftMouseDown;
        _inputService.OnImageLeftMouseUp += OnLeftMouseUp;
        _inputService.OnImageMouseMove += OnMouseMove;

        //_mouseTool = new MouseHUReader(_dicom);
        _mouseTool = new MouseFill(_dicom, _imageService);
    }

    private void DatasetLoaded() {

        _dicom.Data.OnNewFrame += NewFrame;
        _dicom.Data.OnImageUpdated += ImageUpdated;

        NewFrame();
        ImageUpdated();
    }

    private void NewFrame() {
        ShowFrameSlider = _dicom.Data.FrameCount > 1;

        //frame slider
        CurrentFrame = _dicom.Data.CurrentFrame;
        MaxFrames = _dicom.Data.FrameCount - 1;

        //window level slider
        MinWindowValue = _dicom.Data.CurrentSlice.MinValue;
        MaxWindowValue = _dicom.Data.CurrentSlice.MaxValue;

        LowerWindowValue = _dicom.Data.LowerWindowValue;
        UpperWindowValue = _dicom.Data.UpperWindowValue;

        //text
        LayerText = _dicom.Data.CurrentSlice.FrameText;
    }

    private void ImageUpdated() {
        //window level slider
        LowerWindowValue = _dicom.Data.LowerWindowValue;
        UpperWindowValue = _dicom.Data.UpperWindowValue;

        DisplayImage = _dicom.Data.CurrentSlice.GetWindowedImage(LowerWindowValue, UpperWindowValue);
    }

    //layer images
    private void OverlayImageUpdated() {
        LayerImage = _imageService.OverlayImage;
    }

    //mouse events
    public void OnLeftMouseDown(Point point) => _mouseTool.OnMouseDown(point);
    public void OnLeftMouseUp(Point point) => _mouseTool.OnMouseUp(point);
    public void OnMouseMove(Point point) => _mouseTool.OnMouseMove(point);

}

interface IMouseTool {
    void OnMouseDown(Point point);
    void OnMouseUp(Point point);
    void OnMouseMove(Point point);
}

class MouseHUReader : IMouseTool {
    private readonly IDicomService _dicom;
    private bool _isMouseCaptured;

    public MouseHUReader(IDicomService dicom) {
        _dicom = dicom;
        _isMouseCaptured= false;
    }

    public void OnMouseDown(Point point) {
        _isMouseCaptured = true;
        OnMouseMove(point);
    }

    public void OnMouseMove(Point point) {
        if (!_isMouseCaptured) return;
        double pointValue = _dicom.Data.CurrentSlice.GetHU(point);
        string text = $"X: {point.X}\r\nY: {point.Y}\r\nHU: {pointValue.ToString("0.0")}";
        WeakReferenceMessenger.Default.Send(new InfoMessage(text));
    }

    public void OnMouseUp(Point point) {
        _isMouseCaptured = false;
    }
}

class MouseFill : IMouseTool {
    private readonly IDicomService _dicom;
    private readonly IImageOverlayService _image;
    private bool[,] _isChecked;
    private int _imageHeight, _imageWidth;
    private double[,] _pixelmap;

    private double _lowerWindow, _upperWindow;
    private List<Point> _points;
    private Stack<Point> _stack;

    public MouseFill(IDicomService dicom, IImageOverlayService image) {
        _dicom = dicom;
        _image = image;
    }

    public IImageOverlayService Image { get; }

    public void OnMouseDown(Point point) {
        double pointValue = _dicom.Data.CurrentSlice.GetHU(point);
        //testing
        string text = $"X: {point.X}\r\nY: {point.Y}\r\nHU: {pointValue.ToString("0.0")}";
        WeakReferenceMessenger.Default.Send(new InfoMessage(text));

        //caching values
        _lowerWindow = _dicom.Data.LowerWindowValue;
        _upperWindow = _dicom.Data.UpperWindowValue;

        //is outside the displayed window
        if (pointValue < _lowerWindow ||
            pointValue > _upperWindow)
            return;

        GetFillPoints(point, pointValue);

    }

    public void OnMouseMove(Point point) {
        
    }

    public void OnMouseUp(Point point) {
        
    }

    private void NewFrame() {
        int frameNumber = _dicom.Data.CurrentFrame;
    }

    //TODO: Use a Stack list instead of recursion.
    //current way leads to possible stack overflow
    private void GetFillPoints(Point point, double pointValue) {
        _pixelmap = _dicom.Data.CurrentSlice.GetHUs(); //HU values from the dicom image
        //preping the bool array
        //used to flag a spot has already been inspected
        _imageHeight = _pixelmap.GetLength(0);
        _imageWidth = _pixelmap.GetLength(1);
        _isChecked = new bool[_imageHeight, _imageWidth];

        _points = new(); //output
        _stack = new(); //temp storage

        //initial click
        _stack.Push(new Point(point.X, point.Y));
        while (_stack.Count > 0) {
            GetSurroundingPoints(_stack.Pop());
        }

        var bitmap = BitmapFactory.New(_imageWidth, _imageHeight);
        _points.ForEach((point) => {
            bitmap.SetPixel((int)point.X, (int)point.Y, Colors.Blue);
        });
        _image.SetImage(bitmap);

        //calculate area
        var selectedArea = _points.Count * _dicom.Data.PixelVolume / 1000;
        WeakReferenceMessenger.Default.Send<DicomDetailsMessage>(new DicomDetailsMessage($"Info\r\n\r\nSelected Volume: {selectedArea.ToString("0.00")} mL"));
    }

    private void GetSurroundingPoints(Point point) {
        int pX = (int)point.X;
        int pY = (int)point.Y;

        //setting the limits for the search
        int start_x = 0, start_y = 0, end_x = _imageWidth - 1, end_y = _imageHeight - 1;
        if (pX > start_x) start_x = pX - 1;
        if (pX < end_x) end_x = pX + 1;
        if(pY > start_y) start_y = pY - 1;
        if(pY < end_y) end_y = pY + 1;

        for (int x = start_x; x <= end_x; x++) {
            for (int y = start_y; y <= end_y; y++) {
                if (_isChecked[x, y]) continue; //already been inspected, skip
                _isChecked[x, y] = true; //remove it from being checked again
                var pixelValue = _pixelmap[x, y];
                if (pixelValue < _lowerWindow || pixelValue > _upperWindow) continue; //if not within the window

                //if valid
                var newPoint = new Point(x, y);
                _points.Add(newPoint);
                _stack.Push(newPoint);
            }
        }
        
    }

}
