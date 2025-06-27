using macro.Command;
using macro.Extension;
using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace macro.ViewModel
{
    public class SpriteExtension : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Model.Sprite _owner;

        public Model.SpriteExtension Model
        {
            get => _owner.Extension;
            set => _owner.Extension = value;
        }

        public float Factor
        {
            get => Model?.Factor ?? 0.0f;
            set
            {
                if (Model == null)
                    return;

                Model.Factor = value;
            }
        }

        public bool Activated
        {
            get => Model?.Activated ?? false;
            set
            {
                if (Model == null)
                    return;

                Model.Activated = value;
            }
        }

        public bool DetectColor
        {
            get => Model?.DetectColor ?? false;
            set
            {
                if (Model == null)
                    return;

                Model.DetectColor = value;
            }
        }

        public Color Pivot
        {
            get => Model?.Pivot ?? Color.White;
            set
            {
                if (Model == null)
                    return;

                Model.Pivot = value;
            }
        }
        public System.Windows.Media.Color MediaPivot
        {
            get => System.Windows.Media.Color.FromArgb(255, Pivot.R, Pivot.G, Pivot.B);
            set => Pivot = Color.FromArgb(value.R, value.G, value.B);
        }

        public string FactorText => $"{Factor * 100:0.00}";

        public string Hex
        {
            get => ColorTranslator.ToHtml(Pivot);
            set => Pivot = ColorTranslator.FromHtml(value);
        }

        public SpriteExtension(Model.Sprite owner)
        {
            _owner = owner ?? throw new Exception("sprite cannot be null");
        }
    }

    public class Sprite : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Model.Sprite _model;
        public Model.Sprite Model
        {
            get => _model;
            set
            {
                _model = value;
                Extension = new SpriteExtension(_model);
            }
        }



        // Cached bitmap images to avoid frequent MatPool operations
        private BitmapImage _cachedBitmap;
        private BitmapImage _cachedMaskBitmap;
        private bool _bitmapCacheValid = false;
        private bool _maskBitmapCacheValid = false;

        public PooledMat Source
        {
            get => Model.Source as PooledMat ?? PooledMat.AsReference(Model.Source);
            set
            {
                Model.Source = value;
                InvalidateBitmapCache();
            }
        }

        public string Name
        {
            get => Model.Name;
            set => Model.Name = value;
        }

        public float Threshold
        {
            get => Model.Threshold;
            set
            {
                Model.Threshold = value;
                InvalidateBitmapCache();
            }
        }

        public SpriteExtension Extension { get; private set; }

        /// <summary>
        /// Get mask PooledMat with object pooling optimization
        /// Performance: Uses MatPool when ToMask is called to avoid allocations
        /// </summary>
        public PooledMat Mask
        {
            get
            {
                if (Extension.Activated == false)
                    return Source;

                return Source.ToMask(Extension.Pivot, Extension.Factor);
            }
        }

        /// <summary>
        /// Get destination PooledMat with object pooling optimization  
        /// Performance: Uses MatPool for cloning and Mat creation operations
        /// WARNING: Caller is responsible for disposing the returned PooledMat when IsDestFromMatPool is true
        /// </summary>
        public PooledMat Dest
        {
            get
            {
                if (Extension.Activated)
                {
                    if (Extension.DetectColor)
                    {
                        using var mask = Mask;
                        var detectedRect = mask.GetRotatedRects(Threshold).OrderByDescending(x => x.Size.Width * x.Size.Height).FirstOrDefault();
                        var points = detectedRect.Points().Select(x => (OpenCvSharp.Point)x).ToList();

                        var result = MatPool.GetClone(Source);
                        Cv2.DrawContours(result, [points], -1, Scalar.Lime, 2);
                        return result;
                    }
                    else
                    {
                        var result = MatPool.Get(Source.Rows, Source.Cols, Source.Type());
                        using var mask = Mask;
                        Source.CopyTo(result, mask);
                        return result;
                    }
                }
                else
                {
                    return Source;
                }
            }
        }



        public string NameException { get; set; }
        public string ThresholdText => $"{Threshold * 100:0.00}";
        public BitmapImage Bitmap
        {
            get
            {
                if (_bitmapCacheValid && _cachedBitmap != null)
                    return _cachedBitmap;

                using var dest = Dest;
                _cachedBitmap = dest.ToBitmap();
                _bitmapCacheValid = true;
                return _cachedBitmap;
            }
        }
        public BitmapImage MaskBitmap
        {
            get
            {
                if (_maskBitmapCacheValid && _cachedMaskBitmap != null)
                    return _cachedMaskBitmap;

                using var mask = Mask;
                _cachedMaskBitmap = mask.ToBitmap();
                _maskBitmapCacheValid = true;
                return _cachedMaskBitmap;
            }
        }

        public ICommand CaptureCommand { get; private set; }

        public Sprite(Model.Sprite model)
        {
            Model = model ?? throw new Exception("model cannot be null");

            CaptureCommand = new RelayCommand(OnCapture);

            Extension.PropertyChanged += Extension_PropertyChanged;
        }

        public void OnCapture(object obj)
        {
            var image = obj as System.Windows.Controls.Image;
            if (image.ActualWidth == 0 || image.ActualHeight == 0)
                return;

            var position = Mouse.GetPosition(image);

            var coordinate = new OpenCvSharp.Point
            {
                X = (int)(Source.Width * (position.X / image.ActualWidth)),
                Y = (int)(Source.Height * (position.Y / image.ActualHeight)),
            };

            var (b, g, r) = Source.At<Vec3b>(coordinate.Y, coordinate.X);
            Extension.Pivot = Color.FromArgb(r, g, b);
        }

        private void Extension_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SpriteExtension.Pivot):
                case nameof(SpriteExtension.Factor):
                case nameof(SpriteExtension.Activated):
                case nameof(SpriteExtension.DetectColor):
                    InvalidateBitmapCache();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bitmap)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaskBitmap)));
                    break;
            }
        }

        /// <summary>
        /// Invalidates cached bitmap images when properties change
        /// </summary>
        private void InvalidateBitmapCache()
        {
            _bitmapCacheValid = false;
            _maskBitmapCacheValid = false;
            _cachedBitmap = null;
            _cachedMaskBitmap = null;
        }

        public override string ToString() => Name;

        public void Dispose()
        {
            if (Source != null)
            {
                // PooledMat automatically handles disposal and pool return
                Source.Dispose();
            }

            Extension.PropertyChanged -= Extension_PropertyChanged;
        }
    }
}
