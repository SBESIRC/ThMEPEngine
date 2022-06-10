using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService
{
    /// <summary>
    /// 地图服务：用来控制cad点位和map中的相互转换
    /// </summary>
    public class ToPointMapHelper : MapHelper<Point3d>
    {
        public double minRightSpace = 0;
        public double minDownSpace = 0;
        public double maxRightSpace = 0;
        public double maxDownSpace = 0;

        public int minColumn = 0;
        public int maxColumn = 0;
        public int minRow = 0;
        public int maxRow = 0;

        public ToPointMapHelper(double _step) : base(_step) { }

        /// <summary>
        /// 设置开始和结束信息
        /// </summary>
        /// <param name="_startPt"></param>
        public override AStarEntity SetStartAndEndInfo(Point3d _startPt, Point3d _endPt)
        {
            Point3d transSP = _startPt.TransformBy(ucsMatrix).TransformBy(moveMatrix.Inverse());
            Point3d transEP = _endPt.TransformBy(ucsMatrix).TransformBy(moveMatrix.Inverse());

            //设置service信息
            var valueLst = SetMapServiceInfo(transSP, transEP);
            int sPx = valueLst[0];
            int sPy = valueLst[1];
            int ePx = valueLst[2];
            int ePy = valueLst[3];

            endEntity = new Point(ePx, ePy);
            return new Point(sPx, sPy);
        }

        /// <summary>
        /// 设置service信息
        /// </summary>
        public override List<int> SetMapServiceInfo(Point3d transSP, Point3d transEP)
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
            if (Math.Abs(sRow - eRow) > 0.001)
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
        /// 将map点转到cad
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override Point3d TransformMapPoint(AStarEntity ent)
        {
            var point = (Point)ent;
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
            return pt.TransformBy(moveMatrix).TransformBy(ucsMatrix.Inverse());
        }
    }
}
