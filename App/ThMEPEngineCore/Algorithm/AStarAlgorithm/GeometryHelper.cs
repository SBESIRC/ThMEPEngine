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
        #endregion     
    }
}
