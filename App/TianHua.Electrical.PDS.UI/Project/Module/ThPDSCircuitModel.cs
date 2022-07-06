using System.ComponentModel;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Extension;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSCircuitModel : NotifyPropertyChangedBase
    {
        private readonly ThPDSProjectGraphEdge _edge;
        public ThPDSCircuitModel(ThPDSProjectGraphEdge edge)
        {
            _edge = edge;
        }

        [DisplayName("回路ID")]
        [Category("配电回路参数")]
        public string CircuitID
        {
            get
            {
                return _edge.GetCircuitID();
            }
            set
            {
                _edge.SetCircuitID(value);
                OnPropertyChanged(nameof(CircuitID));
            }
        }


        [DisplayName("消防属性")]
        [Category("配电回路参数")]
        public bool FireLoad
        {
            get => _edge.Target.Load.FireLoad;
            set
            {
                _edge.Target.SetFireLoad(value);
                OnPropertyChanged(nameof(FireLoad));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [Browsable(true)]
        [DisplayName("功率")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double Power
        {
            get => _edge.Target.Details.LoadCalculationInfo.HighPower;
            set
            {
                _edge.Target.SetNodeHighPower(value);
                OnPropertyChanged(nameof(Power));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [Browsable(true)]
        [DisplayName("低速功率")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double LowPower
        {
            get => _edge.Target.Details.LoadCalculationInfo.LowPower;
            set
            {
                _edge.Target.SetNodeLowPower(value);
                OnPropertyChanged(nameof(LowPower));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [Browsable(true)]
        [DisplayName("高速功率")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double HighPower
        {
            get => _edge.Target.Details.LoadCalculationInfo.HighPower;
            set
            {
                _edge.Target.SetNodeHighPower(value);
                OnPropertyChanged(nameof(HighPower));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [DisplayName("相序")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSCircuitPhaseSequenceEnumPropertyEditor), typeof(PropertyEditorBase))]
        public PhaseSequence PhaseSequence
        {
            get => _edge.Target.Details.LoadCalculationInfo.PhaseSequence;
            set
            {
                _edge.Target.SetNodePhaseSequence(value);
                OnPropertyChanged(nameof(PhaseSequence));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [ReadOnly(true)]
        [DisplayName("负载类型")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<PDSNodeType>), typeof(PropertyEditorBase))]
        public PDSNodeType LoadType
        {
            get => _edge.Target.Type;
        }

        [DisplayName("负载编号")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSLoadIdPlainTextPropertyEditor), typeof(PropertyEditorBase))]
        public string LoadId
        {
            get => _edge.Target.Load.ID.LoadID;
            set
            {
                _edge.Target.Load.ID.LoadID = value;
                OnPropertyChanged(nameof(LoadId));
            }
        }

        [DisplayName("功能描述")]
        [Category("配电回路参数")]
        public string Description
        {
            get => _edge.Target.Load.ID.Description;
            set
            {
                _edge.Target.Load.ID.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        [DisplayName("需要系数")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double DemandFactor
        {
            get => _edge.Target.Details.LoadCalculationInfo.HighDemandFactor;
            set
            {
                _edge.Target.SetDemandFactor(value);
                OnPropertyChanged(nameof(DemandFactor));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [DisplayName("功率因数")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double PowerFactor
        {
            get => _edge.Target.Details.LoadCalculationInfo.PowerFactor;
            set
            {
                _edge.Target.SetPowerFactor(value);
                OnPropertyChanged(nameof(PowerFactor));
                OnPropertyChanged(nameof(CalculateCurrent));
            }
        }

        [ReadOnly(true)]
        [DisplayName("计算电流")]
        [Category("配电回路参数")]
        public string CalculateCurrent
        {
            get => string.Format("{0}", _edge.Target.Details.LoadCalculationInfo.HighCalculateCurrent);
        }

        [Browsable(false)]
        public bool CircuitLock
        {
            get => _edge.Details.CircuitLock;
            set
            {
                _edge.Details.CircuitLock = value;
                OnPropertyChanged(nameof(CircuitLock));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsDualPower => _edge.Target.Details.LoadCalculationInfo.IsDualPower;
    }
}
