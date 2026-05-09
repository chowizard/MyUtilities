using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using Summarizer.App.Commands;
using Summarizer.Core;

namespace Summarizer.App.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly MessageConverter converter;
        private readonly AppSettings settings;
        private readonly string settingsFilePath;
        private readonly Action<string> applyTheme;


        public string InputText
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        public string OutputText
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        public bool IsAlwaysOnTop
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                OnPropertyChanged();
            }
        }

        public string CurrentTheme
        {
            get
            {
                return field;
            }
            private set
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsSystemTheme));
            }
        } = "System";

        public string StaffName
        {
            get
            {
                return settings.StaffName;
            }
            set
            {
                settings.StaffName = value;
                AppSettingsLoader.Save(settingsFilePath, settings);
                OnPropertyChanged();
            }
        }

        public bool IsLightTheme => CurrentTheme == "Light";
        public bool IsDarkTheme => CurrentTheme == "Dark";
        public bool IsSystemTheme => CurrentTheme == "System";

        public ICommand ConvertCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CopyOutputCommand { get; }
        public ICommand OpenSettingsFileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand SetLightThemeCommand { get; }
        public ICommand SetDarkThemeCommand { get; }
        public ICommand SetSystemThemeCommand { get; }


        public MainWindowViewModel(AppSettings settings, string settingsFilePath, Action<string> applyTheme)
        {
            this.settings = settings;
            this.settingsFilePath = settingsFilePath;
            this.applyTheme = applyTheme;
            converter = new MessageConverter(settings);

            CurrentTheme = settings.Theme;

            ConvertCommand = new RelayCommand(() => OutputText = converter.Convert(InputText));
            ClearCommand = new RelayCommand(() =>
            {
                InputText = string.Empty;
                OutputText = string.Empty;
            });
            CopyOutputCommand = new RelayCommand(
                () => Clipboard.SetText(OutputText),
                () => !string.IsNullOrEmpty(OutputText));

            OpenSettingsFileCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(settingsFilePath) { UseShellExecute = true }));

            ExitCommand = new RelayCommand(Application.Current.Shutdown);

            ShowAboutCommand = new RelayCommand(() =>
                MessageBox.Show(
                    $"Summarizer v{App.Version}\n\n카카오톡 비즈니스 채팅 메시지를 표준화된 요약 형식으로 변환하는 Windows 유틸리티 앱입니다.",
                    "정보",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information));

            SetLightThemeCommand = new RelayCommand(() => ChangeTheme("Light"));
            SetDarkThemeCommand = new RelayCommand(() => ChangeTheme("Dark"));
            SetSystemThemeCommand = new RelayCommand(() => ChangeTheme("System"));
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void ChangeTheme(string theme)
        {
            CurrentTheme = theme;
            applyTheme(theme);
            settings.Theme = theme;
            AppSettingsLoader.Save(settingsFilePath, settings);
        }
    }
}
