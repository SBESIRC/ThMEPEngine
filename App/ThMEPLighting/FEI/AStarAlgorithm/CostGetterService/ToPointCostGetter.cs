﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService
{
    public class ToPointCostGetter : ICostGetter
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
            return (Math.Abs(cell.X - endInfo.mapEndPoint.X) + Math.Abs(cell.Y - endInfo.mapEndPoint.Y)) * 10;
        }
    }
}
