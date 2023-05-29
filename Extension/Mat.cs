using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace macro.Extension
{
    public static class OpenCvSharpMat
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

        public static IEnumerable<RotatedRect> GetRotatedRects(this Mat mat, float threshold)
        {
            if (mat.Type() != MatType.CV_8UC1)
                mat.ConvertTo(mat, MatType.CV_8UC1);

            mat = mat.Erode(null).Dilate(null);

            mat.FindContours(out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            return contours.Select(x =>
            {
                var detectedRect = Cv2.MinAreaRect(x);
                return detectedRect;
            });
        }
    }
}
