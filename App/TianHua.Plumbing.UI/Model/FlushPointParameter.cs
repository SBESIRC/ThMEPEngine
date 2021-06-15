using System.ComponentModel;

namespace TianHua.Plumbing.UI.Model
{
    public class FlushPointParameter : INotifyPropertyChanged
    {
        public FlushPointParameter()
        {
            areaFullLayoutOfAP = true;
            onlyDrainageFaclityNearbyOfAP = false;
            parkingAreaOfPT = true;
            necessaryArrangeSpaceOfPT = true;
            protectRadius = 30.0;
            plotScale = "1:100";
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private string plotScale = "";
        public string PlotScale
        {
            get
            {
                return plotScale;
            }
            set
            {
                plotScale = value;
                RaisePropertyChanged("PlotScale");
            }
        }

        private string floorSign = "";
        public string FloorSign
        {
            get
            {
                return floorSign;
            }
            set
            {
                floorSign = value;
                RaisePropertyChanged("FloorSign");
            }
        }

        private double protectRadius; //保护半径(0-99)
        public double ProtectRadius 
        {
            get
            { 
                return protectRadius; 
            }
            set
            { 
                protectRadius = value;
                RaisePropertyChanged("ProtectRadius"); 
            }
        }
        private bool parkingAreaOfPT;
        public bool ParkingAreaOfProtectTarget
        {
            get
            {
                return parkingAreaOfPT;
            }
            set
            {
                parkingAreaOfPT = value;
                RaisePropertyChanged("ParkingAreaOfProtectTarget");
            }
        }

        private bool necessaryArrangeSpaceOfPT;
        public bool NecessaryArrangeSpaceOfProtectTarget
        {
            get
            {
                return necessaryArrangeSpaceOfPT;
            }
            set
            {
                necessaryArrangeSpaceOfPT = value;
                RaisePropertyChanged("NecessaryArrangeSpaceOfProtectTarget");
            }
        }

        private bool otherSpaceOfPT;
        public bool OtherSpaceOfProtectTarget
        {
            get
            {
                return otherSpaceOfPT;
            }
            set
            {
                otherSpaceOfPT = value;
                RaisePropertyChanged("OtherSpaceOfProtectTarget");
            }
        }

        private bool necesaryArrangeSpacePointsOfAS;
        public bool NecesaryArrangeSpacePointsOfArrangeStrategy
        {
            get
            {
                return necesaryArrangeSpacePointsOfAS;
            }
            set
            {
                necesaryArrangeSpacePointsOfAS = value;
                RaisePropertyChanged("NecesaryArrangeSpacePointsOfArrangeStrategy");
            }
        }

        private bool parkingAreaPointsOfAS;
        public bool ParkingAreaPointsOfArrangeStrategy
        {
            get
            {
                return parkingAreaPointsOfAS;
            }
            set
            {
                parkingAreaPointsOfAS = value;
                RaisePropertyChanged("ParkingAreaPointsOfArrangeStrategy");
            }
        }

        private bool areaFullLayoutOfAP; // 布置策略->区域满布

        public bool AreaFullLayoutOfArrangePosition
        {
            get
            {
                return areaFullLayoutOfAP;
            }
            set
            {
                areaFullLayoutOfAP = value;
                RaisePropertyChanged("AreaFullLayoutOfArrangePosition");
            }
        }

        private bool onlyDrainageFaclityNearbyOfAP; // 布置策略->仅排水设施附近

        public bool OnlyDrainageFaclityNearbyOfArrangePosition
        {
            get
            {
                return onlyDrainageFaclityNearbyOfAP;
            }
            set
            {
                onlyDrainageFaclityNearbyOfAP = value;
                RaisePropertyChanged("OnlyDrainageFaclityNearbyOfArrangePosition");
            }
        }

        private bool closeDrainageFacility; // 点位标识->靠近排水设施

        public bool CloseDrainageFacility
        {
            get
            {
                return closeDrainageFacility;
            }
            set
            {
                closeDrainageFacility = value;
                RaisePropertyChanged("CloseDrainageFacility");
            }
        }

        private bool farwayDrainageFacility; // 点位标识->远离排水设施

        public bool FarwayDrainageFacility
        {
            get
            {
                return farwayDrainageFacility;
            }
            set
            {
                farwayDrainageFacility = value;
                RaisePropertyChanged("FarwayDrainageFacility");
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler!=null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
