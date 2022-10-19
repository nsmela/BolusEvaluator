using BolusEvaluator.MVVM.Models;
using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.DicomService {
    public interface IDicomService {
        ImportedDicomDataset Data { get; }
        ImportedDicomDataset Control { get; }

        bool IsBusy { get; set; }
        BitmapSource GetDicomImage { get; }
        double MaxWindowLevel { get; }
        double MinWindowLevel { get; }

        double LowerWindowValue { get; }
        double UpperWindowValue { get; }

        int CurrentFrame { get; }
        int FrameCount { get; }

        string FrameText { get; }

        //methods
        void SetLowerWindowLevel(double level);
        void SetUpperWindowLevel(double level);
        void SetWindowLevel(double lowerLevel, double upperLevel);
        void SetFrame(int frameIndex);
        void LoadDataset(List<DicomDataset> datasets);
        void LoadControlDataset(List<DicomDataset> datasets);
        double GetHU(Point point);
        double[,] GetHUs();

        //events
        event Action OnDatasetLoaded, OnNewFrame, OnDicomImageUpdated, OnControlLoaded;
    }
}
