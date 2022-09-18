using KPUGeneralMacro.Extension;
using OpenCvSharp;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace KPUGeneralMacro.Model
{
    public struct ExtensionColor
    {
        public Color Pivot { get; set; }
        public float Factor { get; set; }
    }

    public class Sprite
    {
        private readonly Mat mat;

        public string Name { get; set; }
        public byte[] Bytes { get; private set; }
        public float Factor { get; set; }
        public ExtensionColor? ExtensionColor { get; set; }
        public Rectangle? FixedArea { get; set; }

        public BitmapImage Bitmap => mat.ToBitmap();

        public Sprite(Mat mat)
        {
            this.mat = mat;

            Cv2.ImEncode(".png", mat, out var buffer);
            this.Bytes = buffer;
        }
    }
}