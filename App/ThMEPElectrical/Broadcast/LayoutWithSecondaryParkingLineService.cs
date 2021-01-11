using System;
using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.Broadcast.Service;

namespace ThMEPElectrical.Broadcast
{
    public class LayoutWithSecondaryParkingLineService
    {
        readonly double pLength = 12500;  //超过12.5米副车道要补点
        readonly double layoutLength = 31000;  //超过25米补一点
        readonly double endLength = 10000;  //10米左右补端头一点
        readonly double tol = 5000;
        readonly double exLength = 800;
        readonly double minLength = 15000;    //起点到终点直线小于12米不布置点

        public Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> LayoutBraodcast(Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> mainLayoutInfo
            , List<List<Line>> otherLines, List<Polyline> columns, List<Polyline> walls, Polyline frame)
        {
            Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> resLayoutInfo = new Dictionary<List<Line>, Dictionary<Point3d, Vector3d>>(mainLayoutInfo);
            var allMainLines = resLayoutInfo.Keys.SelectMany(x => x).ToList();
            foreach (var lines in otherLines)
            {
                ParkingLinesService parkingLinesService = new ParkingLinesService();
                var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //根据主车道将副车道分为一节节的
                var classifyLines = ClassifyParkingLines(handleLines, sPt, allMainLines);

                foreach (var pLines in classifyLines)
                {
                    GetParkingLinesEndPt(pLines, out Point3d lineSPt, out Point3d lineEPt);

                    var sLayoutInfo = GetMatchLayoutInfo(lineSPt, resLayoutInfo);
                    var eLayoutInfo = GetMatchLayoutInfo(lineEPt, resLayoutInfo);
                     
                    //获取副车道保护路径的两个端点
                    CalCompareInfo(lineSPt, lineEPt, sLayoutInfo, eLayoutInfo, out Point3d layoutSPt, out Point3d layoutEPt, out Point3d lLineSPt, out Point3d lLineEPt);

                    //计算线上布置点
                    var layoutPts = new List<Point3d>();
                    if (sLayoutInfo != null && sLayoutInfo.Count > 0 && eLayoutInfo != null && eLayoutInfo.Count > 0)
                    {
                        //副车道两端都连接主车道
                        layoutPts = CalLayoutPoints(pLines, lineSPt, lineEPt, layoutSPt, layoutEPt);
                    }
                    else if (sLayoutInfo == null || sLayoutInfo.Count <= 0)
                    {
                        //副车道起点未连接主车道
                        layoutPts = CalLayoutEndPoints(pLines, lineSPt, lineEPt, layoutSPt, layoutEPt, lLineSPt, lLineEPt, true);
                    }
                    else if (eLayoutInfo == null || eLayoutInfo.Count <= 0)
                    {
                        //副车道终点未连接主车道
                       layoutPts =  CalLayoutEndPoints(pLines, lineSPt, lineEPt, layoutSPt, layoutEPt, lLineSPt, lLineEPt, false);
                    }

                    //获取该车道线上的构建
                    StructureService structureService = new StructureService();
                    var lineColumn = structureService.GetStruct(pLines, columns, tol);
                    var lineWall = structureService.GetStruct(pLines, walls, tol);

                    //将构建分为上下部分
                    var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, pLines, tol * 2);
                    var usefulWalls = structureService.SeparateColumnsByLine(lineWall, pLines, tol * 2);

                    //过滤掉不应该使用的构建
                    CheckService checkService = new CheckService();
                    var filterCols = checkService.FilterColumns(usefulColumns[1], lines.First(), frame, lineSPt, lineEPt);
                    var filterWalls = checkService.FilterWalls(usefulWalls[1], lines, tol, exLength);

                    //计算布置信息
                    var dir = (lines.First().EndPoint - lines.First().StartPoint).GetNormal();
                    StructureLayoutService structureLayoutService = new StructureLayoutService();
                    var lInfo = structureLayoutService.GetLayoutStructPt(layoutPts, filterCols, filterWalls, dir);

                    if (lInfo != null && lInfo.Count > 0)
                    {
                        resLayoutInfo.Add(pLines, lInfo);
                    }
                }
            }

            return resLayoutInfo;
        }

