using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace Tianhua.Platform3D.UI.ViewModels
{
    class MainFunctionViewModel : NotifyPropertyChangedBase
    {
        ObservableCollection<FunctionTabItem> _mainTabs { get; set; }
        public ObservableCollection<FunctionTabItem> FunctionTableItems
        {
            get { return _mainTabs; }
            set 
            {
                _mainTabs = value;
                this.RaisePropertyChanged();
            }
        }
        public MainFunctionViewModel() 
        {
            FunctionTableItems = new ObservableCollection<FunctionTabItem>();
        }
    }
}
