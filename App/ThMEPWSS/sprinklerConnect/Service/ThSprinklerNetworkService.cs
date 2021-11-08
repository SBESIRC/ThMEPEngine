using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Operation.Relate;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;

namespace ThMEPWSS.SprinklerConnect.Service
{
    class ThSprinklerNetworkService
    {

        /// <summary>
        /// find all segments having orthogonal angle in Delaunary Triangulation of points
        /// 找所有正交的德劳内三角线段
        /// </summary>
        /// <param name="points"></param>
        public static List<Line> FindOrthogonalAngleFromDT(Point3dCollection points, out List<Line> dtSeg)
        {
            var angleTol = 1;
            List<Line> dtOrthogonalSeg = new List<Line>();

            var dtLine = points.DelaunayTriangulation();
            var dtPls = dtLine.Cast<Polyline>().ToList();
            var dtLinesAll = DTToLines(dtPls);

            foreach (Point3d pt in points)
            {
                var ptLines = GetTriLineOfPt(pt, dtLinesAll);
                if (ptLines.Count > 0)
                {
                    for (int i = 0; i < ptLines.Count; i++)
                    {
                        for (int j = i + 1; j < ptLines.Count; j++)
                        {
                            if (IsOrthogonalAngle(ptLines[i].Angle, ptLines[j].Angle, angleTol))
                            {
                                dtOrthogonalSeg.Add(ptLines[i]);
                                dtOrthogonalSeg.Add(ptLines[j]);
                            }
                        }
                    }
                }
            }

            dtOrthogonalSeg = dtOrthogonalSeg.Distinct().ToList();
            dtSeg = dtLinesAll.Distinct().ToList();

            DrawUtils.ShowGeometry(dtSeg, "l0DT", 154);
            DrawUtils.ShowGeometry(dtOrthogonalSeg, "l0DTlins", 1);

            return dtOrthogonalSeg;
        }

        private static List<Line> GetTriLineOfPt(Point3d pt, List<Line> dtLines)
        {
            var tol = new Tolerance(10, 10);

            var ptLines = dtLines.Where(x => x.StartPoint.IsEqualTo(pt, tol) || x.EndPoint.IsEqualTo(pt, tol)).ToList();

            return ptLines;
        }

        /// <summary>
        /// 判断角A角B是否正交。角A角B弧度制
        /// tol:角度容差（角度制），数值大于0 小于90
        /// </summary>
        /// <param name="angleA"></param>
        /// <param name="angleB"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static bool IsOrthogonalAngle(double angleA, double angleB, double tol)
        {
            var bReturn = false;
            var angleDelta = angleA - angleB;
            var cosAngle = Math.Abs(Math.Cos(angleDelta));

            if (cosAngle > Math.Cos(tol * Math.PI / 180) || cosAngle < Math.Cos((90 - tol) * Math.PI / 180))
            {
                bReturn = true;
            }

            return bReturn;
        }

        private static List<Line> DTToLines(List<Polyline> dtPls)
        {
            var tol = new Tolerance(10, 10);
            var dtLines = new List<Line>();
            var dtPts = new List<(Point3d, Point3d)>();

            foreach (var dtPoly in dtPls)
            {
                for (int i = 0; i < dtPoly.NumberOfVertices; i++)
                {
                    var pt1 = dtPoly.GetPoint3dAt(i % dtPoly.NumberOfVertices);
                    var pt2 = dtPoly.GetPoint3dAt((i + 1) % dtPoly.NumberOfVertices);

                    var inList = dtPts.Where(x => (x.Item1.IsEqualTo(pt1, tol) && x.Item2.IsEqualTo(pt2, tol)) ||
                                                    (x.Item1.IsEqualTo(pt2, tol) && x.Item2.IsEqualTo(pt1, tol)));

                    if (inList.Count() == 0 && pt1.IsEqualTo(pt2, tol) == false)
                    {
                        dtPts.Add((pt1, pt2));
                    }
                }
            }

            dtPts.ForEach(x => dtLines.Add(new Line(x.Item1, x.Item2)));

            return dtLines;
        }


