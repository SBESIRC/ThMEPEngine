﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;

using CommunityToolkit.Mvvm.Input;
using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using DotNetARX;
using ThControlLibraryWPF.ControlUtils;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore;
using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Heating;


namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThFloorHeatingCoilViewModel : NotifyPropertyChangedBase
    {
        #region UI Binding
        //----------------右---------------------------------
        private int _PublicRegionConstraint { get; set; }
        public int PublicRegionConstraint
        {
            get { return _PublicRegionConstraint; }
            set
            {
                _PublicRegionConstraint = value;
                this.RaisePropertyChanged();
            }
        }

        private int _IndependentRoomConstraint { get; set; }
        public int IndependentRoomConstraint
        {
            get { return _IndependentRoomConstraint; }
            set
            {
                _IndependentRoomConstraint = value;
                this.RaisePropertyChanged();
            }
        }

        private int _AuxiliaryRoomConstraint { get; set; }
        public int AuxiliaryRoomConstraint
        {
            get { return _AuxiliaryRoomConstraint; }
            set
            {
                _AuxiliaryRoomConstraint = value;
                this.RaisePropertyChanged();
            }
        }

        private int _PrivatePublicMode { get; set; }
        public int PrivatePublicMode
        {
            get { return _PrivatePublicMode; }
            set
            {
                _PrivatePublicMode = value;
                this.RaisePropertyChanged();
            }
        }

        private int _TotalLenthConstraint { get; set; }
        public int TotalLenthConstraint
        {
            get { return _TotalLenthConstraint; }
            set
            {
                _TotalLenthConstraint = value;
                this.RaisePropertyChanged();
            }
        }

        //----------------左---------------------------------

        private int _RouteNum { get; set; }
        public int RouteNum
        {
            get { return _RouteNum; }
            set
            {
                _RouteNum = value;
                this.RaisePropertyChanged();
            }
        }

        private int _SuggestDist { get; set; }
        public int SuggestDist
        {
            get { return _SuggestDist; }
            set
            {
                _SuggestDist = value;
                this.RaisePropertyChanged();
            }
        }

        private int _SuggestDistDefualt { get; set; }
        public int SuggestDistDefualt
        {
            get { return _SuggestDistDefualt; }
            set
            {
                _SuggestDistDefualt = value;
                this.RaisePropertyChanged();
            }
        }

        #endregion

        #region Data Flow
        private ObservableCollection<Polyline> _SelectFrame { get; set; }
        public ObservableCollection<Polyline> SelectFrame
        {
            get { return _SelectFrame; }
            set
            {
                _SelectFrame = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        public ThFloorHeatingCoilViewModel()
        {
            SelectFrame = new ObservableCollection<Polyline>();
            PublicRegionConstraint = 1;
            IndependentRoomConstraint = 1;
            AuxiliaryRoomConstraint = 1;
            PrivatePublicMode = 1;
            TotalLenthConstraint = 120;
            RouteNum = 5;
            SuggestDist = 250;
            SuggestDistDefualt = 200;
        }


        #region cmd
        public ICommand CheckRoomConnectivityCmd => new RelayCommand(CheckRoomConnectivity);
        private void CheckRoomConnectivity()
        {
            SaveSetting();
            FocusToCAD();
            ThFloorHeatingSubCmd.CheckRoomConnectivity(this);
        }

        public ICommand DistributeRouteCmd => new RelayCommand(DistributeRoute);
        private void DistributeRoute()
        {
            SaveSetting();
            FocusToCAD();
            ThFloorHeatingSubCmd.DistributeRoute(this);
        }
        public ICommand ShowRouteCmd => new RelayCommand(ShowRoute);
        private void ShowRoute()
        {
            SaveSetting();
            FocusToCAD();
            ThFloorHeatingSubCmd.ShowRoute(this);
        }

        public ICommand CleanSelectFrameCmd => new RelayCommand(CleanSelectFrameAndData);
        public void CleanSelectFrameAndData()
        {
            SelectFrame.Clear();
            ProcessedData.Clear();
        }

        public ICommand PickRoomOutlineCmd => new RelayCommand(PickRoomOutline);
        private void PickRoomOutline()
        {
            FocusToCAD();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THEROC");
        }
        public ICommand DrawRoomOutlineCmd => new RelayCommand(DrawRoomOutline);
        private void DrawRoomOutline()
        {
            if (AcadApp.DocumentManager.Count > 0)
            {
                FocusToCAD();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    acdb.Database.CreateAIRoomOutlineLayer();
                    acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        public ICommand DrawDoorOutlineCmd => new RelayCommand(DrawDoorOutline);
        private void DrawDoorOutline()
        {
            if (AcadApp.DocumentManager.Count > 0)
            {
                FocusToCAD();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    acdb.Database.CreateAIDoorLayer();
                    acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.DOOR);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        public ICommand DrawObstacleCmd => new RelayCommand(DrawObstacle);
        private void DrawObstacle()
        {
            if (AcadApp.DocumentManager.Count > 0)
            {
                FocusToCAD();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    acdb.Database.CreateAILayer(ThFloorHeatingCommon.Layer_Obstacle, 1);
                    acdb.Database.SetCurrentLayer(ThFloorHeatingCommon.Layer_Obstacle);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        public ICommand DrawRoomSeparatorCmd => new RelayCommand(DrawRoomSeparator);
        private void DrawRoomSeparator()
        {
            if (AcadApp.DocumentManager.Count > 0)
            {
                FocusToCAD();

                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    acdb.Database.CreateAILayer(ThFloorHeatingCommon.Layer_RoomSeparate , 2);
                    acdb.Database.SetCurrentLayer(ThFloorHeatingCommon.Layer_RoomSeparate);
                }

                CommandHandlerBase.ExecuteFromCommandLine(false, "_.PLINE");
            }
        }

        public ICommand InsertWaterSeparatorCmd => new RelayCommand(InsertWaterSeparator);
        private void InsertWaterSeparator()
        {
            FocusToCAD();
            ThFloorHeatingSubCmd.InsertWaterSeparatorBlk(this);
        }

        public ICommand InsertSuggestBlkCmd => new RelayCommand(InsertSuggestBlk);
        private void InsertSuggestBlk()
        {
            FocusToCAD();
            ThFloorHeatingSubCmd.InsertSuggestBlk(this);
        }

        #endregion

        #region function
        private void SaveSetting()
        {
            ThFloorHeatingCoilSetting.Instance.WithUI = true;
        }
        private void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        #endregion
    }


}
