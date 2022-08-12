using AcHelper;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ThMEPWSS.Command;
using ThMEPWSS.FlushPoint.Model;
using ThMEPWSS.FlushPoint.Service;

namespace ThMEPWSS.ViewModel
{
    public class ThFlushPointVM
    {
        public ThFlushPointParameter Parameter { get; set; }
        public ObservableCollection<string> PlotScales { get; set; }
        public ThFlushPointVM()
        {
            Parameter = new ThFlushPointParameter();
            var plotScales = new List<string> {"1:50","1:100","1:150"};
            PlotScales = new ObservableCollection<string>(plotScales);
        }
        
        public ICommand LayOutFlushPointCmd
        {
            get
            {
                return new RelayCommand(LayOutFlushPoint);
            }
        }

        public ICommand FarawayDrainageFacilityCheckBoxChecked
        {
            get
            {
                return new RelayCommand(FarawayDrainageFacilityClicked);
            }
        }

        public ICommand NearbyDrainageFacilityCheckBoxChecked
        {
            get
            {
                return new RelayCommand(NearbyDrainageFacilityClicked);
            }
        }

        private void FarawayDrainageFacilityClicked()
        {
            ThPointIdentificationService.ShowOrHide(Parameter.FarwayDrainageFacility, Parameter.CloseDrainageFacility);
            SetFocusToDwgView();
        }

        private void NearbyDrainageFacilityClicked()
        {
            ThPointIdentificationService.ShowOrHide(Parameter.FarwayDrainageFacility, Parameter.CloseDrainageFacility);
            SetFocusToDwgView();
        }

        private void LayOutFlushPoint()
        {
            if(CheckParameter())
            {
                SetFocusToDwgView();
                using (var flushPointCmd = new THLayoutFlushPointCmd())
                {
                    flushPointCmd.Execute();
                }
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
