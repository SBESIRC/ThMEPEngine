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
        private readonly ThPDSProjectGraphEdge<ThPDSProjectGraphNode> _edge;

        public ThPDSCircuitModel(ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            _edge = edge;
        }

        [ReadOnly(true)]
        [DisplayName("回路编号")]
        public string CircuitNumber
        {
            get => _edge.Circuit.ID.CircuitNumber.LastOrDefault();
        }

        [ReadOnly(true)]
        [DisplayName("回路形式")]
        [Editor(typeof(ThPDSEnumPropertyEditor<ThPDSCircuitType>), typeof(PropertyEditorBase))]
        public ThPDSCircuitType CircuitType
        {
            get => _edge.Target.Load.CircuitType;
        }

        [DisplayName("功率")]
        public double Power
        {
            get => _edge.Target.Details.LowPower;
            set
            {
                _edge.Target.Details.LowPower = value;
                OnPropertyChanged(nameof(Power));
            }
        }

        [DisplayName("相序")]
        [EditorAttribute(typeof(ThPDSCircuitPhaseSequenceEnumPropertyEditor), typeof(PropertyEditorBase))]
        public PhaseSequence PhaseSequence
        {
            get => _edge.Target.Details.PhaseSequence;
            set
            {
                _edge.Target.Details.PhaseSequence = value;
                OnPropertyChanged(nameof(PhaseSequence));
            }
        }

        [ReadOnly(true)]
        [DisplayName("负载类型")]
        [Editor(typeof(ThPDSEnumPropertyEditor<PDSNodeType>), typeof(PropertyEditorBase))]
        public PDSNodeType LoadType
        {
            get => _edge.Target.Type;
        }

        [DisplayName("负载编号")]
        [EditorAttribute(typeof(ThPDSLoadIdPlainTextPropertyEditor), typeof(PropertyEditorBase))]
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
        [EditorAttribute(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double DemandFactor
        {
            get => _edge.Target.Load.DemandFactor;
            set
            {
                _edge.Target.Load.DemandFactor = value;
                OnPropertyChanged(nameof(DemandFactor));
            }
        }

        [DisplayName("功率因数")]
        [EditorAttribute(typeof(ThPDSRangedNumberPropertyEditor), typeof(PropertyEditorBase))]
        public double PowerFactor
        {
            get => _edge.Target.Load.PowerFactor;
            set
            {
                _edge.Target.Load.PowerFactor = value;
                OnPropertyChanged(nameof(PowerFactor));
            }
        }

        [ReadOnly(true)]
        [DisplayName("计算电流")]
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
    }
}
