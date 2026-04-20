using System.ComponentModel;
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


        public string InputText
        {
            get => field;
            set { field = value; OnPropertyChanged(); }
        } = string.Empty;

        public string OutputText
        {
            get => field;
            set { field = value; OnPropertyChanged(); }
        } = string.Empty;

        public bool IsAlwaysOnTop
        {
            get => field;
            set { field = value; OnPropertyChanged(); }
        }

        public ICommand ConvertCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CopyOutputCommand { get; }


        public MainWindowViewModel(AppSettings settings)
        {
            converter = new MessageConverter(settings);

            ConvertCommand = new RelayCommand(() => OutputText = converter.Convert(InputText));
            ClearCommand = new RelayCommand(() => { InputText = string.Empty; OutputText = string.Empty; });
            CopyOutputCommand = new RelayCommand(
                () => Clipboard.SetText(OutputText),
                () => !string.IsNullOrEmpty(OutputText));
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
