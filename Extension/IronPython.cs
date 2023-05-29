using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace macro.Extension
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

        public static IEnumerable<Keys> ToKeys(this PythonTuple keys)
        {
            return keys.Select(x => x as string)
                .Select(x =>
                {
                    switch (x)
                    {
                        case "ALT":
                            return Keys.LMenu;

                        case "ENTER":
                            return Keys.Enter;

                        case "CTRL":
                            return Keys.LControlKey;

                        case "`":
                            return Keys.Oem3;

                        case "SHIFT":
                            return Keys.LShiftKey;

                        case "ESCAPE":
                            return Keys.Escape;

                        default:
                            if (Enum.TryParse<Keys>(x, out var key) == false)
                                throw new Exception("invalid key");

                            return key;
                    }
                }).ToArray();
        }

        public static PythonDictionary ToPythonDictionary<K, V>(this IDictionary<K, V> dict)
        {
            var pythonDict = new PythonDictionary();
            foreach (var pair in dict)
                pythonDict.Add(pair.Key, pair.Value);

            return pythonDict;
        }
    }
}
