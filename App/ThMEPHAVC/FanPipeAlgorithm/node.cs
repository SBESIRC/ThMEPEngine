using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    public class Node : IEquatable<Node>
    {
        //基本属性
        public double rx, ry;
        public int x, y;
        //public int index = -1;

        //扩展属性
        public List<List<int>> DistanceMap = new List<List<int>>();

        //
        public int FatherIndex = -1;
        public List<int> ChildIndex = new List<int>();
        public List<List<Edge>> paths = new List<List<Edge>>();

        public Node(int x, int y)
        {
            //this.index = index;
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode()
        {
            return (int)x ^ (int)y;
        }
        public bool Equals(Node other)
        {
            return x == other.x && y == other.y;
        }
    }
}
