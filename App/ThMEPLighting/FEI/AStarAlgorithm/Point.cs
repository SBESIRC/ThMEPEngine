using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    public class Point
    {
        public Point(int xValue, int yValue)
        {
            this.x = xValue;
            this.y = yValue;
        }

        /// <summary>
        /// column坐标
        /// </summary>
        private int x = 0;
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
        private int y = 0;
        public int Y { 
            get {
                return y;
            }
        }
    }
}
