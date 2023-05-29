using macro.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace macro.ViewModel
{
    public class EditResourceWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ViewModel.Sprite> Sprites { get; set; }
        public ViewModel.Sprite Selected { get; set; }

        public ObservableCollection<ViewModel.Sprite> SearchedList
        {
            get
            {
                if (string.IsNullOrEmpty(Filter))
                    return Sprites;

                return new ObservableCollection<ViewModel.Sprite>(Sprites.Where(x => x.Name.Contains(Filter)));
            }
        }

        private string _filter = string.Empty;

        public string Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchedList)));
            }
        }

        public ICommand DeleteCommand { get; private set; }
        public ICommand CompleteCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public EditResourceWindow(IEnumerable<Model.Sprite> sprites)
        {
            Sprites = new ObservableCollection<Sprite>(sprites.Select(x => new ViewModel.Sprite(x)));

            DeleteCommand = new RelayCommand(OnDelete);
        }

        private void OnDelete(object obj)
        {
            try
            {
                if (Selected == null)
                    throw new Exception("선택된 항목이 없습니다.");

                Sprites.Remove(Selected);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchedList)));

                Selected = null;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "실패");
            }
        }
    }
}
