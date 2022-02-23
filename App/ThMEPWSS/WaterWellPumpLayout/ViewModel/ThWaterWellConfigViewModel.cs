using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.WaterWellPumpLayout.Model;

namespace ThMEPWSS.WaterWellPumpLayout.ViewModel
{
    public class ThWaterWellConfigViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public object Clone()
        {
            throw new NotImplementedException();
        }
        private ObservableCollection<ThWaterWellConfigInfo> TmpConfigInfo = new ObservableCollection<ThWaterWellConfigInfo>();
        public ObservableCollection<ThWaterWellConfigInfo> WellConfigInfo
        {
            get
            {
                return TmpConfigInfo;
            }
            set
            {
                TmpConfigInfo = value;
                this.RaisePropertyChanged();
            }
        }

    }
}
