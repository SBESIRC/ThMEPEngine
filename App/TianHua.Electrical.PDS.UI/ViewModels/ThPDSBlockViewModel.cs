using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSBlockViewModel : NotifyPropertyChangedBase
    {
        public void RaisePropertyChangedEvent()
        {
            OnPropertyChanged(null);
        }
        public string BlockName { get; set; }
        public ICommand UpdatePropertyGridCommand { get; set; }
        public IEnumerable ContextMenuItems { get; set; }
        public void UpdatePropertyGrid()
        {
            var cmd = UpdatePropertyGridCommand;
            if (cmd != null && cmd.CanExecute(null)) cmd.Execute(null);
        }
    }
}
