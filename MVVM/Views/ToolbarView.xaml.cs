using BolusEvaluator.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
