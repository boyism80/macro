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

        public PooledMat Source { get; set; }
        public string Name { get; set; }
        public float Threshold { get; set; }
        public SpriteExtension Extension { get; set; }

        /// <summary>
        /// Template matching with object pooling optimization
        /// Performance: Uses MatPool for ROI operations and intermediate Mat objects
        /// </summary>
        public DetectionResult MatchTo(PooledMat frame, OpenCvSharp.Rect? area = null)
        {
            if (frame == null)
                throw new Exception("frame cannot be null");

            PooledMat workingFrame = null;
            var shouldDisposeFrame = false;

            try
            {
                if (area != null)
                {
                    // Performance: Use PooledMat for ROI operation
                    using var roiMat = PooledMat.AsReference(new Mat(frame, area.Value));
                    workingFrame = MatPool.GetClone(roiMat);
                    shouldDisposeFrame = true;
                }
                else
                {
                    workingFrame = frame;
                }

                var result = new DetectionResult();

                if (Extension.Activated && Extension.DetectColor)
                {
                    using var frameMask = workingFrame.ToMask(Extension.Pivot, Extension.Factor);
                    var detectedRect = frameMask.GetRotatedRects(Threshold).OrderByDescending(x => x.Size.Width * x.Size.Height).FirstOrDefault();
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
                    PooledMat fromMat = null;
                    PooledMat toMat = null;

                    try
                    {
                        // Performance: Use pooled Mat objects for template matching operations
                        fromMat = Extension.Activated ? workingFrame.ToMask(Extension.Pivot, Extension.Factor) : workingFrame;
                        toMat = Extension.Activated ? Source.ToMask(Extension.Pivot, Extension.Factor) : Source;

                        using var matched = MatPool.Get(fromMat.Rows - toMat.Rows + 1, fromMat.Cols - toMat.Cols + 1, MatType.CV_32FC1);

                        var templateSource = Extension.Activated ? toMat : Source;
                        var matchSource = Extension.Activated ? fromMat : workingFrame;

                        Cv2.MatchTemplate(matchSource, templateSource, matched, TemplateMatchModes.CCoeffNormed);
                        matched.MinMaxLoc(out var minval, out var maxval, out var minloc, out var maxloc);

                        var percentage = maxval;
                        var center = new OpenCvSharp.Point(maxloc.X + Source.Width / 2, maxloc.Y + Source.Height / 2);

                        result = new DetectionResult
                        {
                            Rect = new Rect(maxloc.X, maxloc.Y, Source.Width, Source.Height),
                            Position = center,
                            Percentage = percentage
                        };
                    }
                    finally
                    {
                        // Performance: Dispose pooled Mat objects only if they were created by ToMask
                        if (Extension.Activated)
                        {
                            if (fromMat != workingFrame) (fromMat as IDisposable)?.Dispose();
                            if (toMat != Source) (toMat as IDisposable)?.Dispose();
                        }
                    }
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

                return result;
            }
            catch (Exception)
            {
                return new DetectionResult();
            }
            finally
            {
                if (shouldDisposeFrame && workingFrame != null)
                {
                    workingFrame.Dispose();
                }
            }
        }

        /// <summary>
        /// Multiple template matching with object pooling optimization
        /// Performance: Uses MatPool.GetClone instead of frame.Clone() for working copy
        /// </summary>
        public List<DetectionResult> MatchToAll(PooledMat frame, float percent, OpenCvSharp.Rect? area = null)
        {
            var result = new List<DetectionResult>();

            // Performance: Use MatPool for cloning operation instead of frame.Clone()
            using var workingFrame = MatPool.GetClone(frame);

            while (true)
            {
                var detectionResult = MatchTo(workingFrame, area);
                if (detectionResult.Percentage < percent)
                    break;

                // Black out detected area to find next occurrence
                using (var roi = new Mat(workingFrame, detectionResult.Rect))
                {
                    roi.SetTo(Scalar.Black);
                }

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
                    Source = PooledMat.AsReference(Cv2.ImDecode(bytes, ImreadModes.AnyColor)),
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
