using System;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.CostGetterService
{
    public class ToLineCostGetter : ICostGetter
    {
        public double GetGCost(AStarBaseNode currentNode, AStarEntity nextNode)
        {
            var curAStarNode = (AStarNode)currentNode;
            var pointNode = (Point)nextNode;
            double parentG = curAStarNode != null ? curAStarNode.CostG : 0;
            return (Math.Abs(curAStarNode.Location.X - pointNode.X) + Math.Abs(curAStarNode.Location.Y - pointNode.Y)) * 10 + parentG;
        }

        public double GetHCost(AStarEntity cell, AStarEntity endLine)
        {
            double costH = ((AStarLine)endLine).GetDistancePoint((Point)cell) * 10;    //计算H值
            return costH;
        }
    }
}