        public static double GetDTLength(List<Line> dtOrthogonalSeg)
        {
            var length = 2500.0;

            var lengthGroup = dtOrthogonalSeg.GroupBy(x => x.Length).ToDictionary(g => g.Key, g => g.ToList()).OrderByDescending(x => x.Value.Count).ToList();

            var averageC = 0;
            var lengthTemp = 0.0;

            for (int i = 0; i < 3; i++)
            {
                if (i <= lengthGroup.Count())
                {
                    lengthTemp = lengthTemp + lengthGroup[i].Key;
                    averageC = averageC + 1;
                }
            }
            if (lengthTemp > 0)
            {
                length = lengthTemp / averageC;
            }


            return length;
        }

        /// <summary>
        /// 容差1角度制内角度分类。可能有容差带来的累计错误
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <returns></returns>
        public static List<KeyValuePair<double, List<Line>>> ClassifyOrthogonalSeg(List<Line> dtOrthogonalSeg)
        {
            var angleTol = 1;
            var angleGroupDict = new Dictionary<double, List<Line>>();

            var angleGroupTemp = dtOrthogonalSeg.GroupBy(x => x.Angle).ToDictionary(g => g.Key, g => g.ToList()).OrderByDescending(x => x.Value.Count).ToDictionary(x => x.Key, x => x.Value);
            angleGroupDict.Add(angleGroupTemp.ElementAt(0).Key, angleGroupTemp.ElementAt(0).Value);

            for (int i = 1; i < angleGroupTemp.Count; i++)
            {
                var angleA = angleGroupTemp.ElementAt(i).Key;
                var bAdded = false;
                for (int j = 0; j < angleGroupDict.Count; j++)
                {
                    var angleB = angleGroupDict.ElementAt(j).Key;

                    if (IsOrthogonalAngle(angleA, angleB, angleTol))
                    {
                        angleGroupDict[angleB].AddRange(angleGroupTemp[angleA]);
                        bAdded = true;
                        break;
                    }
                }
                if (bAdded == false)
                {
                    angleGroupDict.Add(angleA, angleGroupTemp[angleA]);
                }
            }

            var angleGroup = angleGroupDict.ToList();

            return angleGroup;
        }


        public static void AddSingleDTLineToGroup(List<Line> dtSeg, List<KeyValuePair<double, List<Line>>> angleGroup)
        {
            var angleTol = 1;
            var dtSegNotIn = dtSeg.Where(x => angleGroup.Where(g => g.Value.Contains(x)).Count() == 0).ToList();

            for (int i = 0; i < dtSegNotIn.Count; i++)
            {
                addLineToGroup(dtSegNotIn[i], ref angleGroup, angleTol);
            }
        }

        public static void AddSinglePTToGroup(List<Line> dtLines, List<KeyValuePair<double, List<Line>>> group, List<Point3d> pts, double lengthTol)
        {
            var angleTol = 1;
            var newAddedline = new List<Line>();
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var nearPts = pts.Where(x => x.DistanceTo(pt) <= lengthTol && x != pt).OrderBy(x => x.DistanceTo(pt)).ToList();

                if (848620 <= pt.X && pt.X <= 848622 && 379571 <= pt.Y && pt.Y <= 379573)
                {
                    var a = "debug";
                }

                for (int j = 0; j < nearPts.Count; j++)
                {
                    var nearPt = nearPts[j];
                    var newLine = new Line(pt, nearPt);

                    var overlapDT = dtLines.Where(x => overlapLine(x, newLine) == true);
                    var overlapTempGroup = newAddedline.Where(x => overlapLine(x, newLine) == true);

                    if (overlapDT.Count() == 0 && overlapTempGroup.Count() == 0)
                    {
                        var bAdd = addLineToGroup(newLine, ref group, angleTol);
                        if (bAdd == true)
                        {
                            newAddedline.Add(newLine);
                        }
                    }
                }
            }
        }




        private static bool overlapLine(Line A, Line B)
        {
            var bReturn = false;
            var matrix = RelateOp.Relate(A.ToNTSLineString(), B.ToNTSLineString());
            var r1 = matrix.IsCrosses(NetTopologySuite.Geometries.Dimension.Surface, NetTopologySuite.Geometries.Dimension.Surface);
            var r2 = matrix.IsOverlaps(NetTopologySuite.Geometries.Dimension.Surface, NetTopologySuite.Geometries.Dimension.Surface);
            var r3 = matrix.IsContains();
            var r4 = matrix.IsCoveredBy();
            var r5 = matrix.IsTouches(NetTopologySuite.Geometries.Dimension.Surface, NetTopologySuite.Geometries.Dimension.Surface);

            if (r1 || r2 || r3 || r4 || r5)
            {
                bReturn = true;
            }

            return bReturn;
        }

