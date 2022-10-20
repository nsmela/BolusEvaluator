using BolusEvaluator.Messages;
using BolusEvaluator.Services.DicomService;
using BolusEvaluator.Services.ImageOverlayService;
using BolusEvaluator.Services.InputService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;
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
    [ObservableProperty] private int _imageWidth = 512, _imageHeight = 512; //image pixel sizes

    public ImageViewModel() {
        _dicom = App.AppHost.Services.GetService<IDicomService>();

        _dicom.OnNewFrame += NewFrame;
        _dicom.OnDicomImageUpdated += ImageUpdated;

        _imageService = App.AppHost.Services.GetService<IImageOverlayService>();
        _imageService.OnImageUpdated += OverlayImageUpdated;

        _inputService = App.AppHost.Services.GetService<IInputService>();

        WeakReferenceMessenger.Default.Register<InfoMessage>(this, (r, m) => {
            InfoText = m.Value;
        });

        LayerText = string.Empty;
    }

    private void NewFrame() {
        //frame slider
        CurrentFrame = _dicom.CurrentFrame;
        MaxFrames = _dicom.FrameCount - 1;

        //window level slider
        MinWindowValue = _dicom.MinWindowLevel;
        MaxWindowValue = _dicom.MaxWindowLevel;

        //text
        LayerText = _dicom.FrameText;
    }

    private void ImageUpdated() {
        //window level slider
        LowerWindowValue = _dicom.LowerWindowValue;
        UpperWindowValue = _dicom.UpperWindowValue;

        DisplayImage = _dicom.GetDicomImage;
    }

    private void WindowLevelChanged() {
        
    }

    //layer images
    private void OverlayImageUpdated() {
        LayerImage = _imageService.OverlayImage;
    }

 
    //mouse events
    public void OnLeftMouseDown(Point mousePoint) => _inputService.Image_LeftMouseDown(mousePoint);
    public void OnLeftMouseUp(Point mousePoint) => _inputService.Image_LeftMouseUp(mousePoint);
    public void OnMouseMove(Point mousePoint) => _inputService.Image_MouseMove(mousePoint);
}
