using BolusEvaluator.MVVM.Models;
using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.DicomService {
    public interface IDicomService {
        DicomSet Data { get; }
        DicomSet Control { get; }

        bool IsBusy { get; set; }

        //methods
        Task LoadControlDicomSet(List<DicomDataset> data);
        Task LoadDicomSet(List<DicomDataset> data);

        //events
        event Action OnDatasetLoaded, OnControlLoaded;
    }
}
