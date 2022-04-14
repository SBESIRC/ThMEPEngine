using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project.Module
{
    //二级回路跟控制回路是同一个东西
    public class ThPDSSecondaryCircuitModel
    {
        private readonly SecondaryCircuit _sc;

        public ThPDSSecondaryCircuitModel(SecondaryCircuit sc)
        {
            _sc = sc;
        }
        [Category("控制回路参数")]
        [DisplayName("回路编号")]
        public string CircuitID => _sc.CircuitID;
        [Category("控制回路参数")]
        [DisplayName("功能描述")]
        public string CircuitDescription => _sc.CircuitDescription;
    }
}
