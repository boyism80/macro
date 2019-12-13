using OpenCvSharp;
using System.Collections.Generic;

namespace KPU_General_macro.Model
{
    public class Detector
    {
        private Sprite _sprite;
        private Status _status;

        public Detector(Sprite sprite, Status status)
        {
            this._sprite = sprite;
            this._status = status;
        }

        public Dictionary<string, Point> Detect(Mat frame, Status.Element status)
        {
            Dictionary<string, Point> points = new Dictionary<string, Point>();
            foreach (var component in status.Components)
            {
                var percentage = 0.0;
                var point = component.template.MatchTo(frame, ref percentage, null, null);
                //System.Console.WriteLine("이름 : {0}, 일치율 : {1}", component.template.Name, percentage);
                if (point == null)
                {
                    if (component.requirement == false)
                        continue;

                    return null;
                }

                points.Add(component.template.Name, point.Value);
            }

            return points;
        }

        public string Detect(Mat frame, out Dictionary<string, Point> points)
        {
            foreach (var status in this._status)
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