        public static List<KeyValuePair<double, List<Line>>> SeparateGroupDist(List<KeyValuePair<double, List<Line>>> angleGroup, double tol)
        {
            var angleGroupTemp = new List<KeyValuePair<double, List<Line>>>();






            return angleGroupTemp;
        }



        private static bool addLineToGroup(Line line, ref List<KeyValuePair<double, List<Line>>> angleGroup, double angleTol)
        {
            var bAdd = false;
            var angleA = line.Angle;

            for (int j = 0; j < angleGroup.Count; j++)
            {
                var angleB = angleGroup[j].Key;

                if (IsOrthogonalAngle(angleA, angleB, angleTol))
                {
                    angleGroup[j].Value.Add(line);
                    bAdd = true;
                    break;
                }
            }
            return bAdd;
        }


        /// <summary>
        /// 删掉除了最多线以外组里出现过的线（删斜线）
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <returns></returns>
        public static List<KeyValuePair<double, List<Line>>> FilterMargedGroup(List<KeyValuePair<double, List<Line>>> angleGroup)
        {
            var tol = new Tolerance(10, 10);
            var filterGroup = new List<KeyValuePair<double, List<Line>>>();
            var groupPt = new List<Point3d>();

            angleGroup = angleGroup.OrderByDescending(x => x.Value.Count).ToList();
            filterGroup.Add(new KeyValuePair<double, List<Line>>(angleGroup[0].Key, angleGroup[0].Value));
            groupPt.AddRange(lineListToPtList(angleGroup[0].Value));

            for (int i = 1; i < angleGroup.Count; i++)
            {
                var lineList = angleGroup[i].Value;

                for (int j = lineList.Count - 1; j >= 0; j--)
                {
                    var startInList = groupPt.Where(x => x.IsEqualTo(lineList[j].StartPoint, tol));
                    var endInList = groupPt.Where(x => x.IsEqualTo(lineList[j].EndPoint, tol));

                    //if (startInList.Count() > 0 || endInList.Count() > 0) 同时在两个组里的点会被删掉
                    if (startInList.Count() > 0 && endInList.Count() > 0)
                    {
                        lineList.RemoveAt(j);
                    }
                }
                if (lineList.Count > 0)
                {
                    filterGroup.Add(new KeyValuePair<double, List<Line>>(angleGroup.ElementAt(i).Key, lineList));
                    groupPt.AddRange(lineListToPtList(lineList));
                }
            }

            return filterGroup;
        }

        private static List<Point3d> lineListToPtList(List<Line> lines)
        {
            var tol = new Tolerance(10, 10);
            var ptList = new List<Point3d>();
            for (int i = 0; i < lines.Count; i++)
            {
                var startInList = ptList.Where(x => x.IsEqualTo(lines[i].StartPoint, tol));
                if (startInList.Count() == 0)
                {
                    ptList.Add(lines[i].StartPoint);
                }
                var endInList = ptList.Where(x => x.IsEqualTo(lines[i].EndPoint, tol));
                if (endInList.Count() == 0)
                {
                    ptList.Add(lines[i].EndPoint);
                }
            }

            return ptList;
        }




















