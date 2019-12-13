using KPU_General_macro.Model;
using System;
using System.Windows.Media;

namespace KPU_General_macro.ViewModel
{
    public class LogViewModel : BaseViewModel
    {
        public Log Log { get; private set; }

        public string Message
        {
            get
            {
                return this.Log.Message;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return this.Log.DateTime;
            }
        }

        public Color Color
        {
            get
            {
                return this.Log.Color;
            }
        }

        public SolidColorBrush SolidColorBrush
        {
            get
            {
                return new SolidColorBrush(this.Color);
            }
        }

        public LogViewModel(Log log)
        {
            this.Log = log;
        }

        public LogViewModel(string message, DateTime datetime) : this(new Log(message, datetime))
        { }
    }
}