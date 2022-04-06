using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Tree;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    public class ThPipeModel
    {
        public List<ThTreeNode<ThPointModel>> PointNodeList { set; get; }
    }
}
