﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Broadcast.Service;

namespace ThMEPElectrical.Broadcast
{
    public class LayoutWithSecondaryParkingLineService
    {
        readonly double pLength = 25000;  //超过25米副车道要补点
        readonly double tol = 5000;

        public Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> LayoutBraodcast(Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> mainLayoutInfo
            , List<List<Line>> otherLines, List<Polyline> columns, List<Polyline> walls)
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
                    CalCompareInfo(lineSPt, lineEPt, sLayoutInfo, eLayoutInfo, out Point3d layoutSPt, out Point3d layoutEPt);

                    //计算线上布置点
                    var layoutPts = CalLayoutPoints(pLines, lineSPt, lineEPt, layoutSPt, layoutEPt);

                    //获取该车道线上的构建
                    StructureService structureService = new StructureService();
                    var lineColumn = structureService.GetStruct(pLines, columns, tol);
                    var lineWall = structureService.GetStruct(pLines, walls, tol);

                    //将构建分为上下部分
                    var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, lines.First());
                    var usefulWalls = structureService.SeparateColumnsByLine(lineWall, lines.First());

                    //计算布置信息
                    var dir = (lines.First().EndPoint - lines.First().StartPoint).GetNormal();
                    StructureLayoutService structureLayoutService = new StructureLayoutService();
                    var lInfo = structureLayoutService.GetLayoutStructPt(layoutPts, usefulColumns[1], usefulWalls[1], dir);

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
                if (sPt.IsEqualTo(line.StartPoint))
                {
                    sPt = line.EndPoint;
                    var matchRes = mainParikingLines.Where(x => x.StartPoint.IsEqualTo(line.EndPoint) || x.EndPoint.IsEqualTo(line.EndPoint));
                    if (matchRes.Count() > 0)
                    {
                        resLines.Add(new List<Line>(lineCollection));
                        lineCollection.Clear();
                    }
                }
                else if (sPt.IsEqualTo(line.EndPoint))
                {
                    sPt = line.StartPoint;
                    var matchRes = mainParikingLines.Where(x => x.StartPoint.IsEqualTo(line.StartPoint) || x.EndPoint.IsEqualTo(line.StartPoint));
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
            , Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> eLayoutInfo, out Point3d sPt, out Point3d ePt)
        {
            sPt = lineSPt;
            ePt = lineEPt;
            if (sLayoutInfo != null && sLayoutInfo.Count() > 0)
            {
                List<Point3d> pts = sLayoutInfo.Values.SelectMany(x => x.Keys).ToList();
                sPt = GetClosetPt(sLayoutInfo.Keys.SelectMany(x => x).ToList(), pts, lineSPt);
            }

            if (eLayoutInfo != null && eLayoutInfo.Count() > 0)
            {
                List<Point3d> pts = eLayoutInfo.Values.SelectMany(x => x.Keys).ToList();
                ePt = GetClosetPt(eLayoutInfo.Keys.SelectMany(x => x).ToList(), pts, lineEPt);
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
        private Point3d GetClosetPt(List<Line> line, List<Point3d> pts, Point3d pt)
        {
            var closetPt = pts.OrderBy(x => x.DistanceTo(pt)).First();
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

            List<Line> resLines = new List<Line>();
            var num = Math.Ceiling(allLength / pLength) - 1;
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

                while (lineLength > moveLength || (excessLength > 0 && lineLength > excessLength))
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
