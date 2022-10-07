using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FellowOakDicom.Imaging;
using System;
using System.Windows;
using System.Windows.Media;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
[ObservableRecipient]
public partial class ImageViewModel {
    [ObservableProperty] private ImageSource? _displayImage; //Creates DisplayImage property
    [ObservableProperty] private double? _lowerWindowValue;
    [ObservableProperty] private double? _upperWindowValue;
    partial void OnUpperWindowValueChanged(double? value) => RecalculateImageWindow();
    partial void OnLowerWindowValueChanged(double? value) => RecalculateImageWindow();

    private DicomImage? _dicomImage;

    private bool _isBusy = false;


    private void RecalculateImageWindow() {
        if (_isBusy) return;
        if(_dicomImage is null) return;

        _isBusy = true;
        var window = (UpperWindowValue - LowerWindowValue);
        var center = (LowerWindowValue + (window / 2));

        //reset window level
        _dicomImage.WindowWidth = window is null ? 100.0f : (double)window;
        _dicomImage.WindowCenter = center is null ? 0.0f : (double)center;

        UpdateDisplayImage();
        _isBusy = false;
            
    }

    public ImageViewModel() {
        WeakReferenceMessenger.Default.Register<DicomImageMessage>(this, (r, m) => {
            ReceiveDicomMessage(m.Value);
        });
    }

    private void ReceiveDicomMessage(DicomImage value) {
        try {
            //if image is cleared
            if(value is null) {
                _dicomImage = null;
                DisplayImage = null;
                return;
            }

            _dicomImage = value;

            //create image source
            UpdateDisplayImage();
            
            //update slider values
            var range = _dicomImage.WindowWidth/2;
            var center = _dicomImage.WindowCenter;

            _isBusy = true;
            LowerWindowValue = center - range;
            UpperWindowValue = center + range;
            _isBusy = false;


        } catch (Exception ex) {
            MessageBox.Show("Error: " + ex.Message);
        }
    }

    private void UpdateDisplayImage() {
        var image = _dicomImage.RenderImage().AsWriteableBitmap();
        DisplayImage = image;
    }
}

