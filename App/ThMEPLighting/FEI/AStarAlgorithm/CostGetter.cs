using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    public class CostGetter
    {
        #region ICostGetter 成员
        public int GetCost(AStarNode currentNode, CompassDirections moveDirection)
        {
            if (moveDirection == CompassDirections.NotSet)
            {
                return 0;
            }

            int parentG = currentNode != null && currentNode.ParentNode != null ? currentNode.ParentNode.CostG : 0;
            if (moveDirection == CompassDirections.UP || moveDirection == CompassDirections.Down || moveDirection == CompassDirections.Left || moveDirection == CompassDirections.Right)
            {
                return 10 + parentG;
            }

            return 0;
        }

        public int GetECost(AStarNode currNode, Point nextNode, Point endNode)
        {
            // 第一个点或直线点
            if (currNode.ParentNode == null || nextNode.X == currNode.ParentNode.Location.X
                    || nextNode.Y == currNode.Location.Y)
            {
                return 0;
            }

            // 拐向终点的点
            if (nextNode.X == endNode.X || nextNode.Y == endNode.Y)
            {
                return 10;
            }

            // 普通拐点
            return 20;
        }
        #endregion
    }
}
