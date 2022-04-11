using System;
using QuikGraph;
using TianHua.Electrical.PDS.Model;

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
