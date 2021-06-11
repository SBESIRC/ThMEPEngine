using System;
using System.Windows.Input;

namespace TianHua.Plumbing.UI.Command
{
    public class RelayCommand : ICommand
    {
        #region Fields
        private readonly Func<Object, Boolean> _canExecute;
        private readonly Action<Object> _execute;
        #endregion

        #region Constructors
        public RelayCommand(Action<Object> execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<Object> execute, Func<Object, Boolean> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }
        #endregion

        #region ICommand Members
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public Boolean CanExecute(Object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public void Execute(Object parameter)
        {
            _execute(parameter);
        }
        #endregion
    }
}