        /// <summary>
        /// 根据主车道节点分类副车道
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <param name="mainParikingLines"></param>
        /// <returns></returns>
        private List<List<Line>> ClassifyParkingLines(List<Line> lines, Point3d sPt, List<Line> mainParikingLines)
        {
            List<List<Line>> resLines = new List<List<Line>>();
            List<Line> lineCollection = new List<Line>();
            foreach (var line in lines)
            {
                lineCollection.Add(line);
                if (sPt.DistanceTo(line.StartPoint) < ToleranceService.parkingLineTolerance)
                {
                    sPt = line.EndPoint;
                    var matchRes = mainParikingLines.Where(x => x.StartPoint.DistanceTo(line.EndPoint) < ToleranceService.parkingLineTolerance
                        || x.EndPoint.DistanceTo(line.EndPoint) < ToleranceService.parkingLineTolerance);
                    if (matchRes.Count() > 0)
                    {
                        resLines.Add(new List<Line>(lineCollection));
                        lineCollection.Clear();
                    }
                }
                else if (sPt.DistanceTo(line.EndPoint) < ToleranceService.parkingLineTolerance)
                {
                    sPt = line.StartPoint;
                    var matchRes = mainParikingLines.Where(x => x.StartPoint.DistanceTo(line.StartPoint) < ToleranceService.parkingLineTolerance
                        || x.EndPoint.DistanceTo(line.StartPoint) < ToleranceService.parkingLineTolerance);
                    if (matchRes.Count() > 0)
                    {
                        resLines.Add(new List<Line>(lineCollection));
                        lineCollection.Clear();
                    }
                }
                else
                {
                    resLines.Add(new List<Line>(lineCollection));
                    lineCollection.Clear();
                }
            }

            if (lineCollection.Count > 0)
            {
                resLines.Add(lineCollection);
            }
            return resLines;
        }

        /// <summary>
        /// 获取副车道一端的主车道信息
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="mainLayoutInfo"></param>
        /// <returns></returns>
        private Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> GetMatchLayoutInfo(Point3d pt, Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> mainLayoutInfo)
        {
            var layoutInfo = mainLayoutInfo.Where(x => x.Key
                        .Where(y => y.StartPoint.IsEqualTo(pt, new Tolerance(1, 1)) || y.EndPoint.IsEqualTo(pt, new Tolerance(1, 1))).Count() > 0)
                        .ToDictionary(x => x.Key, y => y.Value);
            return layoutInfo;
        }

        /// <summary>
        /// 获取副车道保护路径的两个端点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sLayoutInfo"></param>
        /// <param name="eLayoutInfo"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <returns></returns>
        private List<Line> CalCompareInfo(Point3d lineSPt, Point3d lineEPt, Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> sLayoutInfo
            , Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> eLayoutInfo, out Point3d sPt, out Point3d ePt, out Point3d layoutSPt, out Point3d layoutEPt)
        {
            layoutSPt = lineSPt;
            layoutEPt = lineEPt;
            sPt = lineSPt;
            ePt = lineEPt;
            if (sLayoutInfo != null && sLayoutInfo.Count() > 0)
            {
                List<Point3d> pts = sLayoutInfo.Values.SelectMany(x => x.Keys).ToList();
                sPt = GetClosetPt(sLayoutInfo.Keys.SelectMany(x => x).ToList(), pts, lineSPt, out layoutSPt);
            }

            if (eLayoutInfo != null && eLayoutInfo.Count() > 0)
            {
                List<Point3d> pts = eLayoutInfo.Values.SelectMany(x => x.Keys).ToList();
                ePt = GetClosetPt(eLayoutInfo.Keys.SelectMany(x => x).ToList(), pts, lineEPt, out layoutEPt);
            }

            return null;
        }

        /// <summary>
        /// 找到线上的布置点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pts"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Point3d GetClosetPt(List<Line> line, List<Point3d> pts, Point3d pt, out Point3d structClosetPt)
        {
            var closetPt = pts.OrderBy(x => x.DistanceTo(pt)).First();
            structClosetPt = closetPt;
            var lineClosetPt = line.Select(x=>x.GetClosestPointTo(closetPt, true)).OrderBy(x => x.DistanceTo(pt)).First();
            return lineClosetPt;
        }

