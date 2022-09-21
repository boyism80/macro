using KPUGeneralMacro.Extension;
using OpenCvSharp;
using PropertyChanged;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace KPUGeneralMacro.ViewModel
{
    [ImplementPropertyChanged]
    public class ExtensionColor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) => PropertyChanged(this, new PropertyChangedEventArgs(name));
        public void OnPropertyChanged(object sender, string name) => PropertyChanged(sender, new PropertyChangedEventArgs(name));

        private Sprite _owner;

        private bool _activated = false;
        public bool Activated
        {
            get => _activated;
            set
            {
                _activated = value;
                this.OnPropertyChanged(this._owner, nameof(Sprite.Bitmap));
                this.OnPropertyChanged(this._owner, nameof(Sprite.MaskBitmap));
            }
        }

        private Color _pivot = Color.FromArgb(255, Color.White);
        public Color Pivot
        {
            get => _pivot;
            set
            {
                _pivot = value;
                this.OnPropertyChanged(nameof(this.Hex));
                this.OnPropertyChanged(this._owner, nameof(Sprite.Bitmap));
                this.OnPropertyChanged(this._owner, nameof(Sprite.MaskBitmap));
            }
        }
        public System.Windows.Media.Color MediaPivot
        {
            get => System.Windows.Media.Color.FromArgb(255, this.Pivot.R, this.Pivot.G, this.Pivot.B);
            set => this.Pivot = Color.FromArgb(value.R, value.G, value.B);
        }

        private float _factor = 1.0f;
        public float Factor
        {
            get => _factor;
            set
            {
                _factor = value;
                this.OnPropertyChanged(nameof(this.FactorPercent));
                this.OnPropertyChanged(this._owner, nameof(Sprite.Bitmap));
                this.OnPropertyChanged(this._owner, nameof(Sprite.MaskBitmap));
            }
        }

        public string Hex => ColorTranslator.ToHtml(Pivot);
        public string FactorPercent => $"{Factor * 100:0.00}";

        public Model.ExtensionColor Model => new Model.ExtensionColor
        { 
            Activated = Activated,
            Pivot = Pivot,
            Factor = Factor
        };


        public ExtensionColor(Sprite owner)
        {
            this._owner = owner;
        }
    }

    [ImplementPropertyChanged]
    public class Sprite
    {
        public Mat Source { get; private set; } = new Mat();
        public Mat Dest
        {
            get
            {
                if (this.ExtColor.Activated)
                {
                    var mat = new Mat();
                    this.Source.CopyTo(mat, Mask);
                    return mat;
                }
                else
                {
                    return this.Source;
                }
            }
        }
        public Mat Mask
        {
            get
            {
                if (this.ExtColor.Activated == false)
                    return this.Source;
                else
                    return this.Source.ToMask(this.ExtColor.Pivot, this.ExtColor.Factor);
            }
        }
        public string Name { get; set; }
        public float Factor { get; set; }

        private readonly ExtensionColor _extension;
        public ExtensionColor ExtColor => _extension;
        public Rectangle? FixedArea { get; set; }
        public BitmapImage Bitmap => Dest.ToBitmap();
        public BitmapImage MaskBitmap => Mask.ToBitmap();
        public byte[] Bytes
        {
            get
            {
                Cv2.ImEncode(".png", Source, out var buffer);
                return buffer;
            }
        }

        public Model.Sprite Model => new Model.Sprite
        { 
            Name = Name,
            Mat = Source,
            ExtensionColor = ExtColor.Model
        };

        public Sprite(Mat mat)
        {
            this._extension = new ExtensionColor(this);

            mat.CopyTo(this.Source);
            mat.CopyTo(this.Dest);
        }
    }
}