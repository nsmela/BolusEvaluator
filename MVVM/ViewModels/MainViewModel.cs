using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.IO.Buffer;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace BolusEvaluator.MVVM.ViewModels;

public class DicomImageMessage : ValueChangedMessage<List<DicomImage>> {
    public DicomImageMessage(List<DicomImage> value) : base(value) { }
}

[ObservableObject]
public partial class MainViewModel {
    [ObservableProperty] private bool _isBusy = false; //IsBusy observable property
    [ObservableProperty] private bool _isNotBusy;
    [ObservableProperty] private string? _fileInfo;

    partial void OnIsBusyChanged(bool value) { 
        IsNotBusy = !_isBusy;
    }

    [RelayCommand]
    private async Task GetDicomFile(CancellationToken token) {
        try {
            //file explorer
            OpenFileDialog openFile = new() {
                Filter = "DICOM Files (*.dcm)|*.dcm|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFile.ShowDialog() != true)
                return;

            IsBusy = true;

            var file = await DicomFile.OpenAsync(openFile.FileName);

            //generating an image
            var image = await GetFile(openFile);
            WeakReferenceMessenger.Default.Send(new DicomImageMessage(image));

            //generate details text
            FileInfo = await GetAllTags(file);
            IsBusy = false;

        } catch (OperationCanceledException) {
            IsBusy = false;
        }
    }

    private async Task<string> GetAllTags(DicomFile file) {
        var task = await Task.Run(() => {
            string result = "Results: \r\n";
            foreach(var tag in file.Dataset) {
                if(file.Dataset.TryGetString(tag.Tag, out string value)) {
                    result += $"\r\n {tag.Tag.DictionaryEntry.Name} : {value}";
                }
            }
            return result;
        });

        return task;
    }

    private async Task<List<DicomImage>> GetFile(OpenFileDialog fileDialog) {
        if (fileDialog.FileNames.Length == 1) return new List<DicomImage>() { new DicomImage(fileDialog.FileName) };

        var task = await Task.Run(() => {
            List<DicomImage> images = new();
            for (int i = 0; i < fileDialog.FileNames.Length; i++) {
                images.Add(new DicomImage(fileDialog.FileNames[i]));
            }

            return images;
        });

        return task;
    }
}

