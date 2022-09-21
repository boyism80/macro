using OpenCvSharp;
using System;
using System.Drawing;
using System.Linq;

namespace KPUGeneralMacro.Extension
{
    public static class MatExt
    {
        public static Mat ToMask(this Mat mat, Color pivot, float factor)
        {
            var e = 255 * factor;
            var colors = new int[] { pivot.B, pivot.G, pivot.R };
            var splitted = mat.Split().Select((x, i) =>
            {
                var inv = new Mat();
                Cv2.BitwiseNot(x, inv);

                var over = x.Threshold((int)Math.Max(0, colors[i] - e), 255, ThresholdTypes.Binary);
                var under = inv.Threshold((int)Math.Max(0, 255 - colors[i] - e), 255, ThresholdTypes.Binary);

                Cv2.BitwiseAnd(over, under, x);
                return x;
            }).ToArray();

            return (splitted[0] & splitted[1] & splitted[2]).ToMat();
        }
    }
}
