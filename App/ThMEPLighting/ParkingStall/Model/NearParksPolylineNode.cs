using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    /// <summary>
    /// 一组相连车位的集合， 需再加工
    /// </summary>
    public class NearParksPolylineNode
    {
        public List<PolylineNode> ParksPolylineNodes;
        public NearParksPolylineNode(List<PolylineNode> polylineNodes)
        {
            ParksPolylineNodes = polylineNodes;
        }
    }
}
