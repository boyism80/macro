namespace KPUGeneralMacro
{
    public partial class MainWindowViewModel
    {
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
    }
}
