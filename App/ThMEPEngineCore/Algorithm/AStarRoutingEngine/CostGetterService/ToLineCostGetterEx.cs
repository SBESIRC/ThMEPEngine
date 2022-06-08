using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.GlobelAStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.CostGetterService
{
    public class ToLineCostGetterEx : ICostGetter
    {
        public Dictionary<Point, double> RoomCast { set; get; }
        public double GetGCost(AStarBaseNode currentNode, AStarEntity nextNode)
        {
            var curAStarNode = (AStarNode)currentNode;
            var pt = (Point)nextNode;
            double parentG = curAStarNode != null ? curAStarNode.CostG : 0;
            var resDic = RoomCast.Keys.Where(x => x.X == pt.X && x.Y == pt.Y).ToList();
            if (resDic.Count > 0)
            {
                return (int)(10 * RoomCast[resDic[0]] + parentG);
            }
            else
            {
                return 10 + parentG;
            }
        }

        public double GetHCost(AStarEntity cell, AStarEntity endLine)
        {
            int costH = ((AStarLine)endLine).GetDistancePoint((Point)cell) * 10;    //计算H值
            return costH;
        }
    }
}
