using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project.Module
{
    /// <summary>
    /// 二级回路（控制回路）
    /// </summary>
    public class ThPDSSecondaryCircuitModel : NotifyPropertyChangedBase
    {
        private readonly SecondaryCircuit _sc;
        public ThPDSSecondaryCircuitModel(SecondaryCircuit sc)
        {
            _sc = sc;
        }

        [ReadOnly(true)]
        [Category("控制回路参数")]
        [DisplayName("回路编号")]
        public string CircuitID => _sc.CircuitID;

        [Category("控制回路参数")]
        [DisplayName("功能描述")]
        public string CircuitDescription => _sc.CircuitDescription;
    }
}
