using BolusEvaluator.MVVM.Models;
using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.DicomService;
internal class DicomService : IDicomService {
    private DicomSet _data, _control;
    public DicomSet Data => _data;
    public DicomSet Control => _control;

    public bool IsBusy { get; set; }

    public event Action OnDatasetLoaded, OnControlLoaded;

    public async Task LoadControlDicomSet(List<DicomDataset> data) {
        await Task.Run(() => {
            _control = new DicomSet(data);

        });
        OnControlLoaded?.Invoke();
    }

    public async Task LoadDicomSet(List<DicomDataset> data) {
        await Task.Run(() => {
            _data = new DicomSet(data);
        });
        OnDatasetLoaded?.Invoke();
    }

}