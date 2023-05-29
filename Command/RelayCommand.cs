using System;
using System.Windows.Input;

namespace macro.Command
{
    public class RelayCommand<T> : ICommand where T : new()
    {
        #region Private members
        private Action<T> _action;
        #endregion

        #region Public events
        public event EventHandler CanExecuteChanged = (sender, e) => { };
        #endregion

        #region Constructor
        public RelayCommand(Action<T> action)
        {
            this._action = action;
        }
        #endregion

        #region Command methods

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this._action((T)parameter);
        }
        #endregion
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action<object> action) : base(action)
        { }
    }
}
