using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.ViewModel
{
    public class FireHydrantSystemSetViewModel : NotifyPropertyChangedBase
    {
        public FireHydrantSystemSetViewModel()
        {
            FloorLineSpace = 5000;
        }
        private double floorLineSpace { get; set; }
        /// <summary>
        /// 楼层线间距
        /// </summary>
        public double FloorLineSpace
        {
            get { return floorLineSpace; }
            set
            {
                floorLineSpace = value;
                this.RaisePropertyChanged();
            }
        }

        public FireHydrantSystemSetViewModel Clone()
        {
            var cloned = new FireHydrantSystemSetViewModel();
            cloned.FloorLineSpace = this.floorLineSpace;

            return cloned;
        }
    }
}
