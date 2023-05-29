using System;
using System.Windows.Media;

namespace macro.Model
{
    public class Log
    {
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
        public Color Color { get; set; }

        public Log(string message, DateTime datetime, Color color)
        {
            Message = message;
            DateTime = datetime;
            Color = color;
        }

        public Log(string message, DateTime datetime) : this(message, datetime, Colors.Gray)
        { }
    }
}
