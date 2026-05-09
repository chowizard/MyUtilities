using System.IO;
using System.Windows;

using Microsoft.Win32;

using Summarizer.App.ViewModels;
using Summarizer.Core;

namespace Summarizer.App
{
    public partial class App : Application
    {
        internal const string Version = "1.2.2";

        private string settingsPath = string.Empty;
        private AppSettings settings = new();

        private void OnStartup(object sender, StartupEventArgs e)
        {
            settingsPath = Path.Combine(AppContext.BaseDirectory, "AppSettings.json");
            settings = AppSettingsLoader.Load(settingsPath);

            if (e.Args.Length > 0)
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                RunBatchMode(e.Args[0]);
                Shutdown();
                return;
            }

            ApplyTheme(settings.Theme);

            var viewModel = new MainWindowViewModel(settings, settingsPath, ApplyTheme);
            var window = new MainWindow(viewModel);
            window.Show();
        }

        private void RunBatchMode(string inputFilePath)
        {
            try
            {
                var batchConverter = new BatchFileConverter(settings);
                batchConverter.ConvertFile(inputFilePath);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"배치 변환 중 오류가 발생했습니다.\n\n{exception.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyTheme(string theme)
        {
            var uri = ResolveThemeUri(theme);
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
        }

        private static Uri ResolveThemeUri(string theme)
        {
            var themeName = ResolveThemeName(theme);
            return new Uri(string.Concat("Themes/", themeName, "Theme.xaml"), UriKind.Relative);
        }

        private static string ResolveThemeName(string theme)
        {
            if (theme == "Light") return "Light";
            if (theme == "Dark") return "Dark";
            return IsWindowsLightTheme() ? "Light" : "Dark";
        }

        private static bool IsWindowsLightTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 1;
        }
    }
}
