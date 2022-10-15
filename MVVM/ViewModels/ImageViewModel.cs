using BolusEvaluator.ImageTools;
using BolusEvaluator.Messages;
using BolusEvaluator.Services.DicomService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
[ObservableRecipient]
public partial class ImageViewModel {
    //Services
    private readonly IDicomService _dicom;

    //images
    [ObservableProperty] private ImageSource? _displayImage;
    [ObservableProperty] private ImageSource? _layerImage;

    //window levels slider
    [ObservableProperty] private double? _maxWindowValue = 40;
    [ObservableProperty] private double? _minWindowValue = -40;
    [ObservableProperty] private double? _lowerWindowValue;
    [ObservableProperty] private double? _upperWindowValue;
    partial void OnUpperWindowValueChanged(double? value) => _dicom.SetUpperWindowLevel((double)value);
    partial void OnLowerWindowValueChanged(double? value) => _dicom.SetLowerWindowLevel((double)value);

    //current frame slider
    [ObservableProperty] private int _currentFrame;
    [ObservableProperty] private int _maxFrames;

    partial void OnCurrentFrameChanged(int value) => _dicom.SetFrame(value);

    //test image
    [ObservableProperty] private bool _showTestImage = false;

    //info text bottom right
    [ObservableProperty] private string? _infoText;

    //frame data
    private List<BitmapSource>? _bitmapData;
    [ObservableProperty] private int _imageWidth = 512, _imageHeight = 512; //image pixel sizes

    //image tools
    private Dictionary<string, IImageTool> _tools;
    private IImageViewState _state;
    private bool _isBusy = false;
    private void IsBusy(bool value) {
        _isBusy = value;
        WeakReferenceMessenger.Default.Send(new IsBusyMessage(value));
    }

    //events
    public event Action<Point> OnMouseLeftButtonDown, OnMouseLeftButtonUp, OnMouseRightButtonDown, OnMouseHasMoved;
    public event Action OnImageUpdated, OnNewFrame;

    public ImageViewModel() {
        _dicom = App.AppHost.Services.GetService<IDicomService>();

        _dicom.OnNewFrame += NewFrame;
        _dicom.OnDicomImageUpdated += ImageUpdated;

        _tools = new();
        _state = new MouseHUToolState();
        _state.OnStart(this);

        WeakReferenceMessenger.Default.Register<AddImageTool>(this, (r, m) => {
        if (_tools.ContainsKey(m.Value.Label)) return; 

            _tools.Add(m.Value.Label, m.Value);

            UpdateDisplayImage();
        });

        WeakReferenceMessenger.Default.Register<DeleteImageTool>(this, (r, m) => {
            if (!_tools.ContainsKey(m.Value.Label)) return;

            _tools.Remove(m.Value.Label);

            UpdateDisplayImage();
        });

        WeakReferenceMessenger.Default.Register<ChangeImageViewState>(this, (r, m) => {
            _state.OnExit();
            _state = m.Value;
            _state.OnStart(this);
        });
    }

    //public DicomDataset GetCurrentFrameData() => _dicomData[CurrentFrame];


    private void NewFrame() {
        //frame slider
        CurrentFrame = _dicom.CurrentFrame;
        MaxFrames = _dicom.FrameCount;

        //window level slider
        MinWindowValue = _dicom.MinWindowLevel;
        MaxWindowValue = _dicom.MaxWindowLevel;
    }

    private void ImageUpdated() {
        //window level slider
        LowerWindowValue = _dicom.LowerWindowValue;
        UpperWindowValue = _dicom.UpperWindowValue;

        DisplayImage = _dicom.GetDicomImage;
    }

    private void WindowLevelChanged() {
        
    }


    private void UpdateImageTools() {
        foreach (var tool in _tools) {
            tool.Value.Execute(this);
        }

        OnImageUpdated?.Invoke();
    }

    //layer images
    public void SetImage(BitmapSource bitmap) {
        _bitmapData[CurrentFrame] = bitmap;
        LayerImage = bitmap;
        UpdateImageTools();
    }

    public WriteableBitmap GetCurrentLayerImage() => (WriteableBitmap)_bitmapData[CurrentFrame];

