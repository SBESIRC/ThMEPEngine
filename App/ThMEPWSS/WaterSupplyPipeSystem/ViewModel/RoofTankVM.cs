using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.Diagram.ViewModel;

namespace ThMEPWSS.WaterSupplyPipeSystem.ViewModel
{
    public class RoofTankVM : NotifyPropertyChangedBase
    {

        public RoofTankVM(int maxFloor)
        {
            TankLength = 2.5;
            TankWidth = 2.5;
            TankHeight = 2.5;
            TankVolume = TankLength * TankWidth * (TankHeight-0.6);
            Elevation = 39.70;
            LowestFloor = 1;
            HighestFloor = maxFloor;

            SterilizerType = new ObservableCollection<DynamicRadioButton>();
            SterilizerType.Add(new DynamicRadioButton() { Content = "内置式", GroupName = "消毒器类型", IsChecked = false });
            SterilizerType.Add(new DynamicRadioButton() { Content = "外置式", GroupName = "消毒器类型", IsChecked = true });

            PressurizedFloor = new ObservableCollection<DynamicRadioButton>();
            PressurizedFloor.Add(new DynamicRadioButton() { Content = "3层", GroupName = "加压楼层", IsChecked = false });
            PressurizedFloor.Add(new DynamicRadioButton() { Content = "4层", GroupName = "加压楼层", IsChecked = true });
        }

        private double tankLength { get; set; }
        public double TankLength
        {
            get { return tankLength; }
            set
            {
                tankLength = value;
                this.RaisePropertyChanged();
            }
        }

        private double tankWidth { get; set; }
        public double TankWidth
        {
            get { return tankWidth; }
            set
            {
                tankWidth = value;
                this.RaisePropertyChanged();
            }
        }

        private double tankHeight { get; set; }
        public double TankHeight
        {
            get { return tankHeight; }
            set
            {
                tankHeight = value;
                this.RaisePropertyChanged();
            }
        }

        private double tankVolume { get; set; }
        public double TankVolume
        {
            get { return tankVolume; }
            set
            {
                //tankVolume = value;
                tankVolume = TankLength * TankWidth * (TankHeight - 0.6);
                this.RaisePropertyChanged();
            }
        }

        private double elevation { get; set; }
        public double Elevation
        {
            get { return elevation; }
            set
            {
                elevation = value;
                this.RaisePropertyChanged();
            }
        }

        private int lowestFloor { get; set; }
        public int LowestFloor
        {
            get { return lowestFloor; }
            set
            {
                lowestFloor = value;
                this.RaisePropertyChanged();
            }
        }

        private int highestFloor { get; set; }
        public int HighestFloor
        {
            get { return highestFloor; }
            set
            {
                highestFloor = value;
                this.RaisePropertyChanged();
            }
        }


        private ObservableCollection<DynamicRadioButton> sterilizerType { get; set; }
        public ObservableCollection<DynamicRadioButton> SterilizerType
        {
            get { return sterilizerType; }
            set
            {
                this.sterilizerType = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButton> pressurizedFloor { get; set; }
        
        public ObservableCollection<DynamicRadioButton> PressurizedFloor
        {
            get { return pressurizedFloor; }
            set
            {
                this.pressurizedFloor = value;
                this.RaisePropertyChanged();
            }
        }

        public RoofTankVM Clone(int maxFloor)
        {
            var clone = new RoofTankVM(maxFloor);
            
            clone.TankLength = TankLength;
            clone.TankWidth = TankWidth;
            clone.TankHeight = TankHeight;
            clone.TankVolume = TankVolume;
            clone.Elevation = Elevation;
            clone.LowestFloor = LowestFloor;
            clone.HighestFloor = maxFloor;

            return clone;
        }
    }
}
