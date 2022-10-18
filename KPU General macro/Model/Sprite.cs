using IronPython.Runtime;
using KPUGeneralMacro.Extension;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace KPUGeneralMacro.Model
{
    public class ExtensionColor
    {
        public bool Activated { get; set; }
        public Color Pivot { get; set; }
        public float Factor { get; set; }
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

        public string Name { get; set; }
        public Mat Mat { get; set; }
        public ExtensionColor ExtensionColor { get; set; }

        public DetectionResult MatchTo(Mat frame, OpenCvSharp.Rect? area = null)
        {
            if (frame == null)
                throw new Exception("frame cannot be null");

            var from = this.ExtensionColor.Activated ?
                frame.ToMask(this.ExtensionColor.Pivot, this.ExtensionColor.Factor) : frame.Clone();
            var to = this.ExtensionColor.Activated ?
                this.Mat.ToMask(this.ExtensionColor.Pivot, this.ExtensionColor.Factor) : this.Mat.Clone();

            if (area != null)
                from = new Mat(from, area.Value);

            try
            {
                var matched = from.MatchTemplate(to, TemplateMatchModes.CCoeffNormed);
                matched.MinMaxLoc(out var minval, out var maxval, out var minloc, out var maxloc);

                var percentage = maxval;
                maxloc.X += this.Mat.Width / 2;
                maxloc.Y += this.Mat.Height / 2;

                var result = new DetectionResult
                {
                    Rect = new Rect(maxloc.X - this.Mat.Width, maxloc.Y - this.Mat.Height, maxloc.X + this.Mat.Width, maxloc.Y + this.Mat.Width),
                    Position = new OpenCvSharp.Point(maxloc.X, maxloc.Y),
                    Percentage = percentage
                };

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
            catch(Exception e)
            {
                throw;
            }
            finally
            {
                frame.Dispose();
                to.Dispose();
            }
        }
    }
}
