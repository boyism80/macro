using IronPython.Runtime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace KPU_General_macro.Model
{
    public class Sprite : Dictionary<string, Sprite.Template>, IDisposable
    {
        public class Template : IDisposable
        {
            public string Name { get; set; }
            public byte[] Bytes { get; set; }
            public float Threshold { get; set; }
            public Nullable<Color> Color { get; set; }
            public float ErrorFactor { get; set; }
            public Mat Frame { get; set; }


            public Template(string name, byte[] template, float threshold = 0.95f)
            {
                this.Name = name;
                this.Bytes = template;
                this.Threshold = threshold;
                this.Frame = Cv2.ImDecode(this.Bytes, ImreadModes.AnyColor);
            }

            public Template(string name, byte[] template, float threshold, Nullable<Color> color, float errorFactor = 0.2f) : this(name, template, threshold)
            {
                this.Color = color;
                this.ErrorFactor = errorFactor;
            }

            public Template Clone()
            {
                return new Template(this.Name, this.Bytes, this.Threshold, this.Color, this.ErrorFactor);
            }

            public static Mat ClampThreshold(Mat data, Color color, float errorFactor)
            {
                var splitedData = data.Split();
                var splitedColor = new int[] { color.B, color.G, color.R };
                var limit = 255 * errorFactor;

                for (var i = 0; i < splitedData.Length; i++)
                {
                    var mask_ovr = splitedData[i].Threshold((int)(splitedColor[i] - limit), 255, ThresholdTypes.Binary);
                    var mask_und = splitedData[i].Threshold((int)(splitedColor[i] + limit), 255, ThresholdTypes.BinaryInv);

                    Cv2.BitwiseAnd(mask_ovr, mask_und, splitedData[i]);
                }

                var ret = (splitedData[0] & splitedData[1] & splitedData[2]).ToMat();
                return ret;
            }

            public Nullable<OpenCvSharp.Point> MatchTo(Mat frame, PythonTuple begin = null, PythonTuple size = null, bool useBinaryThreshold = false)
            {
                var percentage = 0.0;
                if (begin == null || size == null)
                    return this.MatchTo(frame, ref percentage, null as Nullable<OpenCvSharp.Point>, null as Nullable<OpenCvSharp.Size>, useBinaryThreshold, false);

                return this.MatchTo(frame, ref percentage, new OpenCvSharp.Point((int)begin[0], (int)begin[1]), new OpenCvSharp.Size((int)size[0], (int)size[1]), useBinaryThreshold);
            }

            public Nullable<OpenCvSharp.Point> MatchTo(Mat frame, ref double percentage, Nullable<OpenCvSharp.Point> begin = null, Nullable<OpenCvSharp.Size> size = null, bool useBinaryThreshold = false, bool debug = false)
            {
                if (frame == null)
                    return null;

                var convertedFrame = frame.Clone();
                var convertedData = this.Frame.Clone();

                if (useBinaryThreshold)
                {
                    convertedFrame = convertedFrame.CvtColor(ColorConversionCodes.BGR2GRAY);
                    convertedFrame = convertedFrame.Threshold(64, 255, ThresholdTypes.Binary);
                    convertedFrame = convertedFrame.CvtColor(ColorConversionCodes.GRAY2BGR);

                    convertedData = convertedData.CvtColor(ColorConversionCodes.BGR2GRAY);
                    convertedData = convertedData.Threshold(64, 255, ThresholdTypes.Binary);
                    convertedData = convertedData.CvtColor(ColorConversionCodes.GRAY2BGR);

                    if (debug)
                    {
                        Cv2.ImShow("converted frame", convertedFrame.Resize(new OpenCvSharp.Size(convertedFrame.Width * 5, convertedFrame.Height * 5)));
                        Cv2.ImShow("converted data", convertedData.Resize(new OpenCvSharp.Size(convertedData.Width * 5, convertedData.Height * 5)));
                        Cv2.WaitKey(0);
                    }
                }
                try
                {
                    if (begin != null && size != null)
                        convertedFrame = convertedFrame.Clone(new OpenCvSharp.Rect(begin.Value, size.Value));

                    if (this.Color != null)
                    {
                        convertedFrame = ClampThreshold(convertedFrame, this.Color.Value, this.ErrorFactor);
                        convertedData = ClampThreshold(convertedData, this.Color.Value, this.ErrorFactor);
                    }

                    var matched = convertedFrame.MatchTemplate(convertedData, TemplateMatchModes.CCoeffNormed);
                    double minval, maxval;
                    OpenCvSharp.Point minloc, maxloc;
                    matched.MinMaxLoc(out minval, out maxval, out minloc, out maxloc);

                    if (debug)
                        Console.WriteLine(maxval);
                    percentage = maxval;
                    if (maxval < this.Threshold)
                        return null;

                    maxloc.X += this.Frame.Width / 2;
                    maxloc.Y += this.Frame.Height / 2;

                    if (begin != null && size != null)
                        return new OpenCvSharp.Point(begin.Value.X + maxloc.X, begin.Value.Y + maxloc.Y);
                    else
                        return new OpenCvSharp.Point(maxloc.X, maxloc.Y);
                }
                catch (Exception e)
                {
                    return null;
                }
                finally
                {
                    convertedFrame.Dispose();
                }
            }

            public void Dispose()
            {
                if(this.Frame != null)
                    this.Frame.Dispose();
            }
        }

        public bool load(string filename)
        {
            try
            {
                using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    var spriteSize = reader.ReadInt32();
                    for (var i = 0; i < spriteSize; i++)
                    {
                        var name = reader.ReadString();
                        var threshold = reader.ReadSingle();
                        var templateSize = reader.ReadInt32();
                        var template = reader.ReadBytes(templateSize);
                        var usedColor = reader.ReadBoolean();
                        if (usedColor)
                            this.Add(name, new Sprite.Template(name, template, threshold, Color.FromArgb(reader.ReadInt32()), reader.ReadSingle()));
                        else
                            this.Add(name, new Sprite.Template(name, template, threshold));
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool save(string filename)
        {
            try
            {
                using (var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
                {
                    writer.Write(this.Count);                               // write script count : 4 bytes
                    foreach (var sprite in this.Values)
                    {
                        writer.Write(sprite.Name);                          // write sprite name : length + value
                        writer.Write(sprite.Threshold);                     // write threshold : 4bytes
                        writer.Write(sprite.Bytes.Length);                  // write bytes length : 4bytes
                        writer.Write(sprite.Bytes);                         // write bytes : bytes length
                        writer.Write(sprite.Color != null);
                        if (sprite.Color != null)
                        {
                            writer.Write(sprite.Color.Value.ToArgb());
                            writer.Write(sprite.ErrorFactor);
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public PythonDictionary ToDict()
        {
            var pythonDict = new PythonDictionary();
            foreach (var pair in this)
                pythonDict.Add(pair.Key, pair.Value);

            return pythonDict;
        }

        public void Dispose()
        {
            foreach (var template in this.Values)
                template.Dispose();

            this.Clear();
        }
    }
}
