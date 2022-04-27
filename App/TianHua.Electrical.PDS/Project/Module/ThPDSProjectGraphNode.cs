using System;
using QuikGraph;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;

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
            Type = PDSNodeType.None;
            Details = new NodeDetails();
        }

        /// <summary>
        /// 修改功率/修改高功率
        /// </summary>
        public void SetNodeHighPower(double power)
        {
            this.Details.HighPower = power;
            this.UpdateWithNode(false);
        }

        /// <summary>
        /// 修改低功率
        /// </summary>
        public void SetNodeLowPower(double power)
        {
            this.Details.LowPower = power;
            this.UpdateWithNode(false);
        }

        /// <summary>
        /// 修改相序
        /// </summary>
        public void SetNodePhaseSequence(PhaseSequence phase)
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

        /// <summary>
        /// 修改功率因数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetPowerFactor(double powerFactor)
        {
            Load.PowerFactor = powerFactor;
            this.UpdateWithNode(false);
        }

        /// <summary>
        /// 修改需要系数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetDemandFactor(double demandFactor)
        {
            Load.DemandFactor = demandFactor;
            this.UpdateWithNode(false);
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
                return this.Type == other.Type && this.Load.Equals(other.Load);
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
