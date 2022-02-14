using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using ThControlLibraryWPF.ControlUtils;

namespace ThMEPElectrical.AFAS.ViewModel
{
    public class FireAlarmViewModel : NotifyPropertyChangedBase
    {
        private UListItemData _scale { get; set; }
        public UListItemData ScaleItem
        {
            get { return _scale; }
            set
            {
                _scale = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _scaleListItem = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ScaleListItems
        {
            get { return _scaleListItem; }
            set
            {
                _scaleListItem = value;
                this.RaisePropertyChanged();
            }
        }
        //-------布置场景
        private int _SelectFloorRoom { get; set; }
        public int SelectFloorRoom
        {
            get
            {
                return _SelectFloorRoom;
            }
            set
            {
                _SelectFloorRoom = value;
                this.RaisePropertyChanged();
            }
        }

        private int _FloorUpDown { get; set; }
        public int FloorUpDown
        {
            get
            {
                return _FloorUpDown;
            }
            set
            {
                _FloorUpDown = value;
                this.RaisePropertyChanged();
            }
        }


        //-------梁
        private BeamType _Beam { get; set; }
        public BeamType Beam
        {
            get
            {
                return _Beam;
            }
            set
            {
                _Beam = value;
                this.RaisePropertyChanged();
            }
        }

        private double _RoofThickness { get; set; }
        public double RoofThickness
        {
            get { return _RoofThickness; }
            set
            {
                _RoofThickness = value;
                this.RaisePropertyChanged();
            }
        }

        private double _BufferDist { get; set; }
        public double BufferDist
        {
            get { return _BufferDist; }
            set
            {
                _BufferDist = value;
                this.RaisePropertyChanged();
            }
        }

        //-------布置类型
        private bool _LayoutSmoke { get; set; }
        public bool LayoutSmoke
        {
            get { return _LayoutSmoke; }
            set
            {
                _LayoutSmoke = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _LayoutGas { get; set; }
        public bool LayoutGas
        {
            get { return _LayoutGas; }
            set
            {
                _LayoutGas = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _LayoutBroadcast { get; set; }
        public bool LayoutBroadcast
        {
            get { return _LayoutBroadcast; }
            set
            {
                _LayoutBroadcast = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _LayoutManualAlart { get; set; }
        public bool LayoutManualAlart
        {
            get { return _LayoutManualAlart; }
            set
            {
                _LayoutManualAlart = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _LayoutDisplay { get; set; }
        public bool LayoutDisplay
        {
            get { return _LayoutDisplay; }
            set
            {
                _LayoutDisplay = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _LayoutMonitor { get; set; }
        public bool LayoutMonitor
        {
            get { return _LayoutMonitor; }
            set
            {
                _LayoutMonitor = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _LayoutTel { get; set; }
        public bool LayoutTel
        {
            get { return _LayoutTel; }
            set
            {
                _LayoutTel = value;
                this.RaisePropertyChanged();
            }
        }

        //-------烟温感
        private UListItemData _RoofHight { get; set; }
        public UListItemData RoofHight
        {
            get { return _RoofHight; }
            set
            {
                _RoofHight = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _RoofHightList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> RoofHightList
        {
            get { return _RoofHightList; }
            set
            {
                _RoofHightList = value;
                this.RaisePropertyChanged();
            }
        }

        private UListItemData _RoofGrade { get; set; }
        public UListItemData RoofGrade
        {
            get { return _RoofGrade; }
            set
            {
                _RoofGrade = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _RoofGradeList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> RoofGradeList
        {
            get { return _RoofGradeList; }
            set
            {
                _RoofGradeList = value;
                this.RaisePropertyChanged();
            }
        }

        private UListItemData _FixRef { get; set; }
        public UListItemData FixRef
        {
            get { return _FixRef; }
            set
            {
                _FixRef = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _FixRefList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> FixRefList
        {
            get { return _FixRefList; }
            set
            {
                _FixRefList = value;
                this.RaisePropertyChanged();
            }
        }

        //-------广播
        private BroadcastLayoutType _BroadcastLayoutType { get; set; }
        public BroadcastLayoutType BroadcastLayoutType
        {
            get
            {
                return _BroadcastLayoutType;
            }
            set
            {
                _BroadcastLayoutType = value;
                this.RaisePropertyChanged();
            }
        }
        private double _StepLengthBC { get; set; }
        public double StepLengthBC
        {
            get { return _StepLengthBC; }
            set
            {
                _StepLengthBC = value;
                this.RaisePropertyChanged();
            }
        }

        //-------手报
        private double _StepLengthAM { get; set; }
        public double StepLengthMA
        {
            get { return _StepLengthAM; }
            set
            {
                _StepLengthAM = value;
                this.RaisePropertyChanged();
            }
        }

        //-------可燃气
        private double _GasProtectRadius { get; set; }
        public double GasProtectRadius
        {
            get { return _GasProtectRadius; }
            set
            {
                _GasProtectRadius = value;
                this.RaisePropertyChanged();
            }
        }

        //-------楼层显示器
        private DisplayBuildingType _DisplayBuildingType { get; set; }
        public DisplayBuildingType DisplayBuilding
        {
            get
            {
                return _DisplayBuildingType;
            }
            set
            {
                _DisplayBuildingType = value;
                this.RaisePropertyChanged();
            }
        }

        private DisplayBlkType _DisplayBlkType { get; set; }
        public DisplayBlkType DisplayBlk
        {
            get
            {
                return _DisplayBlkType;
            }
            set
            {
                _DisplayBlkType = value;
                this.RaisePropertyChanged();
            }
        }
        //-------

        public FireAlarmViewModel()
        {

            SetScale();
            Beam = BeamType.ConsiderBeam;
            SelectFloorRoom = 0;
            FloorUpDown = 1;

            //---layout item
            LayoutSmoke = true;
            LayoutBroadcast = false;
            LayoutDisplay = true;
            LayoutTel = true;
            LayoutGas = true;
            LayoutManualAlart = false;
            LayoutMonitor = true;

            //---beam
            RoofThickness = 100;
            BufferDist = 500;

            //---smoke
            SetRoofHight();
            SetRoofGrade();
            SetFixRef();

            //---broadcast
            BroadcastLayoutType = BroadcastLayoutType.Wall;
            StepLengthBC = 25;

            //---manual alarm
            StepLengthMA = 25;

            //---gas
            GasProtectRadius = 8000;

            //---Floor Display
            DisplayBuilding = DisplayBuildingType.Resident;
            DisplayBlk = DisplayBlkType.Floor;
        }

        private void SetScale()
        {
            ScaleListItems.Add(new UListItemData("100", 0, (double)100));
            ScaleListItems.Add(new UListItemData("150", 1, (double)150));
            ScaleItem = ScaleListItems.FirstOrDefault();
        }
        private void SetRoofHight()
        {
            RoofHightList.Add(new UListItemData("h<=12", 0, (int)0));
            RoofHightList.Add(new UListItemData("6<=h<=12", 1, (int)1));
            RoofHightList.Add(new UListItemData("h<=6", 2, (int)2));
            RoofHightList.Add(new UListItemData("h<=8", 3, (int)3));
            RoofHight = RoofHightList[2];
        }
        private void SetRoofGrade()
        {
            RoofGradeList.Add(new UListItemData("θ<=15°", 0, (int)0));
            RoofGradeList.Add(new UListItemData("15°<=θ<=30°", 1, (int)1));
            RoofGradeList.Add(new UListItemData("θ>30°", 2, (int)2));
            RoofGrade = RoofGradeList[0];
        }
        private void SetFixRef()
        {
            FixRefList.Add(new UListItemData("0.7", 0, (double)0.7));
            FixRefList.Add(new UListItemData("0.8", 1, (double)0.8));
            FixRefList.Add(new UListItemData("0.9", 2, (double)0.9));
            FixRefList.Add(new UListItemData("1.0", 3, (double)1.0));
            FixRef = FixRefList[3];
        }

    }

    public enum BeamType
    {
        NotConsiderBeam = 0,
        ConsiderBeam = 1,
    }

    public enum BroadcastLayoutType
    {
        Ceiling = 0,
        Wall = 1,
    }

    public enum DisplayBuildingType
    {
        Resident = 0,
        Public = 1,
    }

    public enum DisplayBlkType
    {
        Floor = 0,
        District = 1,
    }
}
