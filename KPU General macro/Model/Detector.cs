using OpenCvSharp;
using System.Collections.Generic;

namespace KPU_General_macro.Model
{
    public class Detector
    {
        private SpriteContainer _spriteContainer;
        private StatusContainer _statusContainer;

        public Detector(SpriteContainer sprite, StatusContainer status)
        {
            this._spriteContainer = sprite;
            this._statusContainer = status;
        }

        public Dictionary<string, Point> Detect(Mat frame, Status status)
        {
            Dictionary<string, Point> points = new Dictionary<string, Point>();
            foreach (var component in status.Components)
            {
                var percentage = 0.0;
                var point = component.sprite.MatchTo(frame, ref percentage, null, null);
                //System.Console.WriteLine("이름 : {0}, 일치율 : {1}", component.sprite.Name, percentage);
                if (point == null)
                {
                    if (component.requirement == false)
                        continue;

                    return null;
                }

                points.Add(component.sprite.Name, point.Value);
            }

            return points;
        }

        public string Detect(Mat frame, out Dictionary<string, Point> points)
        {
            foreach (var status in this._statusContainer)
            {
                var detectedPoints = this.Detect(frame, status.Value);
                if (detectedPoints != null)
                {
                    points = detectedPoints;
                    return status.Key;
                }
            }

            points = null;
            return null;
        }
    }
}
