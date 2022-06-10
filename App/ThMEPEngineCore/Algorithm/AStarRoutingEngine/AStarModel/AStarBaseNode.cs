using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel
{
    public class AStarBaseNode : IComparable<AStarBaseNode>
    {
        public int CompareTo(AStarBaseNode other)
        {
            return 1;
            //throw new NotImplementedException();
        }
    }
}