        /// <summary>
        /// 使用德劳内三角划分进行初步链接
        /// </summary>
        /// <param name="points"></param>
        public static void DelaunayTriangulationConnect(Point3dCollection points)
        {
            HashSet<Line> lines = new HashSet<Line>();
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0 ：初始化 1：最长线 

            var dtLine = points.DelaunayTriangulation();
            foreach (Entity diagram in dtLine)
            {
                if (diagram is Polyline pl)
                {
                    Line maxLine = new Line();
                    double maxLen = 0.0;
                    for (int i = 0; i < pl.NumberOfVertices - 1; ++i) // pl.NumberOfVertices == 4
                    {
                        Line line = new Line(pl.GetPoint3dAt(i), pl.GetPoint3dAt(i + 1));
                        linesType.Add(new Tuple<Point3d, Point3d>(line.StartPoint, line.EndPoint), 0);
                        lines.Add(line);
                        if (line.Length > maxLen)
                        {
                            maxLen = line.Length;
                            maxLine = line;
                        }
                    }
                    linesType[new Tuple<Point3d, Point3d>(maxLine.StartPoint, maxLine.EndPoint)] = 1;
                    if (linesType.ContainsKey(new Tuple<Point3d, Point3d>(maxLine.EndPoint, maxLine.StartPoint)))
                    {
                        linesType[new Tuple<Point3d, Point3d>(maxLine.EndPoint, maxLine.StartPoint)] = 1;
                    }
                }
            }

            DrawUtils.ShowGeometry(dtLine.Cast<Polyline>().ToList(), "l0dt", 154);

            foreach (var line in linesType.Keys)
            {
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0)
                {
                    DrawUtils.ShowGeometry(new Line(line.Item1, line.Item2), "l0DTlins", 1);

                }
            }
        }

        public static void VoronoiDiagramConnect(Point3dCollection points)
        {
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); //通过有方向的某根线找到其包围的点
            Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines = new Dictionary<Point3d, List<Tuple<Point3d, Point3d>>>(); // 通过柱中点找到柱维诺图边界的点

            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());
            var voronoiPoly = voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory);

            foreach (Polygon polygon in voronoiPoly)
            {
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt))
                    {
                        List<Tuple<Point3d, Point3d>> aroundLines = new List<Tuple<Point3d, Point3d>>();
                        for (int i = 0; i < polyline.NumberOfVertices - 1; ++i)
                        {
                            Tuple<Point3d, Point3d> border = new Tuple<Point3d, Point3d>(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1));
                            line2pt.Add(border, pt);
                            aroundLines.Add(border);
                        }
                        if (!pt2lines.ContainsKey(pt))
                        {
                            pt2lines.Add(pt, aroundLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList());
                        }
                        break;
                    }
                }
            }

            HashSet<Tuple<Point3d, Point3d>> connectLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (Point3d pt in points)
            {
                //HostApplicationServices.WorkingDatabase.AddToModelSpace(pt2Polygon[pt].ToDbEntity());//
                connectNaighbor(pt, pt2lines, line2pt, connectLines);
            }

            voronoiPoly.Cast<Polygon>().ForEach(x => DrawUtils.ShowGeometry(x.ToDbPolylines().First(), "l0voronoi", 154));


            foreach (var line in connectLines)
            {
                if (connectLines.Contains(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))) //是否双线
                {
                    DrawUtils.ShowGeometry(new Line(line.Item1, line.Item2), "l0VoronoiLins", 1);
                }
            }
        }

        /// <summary>
        /// 容差0.5角度制内角度分类。可能有容差带来的累计错误
        /// </summary>
        /// <param name="dtOrthogonalSeg"></param>
        /// <returns></returns>
        private static Dictionary<double, int> countAngle(List<Line> dtOrthogonalSeg)
        {
            var angleTol = 0.5;
            var angleGroup = new Dictionary<double, int>();

            for (int i = 0; i < dtOrthogonalSeg.Count; i++)
            {
                var bAdded = false;
                for (int j = 0; j < angleGroup.Keys.Count; j++)
                {
                    if (IsOrthogonalAngle(dtOrthogonalSeg[i].Angle, angleGroup[j], angleTol))
                    {
                        var count = angleGroup[j];
                        angleGroup[j] = count + 1;
                        bAdded = true;
                        break;
                    }
                }
                if (bAdded == false)
                {
                    angleGroup.Add(dtOrthogonalSeg[i].Angle, 1);
                }
            }

            return angleGroup;
        }

        /// <summary>
        /// 当前点链接周围的点
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pt2lines"></param>
        /// <param name="line2pt"></param>
        /// <param name="connectLines"></param>
        private static void connectNaighbor(Point3d point, Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines, Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt, HashSet<Tuple<Point3d, Point3d>> connectLines)
        {
            int cnt = 0;
            if (pt2lines.ContainsKey(point))
            {
                foreach (Tuple<Point3d, Point3d> line in pt2lines[point])
                {
                    Tuple<Point3d, Point3d> conversLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                    if (cnt > 3 || !line2pt.ContainsKey(conversLine))
                    {
                        break;
                    }
                    Tuple<Point3d, Point3d> connectLine = new Tuple<Point3d, Point3d>(point, line2pt[conversLine]);

                    connectLines.Add(connectLine);

                    //draw_line(point, line2pt[conversLine], 130);
                    ++cnt;
                }
            }
        }


    }
}
