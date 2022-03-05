using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService
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

        public int GetHCost(Point cell, AStarEntity endLine)
        {
            int costH = ((AStarLine)endLine).GetDistancePoint(cell) * 10;    //计算H值
            return costH;
        }

        public double GetGCost(GlobleNode currentNode, GloblePoint nextNode)
        {
            double parentG = currentNode != null ? currentNode.CostG : 0;
            return (Math.Abs(currentNode.Location.X - nextNode.X) + Math.Abs(currentNode.Location.Y - nextNode.Y)) * 10 + parentG;
        }

        public double GetHCost(GloblePoint cell, AStarEntity endLine)
        {
            double costH = ((GlobleLine)endLine).GetDistancePoint(cell) * 10;    //计算H值
            return costH;
        }
    }
}
