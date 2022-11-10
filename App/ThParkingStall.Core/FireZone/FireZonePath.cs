using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.FireZone
{
    public class FireZonePath//从边界节点到达另一node定义为一个path
    {
        public List<int> Path = new List<int>();//路径
        public double Cost;
        public bool Ended//奇数个则最后一个node为边界
        {
            get
            {
                return Path.Count % 2 == 1;
            }
        }
        public FireZonePath Step(FireZoneEdge edge,FireZoneNode node)
        {
            return Step(edge.ObjId,node.ObjId,edge.Cost);
        }
        public FireZonePath Step(int edgeId, int nodeId, double cost)
        {
            var clone = new FireZonePath();
            clone.Cost = Cost + cost;
            clone.Path = Path.ToList();
            clone.Path.Add(edgeId);
            if (nodeId != 0) clone.Path.Add(nodeId);
            return clone;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is FireZonePath other)
            {
                if (this.Path.Count != other.Path.Count) return false;
                var PosEqual = PosEquals(other);
                if (!Ended) return PosEqual;
                return PosEqual || NegEquals(other);
            }
            return false;
        }
        public bool Contains(FireZoneNode node)
        {
            return Contains(node.ObjId);
        }
        public bool Contains(FireZoneEdge edge)
        {
            return Contains(edge.ObjId);
        }
        public bool Contains(int Id)
        {
            return Path.Contains(Id);
        }
        private bool PosEquals(FireZonePath other)
        {
            var count = this.Path.Count;
            for (int i = 0; i < count; i++)
            {
                if (this.Path[i] != other.Path[i])
                {
                    return false;
                }
            }
            return true;
        }
        private bool NegEquals(FireZonePath other)
        {
            var count = this.Path.Count;
            for (int i = 0; i < count; i++)
            {
                if (this.Path[count - 1 - i] != other.Path[i])
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            var hashCodeToReturn = 0x56E5EEC8;
            Path.ForEach(p => hashCodeToReturn ^= p.GetHashCode());
            return hashCodeToReturn;
        }
    }
}
