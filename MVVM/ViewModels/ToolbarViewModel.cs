using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FellowOakDicom;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Windows;
using BolusEvaluator.Messages;
using BolusEvaluator.ImageTools;

namespace BolusEvaluator.MVVM.ViewModels;

[ObservableObject]
public partial class ToolbarViewModel {
    [ObservableProperty] private string? _currentTool = string.Empty; //placeholder for more advanced tools    

    private void IsBusy (bool value) => WeakReferenceMessenger.Default.Send(new IsBusyMessage(value));
    private bool _isHighlightImageActive = false;
    [RelayCommand]
    private async Task ShowHighlight() {
        _isHighlightImageActive = !_isHighlightImageActive;

        if(_isHighlightImageActive) WeakReferenceMessenger.Default.Send(new AddImageTool(new HighlightImageWindow()));
        else WeakReferenceMessenger.Default.Send(new DeleteImageTool(new HighlightImageWindow()));

    }

    [RelayCommand]
    private async Task LoadDicomFile(CancellationToken token) {
        try {
            //file explorer
            OpenFileDialog openFile = new() {
                Filter = "DICOM Files (*.dcm)|*.dcm|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFile.ShowDialog() != true)
                return;

            IsBusy(true);

            //generating an image
            var file = await DicomFile.OpenAsync(openFile.FileName);
            WeakReferenceMessenger.Default.Send(new DicomDatasetMessage(await GetData(openFile)));

            //dump DICOM tags
            WeakReferenceMessenger.Default.Send(new DicomDetailsMessage( await GetAllTags(file)));

        } catch (OperationCanceledException e) {
            MessageBox.Show("Load Dicom File failed: " + e.Message);
        }
            IsBusy(false);
    }


    private async Task<string> GetAllTags(DicomFile file) {
        var task = await Task.Run(() => {
            string result = "Results: \r\n";
            foreach (var tag in file.Dataset) {
                if (file.Dataset.TryGetString(tag.Tag, out string value)) {
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


