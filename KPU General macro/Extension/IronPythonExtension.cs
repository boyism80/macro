using IronPython.Runtime;
using System.Collections.Generic;

namespace KPU_General_macro.Extension
{
    public static class IronPythonEx
    {
        public static PythonTuple ToTuple(this OpenCvSharp.Point point)
        {
            return new PythonTuple(new[] { point.X, point.Y });
        }

        public static PythonTuple ToTuple(this OpenCvSharp.Size size)
        {
            return new PythonTuple(new[] { size.Width, size.Height });
        }

        public static PythonTuple ToTuple(this System.Drawing.Point point)
        {
            return new PythonTuple(new[] { point.X, point.Y });
        }

        public static PythonTuple ToTuple(this System.Drawing.Size size)
        {
            return new PythonTuple(new[] { size.Width, size.Height });
        }

        public static PythonTuple ToTuple(this System.Drawing.Rectangle rect)
        {
            return new PythonTuple(new[] { rect.X, rect.Y, rect.Width, rect.Height });
        }

        public static PythonDictionary ToDict(this Dictionary<string, OpenCvSharp.Point> dict)
        {
            var pythonDict = new PythonDictionary();
            foreach (var pair in dict)
                pythonDict.Add(pair.Key, pair.Value);

            return pythonDict;
        }
    }
}
