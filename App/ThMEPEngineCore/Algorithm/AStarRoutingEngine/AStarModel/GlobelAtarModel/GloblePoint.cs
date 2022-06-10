using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.GlobelAStarModel
{
    public class GloblePoint : AStarEntity, IEquatable<GloblePoint>
    {
        public GloblePoint(double xValue, double yValue)
        {
            this.x = Convert.ToDouble(xValue.ToString("0.000000"));
            this.y = Convert.ToDouble(yValue.ToString("0.000000"));
        }

        /// <summary>
        /// column坐标
        /// </summary>
        private double x;
        public double X
        {
            get
            {
                return x;
            }
        }

        /// <summary>
        /// Row坐标
        /// </summary>
        private double y;
        public double Y
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

        public bool Equals(GloblePoint other)
        {
            if ((x == other.x) && (y == other.y))
            {
                return true;
            }
            return false;
        }
    }
}
