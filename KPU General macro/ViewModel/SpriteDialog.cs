using OpenCvSharp;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;

namespace KPUGeneralMacro.ViewModel
{
    public enum SpriteDialogMode
    {
        Create, Edit
    }

    [ImplementPropertyChanged]
    public class SpriteDialog : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SpriteDialogMode Mode { get; private set; }

        private ViewModel.Sprite _sprite;
        public ViewModel.Sprite Sprite
        {
            get => this._sprite;
            set => this._sprite = value;
        }

        public string _nameException = string.Empty;
        public string NameException
        {
            get => _nameException;
            set => _nameException = value;
        }

        public bool IsEnabled
        {
            get
            {
                if (this.Mode == SpriteDialogMode.Edit)
                {
                    if (this.Sprite == null)
                        return false;
                }

                if (string.IsNullOrEmpty(NameException))
                    return true;

                return false;
            }
        }

        public ObservableCollection<ViewModel.Sprite> Sprites { get; private set; } = new ObservableCollection<Sprite>();

        public ICommand CaptureCommand { get; private set; }
        public ICommand CompleteCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand RemoveCommand { get; set; }

        public SpriteDialog(IEnumerable<Model.Sprite> sprites)
        {
            this.Mode = SpriteDialogMode.Edit;
            this.Sprites = new ObservableCollection<Sprite>(sprites.OrderBy(x => x.Name).Select(x => new Sprite(this, x)));
            this.CaptureCommand = new RelayCommand(this.OnCapture);
            this.CompleteCommand = new RelayCommand(this.OnComplete);
            this.CancelCommand = new RelayCommand(this.OnCancel);
            this.RemoveCommand = new RelayCommand(this.OnRemove);
        }

        private void OnRemove(object obj)
        {
            var sprite = obj as Sprite;
            Sprites.Remove(sprite);
        }

        public SpriteDialog(Mat frame, IEnumerable<Model.Sprite> sprites) : this(sprites)
        {
            this.Mode = SpriteDialogMode.Create;
            this.Sprite = new Sprite(this, frame);
        }

        private void OnCancel(object obj)
        {
            this.Sprite = null;
        }

        private void OnComplete(object obj)
        {
            
        }

        private void OnCapture(object obj)
        {
            var image = obj as System.Windows.Controls.Image;
            var position = Mouse.GetPosition(image);

            var coordinate = new OpenCvSharp.Point
            {
                X = (int)(this.Sprite.Source.Width * (position.X / image.ActualWidth)),
                Y = (int)(this.Sprite.Source.Height * (position.Y / image.ActualHeight)),
            };

            var pixel = this.Sprite.Source.At<Vec3b>(coordinate.Y, coordinate.X);
            this.Sprite.ExtColor.Pivot = Color.FromArgb(pixel.Item2, pixel.Item1, pixel.Item0);
        }
    }
}
