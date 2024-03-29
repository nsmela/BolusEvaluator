﻿using BolusEvaluator.MVVM.Views;
using BolusEvaluator.Services.DicomService;
using BolusEvaluator.Services.ImageOverlayService;
using BolusEvaluator.Services.InputService;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace BolusEvaluator;
    public partial class App : Application {
    public static IHost? AppHost { get; private set; }

    public App() {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) => {
                services.AddSingleton<MainView>();
                services.AddSingleton<IDicomService, DicomService>();
                services.AddSingleton<IImageOverlayService, ImageOverlayService>();
                services.AddSingleton<IInputService, InputService>();
            })
            .Build();

        new DicomSetupBuilder()
            .RegisterServices(s => s
                .AddFellowOakDicom().AddImageManager<WPFImageManager>())
            .Build();
        
    }

    protected override async void OnStartup(StartupEventArgs e) {
        await AppHost!.StartAsync();

        var startupForm = AppHost.Services.GetRequiredService<MainView>();
        startupForm.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e) {
        await AppHost!.StopAsync();
        base.OnExit(e);
    }
}

