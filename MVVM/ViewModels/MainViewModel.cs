using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FellowOakDicom;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BolusEvaluator.MVVM.ViewModels;

public class DicomDatasetMessage : ValueChangedMessage<List<DicomDataset>> {
    public DicomDatasetMessage(List<DicomDataset> value) : base(value) { }
}

public class DicomDetailsMessage : ValueChangedMessage<string> {
    public DicomDetailsMessage(string value) : base(value) { }
}

public class IsBusyMessage : ValueChangedMessage<bool> {
    public IsBusyMessage(bool value) : base(value) { }
}

[ObservableObject]
[ObservableRecipient]
public partial class MainViewModel {
    [ObservableProperty] private bool _isBusy; //IsBusy observable property
    [ObservableProperty] private bool _isNotBusy;
    [ObservableProperty] private string? _fileInfo;

    partial void OnIsBusyChanged(bool value) { 
        IsNotBusy = !_isBusy;
    }

    public MainViewModel() {
        WeakReferenceMessenger.Default.Register<DicomDetailsMessage>(this, (r, m) => {
            FileInfo =m.Value;
        });

        WeakReferenceMessenger.Default.Register<IsBusyMessage>(this, (r, m) => {
            IsBusy = m.Value;
        });

        IsBusy = false;
    }

}

