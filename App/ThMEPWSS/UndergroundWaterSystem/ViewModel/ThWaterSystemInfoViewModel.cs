using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.UndergroundWaterSystem.ViewModel
{
    public class ThWaterSystemInfoViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public object Clone()
        {
            throw new NotImplementedException();
        }
        private List<string> _FloorListDatas = new List<string>();
        public List<string> FloorListDatas
        {
            get
            {
                return _FloorListDatas;
            }
            set
            {
                _FloorListDatas = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
