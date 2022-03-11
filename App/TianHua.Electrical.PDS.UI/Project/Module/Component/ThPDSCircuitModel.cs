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
        string lastDemandFactorValue = "1.00";
        [DisplayName("需要系数")]
        [Range(typeof(double), "0.01", "1.00")]
        public string DemandFactor
        {
            get
            {
                var v = edge.Target.Load.DemandFactor;
                if (v > 0 && v <= 1)
                {
                    var s = $"{v:F2}";
                    if (s == "0.00") s = "0.01";
                    lastDemandFactorValue = s;
                    return s;
                }
                edge.Target.Load.DemandFactor = double.Parse(lastDemandFactorValue);
                return lastDemandFactorValue;
            }
            set
            {
                if (double.TryParse(value, out var v))
                {
                    if (v > 0 && v <= 1)
                    {
                        edge.Target.Load.DemandFactor = v;
                        lastDemandFactorValue = $"{v:F2}";
                    }
                }
                OnPropertyChanged(nameof(DemandFactor));
            }
        }
        string lastPowerFactorValue = "1.00";
        [DisplayName("功率因数")]
        [Range(typeof(double), "0.01", "1.00")]
        public string PowerFactor
        {
            get
            {
                var v = edge.Target.Load.PowerFactor;
                if (v > 0 && v <= 1)
                {
                    var s = $"{v:F2}";
                    if (s == "0.00") s = "0.01";
                    lastPowerFactorValue = s;
                    return s;
                }
                edge.Target.Load.PowerFactor = double.Parse(lastPowerFactorValue);
                return lastPowerFactorValue;
            }
            set
            {
                if (double.TryParse(value, out var v))
                {
                    if (v > 0 && v <= 1)
                    {
                        edge.Target.Load.PowerFactor = v;
                        lastPowerFactorValue = $"{v:F2}";
                    }
                }
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
