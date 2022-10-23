using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.MVVM.Models;
public class DicomSet {
    string _filename;
    List<Slice> _slices;

    //details
    int _imageWidth, _imageHeight;
    double _pixelWidth, _pixelHeight, _pixelArea, _pixelVolume;
    double _sliceThickness;
    double _rescaleSlope, _rescaleIntercept;

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
            _rescaleSlope = data[0].GetSingleValue<double>(DicomTag.RescaleSlope);
            _rescaleIntercept = data[0].GetSingleValue<double>(DicomTag.RescaleIntercept);

            _imageWidth = data[0].GetSingleValue<int>(DicomTag.Columns);
            _imageHeight = data[0].GetSingleValue<int>(DicomTag.Rows);

            _sliceThickness = data[0].GetSingleValue<double>(DicomTag.SliceThickness);

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
    public DicomDataset Data { get; private set; }
    public Vector3 Position { get; set; }
    public IPixelData PixelMap { get; set; }

    public Slice(DicomDataset data) {
        Data = data;
        var position = Data.TryGetValues<double>(DicomTag.ImagePositionPatient, out var pos) ? pos : Array.Empty<double>();
        Position = new Vector3((float)position[0], (float)position[1], (float)position[2]);

        var header = DicomPixelData.Create(Data);
        PixelMap = (PixelDataFactory.Create(header, 0));
    }

    public WriteableBitmap GetWindowedImage(double lowerWindow, double upperWindow) {

        return new DicomImage(Data).RenderImage().AsWriteableBitmap();
    }
}
