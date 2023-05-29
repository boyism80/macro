using System;
using System.ComponentModel;
using System.Windows.Media;

namespace macro.ViewModel
{
    public class Log : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Model.Log Model { get; private set; }

        public string Message
        {
            get
            {
                return Model.Message;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return Model.DateTime;
            }
        }

        public Color Color
        {
            get
            {
                return Model.Color;
            }
        }

        public SolidColorBrush SolidColorBrush
        {
            get
            {
                return new SolidColorBrush(Color);
            }
        }

        public Log(Model.Log model)
        {
            Model = model;
        }

        public Log(string message, DateTime datetime) : this(new Model.Log(message, datetime))
        { }
    }
}
