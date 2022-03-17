using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSCPSModel : NotifyPropertyChangedBase
    {
        readonly CPS cps;
        public ThPDSCPSModel(CPS cps)
        {
            this.cps = cps;
        }
        [DisplayName("型号")]
        public string CPSType { get => cps.CPSType; set => cps.CPSType = value; }
        [DisplayName("壳架规格")]
        public string FrameSpecifications { get => cps.FrameSpecifications; set => cps.FrameSpecifications = value; }
        [DisplayName("极数")]
        public string PolesNum { get => cps.PolesNum; set => cps.PolesNum = value; }
        [DisplayName("额定电流")]
        public string RatedCurrent { get => cps.RatedCurrent; set => cps.RatedCurrent = value; }
        [DisplayName("组合形式")]
        public string Combination { get => cps.Combination; set => cps.Combination = value; }
        [DisplayName("级别代号")]
        public string CodeLevel { get => cps.CodeLevel; set => cps.CodeLevel = value; }
    }
}
