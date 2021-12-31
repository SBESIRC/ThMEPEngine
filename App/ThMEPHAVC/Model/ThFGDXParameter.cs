using System.Linq;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.Service;

namespace ThMEPHVAC.Model
{
    public class ThFGDXParameter : NotifyPropertyChangedBase
    {
        public ThFGDXParameter()
        {
            SystemTypes = new ObservableCollection<string>(ThMEPHAVCDataManager.GetSystemTypes());
            systemType = SystemTypes.FirstOrDefault();
        }
        public ObservableCollection<string> SystemTypes { get; set; }

        private string systemType = "";
        public string SystemType
        {
            get
            {
                return systemType;
            }
            set
            {
                systemType = value;
                RaisePropertyChanged("SystemType");
            }
        }
    }
}
