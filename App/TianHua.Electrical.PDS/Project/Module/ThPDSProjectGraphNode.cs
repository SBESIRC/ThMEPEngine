using System;
using System.Linq;
using QuikGraph;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public class ThPDSProjectGraphNode : IEquatable<ThPDSProjectGraphNode>, ITagged<ThPDSProjectGraphNodeTag>
    {
        public ThPDSLoad Load { get; set; }
        public PDSNodeType Type { get; set; }
        public NodeDetails Details { get; set; }
        public ThPDSProjectGraphNodeTag Tag { get; set; }
        public ThPDSProjectGraphNode()
        {
            Load = new ThPDSLoad();
            Type = PDSNodeType.Empty;
            Details = new NodeDetails();
        }

        /// <summary>
        /// 修改功率/修改高功率
        /// </summary>
        public void SetNodeHighPower(double power)
        {
            if (this.Details.HighPower != power)
            {
                this.Details.HighPower = power;
                if (this.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
                {
                    switch (PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType)
                    {
                        case MeterBoxCircuitType.上海住宅:
                            {
                                var config = DistributionMeteringConfiguration.ShanghaiResidential.FirstOrDefault(o => o.HighPower >= this.Details.HighPower);
                                if (config.IsNull() || config.Phase == ThPDSPhase.三相)
                                {
                                    if (this.Load.Phase == ThPDSPhase.一相)
                                    {
                                        this.Load.Phase = ThPDSPhase.三相;
                                        this.Details.PhaseSequence = PhaseSequence.L123;
                                    }
                                }
                                else
                                {
                                    if (this.Load.Phase == ThPDSPhase.三相)
                                    {
                                        this.Load.Phase = ThPDSPhase.一相;
                                        this.Details.PhaseSequence = PhaseSequence.L1;
                                    }
                                }
                                break;
                            }
                        case MeterBoxCircuitType.江苏住宅:
                            {
                                var config = DistributionMeteringConfiguration.JiangsuResidential.FirstOrDefault(o => o.HighPower >= this.Details.HighPower);
                                if (config.IsNull() || config.Phase == ThPDSPhase.三相)
                                {
                                    if (this.Load.Phase == ThPDSPhase.一相)
                                    {
                                        this.Load.Phase = ThPDSPhase.三相;
                                        this.Details.PhaseSequence = PhaseSequence.L123;
                                    }
                                }
                                else
                                {
                                    if (this.Load.Phase == ThPDSPhase.三相)
                                    {
                                        this.Load.Phase = ThPDSPhase.一相;
                                        this.Details.PhaseSequence = PhaseSequence.L1;
                                    }
                                }
                                break;
                            }
                        case MeterBoxCircuitType.国标_表在断路器前:
                        case MeterBoxCircuitType.国标_表在断路器后:
                        default:
                            break;
                    }
                }
                this.UpdateWithNode(false);
            }
        }

        /// <summary>
        /// 修改低功率
        /// </summary>
        public void SetNodeLowPower(double power)
        {
            if (this.Details.LowPower != power)
            {
                this.Details.LowPower = power;
                this.UpdateWithNode(false);
            }
        }

        /// <summary>
        /// 修改相序
        /// </summary>
        public void SetNodePhaseSequence(PhaseSequence phase)
        {
            if (this.Details.PhaseSequence != phase)
            {
                this.Details.PhaseSequence = phase;
                switch (phase)
                {
                    case PhaseSequence.L1:
                    case PhaseSequence.L2:
                    case PhaseSequence.L3:
                        {
                            this.Load.Phase = ThPDSPhase.一相;
                            break;
                        }
                    case PhaseSequence.L123:
                        {
                            this.Load.Phase = ThPDSPhase.三相;
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
                this.UpdateWithNode(true);
            }
        }

        /// <summary>
        /// 修改功率因数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetPowerFactor(double powerFactor)
        {
            if (Load.PowerFactor != powerFactor)
            {
                Load.PowerFactor = powerFactor;
                this.UpdateWithNode(false);
            }
        }

        /// <summary>
        /// 修改需要系数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetDemandFactor(double demandFactor)
        {
            if (Load.DemandFactor != demandFactor)
            {
                Load.DemandFactor = demandFactor;
                this.UpdateWithNode(false);
            }
        }

        public void SetFireLoad(bool isFireLoad)
        {
            if (Load.FireLoad != isFireLoad)
            {
                this.Load.SetFireLoad(isFireLoad);
                this.UpdateWithNode(false);
            }
        }

        #region
        public event EventHandler TagChanged;
        protected virtual void OnTagChanged(EventArgs args)
        {
            this.TagChanged?.Invoke(this, args);
        }
        #endregion

        #region
        public virtual bool Equals(ThPDSProjectGraphNode other)
        {
            if (other != null)
            {
                return this.Load.LoadUID == other.Load.LoadUID;
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as ThPDSProjectGraphNode);
        }
        public override int GetHashCode()
        {
            return this.Load.GetHashCode();
        }
        #endregion
    }
}
