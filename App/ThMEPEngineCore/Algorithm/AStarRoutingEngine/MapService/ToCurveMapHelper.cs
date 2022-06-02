using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService
{
    public class ToCurveMapHelper : MapHelper<Line>
    {
        public double minRightSpace = 0;
        public double middleRightSpace = 0;
        public double maxRightSpace = 0;
        public double minDownSpace = 0;
        public double maxDownSpace = 0;

        public int minColumn = 0;
        public int middleColumn = 0;
        public int maxColumn = 0;
        public int minRow = 0;
        public int maxRow = 0;

        public ToCurveMapHelper(double _step) : base(_step) { }

        /// <summary>
        /// 设置开始和结束信息
        /// </summary>
        /// <param name="_startPt"></param>
        public override AStarEntity SetStartAndEndInfo(Point3d _startPt, Line endLine)
        {
            Point3d transSP = _startPt.TransformBy(ucsMatrix).TransformBy(moveMatrix.Inverse());
            Line cloneLine = endLine.Clone() as Line;
            cloneLine.TransformBy(ucsMatrix);
            cloneLine.TransformBy(moveMatrix.Inverse());

            //设置service信息
            var valueLst = SetMapServiceInfo(transSP, cloneLine);
            int sPx = valueLst[0];
            int sPy = valueLst[1];

            endEntity = new AStarLine(new Point(valueLst[2], valueLst[4]), new Point(valueLst[3], valueLst[4]));
            return new Point(sPx, sPy);
        }

        /// <summary>
        /// 设置service信息
        /// </summary>
        public override List<int> SetMapServiceInfo(Point3d transSP, Line endLine)
        {
            //设置地图服务起点信息
            double sColumn = transSP.X / step;
            double sRow = transSP.Y / step;
            int sPx = Convert.ToInt32(Math.Ceiling(sColumn));     //start point的X值
            int sPy = Convert.ToInt32(Math.Ceiling(sRow));     //start point的Y值

            //设置地图服务终点信息
            double lineStartColumn = endLine.StartPoint.X / step;
            double lineEndColumn = endLine.EndPoint.X / step;
            double lineRow = endLine.StartPoint.Y / step;
            int lineStartPx = Convert.ToInt32(Math.Ceiling(lineStartColumn));     //line start point的X值
            int lineEndPx = Convert.ToInt32(Math.Ceiling(lineEndColumn));         //line end point的X值
            int linePy = Convert.ToInt32(Math.Ceiling(lineRow));                  //line point的Y值

            var sRightSpace = sPx * step - transSP.X;
            var sDownSpace = sPy * step - transSP.Y;
            var lineSRightSpace = lineStartPx * step - endLine.StartPoint.X;
            var lineERightSpace = lineEndPx * step - endLine.EndPoint.X;
            var lineDownSpace = linePy * step - endLine.StartPoint.Y;

            if (sColumn > lineEndColumn)
            {
                sPx = sPx + 2;
                lineEndPx = lineEndPx + 1;
            }
            else if (sColumn == lineEndColumn)
            {
                sPx = sPx + 1;
                lineEndPx = lineEndPx + 1;
            }
            else if (sColumn > lineStartColumn)
            {
                sPx = sPx + 1;
                lineEndPx = lineEndPx + 2;
            }
            else if (sColumn == lineStartColumn)
            {
                lineEndPx = lineEndPx + 1;
            }
            else
            {
                lineEndPx = lineEndPx + 2;
                lineStartPx = lineStartPx + 1;
            }
            if (Math.Abs(sRow - lineRow) > 0.01)
            {
                if (sRow < lineRow)
                {
                    linePy = linePy + 1;
                }
                else
                {
                    sPy = sPy + 1;
                }
            }

            minColumn = sPx;
            middleColumn = lineStartPx;
            maxColumn = lineEndPx;
            minRightSpace = sRightSpace;
            middleRightSpace = lineSRightSpace;
            maxRightSpace = lineERightSpace;
            if (sPx >= lineEndPx)
            {
                minColumn = lineStartPx;
                middleColumn = lineEndPx;
                maxColumn = sPx;
                minRightSpace = lineSRightSpace;
                middleRightSpace = lineERightSpace;
                maxRightSpace = sRightSpace;
            }
            else if (sPx >= lineStartPx)
            {
                minColumn = lineStartPx;
                middleColumn = sPx;
                maxColumn = lineEndPx;
                minRightSpace = lineSRightSpace;
                middleRightSpace = sRightSpace;
                maxRightSpace = lineERightSpace;
            }

            minRow = sPy;
            maxRow = linePy;
            minDownSpace = sDownSpace;
            maxDownSpace = lineDownSpace;
            if (sPy > linePy)
            {
                minRow = linePy;
                maxRow = sPy;
                minDownSpace = lineDownSpace;
                maxDownSpace = sDownSpace;
            }

            return new List<int>() { sPx, sPy, lineStartPx, lineEndPx, linePy };
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
            if (point.X == minColumn)
            {
                xValue = xValue - minRightSpace;
            }
            else if (minColumn < point.X && point.X < middleColumn)
            {
                xValue = (point.X - 1) * step;
            }
            else if (point.X == middleColumn)
            {
                xValue = (point.X - 1) * step - middleRightSpace;
            }
            else if (point.X > middleColumn && point.X < maxColumn)
            {
                xValue = (point.X - 2) * step;
            }
            else if (point.X == maxColumn)
            {
                xValue = (point.X - 2) * step - maxRightSpace;
            }
            else
            {
                xValue = (point.X - 3) * step;
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
