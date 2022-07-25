using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 导体
    /// </summary>
    public class ThPDSConductorModel : NotifyPropertyChangedBase
    {
        private readonly Conductor _conductor;
        public ThPDSConductorModel(Conductor conductor)
        {
            _conductor = conductor;
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("燃料特性代号")]
        public string ConductorMaterial
        {
            get => _conductor.ConductorMaterial;
            set
            {
                _conductor.SetConductorMaterial(value);
                OnPropertyChanged(nameof(ConductorMaterial));
            }
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("材料特征及结构")]
        [Editor(typeof(ThPDSConductorMaterialStructurePropertyEditor), typeof(PropertyEditorBase))]
        public MaterialStructure OuterSheathMaterial
        {
            get => _conductor.OuterSheathMaterial;
            set
            {
                _conductor.SetMaterialStructure(value);
                OnPropertyChanged(nameof(OuterSheathMaterial));
            }
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("导体用途")]
        [Editor(typeof(ThPDSConductorTypePropertyEditor), typeof(PropertyEditorBase))]
        public ConductorType Type
        {
            get => _conductor.ConductorType;
            set
            {
                _conductor.SetConductorType(value);
                OnPropertyChanged(nameof(Type));
                OnPropertyChanged(nameof(ConductorMaterial));
                OnPropertyChanged(nameof(OuterSheathMaterial));
            }
        }

        [Browsable(true)]
        [DisplayName("电缆根数")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorWireNumbersPropertyEditor), typeof(PropertyEditorBase))]
        public int NumberOfPhaseWire
        {
            get => _conductor.NumberOfPhaseWire;
            set
            {
                _conductor.SetNumberOfPhaseWire(value);
                OnPropertyChanged(nameof(NumberOfPhaseWire));
                OnPropertyChanged(nameof(Content));
            }
        }


        [Browsable(true)]
        [DisplayName("控制线芯数")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorWireNumbersPropertyEditor), typeof(PropertyEditorBase))]
        public int ConductorCount
        {
            get => _conductor.ConductorCount;
            set
            {
                _conductor.SetConductorCount(value);
                OnPropertyChanged(nameof(ConductorCount));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("相导体截面")]
        [Editor(typeof(ThPDSConductorCrossSectionalAreasPropertyEditor), typeof(PropertyEditorBase))]
        public double ConductorCrossSectionalArea
        {
            get => _conductor.ConductorCrossSectionalArea;
            set
            {
                _conductor.SetConductorCrossSectionalArea(value);
                OnPropertyChanged(nameof(ConductorCrossSectionalArea));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("控制线导体截面")]
        [Editor(typeof(ThPDSConductorCrossSectionalAreasPropertyEditor), typeof(PropertyEditorBase))]
        public double ControlConductorCrossSectionalArea
        {
            get => _conductor.ConductorCrossSectionalArea;
            set
            {
                _conductor.SetConductorCrossSectionalArea(value);
                OnPropertyChanged(nameof(ControlConductorCrossSectionalArea));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [DisplayName("敷设路径")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorLayingPathPropertyEditor), typeof(PropertyEditorBase))]
        public ConductorLayingPath LayingPath
        {
            get
            {
                return _conductor.ConductorLayingPath;
            }
            set
            {
                _conductor.SetConductorLayingPath(value);
                OnPropertyChanged(nameof(LayingPath));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [DisplayName("敷设方式")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<BridgeLaying>), typeof(PropertyEditorBase))]
        public BridgeLaying BridgeLaying
        {
            get
            {
                return _conductor.BridgeLaying;
            }
            set
            {
                _conductor.SetBridgeLaying(value);
                OnPropertyChanged(nameof(BridgeLaying));
                OnPropertyChanged(nameof(LayingPath));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [DisplayName("敷设部位1")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorLayingSitePropertyEditor), typeof(PropertyEditorBase))]
        public LayingSite LayingSite1
        {
            get
            {
                return _conductor.LayingSite1;
            }
            set
            {
                _conductor.SetLayingSite1(value);
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(LayingSite1));
            }
        }

        [Browsable(true)]
        [DisplayName("敷设部位2")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorLayingSitePropertyEditor), typeof(PropertyEditorBase))]
        public LayingSite LayingSite2
        {
            get
            {
                return _conductor.LayingSite2;
            }
            set
            {
                _conductor.SetLayingSite2(value);
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(LayingSite2));
            }
        }

        [ReadOnly(true)]
        [Browsable(true)]
        [DisplayName("管材")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<PipeMaterial>), typeof(PropertyEditorBase))]
        public PipeMaterial PipeMaterial
        {
            get => _conductor.PipeMaterial;
        }

        [ReadOnly(true)]
        [Browsable(true)]
        [DisplayName("穿管直径")]
        [Category("电线电缆参数")]
        public int PipeDiameter
        {
            get => _conductor.PipeDiameter;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content
        {
            get => _conductor.Content;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<int> AlternativeWireNumbers
        {
            get => _conductor.GetNumberOfPhaseWires();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<int> ConductorCounts => _conductor.GetConductorCounts();

        [ReadOnly(true)]
        [Browsable(false)]
        public List<double> AlternativeConductorCrossSectionalAreas
        {
            get => _conductor.GetConductorCrossSectionalAreas();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<ConductorLayingPath> AlternativeConductorLayingPaths
        {
            get => _conductor.GetConductorLayingPaths();
        }


        [ReadOnly(true)]
        [Browsable(false)]
        public List<LayingSite> AlternativeLayingSites1
        {
            get => _conductor.GetLayingSites1();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<LayingSite> AlternativeLayingSites2
        {
            get => _conductor.GetLayingSites2();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<ConductorType> AlternativeConductorTypes
        {
            get => _conductor.GetConductorTypes();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<MaterialStructure> AlternativeOuterSheathMaterialTypes
        {
            get => _conductor.GetMaterialStructures();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public ComponentType ComponentType => _conductor.ComponentType;

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsCustom => _conductor.IsCustom;

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsBAControl => _conductor.IsBAControl;
    }
}
