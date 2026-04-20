using Summarizer.App.ViewModels;

namespace Summarizer.App
{
    public partial class MainWindow
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
