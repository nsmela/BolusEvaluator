using BolusEvaluator.MVVM.ViewModels;
using BolusEvaluator.Services.DicomService;
using System.Windows.Controls;

namespace BolusEvaluator.MVVM.Views {
    /// <summary>
    /// Interaction logic for ToolbarView.xaml
    /// </summary>
    public partial class ToolbarView : UserControl {
        private ToolbarViewModel viewModel => (ToolbarViewModel)DataContext;

        public ToolbarView() {
            DataContext = new ToolbarViewModel();
            InitializeComponent();
        }
    }
}
