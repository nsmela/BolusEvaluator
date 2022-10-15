using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace BolusEvaluator.MVVM.ViewModels;

public class DicomDetailsMessage : ValueChangedMessage<string> {
    public DicomDetailsMessage(string value) : base(value) { }
}

public class IsBusyMessage : ValueChangedMessage<bool> {
    public IsBusyMessage(bool value) : base(value) { }
}

public class ImageDisplayActionMessage : ValueChangedMessage<Action<ImageViewModel>> {
    public ImageDisplayActionMessage(Action<ImageViewModel> value) : base(value) { }
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

