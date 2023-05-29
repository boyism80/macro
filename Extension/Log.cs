using System;
using System.Collections.ObjectModel;

namespace macro.Extension
{
    public static class LogExt
    {
        public static void Add(this ObservableCollection<ViewModel.Log> logs, string message)
        {
            try
            {
                logs.Add(new ViewModel.Log(message, DateTime.Now));
            }
            catch
            { }
        }
    }
}
