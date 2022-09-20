using OpenCvSharp;
using PropertyChanged;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Input;

namespace KPUGeneralMacro.ViewModel
{
    [ImplementPropertyChanged]
    public class SpriteDialog : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private Model.Sprite _sprite;
        public Model.Sprite Sprite
        {
            get => this._sprite;
            private set => this._sprite = value;
        }


        private bool _isCapturePivot = false;
        public bool IsCapturePivot
        {
            get => this._isCapturePivot;
            set => this._isCapturePivot = value;
        }

        public ICommand CaptureCommand { get; private set; }

        public SpriteDialog(Model.Sprite sprite)
        {
            this.Sprite = sprite;

            this.CaptureCommand = new RelayCommand(this.OnCapture);
        }

        private void OnCapture(object obj)
        {
            IsCapturePivot = false;

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
