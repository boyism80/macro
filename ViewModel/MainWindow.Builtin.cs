using IronPython.Runtime;
using macro.Extension;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace macro.ViewModel
{
    public partial class MainWindow
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        public PythonDictionary heap { get; private set; } = new PythonDictionary();

        public void OnXButton2Down()
        {
            if ((GetAsyncKeyState(System.Windows.Forms.Keys.LControlKey) & 0x8000) == 0x8000)
            {
                Run();
            }
            else
            {
                ExecPython("g.py");
            }
        }

        public void OnWheelClick()
        {
            ExecPython("wheel.py");
        }

        public void OnXButton1Down()
        {
            if ((GetAsyncKeyState(System.Windows.Forms.Keys.LControlKey) & 0x8000) == 0x8000)
            {
                Stop();
            }
            else
            {
                ExecPython("dialogue.py");
            }
        }

        public bool DrawRectangles(Mat frame, List<Rect> areas, uint color = 0xffff0000)
        {
            if (frame == null)
                return false;

            foreach (var area in areas)
            {
                var r = (color & 0x00ff0000) >> 4;
                var g = (color & 0x0000ff00) >> 2;
                var b = (color & 0x000000ff);
                frame.Rectangle(area, new Scalar(b, g, r));
            }
            return true;
        }

        public bool DrawRectangles(Mat frame, PythonList areas, uint color = 0xffff0000)
        {
            try
            {
                var csAreaList = new List<Rect>();
                foreach (var area in areas)
                {
                    var pythonArea = area as PythonTuple;
                    csAreaList.Add(new Rect((int)pythonArea[0], (int)pythonArea[1], (int)pythonArea[2], (int)pythonArea[3]));
                }

                return DrawRectangles(frame, csAreaList, color);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Dictionary<string, Model.Sprite.DetectionResult> Detect(List<string> spriteNames, Rect? area = null)
        {
            var frame = Frame;
            if (frame == null)
                return new Dictionary<string, Model.Sprite.DetectionResult>();

            return spriteNames.Select(x => Sprites.FirstOrDefault(x2 => x2.Name == x))
            .Where(x => x != null)
            .ToDictionary(x => x.Name, x => x.Model.MatchTo(frame, area));
        }

        public PythonDictionary Detect(PythonTuple spriteNames, double minPercentage = 0.8, PythonDictionary area = null, int timeout = -1)
        {
            var begin = DateTime.Now;
            while (Running)
            {
                var areaCv = area != null ?
                    (Rect?)new Rect((int)area["x"], (int)area["y"], (int)area["width"], (int)area["height"]) :
                    null;

                var result = Detect(spriteNames.Select(x => x as string).ToList(), area: areaCv);
                result = result.Where(x => x.Value.Percentage >= minPercentage).ToDictionary(x => x.Key, x => x.Value);
                if (result.Count == 0)
                {
                    var elapsed = DateTime.Now - begin;
                    if (elapsed.TotalMilliseconds < (uint)timeout)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                }

                return result.ToDictionary(x => x.Key, x => x.Value.ToPythonDictionary())
                    .ToPythonDictionary();
            }

            return new PythonDictionary();
        }

        public PythonDictionary Detect(string spriteName, double minPercentage = 0.8, PythonDictionary area = null, int timeout = -1)
        {
            return Detect(new PythonTuple(new[] { spriteName }), minPercentage, area, timeout);
        }

        public PythonList DetectAll(string spriteName, float percentage = 0.8f, PythonDictionary area = null)
        {
            var frame = Frame;
            var result = new PythonList();
            var areaCv = area != null ?
                    (Rect?)new Rect((int)area["x"], (int)area["y"], (int)area["width"], (int)area["height"]) :
                    null;

            var sprite = Sprites.FirstOrDefault(x => x.Name == spriteName);
            if (sprite == null)
                return result;

            var detectionResults = sprite.Model.MatchToAll(frame, percentage, areaCv).Select(x => x.ToPythonDictionary());
            foreach (var detectionResult in detectionResults)
            {
                result.Add(detectionResult);
            }

            return result;
        }

        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        public void add_log(string message)
        {
            Logs.Add(message);
        }

        public void clear_log()
        {
            while (Logs.Count > 0)
            {
                Logs.RemoveAt(0);
            }
        }

        public IronPython.Runtime.PythonDictionary LoadCache(string filename)
        {
            var cache = new IronPython.Runtime.PythonDictionary();
            if (File.Exists(filename))
            {
                using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    var count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var id = reader.ReadString();
                        var percent = reader.ReadDouble();

                        cache.Add(id, percent);
                    }
                }
            }

            return cache;
        }
    }
}
