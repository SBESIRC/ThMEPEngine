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
        public string ConductorMaterial
        {
            get=>_conductorUse.ConductorMaterial;
        }

        public bool IsSpecialConductorType => _conductorUse.IsSpecialConductorType;

        public string Content => _conductorUse.Content;
        public bool HalogenFree
        {
            get => _conductorUse.HalogenFree;
            set
            {
                _conductorUse.HalogenFree = value;
                OnPropertyChanged(null);
            }
        }
        public bool LowSmoke
        {
            get => _conductorUse.LowSmoke;
            set
            {
                _conductorUse.LowSmoke = value;
                OnPropertyChanged(null);
            }
        }
        public bool LowToxicity
        {
            get => _conductorUse.LowToxicity;
            set
            {
                _conductorUse.LowToxicity = value;
                OnPropertyChanged(null);
            }
        }
        public bool FlameRetardant
        {
            get => _conductorUse.FlameRetardant;
            set
            {
                _conductorUse.FlameRetardant = value;
                OnPropertyChanged(null);
            }
        }
        public bool Refractory
        {
            get => _conductorUse.Refractory;
            set
            {
                _conductorUse.Refractory = value;
                OnPropertyChanged(null);
            }
        }
        public ConductorLevel Level
        {
            get => _conductorUse.Level;
            set
            {
                _conductorUse.Level = value;
                OnPropertyChanged(null);
            }
        }
    }
}
