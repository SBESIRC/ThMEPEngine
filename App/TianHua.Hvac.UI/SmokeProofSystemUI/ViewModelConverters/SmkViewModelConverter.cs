using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.ViewModelConverters
{
    public static class SmkViewModelConverter
    {
        public static void ConvertFireElevatorFrontRoomViewModel(FireElevatorFrontRoomViewModel fireElevatorFrontRoomViewModel)
        {
            if (ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel == null)
            {
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel = fireElevatorFrontRoomViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.OverAk = fireElevatorFrontRoomViewModel.OverAk;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.FloorType = fireElevatorFrontRoomViewModel.FloorType;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.FloorNum = fireElevatorFrontRoomViewModel.FloorNum;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.SectionLength = fireElevatorFrontRoomViewModel.SectionLength;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.SectionWidth = fireElevatorFrontRoomViewModel.SectionWidth;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.LjTotal = fireElevatorFrontRoomViewModel.LjTotal;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.OpenDorrAirSupply = fireElevatorFrontRoomViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.VentilationLeakage = fireElevatorFrontRoomViewModel.VentilationLeakage;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.ListTabControl = fireElevatorFrontRoomViewModel.ListTabControl;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.SelectTabControlIndex = fireElevatorFrontRoomViewModel.SelectTabControlIndex;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.CheckTableVal = fireElevatorFrontRoomViewModel.CheckTableVal;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.MiddleWind = fireElevatorFrontRoomViewModel.MiddleWind;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.HighWind = fireElevatorFrontRoomViewModel.HighWind;
                ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.FinalValue = fireElevatorFrontRoomViewModel.FinalValue;
            }
        }

        public static void ConvertSeparateOrSharedNaturalViewModel(SeparateOrSharedNaturalViewModel separateOrSharedNaturalViewModel)
        {
            if (ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel == null)
            {
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel = separateOrSharedNaturalViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.OverAk = separateOrSharedNaturalViewModel.OverAk;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.OverAl = separateOrSharedNaturalViewModel.OverAl;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FloorType = separateOrSharedNaturalViewModel.FloorType;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FloorNum = separateOrSharedNaturalViewModel.FloorNum;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.SectionLength = separateOrSharedNaturalViewModel.SectionLength;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.SectionWidth = separateOrSharedNaturalViewModel.SectionWidth;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.LjTotal = separateOrSharedNaturalViewModel.LjTotal;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.OpenDorrAirSupply = separateOrSharedNaturalViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.VentilationLeakage = separateOrSharedNaturalViewModel.VentilationLeakage;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FrontRoomTabControl = separateOrSharedNaturalViewModel.FrontRoomTabControl;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FrontRoomTabControlIndex = separateOrSharedNaturalViewModel.FrontRoomTabControlIndex;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.StairRoomTabControl = separateOrSharedNaturalViewModel.StairRoomTabControl;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.StairRoomTabControlIndex = separateOrSharedNaturalViewModel.StairRoomTabControlIndex;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.CheckTableVal = separateOrSharedNaturalViewModel.CheckTableVal;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.MiddleWind = separateOrSharedNaturalViewModel.MiddleWind;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.HighWind = separateOrSharedNaturalViewModel.HighWind;
                ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.FinalValue = separateOrSharedNaturalViewModel.FinalValue;
            }
        }

        public static void ConvertSeparateOrSharedWindViewModel(SeparateOrSharedWindViewModel separateOrSharedWindViewModel)
        {
            if (ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel == null)
            {
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel = separateOrSharedWindViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.OverAk = separateOrSharedWindViewModel.OverAk;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.OverAl = separateOrSharedWindViewModel.OverAl;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.FloorType = separateOrSharedWindViewModel.FloorType;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.FloorNum = separateOrSharedWindViewModel.FloorNum;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.SectionLength = separateOrSharedWindViewModel.SectionLength;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.SectionWidth = separateOrSharedWindViewModel.SectionWidth;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.LjTotal = separateOrSharedWindViewModel.LjTotal;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.OpenDorrAirSupply = separateOrSharedWindViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.VentilationLeakage = separateOrSharedWindViewModel.VentilationLeakage;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.FrontRoomTabControl = separateOrSharedWindViewModel.FrontRoomTabControl;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.SelectTabControlIndex = separateOrSharedWindViewModel.SelectTabControlIndex;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.CheckTableVal = separateOrSharedWindViewModel.CheckTableVal;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.MiddleWind = separateOrSharedWindViewModel.MiddleWind;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.HighWind = separateOrSharedWindViewModel.HighWind;
                ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.FinalValue = separateOrSharedWindViewModel.FinalValue;
            }
        }

        public static void ConvertStaircaseNoWindViewModel(StaircaseNoWindViewModel staircaseNoWindViewModel)
        {
            if (ThMEPHVACStaticService.Instance.staircaseNoWindViewModel == null)
            {
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel = staircaseNoWindViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.OverAk = staircaseNoWindViewModel.OverAk;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.N2 = staircaseNoWindViewModel.N2;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.StairN1 = staircaseNoWindViewModel.StairN1;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.FloorType = staircaseNoWindViewModel.FloorType;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.StairPosition = staircaseNoWindViewModel.StairPosition;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.BusinessType = staircaseNoWindViewModel.BusinessType;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.FloorNum = staircaseNoWindViewModel.FloorNum;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.SectionLength = staircaseNoWindViewModel.SectionLength;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.SectionWidth = staircaseNoWindViewModel.SectionWidth;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.EvacuationDoorNums = staircaseNoWindViewModel.EvacuationDoorNums;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.LjTotal = staircaseNoWindViewModel.LjTotal;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.OpenDorrAirSupply = staircaseNoWindViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.VentilationLeakage = staircaseNoWindViewModel.VentilationLeakage;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.LeakArea = staircaseNoWindViewModel.LeakArea;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.FrontRoomTabControl = staircaseNoWindViewModel.FrontRoomTabControl;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.SelectTabControlIndex = staircaseNoWindViewModel.SelectTabControlIndex;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.CheckTableVal = staircaseNoWindViewModel.CheckTableVal;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.MiddleWind = staircaseNoWindViewModel.MiddleWind;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.HighWind = staircaseNoWindViewModel.HighWind;
                ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.FinalValue = staircaseNoWindViewModel.FinalValue;
            }
        }

        public static void ConvertStaircaseWindViewModel(StaircaseWindViewModel staircaseWindViewModel)
        {
            if (ThMEPHVACStaticService.Instance.staircaseWindViewModel == null)
            {
                ThMEPHVACStaticService.Instance.staircaseWindViewModel = staircaseWindViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.OverAk = staircaseWindViewModel.OverAk;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.N2 = staircaseWindViewModel.N2;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.FloorType = staircaseWindViewModel.FloorType;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.FloorNum = staircaseWindViewModel.FloorNum;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.StairPosition = staircaseWindViewModel.StairPosition;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.BusinessType = staircaseWindViewModel.BusinessType;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.OpenDorrAirSupply = staircaseWindViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.OpenDorrAirSupply = staircaseWindViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.LeakArea = staircaseWindViewModel.LeakArea;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.VentilationLeakage = staircaseWindViewModel.VentilationLeakage;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.LjTotal = staircaseWindViewModel.LjTotal;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.ListTabControl = staircaseWindViewModel.ListTabControl;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.SelectTabControlIndex = staircaseWindViewModel.SelectTabControlIndex;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.CheckTableVal = staircaseWindViewModel.CheckTableVal;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.MiddleWind = staircaseWindViewModel.MiddleWind;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.HighWind = staircaseWindViewModel.HighWind;
                ThMEPHVACStaticService.Instance.staircaseWindViewModel.FinalValue = staircaseWindViewModel.FinalValue;
            }
        }

        public static void ConvertEvacuationWalkViewModel(EvacuationWalkViewModel evacuationWalkViewModel)
        {
            if (ThMEPHVACStaticService.Instance.evacuationWalkViewModel == null)
            {
                ThMEPHVACStaticService.Instance.evacuationWalkViewModel = evacuationWalkViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.evacuationWalkViewModel.WindVolume = evacuationWalkViewModel.WindVolume;
                ThMEPHVACStaticService.Instance.evacuationWalkViewModel.AreaNet = evacuationWalkViewModel.AreaNet;
                ThMEPHVACStaticService.Instance.evacuationWalkViewModel.AirVolSpec = evacuationWalkViewModel.AirVolSpec;
            }
        }

        public static void ConvertEvacuationFrontViewModel(EvacuationFrontViewModel evacuationFrontViewModel)
        {
            if (ThMEPHVACStaticService.Instance.evacuationFrontViewModel == null)
            {
                ThMEPHVACStaticService.Instance.evacuationFrontViewModel = evacuationFrontViewModel;
            }
            else
            {
                ThMEPHVACStaticService.Instance.evacuationFrontViewModel.OverAk = evacuationFrontViewModel.OverAk;
                ThMEPHVACStaticService.Instance.evacuationFrontViewModel.OpenDorrAirSupply = evacuationFrontViewModel.OpenDorrAirSupply;
                ThMEPHVACStaticService.Instance.evacuationFrontViewModel.ListTabControl = evacuationFrontViewModel.ListTabControl;
                ThMEPHVACStaticService.Instance.evacuationFrontViewModel.SelectTabControlIndex = evacuationFrontViewModel.SelectTabControlIndex;
            }
        }
    }
}
