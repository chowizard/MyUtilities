using System.ComponentModel;
using System.Windows;

using Summarizer.App.ViewModels;

namespace Summarizer.App
{
    public partial class AppSettingsDialog : Window
    {
        public AppSettingsDialog(AppSettingsDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseRequested += (_, _) => Close();
            Closing += OnWindowClosing;
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (DataContext is AppSettingsDialogViewModel viewModel)
                viewModel.SaveSettings();
        }
    }
}
