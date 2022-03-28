using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSConductorUsageModel : NotifyPropertyChangedBase
    {
        readonly ConductorUse _conductorUse;

        public ThPDSConductorUsageModel(ConductorUse conductorUse)
        {
            _conductorUse = conductorUse;
        }
        /// <summary>
        /// 导体用途
        /// </summary>
        public ConductorType ConductorType
        {
            get => _conductorUse.ConductorType;
            set
            {
                if (value == ConductorType) return;
                _conductorUse.ConductorType = value;
                OnPropertyChanged(null);
            }
        }

        /// <summary>
        /// 外护套材质
        /// </summary>
        public MaterialStructure OuterSheathMaterial
        {
            get => _conductorUse.OuterSheathMaterial;
            set
            {
                if (value == OuterSheathMaterial) return;
                _conductorUse.OuterSheathMaterial = value;
                OnPropertyChanged(null);
            }
        }

        /// <summary>
        /// 燃烧特性代号
        /// </summary>
        public string ConductorMaterial => _conductorUse.ConductorMaterial;

        public bool IsSpecialConductorType => _conductorUse.IsSpecialConductorType;

        public string Content => _conductorUse.Content;
    }
}
