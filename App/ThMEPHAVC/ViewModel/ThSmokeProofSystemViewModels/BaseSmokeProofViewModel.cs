using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels
{
    [KnownType(typeof(EvacuationFrontViewModel))]
    [KnownType(typeof(EvacuationWalkViewModel))]
    [KnownType(typeof(FireElevatorFrontRoomViewModel))]
    [KnownType(typeof(SeparateOrSharedNaturalViewModel))]
    [KnownType(typeof(SeparateOrSharedWindViewModel))]
    [KnownType(typeof(StaircaseNoWindViewModel))]
    [KnownType(typeof(StaircaseWindViewModel))]
    public abstract class BaseSmokeProofViewModel : NotifyPropertyChangedBase
    {
    }
}
