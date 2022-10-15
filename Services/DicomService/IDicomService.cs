using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.DicomService {
    public interface IDicomService {
        bool IsBusy { get; set; }
        BitmapSource GetDicomImage { get; }
        double MaxWindowLevel { get; }
        double MinWindowLevel { get; }

        double LowerWindowValue { get; }
        double UpperWindowValue { get; }

        int CurrentFrame { get; }
        int FrameCount { get; }

        //methods
        void SetLowerWindowLevel(double level);
        void SetUpperWindowLevel(double level);
        void SetWindowLevel(double lowerLevel, double upperLevel);
        void LoadDataset(List<DicomDataset> datasets);

        //events
        event Action OnDatasetLoaded, OnNewFrame, OnDicomImageUpdated;
    }
}
