using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FellowOakDicom.Imaging;
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

    private List<DicomImage>? _dicomImages;

    private bool _isBusy = false;

    private void RecalculateImageWindow() {
        if (_isBusy) return;
        if(_dicomImages is null) return;
        if (_dicomImages.Count < 1) return;

        _isBusy = true;
        var window = (UpperWindowValue - LowerWindowValue);
        var center = (LowerWindowValue + (window / 2));
        var currentFrame = CurrentFrame;

        //reset window level
        _dicomImages[currentFrame].WindowWidth = window is null ? 100.0f : (double)window;
        _dicomImages[currentFrame].WindowCenter = center is null ? 0.0f : (double)center;

        UpdateDisplayImage();
        _isBusy = false;
            
    }

    public ImageViewModel() {
        WeakReferenceMessenger.Default.Register<DicomImageMessage>(this, (r, m) => {
            LoadImages(m.Value);
        });
    }

    private void LoadImages(List<DicomImage> images) {
        try {
            if (images == null || images.Count < 1) {
                DisplayImage = null;
                return;
            }

            _dicomImages = images;

            _isBusy = true;
            //images variables
            CurrentFrame = 0;
            MaxFrames = images.Count - 1;

            //update slider values
            var range = _dicomImages[0].WindowWidth / 2;
            var center = _dicomImages[0].WindowCenter;

            LowerWindowValue = center - range;
            UpperWindowValue = center + range;
            
            //create image source
            UpdateDisplayImage();

            _isBusy = false;

        } catch (Exception ex) {
            MessageBox.Show("Error: " + ex.Message);
        }
    }

    private void UpdateDisplayImage() {
        DisplayImage = _dicomImages[CurrentFrame].RenderImage().AsWriteableBitmap();
    }

    public void UpdateMousePoint(Point mousePoint) {
        MousePointText = $"X: {mousePoint.X}\r\nY: {mousePoint.Y}";
        var huValue = GetHU(mousePoint);
    }

    private double GetHU(Point point) {
        var image = _dicomImages[CurrentFrame];
        return 0.0f;
    }
}

