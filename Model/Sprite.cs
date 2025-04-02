using IronPython.Runtime;
using macro.Extension;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace macro.Model
{
    public class SpriteExtension
    {
        public bool Activated { get; set; }
        public float Factor { get; set; }
        public bool DetectColor { get; set; }
        public Color Pivot { get; set; }
    }

    public class Sprite
    {
        public struct DetectionResult
        {
            public OpenCvSharp.Rect Rect { get; set; }
            public OpenCvSharp.Point Position { get; set; }
            public double Percentage { get; set; }

            public PythonDictionary ToPythonDictionary()
            {
                return new PythonDictionary
                {
                    ["rect"] = new PythonDictionary
                    {
                        ["x"] = Rect.X,
                        ["y"] = Rect.Y,
                        ["width"] = Rect.Width,
                        ["height"] = Rect.Height
                    },
                    ["position"] = new PythonTuple(new object[] { Position.X, Position.Y }),
                    ["percent"] = Percentage
                };
            }
        }

        public Mat Source { get; set; }
        public string Name { get; set; }
        public float Threshold { get; set; }
        public SpriteExtension Extension { get; set; }

        public DetectionResult MatchTo(Mat frame, OpenCvSharp.Rect? area = null)
        {
            if (frame == null)
                throw new Exception("frame cannot be null");

            var isClone = false;
            if (area != null)
            {
                isClone = true;
                frame = new Mat(frame, area.Value);
            }

            using var from = Extension.Activated ? frame.ToMask(Extension.Pivot, Extension.Factor) : frame.Clone();
            using var to = Extension.Activated ? Source.ToMask(Extension.Pivot, Extension.Factor) : Source.Clone();

            try
            {
                var result = new DetectionResult();
                if (Extension.Activated && Extension.DetectColor)
                {
                    var detectedRect = from.GetRotatedRects(Threshold).OrderByDescending(x => x.Size.Width * x.Size.Height).FirstOrDefault();
                    result = new DetectionResult
                    {
                        Rect = new Rect((int)(detectedRect.Center.X - detectedRect.Size.Width / 2.0),
                                        (int)(detectedRect.Center.Y - detectedRect.Size.Height / 2.0),
                                        (int)(detectedRect.Center.X + detectedRect.Size.Width / 2.0),
                                        (int)(detectedRect.Center.Y + detectedRect.Size.Height / 2.0)),
                        Position = (OpenCvSharp.Point)detectedRect.Center,
                        Percentage = 1.0
                    };
                }
                else
                {
                    using var matched = from.MatchTemplate(to, TemplateMatchModes.CCoeffNormed);
                    matched.MinMaxLoc(out var minval, out var maxval, out var minloc, out var maxloc);

                    var percentage = maxval;
                    var center = new OpenCvSharp.Point(maxloc.X + Source.Width / 2, maxloc.Y + Source.Height / 2);

                    result = new DetectionResult
                    {
                        Rect = new Rect(maxloc.X, maxloc.Y, Source.Width, Source.Width),
                        Position = center,
                        Percentage = percentage
                    };
                }


                if (area != null)
                {
                    result.Rect = new Rect(result.Rect.X + area.Value.X, result.Rect.Y + area.Value.Y, result.Rect.Width, result.Rect.Height);

                    result.Position = new OpenCvSharp.Point
                    {
                        X = result.Position.X + area.Value.X,
                        Y = result.Position.Y + area.Value.Y,
                    };
                }

                if (isClone)
                    frame.Dispose();

                return result;
            }
            catch (Exception)
            {
                return new DetectionResult();
            }
        }

        public List<DetectionResult> MatchToAll(Mat frame, float percent, OpenCvSharp.Rect? area = null)
        {
            var result = new List<DetectionResult>();
            frame = frame.Clone();
            while (true)
            {
                var detectionResult = MatchTo(frame, area);
                if (detectionResult.Percentage < percent)
                    break;

                var roi = new Mat(frame, detectionResult.Rect);
                roi.SetTo(Scalar.Black);

                result.Add(detectionResult);
            }

            return result;
        }

        public static List<Model.Sprite> Load(string path)
        {
            if (File.Exists(path) == false)
                throw new Exception($"{path} 파일을 찾을 수 없습니다.");

            var result = new List<Model.Sprite>();
            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                var size = reader.ReadInt32();
                var bytes = reader.ReadBytes(size);
                var threshold = reader.ReadSingle();
                var isActiveExt = reader.ReadBoolean();
                var detectColor = reader.ReadBoolean();
                var pivot = Color.FromArgb(reader.ReadInt32());
                var factor = reader.ReadSingle();
                var sprite = new Model.Sprite
                {
                    Name = name,
                    Source = Cv2.ImDecode(bytes, ImreadModes.AnyColor),
                    Threshold = threshold,
                    Extension = new Model.SpriteExtension
                    {
                        Activated = isActiveExt,
                        DetectColor = detectColor,
                        Pivot = pivot,
                        Factor = factor
                    }
                };
                result.Add(sprite);
            }

            return result;
        }

        public static void Save(string path, IEnumerable<Model.Sprite> sprites)
        {
            using var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate));
            writer.Write(sprites.Count());
            foreach (var sprite in sprites)
            {
                var bytes = sprite.Source.ToBytes();

                writer.Write(sprite.Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
                writer.Write(sprite.Threshold);
                writer.Write(sprite.Extension.Activated);
                writer.Write(sprite.Extension.DetectColor);
                writer.Write(sprite.Extension.Pivot.ToArgb());
                writer.Write(sprite.Extension.Factor);
            }
        }
    }
}
