using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

using Summarizer.App.Commands;

namespace Summarizer.App.ViewModels
{
    /// <summary>
    /// 강남언니 필드 하나에 대응하는 레이블 텍스트 목록(예: 고객 이름 → 고객명, 방문자명)을 편집하는 뷰모델.
    /// </summary>
    public class FieldLabelGroupViewModel
    {
        public string Title { get; }

        public ObservableCollection<FieldLabelItemViewModel> Labels { get; } = [];


        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand SelectAllCommand { get; }


        public FieldLabelGroupViewModel(string title, IEnumerable<string> labels)
        {
            Title = title;

            foreach (var label in labels)
                AddLabel(label);

            AddCommand = new RelayCommand(() => AddLabel(string.Empty));

            DeleteCommand = new RelayCommand(
                () =>
                {
                    foreach (var item in Labels.Where(item => item.IsSelected).ToList())
                        Labels.Remove(item);
                },
                () => Labels.Any(item => item.IsSelected));

            MoveUpCommand = new RelayCommand(
                () =>
                {
                    int index = Labels.IndexOf(Labels.First(item => item.IsSelected));
                    Labels.Move(index, index - 1);
                },
                () =>
                {
                    var selected = Labels.Where(item => item.IsSelected).ToList();
                    return selected.Count == 1 && Labels.IndexOf(selected[0]) > 0;
                });

            MoveDownCommand = new RelayCommand(
                () =>
                {
                    int index = Labels.IndexOf(Labels.First(item => item.IsSelected));
                    Labels.Move(index, index + 1);
                },
                () =>
                {
                    var selected = Labels.Where(item => item.IsSelected).ToList();
                    return selected.Count == 1 && Labels.IndexOf(selected[0]) < Labels.Count - 1;
                });

            SelectAllCommand = new RelayCommand(() =>
            {
                foreach (var item in Labels)
                    item.IsSelected = true;
            });
        }


        public string[] ToArray()
        {
            return [.. Labels
                .Select(item => item.Text.Trim())
                .Where(text => text.Length > 0)];
        }

        private void AddLabel(string text)
        {
            var item = new FieldLabelItemViewModel { Text = text };
            item.PropertyChanged += OnItemPropertyChanged;
            Labels.Add(item);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FieldLabelItemViewModel.IsSelected))
                CommandManager.InvalidateRequerySuggested();
        }
    }
}
