﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using ThControlLibraryWPF.ControlUtils;

namespace ThMEPElectrical.FireAlarm.ViewModels
{
    public class FireAlarmNewViewModel : NotifyPropertyChangedBase
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

        private LayoutItemType _LayoutItem { get; set; }
        public LayoutItemType LayoutItem
        {
            get
            {
                return _LayoutItem;
            }
            set
            {
                _LayoutItem = value;
                this.RaisePropertyChanged();
            }
        }

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

        private BroadcastLayoutType _BroadcastLayout { get; set; }
        public BroadcastLayoutType BroadcastLayout
        {
            get
            {
                return _BroadcastLayout;
            }
            set
            {
                _BroadcastLayout = value;
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


        private double _ProtectRadius { get; set; }
        public double ProtectRadius
        {
            get { return _ProtectRadius; }
            set
            {
                _ProtectRadius = value;
                this.RaisePropertyChanged();
            }
        }

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
        public FireAlarmNewViewModel()
        {
            SetScale();
            Beam = BeamType.ConsiderBeam;
            LayoutItem = LayoutItemType.Smoke;

            ///smoke
            SetRoofHight();
            SetRoofGrade();
            RoofThickness = 100;
            SetFixRef();
            
            ///broadcast
            BroadcastLayout = BroadcastLayoutType.Wall;
            StepLengthBC = 25;

            ///manual alarm
            StepLengthMA = 25;

            ///gas
            ProtectRadius = 8000;

            ///Floor Display
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

    public enum LayoutItemType
    {
        Smoke = 0,
        Broadcast = 1,
        FloorDisplay = 2,
        FireTel = 3,
        Gas = 4,
        ManualAlarm = 5,
        FireProofMonitor = 6,
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
