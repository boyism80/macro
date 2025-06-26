using System.IO;
using System.Windows.Media.Imaging;

namespace macro.Extension
{
    public static class Bitmap
    {
        public static BitmapImage Parse(byte[] buffer)
        {
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(buffer))
            {
                stream.Position = 0;
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = null;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }

        public static BitmapImage ToBitmap(this OpenCvSharp.Mat frame)
        {
            return Parse(frame.ToBytes());
        }

        public static byte[] ToBytes(this BitmapImage bitmap)
        {
            if (bitmap == null || bitmap.StreamSource == null)
                return null;

            using (BinaryReader reader = new BinaryReader(bitmap.StreamSource))
            {
                return reader.ReadBytes((int)bitmap.StreamSource.Length);
            }
        }
    }
}
