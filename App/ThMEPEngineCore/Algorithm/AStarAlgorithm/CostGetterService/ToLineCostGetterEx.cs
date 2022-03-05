using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService
{
    public class ToLineCostGetterEx : ICostGetter
    {
        public Dictionary<Point, double> RoomCast { set; get; }
        public int GetGCost(AStarNode currentNode, CompassDirections moveDirection)
        {
            if (moveDirection == CompassDirections.NotSet)
            {
                return 0;
            }

            int parentG = currentNode != null ? currentNode.CostG : 0;
            Point pt = null;
            switch (moveDirection)
            {
                case CompassDirections.UP:
                    pt = new Point(currentNode.Location.X, currentNode.Location.Y + 1);
                    break;
                case CompassDirections.Down:
                    pt = new Point(currentNode.Location.X, currentNode.Location.Y - 1);
                    break;
                case CompassDirections.Left:
                    pt = new Point(currentNode.Location.X-1, currentNode.Location.Y);
                    break;
                case CompassDirections.Right:
                    pt = new Point(currentNode.Location.X+1, currentNode.Location.Y);
                    break;
                default:
                    break;
            }

            if(pt == null)
            {
                return 0;
            }

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

        public double GetGCost(GlobleNode currentNode, GloblePoint nextNode)
        {
            throw new System.NotImplementedException();
        }

        public int GetHCost(Point cell, AStarEntity endLine)
        {
            int costH = ((AStarLine)endLine).GetDistancePoint(cell) * 10;    //计算H值
            return costH;
        }

        public double GetHCost(GloblePoint cell, AStarEntity endLine)
        {
            throw new System.NotImplementedException();
        }
    }
}
