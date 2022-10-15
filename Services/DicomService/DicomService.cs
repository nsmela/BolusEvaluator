using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}

