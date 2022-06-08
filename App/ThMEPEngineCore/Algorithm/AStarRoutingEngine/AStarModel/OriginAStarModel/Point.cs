using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel
{
    public class Point : AStarEntity , IEquatable<Point>
    {
        public Point() { }
        public Point(int xValue, int yValue)
        {
            this.x = xValue;
            this.y = yValue;
        }

        /// <summary>
        /// column坐标
        /// </summary>
        private int x;
        public int X
        {
            get
            {
                return x;
            }
        }

        /// <summary>
        /// Row坐标
        /// </summary>
        private int y;
        public int Y
        {
            get
            {
                return y;
            }
        }

        /// <summary>
        /// 判断当前点是否是拐点
        /// </summary>
        public bool IsInflectionPoint
        {
            get; set;
        }

        public bool Equals(Point other)
        {
            if((x == other.x) && (y == other.y))
            {
                return true;
            }
            return false;
        }
    }
}
