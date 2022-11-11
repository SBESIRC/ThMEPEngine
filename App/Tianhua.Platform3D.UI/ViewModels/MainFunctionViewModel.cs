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
        private string uName { get; set; }
        public string UserName 
        {
            get { return uName; }
            set 
            {
                uName = value;
                this.RaisePropertyChanged();
            }
        }
        private string prjName { get; set; }
        public string ProjectName
        {
            get { return prjName; }
            set
            {
                prjName = value;
                this.RaisePropertyChanged();
            }
        }
        private string subPrjName { get; set; }
        public string SubProjectName
        {
            get { return subPrjName; }
            set
            {
                subPrjName = value;
                this.RaisePropertyChanged();
            }
        }
        private string majorName { get; set; }
        public string MajorName
        {
            get { return majorName; }
            set
            {
                majorName = value;
                this.RaisePropertyChanged();
            }
        }
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
