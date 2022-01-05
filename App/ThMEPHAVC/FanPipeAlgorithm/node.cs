using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    public class node : IEquatable<node>
    {
        //基本属性
        public double rx, ry;
        public int x, y;
        //public int index = -1;

        //扩展属性
        public List<List<int>> distance = new List<List<int>>();

        //
        public int father_index = -1;
        public List<int> child_index = new List<int>();
        public List<List<edge>> paths = new List<List<edge>>();

        public node(int x, int y)
        {
            //this.index = index;
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode()
        {
            return (int)x ^ (int)y;
        }
        public bool Equals(node other)
        {
            return x == other.x && y == other.y;
        }
    }
}
