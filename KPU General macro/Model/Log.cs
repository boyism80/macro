using System;
using System.ComponentModel;
using System.Windows.Media;

namespace KPUGeneralMacro.ViewModel
{
    public class Log
    {
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
        public Color Color { get; set; }

        public Log(string message, DateTime datetime, Color color)
        {
            this.Message = message;
            this.DateTime = datetime;
            this.Color = color;
        }

        public Log(string message, DateTime datetime) : this(message, datetime, Colors.Gray)
        { }
    }
}
