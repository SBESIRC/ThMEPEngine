using System;
using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.Broadcast.Service;
using ThMEPEngineCore.LaneLine;
using Linq2Acad;

namespace ThMEPElectrical.Broadcast
{
    public class LayoutWithParkingLineService
    {
        readonly double protectRange = 34000;
        readonly double oneProtect = 21000;
        readonly double tol = 6000;
        readonly double exLength = 800;

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="mainLines"></param>
        /// <param name="otherLines"></param>
        /// <param name="roomPoly"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> LayoutBraodcast(Polyline frame, List<List<Line>> mainLines, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> layoutInfo = new Dictionary<List<Line>, Dictionary<Point3d, Vector3d>>();
            foreach (var lines in mainLines)
            {
                ParkingLinesService parkingLinesService = new ParkingLinesService();
                var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //获取该车道线上的构建
                StructureService structureService = new StructureService();
                var lineColumn = structureService.GetStruct(lines, columns, tol);
                var lineWall = structureService.GetStruct(lines, walls, tol);
               
                //将构建分为上下部分
                var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, lines, tol * 2);
                var usefulWalls = structureService.SeparateColumnsByLine(lineWall, lines, tol * 2);
                //GetNeedStruct(usefulColumns, usefulWalls, out List<Polyline> needCols, out List<Polyline> needWalls);

                //过滤掉不应该使用的构建
                CheckService checkService = new CheckService();
                var filterCols = checkService.FilterColumns(usefulColumns[1], lines.First(), frame, sPt, ePt);
                var filterWalls = checkService.FilterWalls(usefulWalls[1], lines, tol, exLength);
                
                var pts = new List<Point3d>() { sPt, ePt };

                //计算布置信息
                var maxLengthLine = lines.OrderByDescending(x => x.Length).First();
                var dir = (maxLengthLine.EndPoint - maxLengthLine.StartPoint).GetNormal();
                StructureLayoutService structureLayoutService = new StructureLayoutService();
                var lInfo = structureLayoutService.GetLayoutStructPt(pts, filterCols, filterWalls, dir);

                //计算出构建上的起点和终点在线上的位置
                if (lInfo != null && lInfo.Count > 0)
                {
                    pts = GetStructLayoutPtOnLine(lInfo.Select(x => x.Key).ToList(), lines);
                }

                //计算车道线上布置点
                var lineLayoutPts = GetLayoutLinePoint(lines, pts, sPt, ePt);

                //计算布置信息
                var otherInfo = structureLayoutService.GetLayoutStructPt(lineLayoutPts, filterCols, filterWalls, dir);
                if (otherInfo != null && otherInfo.Count > 0)
                {
                    layoutInfo.Add(lines, otherInfo);
                }
            }

            return layoutInfo;
        }

        /// <summary>
        /// 获取车道线上布置点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        private List<Point3d> GetLayoutLinePoint(List<Line> lines, List<Point3d> pts, Point3d lineSPt, Point3d lineEPt)
        {
            Point3d sPt = pts.OrderBy(x => x.DistanceTo(lineSPt)).First();
            Point3d ePt = pts.OrderBy(x => x.DistanceTo(lineEPt)).First();
            List<Point3d> layoutPts = new List<Point3d>();
            double lineLength = lines.Sum(x => x.Length);
            if (lineLength < 5000)    //车道小于五米不需要布置
            {
                return layoutPts;
            }

            if (lineLength < oneProtect)
            {
                layoutPts.Add(new Point3d((lineSPt.X + lineEPt.X) / 2, (lineSPt.Y + lineEPt.Y) / 2, 0));
            }
            else
            {
                lineLength= lineLength - sPt.DistanceTo(lineSPt) - ePt.DistanceTo(lineEPt);
                if (lineLength > protectRange)
                {
                    var num = Math.Ceiling(lineLength / protectRange);
                    if (num == 1)
                    {
                        num++;
                    }
                    double moveLength = lineLength / num;
                    layoutPts.AddRange(GetLayoutPoint(lines, moveLength, sPt, ePt));
                }
                else
                {
                    layoutPts.AddRange(new List<Point3d>() { sPt, ePt });
                }
            }

            return layoutPts;
        }

        /// <summary>
        /// 计算线上的布置点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="moveLength"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <returns></returns>
        private List<Point3d> GetLayoutPoint(List<Line> lines, double moveLength, Point3d sPt, Point3d ePt)
        {
            List<Point3d> allPts = new List<Point3d>() { sPt };
            double excessLength = 0;
            foreach (var line in lines)
            {
                double lineLength = line.Length;
                Vector3d dir = (line.EndPoint - line.StartPoint).GetNormal();
                Vector3d compareDir = (ePt - sPt).GetNormal();
                if (dir.DotProduct(compareDir) < 0)
                {
                    dir = -dir;
                }

                while (lineLength > moveLength || (excessLength > 0 && lineLength > excessLength + 10))
                {
                    if (excessLength > 0)
                    {
                        lineLength = lineLength - excessLength;
                        Point3d movePt = sPt + dir * excessLength;
                        sPt = movePt;
                        allPts.Add(movePt);
                        excessLength = 0;
                    }
                    else
                    {
                        lineLength = lineLength - moveLength;
                        Point3d movePt = sPt + dir * moveLength;
                        sPt = movePt;
                        allPts.Add(movePt);
                    }
                }

                if (excessLength > 0)
                {
                    excessLength = excessLength - lineLength;
                    sPt = sPt + dir * lineLength;
                }
                else
                {
                    excessLength = moveLength - lineLength;
                    sPt = sPt + dir * lineLength;
                }
            }

            //allPts.Add(ePt);
            return allPts; 
        }

        /// <summary>
        /// 获取构建上的排布点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        private List<Point3d> GetStructLayoutPtOnLine(List<Point3d> pts, List<Line> lines)
        {
            List<Point3d> resPts = new List<Point3d>();
            foreach (var pt in pts)
            {
                var closetPt = pts.OrderBy(x => x.DistanceTo(pt)).First();
                var lineClosetPt = lines.Select(x => x.GetClosestPointTo(closetPt, false)).OrderBy(x => x.DistanceTo(pt)).First();
                resPts.Add(lineClosetPt);
            }

            return resPts;
        }

        /// <summary>
        /// 获取需要的构建
        /// </summary>
        /// <param name="usefulColumns"></param>
        /// <param name="usefulWalls"></param>
        /// <param name="needColumns"></param>
        /// <param name="needWalls"></param>
        private void GetNeedStruct(List<List<Polyline>> usefulColumns, List<List<Polyline>> usefulWalls, out List<Polyline> needColumns, out List<Polyline> needWalls)
        {
            needColumns = new List<Polyline>();
            needWalls = new List<Polyline>();
            if (usefulColumns[0].Count > usefulColumns[1].Count)
            {
                needColumns = usefulColumns[0];
                needWalls = usefulWalls[0];
            }
            else
            {
                needColumns = usefulColumns[1];
                needWalls = usefulWalls[1];
            }
        }
    }
}
