using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace macro.Extension
{
    public static class OpenCvSharpMat
    {
        /// <summary>
        /// Convert Mat to mask using color detection with object pooling optimization
        /// Performance: Uses MatPool to avoid frequent Mat allocations during mask operations
        /// </summary>
        public static PooledMat ToMask(this Mat mat, Color pivot, float factor)
        {
            var e = 255 * factor;
            var colors = new int[] { pivot.B, pivot.G, pivot.R };

            var channels = mat.Split();
            var results = new PooledMat[3];

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    // Performance: Get PooledMat objects from pool instead of creating new ones
                    using var inv = MatPool.Get(channels[i].Rows, channels[i].Cols, channels[i].Type());
                    using var over = MatPool.Get(channels[i].Rows, channels[i].Cols, channels[i].Type());
                    using var under = MatPool.Get(channels[i].Rows, channels[i].Cols, channels[i].Type());

                    Cv2.BitwiseNot(channels[i], inv);
                    Cv2.Threshold(channels[i], over, (int)Math.Max(0, colors[i] - e), 255, ThresholdTypes.Binary);
                    Cv2.Threshold(inv, under, (int)Math.Max(0, 255 - colors[i] - e), 255, ThresholdTypes.Binary);

                    results[i] = MatPool.Get(channels[i].Rows, channels[i].Cols, channels[i].Type());
                    Cv2.BitwiseAnd(over, under, results[i]);
                }

                // Performance: Use pooled Mat objects for final operations
                using var result1 = MatPool.Get(results[0].Rows, results[0].Cols, results[0].Type());
                using var result2 = MatPool.Get(results[0].Rows, results[0].Cols, results[0].Type());

                Cv2.BitwiseAnd(results[0], results[1], result1);
                Cv2.BitwiseAnd(result1, results[2], result2);

                // Performance: Clone result using pool to avoid direct Mat allocation
                return MatPool.GetClone(result2);
            }
            finally
            {
                // Clean up channel mats and result mats
                for (int i = 0; i < channels.Length; i++)
                {
                    channels[i]?.Dispose();
                }

                for (int i = 0; i < results.Length; i++)
                {
                    results[i]?.Dispose();
                }
            }
        }

        /// <summary>
        /// Get rotated rectangles from Mat with object pooling optimization
        /// Performance: Uses MatPool for morphological operations to reduce allocations
        /// </summary>
        public static IEnumerable<RotatedRect> GetRotatedRects(this Mat mat, float threshold)
        {
            // Performance: Get working PooledMat objects from pool for morphological operations
            using var workingMat = mat.Type() != MatType.CV_8UC1 ?
                MatPool.Get(mat.Rows, mat.Cols, MatType.CV_8UC1) :
                MatPool.GetClone(mat);

            using var morphed = MatPool.Get(mat.Rows, mat.Cols, MatType.CV_8UC1);
            using var eroded = MatPool.Get(mat.Rows, mat.Cols, MatType.CV_8UC1);
            using var element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));

            if (mat.Type() != MatType.CV_8UC1)
            {
                mat.ConvertTo(workingMat, MatType.CV_8UC1);
            }

            // Morphological operations for contour detection
            Cv2.Erode(workingMat, eroded, element);
            Cv2.Dilate(eroded, morphed, element);

            Cv2.FindContours(morphed, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var results = contours.Select(x =>
            {
                var detectedRect = Cv2.MinAreaRect(x);
                return detectedRect;
            }).ToList();

            return results;
        }
    }
}