    public void ClearCurrentImage() {
        ClearImage(CurrentFrame);
        UpdateDisplayImage();
    }
    public void ClearAllImages() {
        for (int i = 0; i < _bitmapData.Count; i++)
            ClearImage(i);

        UpdateDisplayImage();
    }

    private void ClearImage(int index) {
        var bitmap = BitmapFactory.New(ImageWidth, ImageHeight);
        LayerImage = bitmap;
        _bitmapData[index] = bitmap;

    }

    //mouse events
    public void OnLeftMouseDown(Point mousePoint) {
        if (_state is null) return;
        OnMouseLeftButtonDown?.Invoke(mousePoint);
    }

    public void OnLeftMouseUp(Point mousePoint) {
        if (_state is null) return;
        OnMouseLeftButtonUp?.Invoke(mousePoint);
    }

    public void OnMouseMove(Point mousePoint) {
        if (_state is null) return;
        OnMouseHasMoved?.Invoke(mousePoint);
    }

    //https://stackoverflow.com/questions/22991009/how-to-get-hounsfield-units-in-dicom-file-using-fellow-oak-dicom-library-in-c-sh
    //https://www.sciencedirect.com/topics/medicine-and-dentistry/hounsfield-scale
    //Hounsfield units = (Rescale Slope * Pixel Value) + Rescale Intercept
    public double GetHU(Point point) {
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


}

public interface IImageViewState {
    public void OnStart(ImageViewModel viewModel);
    public void OnExit();
}

public class MouseHUToolState : IImageViewState {
    private ImageViewModel _viewModel;
    private bool _isMouseDown;

    public void OnExit() {
        _viewModel.InfoText = "";
        _viewModel.OnMouseLeftButtonDown -= StartReading;
        _viewModel.OnMouseLeftButtonUp -= EndReading;
        _viewModel.OnMouseHasMoved -= PostHu;
    }

    public void OnStart(ImageViewModel viewModel) {
        _viewModel = viewModel;
        _isMouseDown = false;
        _viewModel.OnMouseLeftButtonDown += StartReading;
        _viewModel.OnMouseLeftButtonUp += EndReading;
        _viewModel.OnMouseHasMoved += PostHu;

        _viewModel.InfoText = "";
    }

    private void StartReading(Point point) {
        _isMouseDown = true;
        PostHu(point);
    }

    private void EndReading(Point point) {
        _isMouseDown = false;
    }

    private void PostHu(Point point) {
        if (_isMouseDown) 
            _viewModel.InfoText = $"X: {point.X}\r\nY: {point.Y}\r\nHU: {_viewModel.GetHU(point)}";
    }
    
}

public class DrawTool : IImageViewState {
    private ImageViewModel _viewModel;
    private System.Windows.Media.Color _drawColor;
    private WriteableBitmap _bitmap;

    public void OnExit() {
        _viewModel.OnMouseLeftButtonDown -= AddPixel;
        _viewModel.OnNewFrame -= NewFrame;

        _viewModel.ClearAllImages();
    }

    public void AddPixel(Point mousePoint) {
        var width = _viewModel.ImageWidth;
        var height = _viewModel.ImageHeight;
        int x = (int)mousePoint.X;
        int y = (int)mousePoint.Y;

        _bitmap.SetPixel(x, y, 255, _drawColor);
        _viewModel.LayerImage = _bitmap;
    }

    private void NewFrame() {
        var width = _viewModel.ImageWidth;
        var height = _viewModel.ImageHeight;
        _bitmap = BitmapFactory.New(width, height);
        _bitmap.DrawLine(width / 2, 0, width / 2, height, _drawColor);
        _bitmap.DrawLine(0, height / 2, width, height / 2, _drawColor);
        _viewModel.SetImage(_bitmap);
    }

    public void OnStart(ImageViewModel viewModel) {
        _viewModel = viewModel;
        _viewModel.OnMouseLeftButtonDown += AddPixel;
        _viewModel.OnNewFrame += NewFrame;

        _drawColor = Colors.Violet;

        NewFrame();
    }


}