        /// <summary>
        /// 计算布置点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <returns></returns>
        private List<Point3d> CalLayoutPoints(List<Line> lines, Point3d lineSPt, Point3d lineEPt, Point3d sPt, Point3d ePt)
        {
            double sLength = lineSPt.DistanceTo(sPt);
            double eLength = lineEPt.DistanceTo(ePt);
            double allLength = lines.Sum(x => x.Length) + sLength + eLength;

            List<Point3d> resPts = new List<Point3d>();
            List<Line> resLines = new List<Line>();
            var num = Math.Ceiling(allLength / layoutLength) - 1;
            if (num > 0)
            {
                double moveLength = allLength / (num + 1);
                if (!lineSPt.IsEqualTo(sPt, new Tolerance(1, 1)))
                {
                    resLines.Add(new Line(sPt, lineSPt));
                }
                resLines.AddRange(lines);
                if (!lineEPt.IsEqualTo(ePt, new Tolerance(1, 1)))
                {
                    resLines.Add(new Line(lineEPt, ePt));
                }

                ParkingLinesService parkingLinesService = new ParkingLinesService();
                resLines = parkingLinesService.HandleParkingLinesDir(resLines, sPt);
                return GetLayoutPoint(resLines, moveLength, sPt, ePt);
            }

            return new List<Point3d>();
        }

        /// <summary>
        /// 计算布置点（一端未连接）
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="lineSPt"></param>
        /// <param name="lineEPt"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <param name="isStart"></param>
        /// <returns></returns>
        private List<Point3d> CalLayoutEndPoints(List<Line> lines, Point3d lineSPt, Point3d lineEPt, Point3d sPt, Point3d ePt, Point3d structSPt, Point3d structEPt, bool isStart)
        {
            List<Point3d> resPts = new List<Point3d>();
            double sLength = lineSPt.DistanceTo(sPt);
            double eLength = lineEPt.DistanceTo(ePt);
            double allLength = lines.Sum(x => x.Length) + sLength + eLength;
            if (allLength > pLength)
            {
                if (allLength > layoutLength)
                {
                    Vector3d dir = (lineEPt - lineSPt).GetNormal();
                    List<Line> resLines = new List<Line>();
                    if (isStart)
                    {
                        lineSPt = lineSPt + dir * endLength;
                        sPt = lineSPt;
                        resPts.Add(lineSPt);
                        resLines.Add(new Line(lineSPt, lineEPt));
                    }
                    else
                    {
                        if (structEPt.DistanceTo(lineSPt) <= minLength)
                        {
                            return resPts;
                        }
                        lineEPt = lineEPt - dir * endLength;
                        ePt = lineEPt;
                        resPts.Add(lineEPt);
                        resLines.Add(new Line(lineSPt, lineEPt));
                    }
                    resPts.AddRange(CalLayoutPoints(resLines, lineSPt, lineEPt, sPt, ePt));
                }
                else
                {
                    if (isStart)
                    {
                        if (structSPt.DistanceTo(lineEPt) <= minLength)
                        {
                            return resPts;
                        }
                    }
                    else
                    {
                        if (structEPt.DistanceTo(lineSPt) <= minLength)
                        {
                            return resPts;
                        }
                    }
                    resPts.Add(new Point3d((lineSPt.X + lineEPt.X) / 2, (lineSPt.Y + lineEPt.Y) / 2, 0));
                }
            }

            return resPts;
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
            List<Point3d> allPts = new List<Point3d>();
            double excessLength = 0;
            foreach (var line in lines)
            {
                double lineLength = line.Length;
                Vector3d dir = (line.EndPoint - line.StartPoint).GetNormal();

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

            return allPts;
        }

        /// <summary>
        /// 获取车道线组的起点和终点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        private void GetParkingLinesEndPt(List<Line> lines, out Point3d sPt, out Point3d ePt)
        {
            Vector3d xDir = (lines.First().EndPoint - lines.First().StartPoint).GetNormal();
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
                });

            var allPts = lines.SelectMany(x => new List<Point3d>() { x.StartPoint.TransformBy(matrix), x.EndPoint.TransformBy(matrix) })
                .OrderBy(x => x.X)
                .ToList();
            sPt = allPts.First().TransformBy(matrix.Inverse());
            ePt = allPts.Last().TransformBy(matrix.Inverse());
        }
    }
}
