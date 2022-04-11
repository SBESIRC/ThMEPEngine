using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.UndergroundWaterSystem.ViewModel
{
    public class ThWaterSystemParamViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public object Clone()
        {
            throw new NotImplementedException();
        }
        private double nFloorLineSpace = 5000.0;
        public string strFloorLineSpace 
        {
            get { return nFloorLineSpace.ToString(); }
            set
            {
                nFloorLineSpace = double.Parse(value);
                this.RaisePropertyChanged();
            }
        }
    }
}
