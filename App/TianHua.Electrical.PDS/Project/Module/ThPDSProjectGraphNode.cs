using System;
using QuikGraph;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphNode : IEquatable<ThPDSProjectGraphNode>, ITagged<ThPDSProjectGraphNodeCompareTag>
    {
        public ThPDSLoad Load { get; set; }
        public PDSNodeType Type { get; set; }
        public NodeDetails Details { get; set; }
        public ThPDSProjectGraphNodeCompareTag Tag { get; set; }
        public bool IsStartVertexOfGraph { get; set; }
        public ThPDSProjectGraphNode()
        {
            Load = new ThPDSLoad();
            Type = PDSNodeType.None;
            IsStartVertexOfGraph = false;
            Details = new NodeDetails();
        }

        /// <summary>
        /// 修改功率/修改高功率
        /// </summary>
        public void SetNodeHighPower(double power)
        {
            this.Details.HighPower = power;
            PDSProject.Instance.graphData.UpdateWithNode(this, false);
        }

        /// <summary>
        /// 修改低功率
        /// </summary>
        public void SetNodeLowPower(double power)
        {
            this.Details.LowPower = power;
            PDSProject.Instance.graphData.UpdateWithNode(this, false);
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
            PDSProject.Instance.graphData.UpdateWithNode(this, true);
        }

        /// <summary>
        /// 修改功率因数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetPowerFactor(double powerFactor)
        {
            Load.PowerFactor = powerFactor;
            PDSProject.Instance.graphData.UpdateWithNode(this, false);
        }

        /// <summary>
        /// 修改需要系数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetDemandFactor(double demandFactor)
        {
            Load.DemandFactor = demandFactor;
            PDSProject.Instance.graphData.UpdateWithNode(this, false);
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
