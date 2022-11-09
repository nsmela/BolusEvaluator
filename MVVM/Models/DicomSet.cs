using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace BolusEvaluator.MVVM.Models;
public class DicomSet  {
    string _filename;

    public event Action OnNewFrame, OnImageUpdated;
    List<Slice> _slices;
    public List<Slice> Slices => _slices;

    private int _currentFrame;    
    public int FrameCount => _slices.Count;
    public Slice CurrentSlice => _slices[CurrentFrame];

    public int CurrentFrame {
        get => _currentFrame;
        set {
            _currentFrame = value;
            OnNewFrame?.Invoke();
            OnImageUpdated?.Invoke();
        }
    }

    //window levels
    private double _lowerWindowValue, _upperWindowValue;
    public double LowerWindowValue { 
        get => _lowerWindowValue;
        set {
            _lowerWindowValue = value;
            OnImageUpdated?.Invoke();
        }
    }
    public double UpperWindowValue {
        get => _upperWindowValue;
        set {
            _upperWindowValue = value;
            OnImageUpdated?.Invoke();
        }
    }

    //details
    int _imageWidth, _imageHeight;
    public int ImageWidth => _imageWidth;
    public int ImageHeight => _imageHeight;
    double _pixelWidth, _pixelHeight, _pixelArea, _pixelVolume;
    public double PixelVolume => _pixelVolume;
    double _sliceThickness;
    public double SliceThickness => _sliceThickness;

    
    public double MaxHUValue(int slice) => _slices[slice].MaxValue;
    public double MinHUValue(int slice) => _slices[slice].MinValue;

    public Point3D FramePosition(int slice) => _slices[slice].Position;

    public BitmapSource GetDicomImage(int slice, double lowerWindow, double upperWindow) {
        if (_slices is null || _slices.Count < 1) return null;
        return _slices[slice].GetWindowedImage(lowerWindow, upperWindow);
    }

    public DicomSet(List<DicomDataset> data) {
        if (data is null || data.Count < 1) {
            MessageBox.Show("DicomImport failed!\r\nImported dataset is null!");

            return;
        }

        _slices = new();
        data.ForEach(x => {
            _slices.Add(new Slice(x));
        });

        try {

            _imageWidth = data[0].GetSingleValue<int>(DicomTag.Columns);
            _imageHeight = data[0].GetSingleValue<int>(DicomTag.Rows);

            _sliceThickness = data[0].GetSingleValue<double>(DicomTag.SliceThickness);

            var range = data[0].GetValue<double>(DicomTag.WindowWidth, 0) / 2;
            var center = data[0].GetValue<double>(DicomTag.WindowCenter, 0);

            _lowerWindowValue = center - range;
            _upperWindowValue = center + range;

            //area for each pixel
            var pixelSpacing = data[0].TryGetValues<double>(DicomTag.PixelSpacing, out var pos) ? pos : Array.Empty<double>();
            if (pixelSpacing != Array.Empty<double>()) {
                _pixelWidth = pixelSpacing[0];
                _pixelHeight = pixelSpacing[1];
                _pixelArea = _pixelWidth * _pixelHeight;
                _pixelVolume = _pixelArea * _sliceThickness;
            } 

        }
        catch (Exception e) {
            System.Windows.MessageBox.Show("DicomSet Error: " + e.Message + "\r\n" + e.InnerException);
        }
    }
}

public class Slice {
    private double _rescaleSlope, _rescaleIntercept;

    public DicomDataset Data { get; private set; }
    public DicomImage Image { get; private set; }
    public Point3D Position { get; private set; }
    public IPixelData PixelMap { get; private set; }
    public double MaxValue { get; private set; }
    public double MinValue { get; private set; }

    public Slice(DicomDataset data) {
        Data = data;
        Image = new DicomImage(Data);

        _rescaleSlope = Data.GetSingleValue<double>(DicomTag.RescaleSlope);
        _rescaleIntercept = Data.GetSingleValue<double>(DicomTag.RescaleIntercept);

        var position = Data.TryGetValues<double>(DicomTag.ImagePositionPatient, out var pos) ? pos : Array.Empty<double>();
        Position = new Point3D((float)position[0], (float)position[1], (float)position[2]);

        var header = DicomPixelData.Create(Data);
        PixelMap = (PixelDataFactory.Create(header, 0));

        GetMinMaxHUs(GetHUs(), out double min, out double max);
        MinValue = min;
        MaxValue = max;
    }

    public WriteableBitmap GetWindowedImage(double lowerWindow, double upperWindow) {
        Image.WindowWidth = (upperWindow - lowerWindow);
        Image.WindowCenter = (upperWindow- Image.WindowWidth / 2);
        return Image.RenderImage().AsWriteableBitmap();
    }

    public string FrameText => $"Position: {Position.X.ToString("0.00")} {Position.Y.ToString("0.00")} {Position.Z.ToString("0.00")}";


    #region Pixel values
    //https://stackoverflow.com/questions/22991009/how-to-get-hounsfield-units-in-dicom-file-using-fellow-oak-dicom-library-in-c-sh
    //https://www.sciencedirect.com/topics/medicine-and-dentistry/hounsfield-scale
    //Hounsfield units = (Rescale Slope * Pixel Value) + Rescale Intercept
    public double GetHU(Point point) {
        return GetHUValue(PixelMap, (int)point.X, (int)point.Y);
    }

    public double[,] GetHUs() {
        var pixelMap = PixelMap;
        double[,] pixels = new double[pixelMap.Height, pixelMap.Width];
        for (int row = 0; row < pixelMap.Height; row++) {
            for (int col = 0; col < pixelMap.Width; col++) {
                pixels[row, col] = GetHUValue(pixelMap, row, col);
            }
        }

        return pixels;
    }

    private double GetHUValue(IPixelData pixelMap, int iX, int iY) {
        if (pixelMap is null) return -2000;

        int index = (int)(iX + pixelMap.Width * iY); //turning a 2D array index into a 1D index
        switch (pixelMap) {
            case GrayscalePixelDataU16:
                return ((GrayscalePixelDataU16)pixelMap).Data[index] * _rescaleSlope + _rescaleIntercept;
            case GrayscalePixelDataS16:
                return ((GrayscalePixelDataS16)pixelMap).Data[index] * _rescaleSlope + _rescaleIntercept;
            default:
                return 0.0f;
        }
    }

    //the default GetMinMax from fo-DICOM doesn't work as well as I hoped. This does it manually
    private void GetMinMaxHUs(double[,] pixels, out double min, out double max) {
        min = 0.0f;
        max = 1.0f;
        foreach(var pixel in pixels) {
            if(pixel < min) min = pixel;
            if(pixel > max) max = pixel;
        }
    }
    #endregion
}
