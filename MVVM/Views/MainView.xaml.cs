
using BolusEvaluator.MVVM.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;

namespace BolusEvaluator.MVVM.Views;
/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class MainView : MetroWindow {
    private MainViewModel _viewModel => (MainViewModel)DataContext;

    public MainView() {
        DataContext = new MainViewModel();
        InitializeComponent();
    }

}

