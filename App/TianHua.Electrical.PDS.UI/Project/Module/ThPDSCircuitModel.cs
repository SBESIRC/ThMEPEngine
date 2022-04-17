using System.Linq;
using System.ComponentModel;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;
﻿using HandyControl.Controls;
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
            get => _edge.Circuit.ID.CircuitID.LastOrDefault();
            set
            {
                _edge.Circuit.ID.CircuitID[_edge.Circuit.ID.CircuitID.Count - 1] = value;
                OnPropertyChanged(nameof(CircuitID));
            }
        }

        [ReadOnly(true)]
        [DisplayName("回路形式")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<ThPDSCircuitType>), typeof(PropertyEditorBase))]
        public ThPDSCircuitType CircuitType
        {
            get => _edge.Target.Load.CircuitType;
        }

        [Browsable(true)]
        [DisplayName("功率")]
        [Category("配电回路参数")]
        public double Power
        {
            get => _edge.Target.Details.HighPower;
            set
            {
                _edge.Target.SetNodeHighPower(value);
                OnPropertyChanged(nameof(Power));
            }
        }

        [Browsable(true)]
        [DisplayName("低速功率")]
        [Category("配电回路参数")]
        public double LowPower
        {
            get => _edge.Target.Details.LowPower;
            set
            {
                _edge.Target.SetNodeLowPower(value);
                OnPropertyChanged(nameof(LowPower));
            }
        }

        [Browsable(true)]
        [DisplayName("高速功率")]
        [Category("配电回路参数")]
        public double HighPower
        {
            get => _edge.Target.Details.HighPower;
            set
            {
                _edge.Target.SetNodeHighPower(value);
                OnPropertyChanged(nameof(HighPower));
            }
        }

        [DisplayName("相序")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSCircuitPhaseSequenceEnumPropertyEditor), typeof(PropertyEditorBase))]
        public PhaseSequence PhaseSequence
        {
            get => _edge.Target.Details.PhaseSequence;
            set
            {
                _edge.Target.SetNodePhaseSequence(value);
                OnPropertyChanged(nameof(PhaseSequence));
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
            get => _edge.Circuit.ID.LoadID;
            set
            {
                _edge.Circuit.ID.LoadID = value;
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
            get => _edge.Target.Load.DemandFactor;
            set
            {
                _edge.Target.SetDemandFactor(value);
                OnPropertyChanged(nameof(DemandFactor));
            }
        }

        [DisplayName("功率因数")]
        [Category("配电回路参数")]
        [Editor(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double PowerFactor
        {
            get => _edge.Target.Load.PowerFactor;
            set
            {
                _edge.Target.SetPowerFactor(value);
                OnPropertyChanged(nameof(PowerFactor));
            }
        }

        [ReadOnly(true)]
        [DisplayName("计算电流")]
        [Category("配电回路参数")]
        public double CalculateCurrent
        {
            get => _edge.Target.Load.CalculateCurrent;
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
        public bool IsDualPower => _edge.Target.Details.IsDualPower;
    }
}
