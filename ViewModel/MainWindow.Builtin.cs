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
        #region Constants

        private const int ASYNC_KEY_STATE_PRESSED = 0x8000;
        private const int DEFAULT_THREAD_SLEEP_MS = 100;
        private const uint DEFAULT_RECTANGLE_COLOR = 0xffff0000;
        private const int RED_SHIFT = 4;
        private const int GREEN_SHIFT = 2;

        #endregion

        #region Win32 API

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        #endregion

        #region Public Properties

        /// <summary>
        /// Python heap for storing temporary data across script executions
        /// </summary>
        public PythonDictionary heap { get; private set; } = new PythonDictionary();

        #endregion

        #region Input Event Handlers

        /// <summary>
        /// Handles XButton2 (mouse side button) press events
        /// Ctrl+XButton2: Run macro, XButton2: Execute g.py script
        /// </summary>
        public void OnXButton2Down()
        {
            if (IsControlKeyPressed())
            {
                Run();
            }
            else
            {
                ExecPython("g.py");
            }
        }

        /// <summary>
        /// Handles mouse wheel click events
        /// Executes wheel.py script
        /// </summary>
        public void OnWheelClick()
        {
            ExecPython("wheel.py");
        }

        /// <summary>
        /// Handles XButton1 (mouse side button) press events
        /// Ctrl+XButton1: Stop macro, XButton1: Execute dialogue.py script
        /// </summary>
        public void OnXButton1Down()
        {
            if (IsControlKeyPressed())
            {
                Stop();
            }
            else
            {
                ExecPython("dialogue.py");
            }
        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Draws rectangles on the specified frame
        /// </summary>
        /// <param name="frame">Target frame to draw on</param>
        /// <param name="areas">List of rectangles to draw</param>
        /// <param name="color">Color in ARGB format (default: red)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool DrawRectangles(Mat frame, List<Rect> areas, uint color = DEFAULT_RECTANGLE_COLOR)
        {
            if (frame == null)
                return false;

            var (r, g, b) = ExtractRgbFromColor(color);

            foreach (var area in areas)
            {
                frame.Rectangle(area, new Scalar(b, g, r));
            }
            return true;
        }

        /// <summary>
        /// Thread-safe version of DrawRectangles using current frame
        /// Creates a clone of the current frame to avoid thread safety issues
        /// </summary>
        /// <param name="areas">List of rectangles to draw</param>
        /// <param name="color">Color in ARGB format (default: red)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool DrawRectangles(List<Rect> areas, uint color = DEFAULT_RECTANGLE_COLOR)
        {
            using var frameClone = GetFrameClone();
            if (frameClone == null)
                return false;

            return DrawRectangles(frameClone, areas, color);
        }

        /// <summary>
        /// Draws rectangles from Python list format
        /// </summary>
        /// <param name="frame">Target frame to draw on</param>
        /// <param name="areas">Python list containing rectangle tuples (x, y, width, height)</param>
        /// <param name="color">Color in ARGB format (default: red)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool DrawRectangles(Mat frame, PythonList areas, uint color = DEFAULT_RECTANGLE_COLOR)
        {
            try
            {
                var csAreaList = ConvertPythonListToRectList(areas);
                return DrawRectangles(frame, csAreaList, color);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Thread-safe version of DrawRectangles for Python usage
        /// Creates a clone of the current frame to avoid thread safety issues
        /// </summary>
        /// <param name="areas">Python list containing rectangle tuples (x, y, width, height)</param>
        /// <param name="color">Color in ARGB format (default: red)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool DrawRectangles(PythonList areas, uint color = DEFAULT_RECTANGLE_COLOR)
        {
            try
            {
                var csAreaList = ConvertPythonListToRectList(areas);
                return DrawRectangles(csAreaList, color);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Detection Methods

        /// <summary>
        /// Thread-safe method to get a cloned frame
        /// This prevents the frame from being returned to MatPool while being processed
        /// </summary>
        /// <returns>Cloned frame or null if no frame available</returns>
        private PooledMat GetFrameClone()
        {
            _frameLock.EnterReadLock();
            try
            {
                if (_frame == null || _frame.IsDisposed)
                    return null;

                return MatPool.GetClone(_frame);
            }
            finally
            {
                _frameLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Internal detection method for multiple sprites
        /// </summary>
        /// <param name="spriteNames">List of sprite names to detect</param>
        /// <param name="area">Optional area to search within</param>
        /// <returns>Dictionary of detection results</returns>
        private Dictionary<string, Model.Sprite.DetectionResult> DetectInternal(List<string> spriteNames, Rect? area = null)
        {
            using var frameClone = GetFrameClone();
            if (frameClone == null)
                return new Dictionary<string, Model.Sprite.DetectionResult>();

            return spriteNames.Select(x => Sprites.FirstOrDefault(x2 => x2.Name == x))
            .Where(x => x != null)
            .ToDictionary(x => x.Name, x => x.Model.MatchTo(frameClone, area));
        }

        /// <summary>
        /// Detects multiple sprites with timeout and minimum percentage
        /// </summary>
        /// <param name="spriteNames">Tuple of sprite names to detect</param>
        /// <param name="minPercentage">Minimum detection confidence (0.0-1.0)</param>
        /// <param name="area">Optional search area dictionary with x, y, width, height keys</param>
        /// <param name="timeout">Timeout in milliseconds (-1 for no timeout)</param>
        /// <returns>Python dictionary of detection results</returns>
        public PythonDictionary Detect(PythonTuple spriteNames, double minPercentage = 0.8, PythonDictionary area = null, int timeout = -1)
        {
            var begin = DateTime.Now;

            while (Running)
            {
                var searchArea = ConvertPythonAreaToRect(area);
                var result = DetectInternal(spriteNames.Select(x => x as string).ToList(), area: searchArea);
                result = result.Where(x => x.Value.Percentage >= minPercentage).ToDictionary(x => x.Key, x => x.Value);

                if (result.Count > 0)
                {
                    return result.ToDictionary(x => x.Key, x => x.Value.ToPythonDictionary())
                        .ToPythonDictionary();
                }

                if (IsTimeoutReached(begin, timeout))
                    break;

                Thread.Sleep(DEFAULT_THREAD_SLEEP_MS);
            }

            return new PythonDictionary();
        }

        /// <summary>
        /// Detects a single sprite with timeout and minimum percentage
        /// </summary>
        /// <param name="spriteName">Name of the sprite to detect</param>
        /// <param name="minPercentage">Minimum detection confidence (0.0-1.0)</param>
        /// <param name="area">Optional search area dictionary with x, y, width, height keys</param>
        /// <param name="timeout">Timeout in milliseconds (-1 for no timeout)</param>
        /// <returns>Python dictionary of detection result</returns>
        public PythonDictionary Detect(string spriteName, double minPercentage = 0.8, PythonDictionary area = null, int timeout = -1)
        {
            return Detect(new PythonTuple(new[] { spriteName }), minPercentage, area, timeout);
        }

        /// <summary>
        /// Detects all instances of a sprite in the current frame
        /// </summary>
        /// <param name="spriteName">Name of the sprite to detect</param>
        /// <param name="percentage">Minimum detection confidence (0.0-1.0)</param>
        /// <param name="area">Optional search area dictionary with x, y, width, height keys</param>
        /// <returns>Python list of all detection results</returns>
        public PythonList DetectAll(string spriteName, float percentage = 0.8f, PythonDictionary area = null)
        {
            var result = new PythonList();
            var searchArea = ConvertPythonAreaToRect(area);

            var sprite = Sprites.FirstOrDefault(x => x.Name == spriteName);
            if (sprite == null)
                return result;

            using var frameClone = GetFrameClone();
            if (frameClone == null)
                return result;

            var detectionResults = sprite.Model.MatchToAll(frameClone, percentage, searchArea).Select(x => x.ToPythonDictionary());
            foreach (var detectionResult in detectionResults)
            {
                result.Add(detectionResult);
            }

            return result;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Thread sleep wrapper for Python scripts
        /// </summary>
        /// <param name="milliseconds">Sleep duration in milliseconds</param>
        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        /// <summary>
        /// Adds a log message to the log collection
        /// </summary>
        /// <param name="message">Message to log</param>
        public void add_log(string message)
        {
            Logs.Add(message);
        }

        /// <summary>
        /// Clears all log messages
        /// </summary>
        public void clear_log()
        {
            while (Logs.Count > 0)
            {
                Logs.RemoveAt(0);
            }
        }

        /// <summary>
        /// Loads cached data from file
        /// </summary>
        /// <param name="filename">Path to cache file</param>
        /// <returns>Python dictionary containing cached data</returns>
        public PythonDictionary LoadCache(string filename)
        {
            var cache = new PythonDictionary();

            if (!File.Exists(filename))
                return cache;

            try
            {
                using var reader = new BinaryReader(File.Open(filename, FileMode.Open));
                var count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    var id = reader.ReadString();
                    var percent = reader.ReadDouble();
                    cache.Add(id, percent);
                }
            }
            catch (Exception)
            {
                // Return empty cache on error
            }

            return cache;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Checks if the Control key is currently pressed
        /// </summary>
        /// <returns>True if Control key is pressed</returns>
        private bool IsControlKeyPressed()
        {
            return (GetAsyncKeyState(System.Windows.Forms.Keys.LControlKey) & ASYNC_KEY_STATE_PRESSED) == ASYNC_KEY_STATE_PRESSED;
        }

        /// <summary>
        /// Extracts RGB values from ARGB color value
        /// </summary>
        /// <param name="color">ARGB color value</param>
        /// <returns>Tuple of (R, G, B) values</returns>
        private (byte r, byte g, byte b) ExtractRgbFromColor(uint color)
        {
            var r = (byte)((color & 0x00ff0000) >> (8 + RED_SHIFT));
            var g = (byte)((color & 0x0000ff00) >> (8 + GREEN_SHIFT));
            var b = (byte)(color & 0x000000ff);
            return (r, g, b);
        }

        /// <summary>
        /// Converts Python list of area tuples to C# Rectangle list
        /// </summary>
        /// <param name="areas">Python list containing (x, y, width, height) tuples</param>
        /// <returns>List of Rect objects</returns>
        private List<Rect> ConvertPythonListToRectList(PythonList areas)
        {
            var csAreaList = new List<Rect>();

            foreach (var area in areas)
            {
                if (area is PythonTuple pythonArea && pythonArea.Count >= 4)
                {
                    csAreaList.Add(new Rect(
                        (int)pythonArea[0],
                        (int)pythonArea[1],
                        (int)pythonArea[2],
                        (int)pythonArea[3]));
                }
            }

            return csAreaList;
        }

        /// <summary>
        /// Converts Python area dictionary to OpenCV Rect
        /// </summary>
        /// <param name="area">Python dictionary with x, y, width, height keys</param>
        /// <returns>Rect object or null if area is null</returns>
        private Rect? ConvertPythonAreaToRect(PythonDictionary area)
        {
            if (area == null)
                return null;

            return new Rect(
                (int)area["x"],
                (int)area["y"],
                (int)area["width"],
                (int)area["height"]);
        }

        /// <summary>
        /// Checks if the specified timeout has been reached
        /// </summary>
        /// <param name="startTime">Start time to measure from</param>
        /// <param name="timeoutMs">Timeout in milliseconds (-1 for no timeout)</param>
        /// <returns>True if timeout reached</returns>
        private bool IsTimeoutReached(DateTime startTime, int timeoutMs)
        {
            if (timeoutMs < 0)
                return false;

            var elapsed = DateTime.Now - startTime;
            return elapsed.TotalMilliseconds >= timeoutMs;
        }

        #endregion
    }
}
