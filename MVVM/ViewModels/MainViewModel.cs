using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Render;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BolusEvaluator.MVVM.ViewModels;

public class DicomDatasetMessage : ValueChangedMessage<List<DicomDataset>> {
    public DicomDatasetMessage(List<DicomDataset> value) : base(value) { }
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

            //generating an image
            var file = await DicomFile.OpenAsync(openFile.FileName);
            WeakReferenceMessenger.Default.Send(new DicomDatasetMessage(await GetData(openFile)));

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

    private async Task<List<DicomDataset>> GetData(OpenFileDialog fileDialog) {
        if (fileDialog.FileNames.Length == 1) 
            return new List<DicomDataset>() { 
                (await DicomFile.OpenAsync(fileDialog.FileName)).Dataset 
            };
            

        var task = await Task.Run(async () => {
            List<DicomDataset> data = new();
            for (int i = 0; i < fileDialog.FileNames.Length; i++) {
                var dataset = await DicomFile.OpenAsync(fileDialog.FileNames[i]);
                data.Add((await DicomFile.OpenAsync(fileDialog.FileNames[i])).Dataset);
            }

            return data;
        });

        return task;
    }
}

