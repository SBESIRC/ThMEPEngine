using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSCircuitModel : NotifyPropertyChangedBase
    {
        ThPDSProjectGraphNode vertice;
        ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge;

        public ThPDSCircuitModel(ThPDSProjectGraphNode vertice, ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        {
            this.vertice = vertice;
            this.edge = edge;
        }
        [DisplayName("回路编号")]
        public string CircuitId
        {
            get => edge.Circuit.ID.CircuitID;
            //set
            //{
            //    edge.Circuit.ID.CircuitID = value;
            //    OnPropertyChanged(nameof(CircuitId));
            //}
        }
        [DisplayName("回路形式")]
        public Model.ThPDSCircuitType CircuitType
        {
            get => edge.Circuit.Type;
            set
            {
                edge.Circuit.Type = value;
                OnPropertyChanged(nameof(CircuitType));
            }
        }
        [DisplayName("功率")]
        public double Power
        {
            get => edge.Target.Details.LowPower;
            set
            {
                edge.Target.Details.LowPower = value;
                OnPropertyChanged(nameof(Power));
            }
        }
        [DisplayName("相序")]
        public Model.ThPDSPhase Phase
        {
            get => edge.Target.Load.Phase;
            set
            {
                edge.Target.Load.Phase = value;
                OnPropertyChanged(nameof(Phase));
            }
        }
        [DisplayName("负载类型")]
        public Model.PDSNodeType LoadType
        {
            get => edge.Target.Type;
            set
            {
                edge.Target.Type = value;
                OnPropertyChanged(nameof(LoadType));
            }
        }
        [DisplayName("负载编号")]
        public string LoadId
        {
            get => edge.Circuit.ID.LoadID;
            //set
            //{
            //    edge.Circuit.ID.LoadID = value;
            //    OnPropertyChanged(nameof(LoadId));
            //}
        }
        [DisplayName("功能描述")]
        public string Description
        {
            get => edge.Target.Load.ID.Description;
            set
            {
                edge.Target.Load.ID.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
        [DisplayName("需要系数")]
        public double DemandFactor
        {
            get => edge.Target.Load.DemandFactor;
            set
            {
                edge.Target.Load.DemandFactor = value;
                OnPropertyChanged(nameof(DemandFactor));
            }
        }
        [DisplayName("功率因数")]
        public double PowerFactor
        {
            get => edge.Target.Load.PowerFactor;
            set
            {
                edge.Target.Load.PowerFactor = value;
                OnPropertyChanged(nameof(PowerFactor));
            }
        }
        [DisplayName("计算电流")]
        public double CalculateCurrent
        {
            get => edge.Target.Load.CalculateCurrent;
        }

    }
}
