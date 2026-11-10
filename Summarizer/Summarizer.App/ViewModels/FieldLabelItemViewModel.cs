using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Summarizer.App.ViewModels
{
    public class FieldLabelItemViewModel : INotifyPropertyChanged
    {
        public bool IsSelected
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

        public string Text
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


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
