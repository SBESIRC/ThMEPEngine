﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel
{
    public class Point : AStarEntity
    {
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
    }
}
