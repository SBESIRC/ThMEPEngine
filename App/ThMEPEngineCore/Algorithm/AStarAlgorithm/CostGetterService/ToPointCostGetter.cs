﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;

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

        public int GetHCost(Point cell, AStarEntity entity)
        {
            Point endPoint = (Point)entity;
            return (Math.Abs(cell.X - endPoint.X) + Math.Abs(cell.Y - endPoint.Y)) * 10;
        }
    }
}
