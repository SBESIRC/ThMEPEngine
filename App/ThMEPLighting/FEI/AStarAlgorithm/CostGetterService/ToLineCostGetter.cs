using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.FEI.AStarAlgorithm.AStarModel;

namespace ThMEPLighting.FEI.AStarAlgorithm.CostGetterService
{
    public class ToLineCostGetter : ICostGetter
    {
        public int GetGCost(AStarNode currentNode, CompassDirections moveDirection)
        {
            if (moveDirection == CompassDirections.NotSet)
            {
                return 0;
            }

            int parentG = currentNode != null ? currentNode.CostG : 0;
            if (moveDirection == CompassDirections.UP || moveDirection == CompassDirections.Down || moveDirection == CompassDirections.Left || moveDirection == CompassDirections.Right)
            {
                return 10 + parentG;
            }

            return 0;
        }

        public int GetHCost(Point cell, EndModel endInfo)
        {
            int costH = endInfo.mapEndLine.GetDistancePoint(cell) * 10;    //计算H值
            return costH;
        }
    }
}
