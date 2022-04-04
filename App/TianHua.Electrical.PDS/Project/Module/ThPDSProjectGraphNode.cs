using System;
using QuikGraph;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphNode : IEquatable<ThPDSProjectGraphNode>, ITagged<ThPDSProjectGraphNodeTag>
    {
        public ThPDSLoad Load { get; set; }
        public PDSNodeType Type { get; set; }
        public NodeDetails Details { get; set; }
        public ThPDSProjectGraphNodeTag Tag { get; set; }
        public bool IsStartVertexOfGraph { get; set; }
        public ThPDSProjectGraphNode()
        {
            Load = new ThPDSLoad();
            Type = PDSNodeType.None;
            IsStartVertexOfGraph = false;
            Details = new NodeDetails();
        }

        public event EventHandler TagChanged;
        protected virtual void OnTagChanged(EventArgs args)
        {
            this.TagChanged?.Invoke(this, args);
        }

        public bool Equals(ThPDSProjectGraphNode other)
        {
            return this.Type == other.Type && this.Load.Equals(other.Load);
        }
    }
}
