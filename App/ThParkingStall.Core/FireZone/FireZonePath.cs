using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.FireZone
{
    public class FireZonePath//以root为起点，经过edge-node-edge-node ... 定义为path
    {
        public List<FireZoneEdge> Edges = new List<FireZoneEdge>();
        private HashSet<FireZoneEdge> _edges = null;
        public HashSet<FireZoneEdge> edgeSet
        {
            get
            {
                if(_edges == null) _edges = Edges.ToHashSet();
                return _edges;  
            }
        }
        public List<FireZoneNode> Nodes =new List<FireZoneNode>();
        private HashSet<FireZoneNode> _nodes = null;
        public HashSet<FireZoneNode> nodeSet
        {
            get
            {
                if (_nodes == null)_nodes = Nodes.ToHashSet();
                return _nodes;
            }
        }
        public bool Ended { get { return Edges.Count > Nodes.Count; } }

        public double Cost = 0;
        public FireZonePath Step(FireZoneEdge edge,FireZoneNode node)
        {
            var clone = new FireZonePath();
            clone.Edges.AddRange(Edges);
            clone.Nodes.AddRange(Nodes);
            clone.Edges.Add(edge);
            if(node.Type!= -1)clone.Nodes.Add(node);
            clone.Cost = Cost + edge.Cost;
            return clone;
        }
        public bool Contains(FireZoneEdge edge)
        {
            return edgeSet.Contains(edge);
        }
        public bool Contains(FireZoneNode node)
        {
            return nodeSet.Contains(node);
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is FireZonePath other)
            {
                if(this.Edges.Count!=other.Edges.Count) return false;
                var PosEqual = PosEquals(other);
                if (!Ended) return PosEqual;
                else
                {
                    return PosEqual || NegEquals(other);
                }
            }
            return false;
        }
        private bool PosEquals(FireZonePath other)
        {
            for(int i = 0; i < Edges.Count; i++)
            {
                if(!Edges[i].Equals(other.Edges[i])) return false;  
            }
            for(int j = 0;j < Nodes.Count; j++)
            {
                if(!Nodes[j].Equals(other.Nodes[j])) return false;
            }
            return true;
        }
        private bool NegEquals(FireZonePath other)
        {
            var ECount = Edges.Count;
            for (int i = 0; i < ECount; i++)
            {
                if (!Edges[ECount -1 -i].Equals(other.Edges[i])) return false;
            }
            var NCount = Nodes.Count;
            for (int j = 0; j < NCount; j++)
            {
                if (!Nodes[NCount - 1 - j].Equals(other.Nodes[j])) return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            var hashCodeToReturn = 0x56E5EEC8;
            Edges.ForEach(p => hashCodeToReturn ^= p.GetHashCode());
            Nodes.ForEach(p => hashCodeToReturn ^= p.GetHashCode());
            return hashCodeToReturn;
        }
    }
}
