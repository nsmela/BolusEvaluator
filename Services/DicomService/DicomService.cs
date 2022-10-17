﻿using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.DicomService;
internal class DicomService : IDicomService {
    private List<DicomDataset> _data;
    private DicomImage _currentImage;
    private WriteableBitmap _bitmap;

    //dicom common values
    private double _rescaleSlope;
    private double _rescaleIntercept;
    private int _imageWidth;
    private int _imageHeight;

    private bool _isBusy = false;
    public bool IsBusy { get => _isBusy; set => _isBusy = value; }

    public BitmapSource GetDicomImage {
        get {
            if (_data is null) return null;
            if (_data.Count < 1) return null;
            if (_currentImage is null) return null;

            return _bitmap;
        }
    }

    public double MaxWindowLevel { get; private set; }
    public double MinWindowLevel { get; private set; }
    public double LowerWindowValue { get; private set; }
    public double UpperWindowValue { get; private set; }
    public string FrameText {
        get {
            if (_data is null) return string.Empty;

            //var patientDistance = _data[CurrentFrame].GetValue(DicomTag.PatientPosition);
            return "Testing";
        }
    }

    public void SetLowerWindowLevel(double level) {
        LowerWindowValue = level;
        UpdateImage();
    }

    public void SetUpperWindowLevel(double level) { 
        UpperWindowValue = level;
        UpdateImage();
    }

    public void SetWindowLevel(double lowerLevel, double upperLevel) {
        LowerWindowValue = lowerLevel;
        UpperWindowValue = upperLevel;
        UpdateImage();
    }

    public int CurrentFrame { get; private set; }
    public void SetFrame(int frameIndex) {
        if (_data is null || _data.Count < 1) return;
        if(frameIndex < 0 || frameIndex >= FrameCount) return;

        CurrentFrame = frameIndex;
        UpdateFrame();
    }

    public int FrameCount => _data.Count;

    public event Action OnDatasetLoaded; //when List of DicomDataset is added or cleared
    public event Action OnNewFrame; //when changing the image within the dataset
    public event Action OnDicomImageUpdated; //when details specific to the current image is changed

    //public methods
    public DicomService() {
        _data = new();
    }
    public void LoadDataset(List<DicomDataset> datasets) {
        if (datasets is null ||
            datasets.Count < 1) {
            _data = null;
            _currentImage = null;

            OnDatasetLoaded?.Invoke();
            return;
        }

        _data = datasets;

        try {
            CurrentFrame = 0;

            var range = _data[0].GetValue<double>(DicomTag.WindowWidth, 0) / 2;
            var center = _data[0].GetValue<double>(DicomTag.WindowCenter, 0);

            LowerWindowValue = center - range;
            UpperWindowValue = center + range;

            _rescaleSlope = _data[0].GetSingleValue<double>(DicomTag.RescaleSlope);
            _rescaleIntercept = _data[0].GetSingleValue<double>(DicomTag.RescaleIntercept);

            _imageWidth = _data[0].GetSingleValue<int>(DicomTag.Columns);
            _imageHeight = _data[0].GetSingleValue<int>(DicomTag.Rows);

        }
        catch (Exception e) {
            System.Windows.MessageBox.Show("Dicom service Error: " + e.Message + "\r\n" + e.InnerException);
        }
        _isBusy = false;
        OnDatasetLoaded?.Invoke();
        UpdateFrame();
    }

    private void UpdateFrame() {
        if (_isBusy) return;
        if (_data is null || _data.Count < 1) return;

        _currentImage = new DicomImage(_data[CurrentFrame]);

        var header = DicomPixelData.Create(_data[CurrentFrame]);
        var pixelMap = PixelDataFactory.Create(header, 0);
        var range = pixelMap.GetMinMax();

        MinWindowLevel = range.Minimum;
        MaxWindowLevel = range.Maximum;
        OnNewFrame?.Invoke();
        UpdateImage();
    }

    private void UpdateImage() {
        if (_isBusy) return;
        if (_data is null || _data.Count < 1) return;

        _currentImage.WindowWidth = (UpperWindowValue - LowerWindowValue);
        _currentImage.WindowCenter = LowerWindowValue + (_currentImage.WindowWidth / 2);

        _bitmap = _currentImage.RenderImage().AsWriteableBitmap();
        OnDicomImageUpdated?.Invoke();
    }

    //https://stackoverflow.com/questions/22991009/how-to-get-hounsfield-units-in-dicom-file-using-fellow-oak-dicom-library-in-c-sh
    //https://www.sciencedirect.com/topics/medicine-and-dentistry/hounsfield-scale
    //Hounsfield units = (Rescale Slope * Pixel Value) + Rescale Intercept
    public double GetHU(Point point) {
        if (_data is null) return 0.0f;
        var header = DicomPixelData.Create(_data[CurrentFrame]);
        var pixelMap = (PixelDataFactory.Create(header, 0));
        return GetHUValue(pixelMap, (int)point.X, (int)point.Y);
    }

    public double[,] GetHUs() {
        if (_data is null || _data.Count < 1) return null;

        var header = DicomPixelData.Create(_data[CurrentFrame]);
        var pixelMap = (PixelDataFactory.Create(header, 0));

        double[,] pixels = new double[pixelMap.Height, pixelMap.Width];
        for (int row = 0; row < pixelMap.Height; row++) {
            for (int col = 0; col < pixelMap.Width; col++) {
                pixels[row, col] =  GetHUValue(pixelMap, row, col);
            }
        }

        return pixels;
    }

    private double GetHUValue(IPixelData pixelMap, int iX, int iY) {
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
