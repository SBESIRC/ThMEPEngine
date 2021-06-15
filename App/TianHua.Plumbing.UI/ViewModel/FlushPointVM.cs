using System;
using AcHelper;
using AcHelper.Commands;
using System.Windows.Input;
using TianHua.Plumbing.UI.Command;
using TianHua.Plumbing.UI.Model;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using ThMEPWSS.FlushPoint.Service;

namespace TianHua.Plumbing.UI.ViewModel
{
    public class FlushPointVM
    {
        public FlushPointParameter Parameter { get; set; }
        public ObservableCollection<string> PlotScales { get; set; }
        public FlushPointVM()
        {
            Parameter = new FlushPointParameter();
            var plotScales = new List<string> {"1:50","1:100","1:150"};
            PlotScales = new ObservableCollection<string>(plotScales);
        }
        
        public ICommand LayOutFlushPointCmd
        {
            get
            {
                return new RelayCommand(LayOutFlushPoint, CanExecute);
            }
        }

        public ICommand FarawayDrainageFacilityCheckBoxChecked
        {
            get
            {
                return new RelayCommand(FarawayDrainageFacilityClicked, CanExecute);
            }
        }

        public ICommand NearbyDrainageFacilityCheckBoxChecked
        {
            get
            {
                return new RelayCommand(NearbyDrainageFacilityClicked, CanExecute);
            }
        }

        private void FarawayDrainageFacilityClicked(Object parameter)
        {
            if (parameter is bool isChecked)
            {
                if(isChecked)
                {
                    ThPointIdentificationService.HighLightFarawayWashPoints();
                }
                else
                {
                    ThPointIdentificationService.UnHighLightFarawayWashPoints();
                }
            }
        }

        private void NearbyDrainageFacilityClicked(Object parameter)
        {
            if (parameter is bool isChecked)
            {
                if (isChecked)
                {
                    ThPointIdentificationService.HighLightNearbyWashPoints();
                }
                else
                {
                    ThPointIdentificationService.UnHighLightNearbyWashPoints();
                }
            }
        }

        private void LayOutFlushPoint(Object parameter)
        {
            if(CheckParameter())
            {
                CollectParameter();
                SetFocusToDwgView();
                CommandHandlerBase.ExecuteFromCommandLine(false, "THDXCXBZ");
            }            
        }

        private bool CheckParameter()
        {
            if(Parameter.OtherSpaceOfProtectTarget)
            {
                if(Parameter.NecesaryArrangeSpacePointsOfArrangeStrategy==false &&
                    Parameter.ParkingAreaPointsOfArrangeStrategy==false)
                {
                    System.Windows.MessageBox.Show("请至少选择一种对其他空间的保护策略！","天华提示",System.Windows.MessageBoxButton.OK);
                    return false;
                }
            }
            if (Parameter.ProtectRadius<=0 || Parameter.ProtectRadius>99)
            {
                System.Windows.MessageBox.Show("保护半径只能输入不大于99的正整数！", "天华提示", System.Windows.MessageBoxButton.OK);
                return false;
            }
            return true;
        }
        private void CollectParameter()
        {
            ThFlushPointParameterService.Instance.FlushPointParameter. // 出图比例
                PlotScale = Parameter.PlotScale;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 楼层标识
                FloorSign = Parameter.FloorSign;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 保护半径
                ProtectRadius = Parameter.ProtectRadius;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 保护目标->停车区域
                ParkingAreaOfProtectTarget = Parameter.ParkingAreaOfProtectTarget;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 保护目标->必布空间
                NecessaryArrangeSpaceOfProtectTarget = Parameter.NecessaryArrangeSpaceOfProtectTarget;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 保护目标->其他空间
                OtherSpaceOfProtectTarget = Parameter.OtherSpaceOfProtectTarget;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 布置策略->必布空间点位
                NecesaryArrangeSpacePointsOfArrangeStrategy = Parameter.NecesaryArrangeSpacePointsOfArrangeStrategy;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 布置策略->停车区域点位
                ParkingAreaPointsOfArrangeStrategy = Parameter.ParkingAreaPointsOfArrangeStrategy;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 布置位置->区域满布
                AreaFullLayoutOfArrangePosition = Parameter.AreaFullLayoutOfArrangePosition;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 布置位置->仅排水设施附近
                OnlyDrainageFaclityNearbyOfArrangePosition = Parameter.OnlyDrainageFaclityNearbyOfArrangePosition;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 点位标识->靠近排水设施
                CloseDrainageFacility = Parameter.CloseDrainageFacility;
            ThFlushPointParameterService.Instance.FlushPointParameter. // 点位标识->远离排水设施
                FarwayDrainageFacility = Parameter.FarwayDrainageFacility;
        }
        private bool CanExecute(Object parameter)
        {
            return true;
        }
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
