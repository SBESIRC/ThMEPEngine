using System.ComponentModel;
using System.Collections.Generic;

namespace ThMEPWSS.FlushPoint.Model
{
    public class ThFlushPointParameter : INotifyPropertyChanged
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; }
        public ThFlushPointParameter()
        {
            floorSign = "B1";
            plotScale = "1:100";
            protectRadius = 30.0;
            parkingAreaOfPT = true;
            onlyLayoutOnColumn = true;
            necessaryArrangeSpaceOfPT = true;
            nearbyDistance = 5;
            ArrangePosition = ArrangePositionOps.AreaFullLayout;
            BlockNameDict = new Dictionary<string, List<string>>();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool onlyLayoutOnColumn;
        /// <summary>
        /// 仅布置在柱子上
        /// </summary>
        public bool OnlyLayoutOnColumn
        {
            get
            {
                return onlyLayoutOnColumn;
            }
            set
            {
                onlyLayoutOnColumn = value;
                RaisePropertyChanged("OnlyLayoutOnColumn");
            }
        }

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
        private double nearbyDistance;
        /// <summary>
        /// 冲洗点位靠近排水实施的距离
        /// </summary>
        public double NearbyDistance
        {
            get
            {
                return nearbyDistance;
            }
            set
            {
                nearbyDistance = value;
                RaisePropertyChanged("NearbyDistance");
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

        private ArrangePositionOps arrangePosition; // 布置位置

        public ArrangePositionOps ArrangePosition
        {
            get
            {
                return arrangePosition;
            }
            set
            {
                arrangePosition = value;
                RaisePropertyChanged("ArrangePosition");
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
    public enum ArrangePositionOps
    {
        AreaFullLayout,
        OnlyDrainageFacility,
    }
}
