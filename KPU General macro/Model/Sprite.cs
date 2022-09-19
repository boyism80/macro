using System.Linq;
using KPUGeneralMacro.Extension;
using OpenCvSharp;
using PropertyChanged;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace KPUGeneralMacro.Model
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
                var mat = new Mat();
                this.Source.CopyTo(mat);

                if (this.ExtColor.Activated)
                {
                    var mask = Mask;
                    Cv2.Merge(mat.Split().Select(x => (x & mask).ToMat()).ToArray(), mat);
                }

                return mat;
            }
        }
        public Mat Mask
        {
            get
            {
                if (this.ExtColor.Activated == false)
                    return this.Source;

                var colors = new int[] { this.ExtColor.Pivot.B, this.ExtColor.Pivot.G, this.ExtColor.Pivot.R };
                var splitted = this.Source.Split().Select((x, i) =>
                {
                    var inv = new Mat();
                    Cv2.BitwiseNot(x, inv);

                    var e = 255 * this.ExtColor.Factor;
                    var over = x.Threshold((int)(colors[i] - e), 255, ThresholdTypes.Binary);
                    var under = inv.Threshold((int)(255 - colors[i] - e), 255, ThresholdTypes.Binary);

                    Cv2.BitwiseAnd(over, under, x);

                    return x;
                }).ToArray();

                return (splitted[0] & splitted[1] & splitted[2]).ToMat();
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

        public Sprite(Mat mat)
        {
            this._extension = new ExtensionColor(this);

            mat.CopyTo(this.Source);
            mat.CopyTo(this.Dest);
        }
    }
}