using System;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSComponentGraphNode : IEquatable<ThPDSComponentGraphNode>
    {
        public string NodeID { get; set; }
        public GRect Box { get; set; }
        public bool Equals(ThPDSComponentGraphNode other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
