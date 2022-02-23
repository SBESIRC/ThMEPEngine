using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Models;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    class SandBoxTableItemViewModel : NotifyPropertyChangedBase
    {
        public SandBoxTableItemViewModel()
        {
            this.FunctionTableItems = new ObservableCollection<UTableItem>();
        }
        private ObservableCollection<UTableItem> _functionTableItems;
        public ObservableCollection<UTableItem> FunctionTableItems
        {
            get { return _functionTableItems; }
            set
            {
                _functionTableItems = value;
                this.RaisePropertyChanged();
            }
        }
        private UTableItem _selectTableItem { get; set; }
        public UTableItem SelectTableItem
        {
            get { return _selectTableItem; }
            set
            {
                _selectTableItem = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
