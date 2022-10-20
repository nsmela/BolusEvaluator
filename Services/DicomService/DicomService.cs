using BolusEvaluator.MVVM.Models;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Mathematics;
using FellowOakDicom.Imaging.Render;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.DicomService;
internal class DicomService : IDicomService {
    private ImportedDicomDataset _data, _control;
    public ImportedDicomDataset Data => _data;
    public ImportedDicomDataset Control => _control;

    public BitmapSource GetDicomImage => _data.GetDicomImage;

    public double MaxWindowLevel => _data.MaxWindowLevel;

    public double MinWindowLevel => _data.MinWindowLevel;

    public double LowerWindowValue { get => _data.LowerWindowValue; }

    public double UpperWindowValue { get => _data.UpperWindowValue; }

    public int CurrentFrame => _data.CurrentFrame;

    public int FrameCount => _data.FrameCount;

    public string FrameText => _data.FrameText;

    public bool IsBusy {get => _data.IsBusy; set => _data.IsBusy = value; }

    public event Action OnDatasetLoaded, OnControlLoaded;
    public event Action OnNewFrame, OnNewControlFrame;
    public event Action OnDicomImageUpdated, OnControlImageUpdated;

    public double GetHU(Point point) {
        throw new NotImplementedException();
    }

    public double[,] GetHUs() {
        throw new NotImplementedException();
    }

    public void LoadDataset(List<DicomDataset> data) {
        _data = new ImportedDicomDataset(data);
        _data.OnDatasetLoaded += OnDatasetLoaded;
        _data.OnNewFrame += OnNewFrame;
        _data.OnDicomImageUpdated += OnDicomImageUpdated;

        _data.Refresh();
    }
    public void LoadControlDataset(List<DicomDataset> data) {
        _control = new ImportedDicomDataset(data);
        OnControlLoaded?.Invoke();
    }


    public void SetFrame(int frameIndex) => _data.SetFrame(frameIndex);


    public void SetLowerWindowLevel(double level) => _data.SetLowerWindowLevel(level);

    public void SetUpperWindowLevel(double level) => _data.SetUpperWindowLevel((int)level);

    public void SetWindowLevel(double lowerLevel, double upperLevel) => _data.SetWindowLevel(lowerLevel, upperLevel);
}