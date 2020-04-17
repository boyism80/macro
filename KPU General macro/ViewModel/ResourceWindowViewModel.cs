using KPU_General_macro.Extension;
using KPU_General_macro.Model;
using OpenCvSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;

namespace KPU_General_macro.ViewModel
{
    public class SpriteWindowViewModel : BaseViewModel
    {
        public static readonly double INIT_THRESHOLD_VALUE = 0.95;

        private SpriteContainer _spriteContainer;

        private Mat _frame;
        public Mat Frame
        {
            get { return this._frame; }
            set { this._frame = value; this.OnPropertyChanged(nameof(this.Bitmap)); }
        }

        public BitmapImage Bitmap
        {
            get { return Frame?.ToBitmap(); }
        }

        public string Name { get; set; }

        public double Threshold { get; set; } = INIT_THRESHOLD_VALUE;

        public string ThresholdLabelText
        {
            get
            {
                return $"{this.Threshold * 100.0:0.##}%";
            }
        }

        public string Color { get; set; }

        public bool EnabledColorFactor
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(this.Color))
                        return false;

                    ColorTranslator.FromHtml(this.Color);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public double ColorErrorFactor { get; set; } = 0.05;
        public string ColorErrorFactorLabelText
        {
            get
            {
                return $"{this.ColorErrorFactor * 100.0:0.##}%";
            }
        }

        public ObservableCollection<Sprite> Sprites
        {
            get { return new ObservableCollection<Sprite>(this._spriteContainer.Values); }
        }

        public SpriteWindowViewModel(SpriteContainer spriteContainer)
        {
            this._spriteContainer = spriteContainer;
        }

        public new void OnPropertyChanged(string name)
        {
            switch (name)
            {
                case nameof(this._spriteContainer):
                    base.OnPropertyChanged(nameof(this.Sprites));
                    break;

                default:
                    base.OnPropertyChanged(name);
                    break;
            }
        }
    }

    public class StatusWindowViewModel : BaseViewModel
    {
        private SpriteContainer _spriteContainer;
        private StatusContainer _statusContainer;

        public string Name { get; set; }
        public string Script { get; set; }

        public ObservableCollection<Status> Statuses
        {
            get { return new ObservableCollection<Status>(this._statusContainer.Values); }
        }

        public List<Status.Component> Components { get; private set; } = new List<Status.Component>();
        
        public ObservableCollection<Status.Component> BindedStatusComponents
        {
            get { return new ObservableCollection<Status.Component>(Components); }
        }

        public ObservableCollection<Sprite> UnbindedSprites
        {
            get
            {
                var unbinded = new ObservableCollection<Sprite>(this._spriteContainer
                    .Select(x => x.Value)
                    .Where(x => Components.Select(c => c.Sprite).Contains(x) == false));

                return unbinded;
            }
        }

        public StatusWindowViewModel(SpriteContainer spriteContainer, StatusContainer statusContainer)
        {
            _spriteContainer = spriteContainer;
            _statusContainer = statusContainer;
        }

        public new void OnPropertyChanged(string name)
        {
            switch (name)
            {
                case nameof(this.Components):
                    base.OnPropertyChanged(nameof(BindedStatusComponents));
                    base.OnPropertyChanged(nameof(UnbindedSprites));
                    break;
            }
            
            base.OnPropertyChanged(name);
        }
    }

    public class ResourceWindowViewModel
    {
        public SpriteWindowViewModel SpriteVM { get; private set; }
        public StatusWindowViewModel StatusVM { get; private set; }

        public ResourceWindowViewModel(SpriteContainer spriteContainer, StatusContainer statusContainer)
        {
            this.SpriteVM = new SpriteWindowViewModel(spriteContainer);
            this.StatusVM = new StatusWindowViewModel(spriteContainer, statusContainer);
        }
    }
}
