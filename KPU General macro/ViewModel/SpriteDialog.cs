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

        private ViewModel.Sprite _sprite;
        public ViewModel.Sprite Sprite
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
        public ICommand CompleteCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public SpriteDialog(Mat frame)
        {
            this.Sprite = new Sprite(frame);
            this.CaptureCommand = new RelayCommand(this.OnCapture);
            this.CompleteCommand = new RelayCommand(this.OnComplete);
            this.CancelCommand = new RelayCommand(this.OnCancel);
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
