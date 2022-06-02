using System;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel
{
    public class AStarLine : AStarEntity
    {
        /// <summary>
        /// 线起点
        /// </summary>
        public Point StartPoint { get; set; }

        /// <summary>
        /// 线终点
        /// </summary>
        public Point EndPoint { get; set; }

        public AStarLine(Point _startPoint, Point _endPoint)
        {
            StartPoint = _startPoint;
            EndPoint = _endPoint;
        }

        /// <summary>
        /// 计算点到线的距离
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int GetDistancePoint(Point point)
        {
            if (StartPoint.X <= point.X && point.X <= EndPoint.X)
            {
                return Math.Abs(point.Y - StartPoint.Y);
            }
            else if (point.X < StartPoint.X)
            {
                return Math.Abs(StartPoint.X - point.X) + Math.Abs(StartPoint.Y - point.Y);
            }
            else
            {
                return Math.Abs(EndPoint.X - point.X) + Math.Abs(EndPoint.Y - point.Y);
            }
        }
    }
}
