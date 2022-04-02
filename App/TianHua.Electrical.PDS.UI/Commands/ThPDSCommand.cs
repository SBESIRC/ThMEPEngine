using System;
using System.Windows.Input;

namespace TianHua.Electrical.PDS.UI.Commands
{
    public class ThPDSCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        readonly Action cb;
        public ThPDSCommand(Action cb)
        {
            this.cb = cb;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }
        public void Execute(object parameter)
        {
            cb();
        }
    }
}
