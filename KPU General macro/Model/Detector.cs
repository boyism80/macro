using IronPython.Runtime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace KPU_General_macro.Model
{
    public class Detector
    {
        private SpriteContainer _spriteContainer;
        private StatusContainer _statusContainer;

        private Mutex _frameLock = new Mutex();
        private Mat _frame;
        public Mat Frame
        {
            get => _frame;
            set
            {
this._frameLock.WaitOne();
                this._frame = value?.Clone();
this._frameLock.ReleaseMutex();
            }
        }

        public Func<int> OnFailedToMatch { get; set; }

        public Detector(SpriteContainer sprite, StatusContainer status)
        {
            this._spriteContainer = sprite;
            this._statusContainer = status;
        }

        public Dictionary<string, Point> Detect(string name, bool infinite = false)
        {
            if (this._statusContainer.TryGetValue(name, out var status) == false)
                return null;

            return this.Detect(status, infinite);
        }

        public Dictionary<string, Point> Detect(Status status, bool infinite = false)
        {
            if (infinite)
            {
                while (this.Frame != null)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var result = this.Detect(status, false);
                    if (result != null)
                        return result;

                    stopwatch.Stop();
                    if (this.OnFailedToMatch != null)
                        Thread.Sleep(Math.Max(0, this.OnFailedToMatch() - (int)stopwatch.ElapsedMilliseconds));
                }

                return null;
            }
            else
            {
                if (this.Frame == null)
                    return null;

                Dictionary<string, Point> points = new Dictionary<string, Point>();
this._frameLock.WaitOne();
                foreach (var component in status.Components)
                {
                    var percentage = 0.0;
                    var point = component.Sprite.MatchTo(this.Frame, ref percentage, null, null);
                    if (point == null)
                    {
                        if (component.Requirement == false)
                            continue;

this._frameLock.ReleaseMutex();
                        return null;
                    }

                    points.Add(component.Sprite.Name, point.Value);
                }
this._frameLock.ReleaseMutex();

                return points;
            }
        }

        public string Detect(out Dictionary<string, Point> points)
        {
            foreach (var status in this._statusContainer)
            {
                var detectedPoints = this.Detect(status.Value);
                if (detectedPoints != null)
                {
                    points = detectedPoints;
                    return status.Key;
                }
            }

            points = null;
            return null;
        }

        public PythonTuple Detect(Sprite sprite)
        {
            var percentage = 0.0;
this._frameLock.WaitOne();
            var point = sprite.MatchTo(this.Frame, ref percentage);
this._frameLock.ReleaseMutex();
            if (point == null)
                return null;

            return new PythonTuple(new object[] { point?.X, point?.Y });
        }
    }
}
