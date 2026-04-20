using System.IO;
using System.Windows;

using Summarizer.App.ViewModels;
using Summarizer.Core;

namespace Summarizer.App
{
    public partial class App : Application
    {
        internal const string Version = "1.2.0";

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "AppSettings.json");
            var settings = AppSettingsLoader.Load(settingsPath);
            var viewModel = new MainWindowViewModel(settings);
            var window = new MainWindow(viewModel);
            window.Show();
        }
    }
}
