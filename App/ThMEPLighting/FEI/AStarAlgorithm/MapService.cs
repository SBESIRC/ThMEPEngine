using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    /// <summary>
    /// 地图服务：用来控制cad点位和map中的相互转换
    /// </summary>
    public class MapService
    {
        double step = 800; //步长
        public MapService() : this(800)
        { }

        public MapService(double _step)
        {
            step = _step;
        }

        public double minRightSpace = 0;
        public double minDownSpace = 0;
        public double maxRightSpace = 0;
        public double maxDownSpace = 0;

        public int minColumn = 0;
        public int maxColumn = 0;
        public int minRow = 0;
        public int maxRow = 0;

        public Matrix3d moveMatrix;
        public Matrix3d ucsMatrix;

        /// <summary>
        /// 设置service信息
        /// </summary>
        public List<int> SetMapServiceInfo(Point3d transSP, Point3d transEP)
        {
            //设置地图服务起点信息
            double sColumn = transSP.X / step;
            double sRow = transSP.Y / step;
            int sPx = Convert.ToInt32(Math.Ceiling(sColumn));     //start point的X值
            int sPy = Convert.ToInt32(Math.Ceiling(sRow));     //start point的Y值
            //设置地图服务终点信息
            double eColumn = transEP.X / step;
            double eRow = transEP.Y / step;
            int ePx = Convert.ToInt32(Math.Ceiling(eColumn));     //end point的X值
            int ePy = Convert.ToInt32(Math.Ceiling(eRow));     //end point的Y值

            var sRightSpace = sPx * step - transSP.X;
            var eRightSpace = ePx * step - transEP.X;
            var sDownSpace = sPy * step - transSP.Y;
            var eDownSpace = ePy * step - transEP.Y;

            if (Math.Abs(sColumn - eColumn) > 0.01)
            {
                if (sColumn < eColumn)
                {
                    ePx = ePx + 1;
                }
                else
                {
                    sPx = sPx + 1;
                }
            }
            if (Math.Abs(sRow - eRow) > 1)
            {
                if (sRow < eRow)
                {
                    ePy = ePy + 1;
                }
                else
                {
                    sPy = sPy + 1;
                }
            }

            minColumn = sPx;
            maxColumn = ePx;
            minRightSpace = sRightSpace;
            maxRightSpace = eRightSpace;
            if (sPx > ePx)
            {
                minColumn = ePx;
                maxColumn = sPx;
                minRightSpace = eRightSpace;
                maxRightSpace = sRightSpace;
            }
            minRow = sPy;
            maxRow = ePy;
            minDownSpace = sDownSpace;
            maxDownSpace = eDownSpace;
            if (sPy > ePy)
            {
                minRow = ePy;
                maxRow = sPy;
                minDownSpace = eDownSpace;
                maxDownSpace = sDownSpace;
            }

            return new List<int>() { sPx, sPy, ePx, ePy };
        }

        /// <summary>
        /// 将map点转到cad（加入起始点前的map）
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point3d TransformMapPointByOriginMap(Point point)
        {
            double xValue = point.X * step;
            double yValue = point.Y * step;
            var pt = new Point3d(xValue, yValue, 0);
            return pt.TransformBy(moveMatrix).TransformBy(ucsMatrix);
        }

        /// <summary>
        /// 将map点转到cad
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point3d TransformMapPoint(Point point)
        {
            double xValue = point.X * step;
            if (minColumn == maxColumn)
            {
                if (point.X == minColumn)
                {
                    xValue = xValue - minRightSpace;
                }
                else if (point.X > minColumn)
                {
                    xValue = (point.X - 1) * step;
                }
            }
            else
            {
                if (point.X == minColumn)
                {
                    xValue = xValue - minRightSpace;
                }
                else if (point.X == maxColumn)
                {
                    xValue = (point.X - 1) * step - maxRightSpace;
                }
                else if (point.X > minColumn && point.X < maxColumn)
                {
                    xValue = (point.X - 1) * step;
                }
                else if (point.X > maxColumn)
                {
                    xValue = (point.X - 2) * step;
                }
            }

            double yValue = point.Y * step;
            if (minRow == maxRow)
            {
                if (point.Y == minRow)
                {
                    yValue = yValue - minDownSpace;
                }
                else if (point.Y > minRow)
                {
                    yValue = (point.Y - 1) * step;
                }
            }
            else
            {
                if (point.Y == minRow)
                {
                    yValue = yValue - minDownSpace;
                }
                else if (point.Y == maxRow)
                {
                    yValue = (point.Y - 1) * step - maxDownSpace;
                }
                else if (point.Y > minRow && point.Y < maxRow)
                {
                    yValue = (point.Y - 1) * step;
                }
                else if (point.Y > maxRow)
                {
                    yValue = (point.Y - 2) * step;
                }
            }

            var pt = new Point3d(xValue, yValue, 0);
            return pt.TransformBy(moveMatrix).TransformBy(ucsMatrix);
        }
    }
}
