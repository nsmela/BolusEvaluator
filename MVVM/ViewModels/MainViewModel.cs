using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using Microsoft.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BolusEvaluator.MVVM.ViewModels;

public class DicomImageMessage : ValueChangedMessage<DicomImage> {
    public DicomImageMessage(DicomImage value) : base(value) { }
}

[ObservableObject]
public partial class MainViewModel {
    [ObservableProperty] private bool _isBusy = false; //IsBusy observable property

    [RelayCommand]
    private async Task GetDicomFile(CancellationToken token) {
        try {
            //file explorer
            OpenFileDialog openFile = new() {
                Filter = "DICOM Files (*.dcm)|*.dcm|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (openFile.ShowDialog() != true)
                return;

            IsBusy = true;

            var file = await DicomFile.OpenAsync(openFile.FileName);

            //generating an image
            var image = new DicomImage(openFile.FileName);
            WeakReferenceMessenger.Default.Send(new DicomImageMessage(image));

            IsBusy = false;

        } catch (OperationCanceledException) {
            IsBusy = false;
        }
    }
}

