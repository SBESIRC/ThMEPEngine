using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm
{
    public static class GeometryHelper
    {
        #region GetAdjacentPoint
        /// <summary>
        /// GetAdjacentPoint 获取某个方向上的相邻点
        /// </summary>       
        public static Point GetAdjacentPoint(Point current, CompassDirections direction)
        {
            switch (direction)
            {
                case CompassDirections.UP:
                    {
                        return new Point(current.X, current.Y + 1);
                    }
                case CompassDirections.Down:
                    {
                        return new Point(current.X, current.Y - 1);
                    }
                case CompassDirections.Left:
                    {
                        return new Point(current.X - 1, current.Y);
                    }
                case CompassDirections.Right:
                    {
                        return new Point(current.X + 1, current.Y);
                    }
                default:
                    {
                        return current;
                    }
            }
        }

        public static GloblePoint GetAdjacentPoint(GloblePoint current, CompassDirections direction, Dictionary<double, int> xDic, Dictionary<double, int> yDic)
        {
            GloblePoint nextPt = current;
            switch (direction)
            {       
                case CompassDirections.UP:
                    nextPt = new GloblePoint(current.X, current.Y + CheckMoveIndex(current, yDic, false, true));
                    break;
                case CompassDirections.Down:
                    nextPt = new GloblePoint(current.X, current.Y - CheckMoveIndex(current, yDic, false, false));
                    break;
                case CompassDirections.Left:
                    nextPt = new GloblePoint(current.X - CheckMoveIndex(current, xDic, true, false), current.Y);
                    break;
                case CompassDirections.Right:
                    nextPt = new GloblePoint(current.X + CheckMoveIndex(current, xDic, true, true), current.Y);
                    break;
                case CompassDirections.NotSet:
                default:
                    break;
            }
            return nextPt;
        }
        #endregion   
        
        private static double CheckMoveIndex(GloblePoint current, Dictionary<double, int> dic, bool isX, bool rightMove)
        {
            var ptIndex = current.X;
            if (!isX)
            {
                ptIndex = current.Y;
            }
            var keyInfo = dic.Where(x => x.Key == ptIndex).ToList();
            if (keyInfo.Count > 0)
            {
                if (rightMove)
                {
                    return 1 - Math.Abs(keyInfo.First().Key - keyInfo.First().Value);
                }
                else
                {
                    return Math.Abs(keyInfo.First().Key - keyInfo.First().Value);
                }
            }
            if (!rightMove)
            {
                ptIndex = ptIndex - 1;
            }
            var xInfo = dic.Where(x => x.Value == (int)Math.Floor(ptIndex)).ToList();
            if (xInfo.Count > 0)
            {
                var value = xInfo.First().Key;
                if (ptIndex < value)
                {
                    if (!rightMove)
                    {
                        return 1 - Math.Abs(value - ptIndex);
                    }
                    else
                    {
                        return Math.Abs(value - ptIndex);
                    }
                }
            }
            return 1;
        }
    }
}
