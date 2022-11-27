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
