using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.UndergroundSpraySystem.ViewModel
{
    public class SprayVMSet : NotifyPropertyChangedBase
    {
        public SprayVMSet()
        {
            FloorLineSpace = 15000;
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

        public SprayVMSet Clone()
        {
            var cloned = new SprayVMSet();
            cloned.FloorLineSpace = this.floorLineSpace;

            return cloned;
        }
    }
}
