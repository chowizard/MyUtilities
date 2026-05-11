using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Summarizer.App.ViewModels
{
    public class ReplaceStaffMessageItemViewModel : INotifyPropertyChanged
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

        public bool HasComment
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

        public string Comment
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                if (string.IsNullOrEmpty(value))
                    HasComment = false;
                OnPropertyChanged();
            }
        } = string.Empty;

        public string Pattern
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

        public bool IsPatternRegex
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

        public bool HasReplacement
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

        public string Replacement
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                if (string.IsNullOrEmpty(value))
                    HasReplacement = false;
                OnPropertyChanged();
            }
        } = string.Empty;

        public bool IsReplacementRegex
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


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
