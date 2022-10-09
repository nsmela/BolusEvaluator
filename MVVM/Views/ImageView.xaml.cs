using BolusEvaluator.MVVM.ViewModels;
using System.Windows.Controls;

namespace BolusEvaluator.MVVM.Views;

public partial class ImageView : UserControl {
    private ImageViewModel viewModel => DataContext as ImageViewModel;
    public ImageView() {
        DataContext = new ImageViewModel();
        InitializeComponent();
    }

    private void Image_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
        viewModel.OnImageZoom(e.Delta);
    }
}

