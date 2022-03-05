using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel
{
    public class GlobleLine : AStarEntity
    {
        /// <summary>
        /// 线起点
        /// </summary>
        public GloblePoint StartPoint { get; set; }

        /// <summary>
        /// 线终点
        /// </summary>
        public GloblePoint EndPoint { get; set; }

        public GlobleLine(GloblePoint _startPoint, GloblePoint _endPoint)
        {
            StartPoint = _startPoint;
            EndPoint = _endPoint;
        }

        /// <summary>
        /// 计算点到线的距离
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public double GetDistancePoint(GloblePoint point)
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
