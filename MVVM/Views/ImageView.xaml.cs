using BolusEvaluator.MVVM.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BolusEvaluator.MVVM.Views;

public partial class ImageView : UserControl {
    private ImageViewModel viewModel => DataContext as ImageViewModel;
    public ImageView() {
        DataContext = new ImageViewModel();
        InitializeComponent();
        this.Loaded += UserControl_Loaded;
    }

    void UserControl_Loaded(object sender, RoutedEventArgs e) {
        MetroWindow window = (MetroWindow)Window.GetWindow(this);
        foreach (InputBinding ib in this.InputBindings) {
            window.InputBindings.Add(ib);
        }
    }

}

