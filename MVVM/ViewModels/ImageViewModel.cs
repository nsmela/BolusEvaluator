﻿using BolusEvaluator.Messages;
using BolusEvaluator.Services.DicomService;
using BolusEvaluator.Services.ImageOverlayService;
using BolusEvaluator.Services.InputService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
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

    //keyboard commands
    [RelayCommand]
    public void IncrementFrame() {
        if (MaxFrames < 2 || CurrentFrame >= MaxFrames) return;
        CurrentFrame++;
    }
    [RelayCommand]
    public void DecrementFrame() {
        if (CurrentFrame < 1) return;
        CurrentFrame--;
    }

    [RelayCommand]
    public void IncrementWindowCenter() {
        UpperWindowValue += 50;
        LowerWindowValue += 50;
    }

    [RelayCommand]
    public void DecrementWindowCenter() {
        UpperWindowValue -= 50;
        LowerWindowValue -= 50;
    }

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
    private readonly IDicomService _dicomService;
    private readonly IImageOverlayService _imageService;
    private bool[,] _isChecked;
    private int _imageHeight, _imageWidth;
    private double[,] _pixelmap;

    private double _lowerWindow, _upperWindow;
    private Dictionary<int, SliceHighlight> _images; //image per frame

    public MouseFill(IDicomService dicom, IImageOverlayService image) {
        _dicomService = dicom;
        _imageService = image;

        _dicomService.OnDatasetLoaded += OnOpen;
    }

    public void OnOpen() {
        _images = new();
        _dicomService.Data.OnNewFrame += NewFrame;
        WeakReferenceMessenger.Default.Send<DicomDetailsMessage>(new DicomDetailsMessage($"Info\r\n\r\nNo highlights"));
    }

    public void OnClose() {
        _dicomService.Data.OnNewFrame -= NewFrame;
        _images.Clear();
    }

    public void OnMouseDown(Point point) {
        double pointValue = _dicomService.Data.CurrentSlice.GetHU(point);
        //testing
        string text = $"X: {point.X}\r\nY: {point.Y}\r\nHU: {pointValue.ToString("0.0")}";
        WeakReferenceMessenger.Default.Send(new InfoMessage(text));

        //caching values
        _lowerWindow = _dicomService.Data.LowerWindowValue;
        _upperWindow = _dicomService.Data.UpperWindowValue;

        //is outside the displayed window
        if (pointValue < _lowerWindow ||
            pointValue > _upperWindow) {
            ClearCurrentFrame();
            SendMessage();
            return;
        }
          

        GetFillPoints(point, pointValue);

    }

    public void OnMouseMove(Point point) {
        
    }

    public void OnMouseUp(Point point) {
        
    }

    private void NewFrame() {
        int frameNumber = _dicomService.Data.CurrentFrame;
        if (!_images.ContainsKey(frameNumber)) {
            _imageService.ClearImage();
        } else {
            _imageService.SetImage(_images[frameNumber].Image);
        }

        SendMessage();

    }

    private void ClearCurrentFrame() {
        var frameIndex = _dicomService.Data.CurrentFrame;
        _images.Remove(frameIndex);
    }

    //TODO: Use a Stack list instead of recursion.
    //current way leads to possible stack overflow
    private void GetFillPoints(Point point, double pointValue) {
        _images = new();

        //used to flag a spot has already been inspected
        var frameCount = _dicomService.Data.Slices.Count;

        //Arrays for checking
        var frameStack = new Stack<int>(); //the frames to check
        frameStack.Push(_dicomService.Data.CurrentFrame); //add our current frame to check first and propagate from there
        var points = new List<Point>();
        points.Add(point);

        //find all relevant pixels on all frames
        while (frameStack.Count > 0) {
            var frame = frameStack.Pop();
            var pixelMap = _dicomService.Data.Slices[frame].GetHUs(); //HU values from the dicom image
            var imageHeight = pixelMap.GetLength(0);
            var imageWidth = pixelMap.GetLength(1);
            points = GetFillPointsFromFrame(pixelMap, imageWidth, imageHeight, _lowerWindow, _upperWindow, points); //replacing old points with new

            if(points is null || points.Count < 1) continue;  //if there were no good points

            //create filled image based on new points
            var bitmap = GenerateBitmap(imageWidth, imageHeight, points, Colors.Blue);

            //store value
            SaveFrame(frame, bitmap, points);

            //add more frames to check if this one had points
            //if (frame > 0) frameStack.Push(frame - 1);
            if (frame < frameCount - 1) frameStack.Push(frame + 1); 
        }

        //update image and messages
        NewFrame();
    }


    private List<Point> GetFillPointsFromFrame(double[,] pixelMap, int frameWidth, int frameHeight, double windowMin, double windowMax, List<Point> startingPoints) {
        List<Point> points = new List<Point>(); //output
        var isChecked = new bool[frameHeight, frameWidth]; //skip pixels already checked
        Stack<Point> stack = new Stack<Point>(); //queued points to check
        startingPoints.ForEach(x => stack.Push(x)); //load the starting points into the queue

        while (stack.Count > 0) {
            var point = stack.Pop(); //removes point from the stack

            int pX = (int)point.X;
            int pY = (int)point.Y;

            //setting the limits for the search
            int start_x = 0, start_y = 0, end_x = frameWidth - 1, end_y = frameHeight - 1;
            if (pX > start_x) start_x = pX - 1;
            if (pX < end_x) end_x = pX + 1;
            if (pY > start_y) start_y = pY - 1;
            if (pY < end_y) end_y = pY + 1;

            for (int x = start_x; x <= end_x; x++) {
                for (int y = start_y; y <= end_y; y++) {
                    if (isChecked[x, y]) continue; //already been inspected, skip

                    isChecked[x, y] = true; //remove it from being checked again
                    var pixelValue = pixelMap[x, y];
                    if (pixelValue < windowMin || pixelValue > windowMax) continue; //if not within the window

                    //if valid
                    var newPoint = new Point(x, y);
                    points.Add(newPoint);
                    stack.Push(newPoint);
                }
            }
        }

        return points;
    }

    private WriteableBitmap GenerateBitmap(int width, int height, List<Point> points, Color color) {
        //create filled image
        var bitmap = BitmapFactory.New(width, height);
        points.ForEach((point) => {
            bitmap.SetPixel((int)point.X, (int)point.Y, color);
        });
        return bitmap;
    }

    private void SaveFrame(int frameIndex, WriteableBitmap bitmap, List<Point> points) {
        var slice = new SliceHighlight {
            Image = bitmap,
            Points = points
        };

        if (_images.ContainsKey(frameIndex)) {
            //replace frame
            _images[frameIndex] = slice;

        } else {
            _images.Add(frameIndex, slice);
        }
    }

    private double TotalVolume() {
        var volumePerPixel = _dicomService.Data.PixelVolume / 1000;
        double value = 0;
        foreach(var slice in _images) value += slice.Value.Points.Count * volumePerPixel;
        return value;
    }

    private void SendMessage() {
        var frameNumber = _dicomService.Data.CurrentFrame;
        if (!_images.ContainsKey(frameNumber)) {
            _imageService.ClearImage();
            WeakReferenceMessenger.Default.Send<DicomDetailsMessage>(new DicomDetailsMessage($"Info\r\n\r\nNo highlights"));
            return;
        }

        var selectedArea = _images[frameNumber].Points.Count * _dicomService.Data.PixelVolume / 1000;
        WeakReferenceMessenger.Default.Send<DicomDetailsMessage>(new DicomDetailsMessage($"Info\r\n\r\nSelected Volume: {selectedArea.ToString("0.00")} mL\r\nTotal Volume: {TotalVolume().ToString("0.00")}"));

    }
}
struct SliceHighlight {
    public WriteableBitmap Image { get; set; }
    public List<Point> Points { get; set; }
}