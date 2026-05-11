using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Summarizer.App.Commands;
using Summarizer.Core;

namespace Summarizer.App.ViewModels
{
    public class AppSettingsDialogViewModel : INotifyPropertyChanged
    {
        private readonly string settingsFilePath;
        private AppSettings settings;


        public string ReservationConfirmMessage
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

        public bool NormalizeBirthNumber
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

        public ObservableCollection<FormMessageItemViewModel> FormMessages { get; } = [];

        public ObservableCollection<ReplaceStaffMessageItemViewModel> ReplaceStaffMessages { get; } = [];


        public ICommand AddFormMessageCommand { get; }
        public ICommand DeleteFormMessagesCommand { get; }
        public ICommand MoveFormMessageUpCommand { get; }
        public ICommand MoveFormMessageDownCommand { get; }
        public ICommand SelectAllFormMessagesCommand { get; }

        public ICommand AddReplaceStaffMessageCommand { get; }
        public ICommand DeleteReplaceStaffMessagesCommand { get; }
        public ICommand MoveReplaceStaffMessageUpCommand { get; }
        public ICommand MoveReplaceStaffMessageDownCommand { get; }
        public ICommand SelectAllReplaceStaffMessagesCommand { get; }

        public ICommand OpenJsonFileCommand { get; }
        public ICommand CloseCommand { get; }


        public event EventHandler? CloseRequested;
        public event PropertyChangedEventHandler? PropertyChanged;


        public AppSettingsDialogViewModel(string settingsFilePath)
        {
            this.settingsFilePath = settingsFilePath;
            settings = AppSettingsLoader.Load(settingsFilePath);
            LoadFromSettings();

            AddFormMessageCommand = new RelayCommand(() =>
            {
                var item = new FormMessageItemViewModel();
                SubscribeItem(item);
                FormMessages.Add(item);
            });

            DeleteFormMessagesCommand = new RelayCommand(
                () =>
                {
                    foreach (var item in FormMessages.Where(item => item.IsSelected).ToList())
                        FormMessages.Remove(item);
                },
                () => FormMessages.Any(item => item.IsSelected));

            MoveFormMessageUpCommand = new RelayCommand(
                () =>
                {
                    int index = FormMessages.IndexOf(FormMessages.First(item => item.IsSelected));
                    FormMessages.Move(index, index - 1);
                },
                () =>
                {
                    var selected = FormMessages.Where(item => item.IsSelected).ToList();
                    return selected.Count == 1 && FormMessages.IndexOf(selected[0]) > 0;
                });

            MoveFormMessageDownCommand = new RelayCommand(
                () =>
                {
                    int index = FormMessages.IndexOf(FormMessages.First(item => item.IsSelected));
                    FormMessages.Move(index, index + 1);
                },
                () =>
                {
                    var selected = FormMessages.Where(item => item.IsSelected).ToList();
                    return selected.Count == 1 && FormMessages.IndexOf(selected[0]) < FormMessages.Count - 1;
                });

            SelectAllFormMessagesCommand = new RelayCommand(() =>
            {
                foreach (var item in FormMessages)
                    item.IsSelected = true;
            });

            AddReplaceStaffMessageCommand = new RelayCommand(() =>
            {
                var item = new ReplaceStaffMessageItemViewModel();
                SubscribeItem(item);
                ReplaceStaffMessages.Add(item);
            });

            DeleteReplaceStaffMessagesCommand = new RelayCommand(
                () =>
                {
                    foreach (var item in ReplaceStaffMessages.Where(item => item.IsSelected).ToList())
                        ReplaceStaffMessages.Remove(item);
                },
                () => ReplaceStaffMessages.Any(item => item.IsSelected));

            MoveReplaceStaffMessageUpCommand = new RelayCommand(
                () =>
                {
                    int index = ReplaceStaffMessages.IndexOf(ReplaceStaffMessages.First(item => item.IsSelected));
                    ReplaceStaffMessages.Move(index, index - 1);
                },
                () =>
                {
                    var selected = ReplaceStaffMessages.Where(item => item.IsSelected).ToList();
                    return selected.Count == 1 && ReplaceStaffMessages.IndexOf(selected[0]) > 0;
                });

            MoveReplaceStaffMessageDownCommand = new RelayCommand(
                () =>
                {
                    int index = ReplaceStaffMessages.IndexOf(ReplaceStaffMessages.First(item => item.IsSelected));
                    ReplaceStaffMessages.Move(index, index + 1);
                },
                () =>
                {
                    var selected = ReplaceStaffMessages.Where(item => item.IsSelected).ToList();
                    return selected.Count == 1 && ReplaceStaffMessages.IndexOf(selected[0]) < ReplaceStaffMessages.Count - 1;
                });

            SelectAllReplaceStaffMessagesCommand = new RelayCommand(() =>
            {
                foreach (var item in ReplaceStaffMessages)
                    item.IsSelected = true;
            });

            OpenJsonFileCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(settingsFilePath) { UseShellExecute = true }));

            CloseCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        }


        public void SaveSettings()
        {
            SaveToSettings();
        }

        private void LoadFromSettings()
        {
            ReservationConfirmMessage = settings.ReservationConfirmMessage;
            NormalizeBirthNumber = settings.NormalizeBirthNumber;

            const string regexPrefix = "regex:";

            foreach (var formMessage in settings.FormMessages)
            {
                bool isRegex = formMessage.StartsWith(regexPrefix, StringComparison.OrdinalIgnoreCase);
                var item = new FormMessageItemViewModel
                {
                    Text = isRegex ? formMessage[regexPrefix.Length..] : formMessage,
                    IsRegex = isRegex
                };
                SubscribeItem(item);
                FormMessages.Add(item);
            }

            foreach (var replaceMessage in settings.ReplaceStaffMessages)
            {
                bool isPatternRegex = replaceMessage.Pattern.StartsWith(regexPrefix, StringComparison.OrdinalIgnoreCase);
                bool hasReplacement = !string.IsNullOrEmpty(replaceMessage.Replacement);
                var item = new ReplaceStaffMessageItemViewModel
                {
                    HasComment = replaceMessage.Comment != null,
                    Comment = replaceMessage.Comment ?? string.Empty,
                    Pattern = isPatternRegex ? replaceMessage.Pattern[regexPrefix.Length..] : replaceMessage.Pattern,
                    IsPatternRegex = isPatternRegex,
                    HasReplacement = hasReplacement,
                    Replacement = replaceMessage.Replacement,
                    IsReplacementRegex = isPatternRegex
                };
                SubscribeItem(item);
                ReplaceStaffMessages.Add(item);
            }
        }

        private void SaveToSettings()
        {
            const string regexPrefix = "regex:";

            settings.ReservationConfirmMessage = ReservationConfirmMessage;
            settings.NormalizeBirthNumber = NormalizeBirthNumber;

            settings.FormMessages = [.. FormMessages
                .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                .Select(item => item.IsRegex ? regexPrefix + item.Text : item.Text)];

            settings.ReplaceStaffMessages = [.. ReplaceStaffMessages
                .Where(item => !string.IsNullOrWhiteSpace(item.Pattern))
                .Select(item => new ReplaceMessage
                {
                    Comment = (item.HasComment && !string.IsNullOrEmpty(item.Comment)) ? item.Comment : null,
                    Pattern = item.IsPatternRegex ? regexPrefix + item.Pattern : item.Pattern,
                    Replacement = item.HasReplacement ? item.Replacement : string.Empty
                })];

            AppSettingsLoader.Save(settingsFilePath, settings);
        }

        private void SubscribeItem(FormMessageItemViewModel item)
            => item.PropertyChanged += OnItemIsSelectedChanged;

        private void SubscribeItem(ReplaceStaffMessageItemViewModel item)
            => item.PropertyChanged += OnItemIsSelectedChanged;

        private void OnItemIsSelectedChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FormMessageItemViewModel.IsSelected))
                CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
