using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.GlobelAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.CostGetterService
{
    public class ToWeightedLineCostGetter : ICostGetter
    {
        public double GetGCost(AStarBaseNode currentNode, AStarEntity nextNode)
        {
            var curAStarNode = (GlobleNode)currentNode;
            var pointNode = (GloblePoint)nextNode;
            double parentG = curAStarNode != null ? curAStarNode.CostG : 0;
            return (Math.Abs(curAStarNode.Location.X - pointNode.X) + Math.Abs(curAStarNode.Location.Y - pointNode.Y)) * 10 + parentG;
        }

        public double GetHCost(AStarEntity cell, AStarEntity endInfo)
        {
            double costH = ((GlobleLine)endInfo).GetDistancePoint((GloblePoint)cell) * 10;    //计算H值
            return costH;
        }
    }
}
