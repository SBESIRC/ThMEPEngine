using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Geometries;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class StructureDealer
    {
        /// <summary>
        /// Connect 4 dege diagram by VoronoiDiagram way
        /// </summary>
        /// <param name="points"></param>
        public static void VoronoiDiagramConnect(Point3dCollection points, ref Dictionary<Point3d, HashSet<Point3d>> graph,Dictionary<Polyline, Point3dCollection> poly2points = null)
        {
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt = new Dictionary<Tuple<Point3d, Point3d>, Point3d>(); //Find a point by its surrounding line
            Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines = new Dictionary<Point3d, List<Tuple<Point3d, Point3d>>>(); //Find surrounding lines by the point in them

            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());
            //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries)
            foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
            {
                if (polygon.IsEmpty)
                {
                    continue;
                }
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt) && !pt2lines.ContainsKey(pt))
                    {
                        if (poly2points != null)
                        {
                            foreach (var houseBorder in poly2points.Keys)
                            {
                                if (houseBorder.Intersects(polyline))
                                {
                                    poly2points[houseBorder].Add(pt);
                                    break;
                                }
                            }
                        }

                        List<Tuple<Point3d, Point3d>> aroundLines = new List<Tuple<Point3d, Point3d>>();
                        for (int i = 0; i < polyline.NumberOfVertices - 1; ++i)
                        {
                            Tuple<Point3d, Point3d> border = new Tuple<Point3d, Point3d>(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1));
                            if (line2pt.ContainsKey(border))
                            {
                                continue;
                            }
                            line2pt.Add(border, pt);
                            aroundLines.Add(border);
                        }
                        pt2lines.Add(pt, aroundLines.OrderByDescending(l => l.Item1.DistanceTo(l.Item2)).ToList());
                        break;
                    }
                }
            }

            HashSet<Tuple<Point3d, Point3d>> connectLines = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (Point3d pt in points)
            {
                ConnectNeighbor(pt, pt2lines, line2pt, connectLines);
            }
            foreach (var line in connectLines)
            {
                if (connectLines.Contains(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))) //judge double edge
                {
                    GraphDealer.AddLineToGraph(line.Item2, line.Item1, ref graph);
                }
            }
        }

        /// <summary>
        /// Find naighbor points
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pt2lines"></param>
        /// <param name="line2pt"></param>
        /// <param name="connectLines"></param>
        public static void ConnectNeighbor(Point3d point, Dictionary<Point3d, List<Tuple<Point3d, Point3d>>> pt2lines,
            Dictionary<Tuple<Point3d, Point3d>, Point3d> line2pt, HashSet<Tuple<Point3d, Point3d>> connectLines)
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
                    ++cnt;
                }
            }
        }


        /// <summary>
        /// Connect 4 dege diagram by DelaunayTriangulation way
        /// </summary>
        /// <param name="points"></param>
        public static HashSet<Tuple<Point3d, Point3d>> DelaunayTriangulationConnect(Point3dCollection points)
        {
            HashSet<Line> lines = new HashSet<Line>();
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0：init 1：lognest line 
            foreach (Entity diagram in points.DelaunayTriangulation())
            {
                if (diagram is Polyline pl)
                {
                    Line maxLine = new Line();
                    double maxLen = 0.0;
                    for (int i = 0; i < pl.NumberOfVertices - 1; ++i)
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

            HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in linesType.Keys)
            {
                if (linesType[line] == 0 && linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))
                    && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0 && !tuples.Contains(line))
                {
                    tuples.Add(line);
                }
            }
            return tuples;
        }

        /// <summary>
        /// 在线集中删除和多边形相交的线
        /// </summary>
        public static void RemoveLinesInterSectWithOutlines(List<Polyline> outlines, ref Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            var tmpTuples = LineDealer.UnifyTuples(graph);
            var tuple2reduce = new Dictionary<Tuple<Point3d, Point3d>, Tuple<Point3d, Point3d>>();
            tmpTuples.ForEach(t => { if (t.Item1.DistanceTo(t.Item2) > 2300) tuple2reduce.Add(t, LineDealer.ReduceTuple(t, 800)); });
            foreach (var tmpTuple in tmpTuples)
            {
                if (tuple2reduce.ContainsKey(tmpTuple))
                {
                    Line reducedLine = new Line(tuple2reduce[tmpTuple].Item1, tuple2reduce[tmpTuple].Item2);
                    Point3d middlePt = new Point3d((tuple2reduce[tmpTuple].Item1.X + tuple2reduce[tmpTuple].Item2.X) / 2, (tuple2reduce[tmpTuple].Item1.Y + tuple2reduce[tmpTuple].Item2.Y) / 2, 0);
                    Circle circle = new Circle(middlePt, Vector3d.ZAxis, 1000);
                    foreach (var outline in outlines)
                    {
                        if (outline.Intersects(reducedLine) || outline.Intersects(circle))
                        {
                            GraphDealer.DeleteFromGraph(tmpTuple.Item1, tmpTuple.Item2, ref graph);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除掉和外边框偏转角度30度内的多边形
        /// </summary>
        public static void RemoveLinesNearOutlines(List<Polyline> outlines, ref Dictionary<Point3d, HashSet<Point3d>> graph, double redius = 1000)
        {
            var pt1s = graph.Keys.ToList();
            var redius2 = redius * 1.6;
            var redius4 = redius * 4;

            foreach (var pt1 in pt1s)
            {
                if (graph.ContainsKey(pt1))
                {
                    var pt2s = graph[pt1].ToList();
                    foreach (var pt2 in pt2s)
                    {
                        if (graph[pt1].Contains(pt2) && pt1.DistanceTo(pt2) > redius4)
                        {
                            var dir = (pt2 - pt1).GetNormal();
                            Circle circle1 = new Circle(pt1 + dir * redius2, Vector3d.ZAxis, redius);
                            Circle circle2 = new Circle(pt2 - dir * redius2, Vector3d.ZAxis, redius);
                            foreach (var pl in outlines)
                            {
                                if (pl.Intersects(circle1) || pl.Intersects(circle2))
                                {
                                    GraphDealer.DeleteFromGraph(pt1, pt2, ref graph);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Close a polyline by its border points
        /// 注意：要考虑最外边框和包含型边框的区别
        /// </summary>
        public static void CloseBorder(List<Polyline> polylines, HashSet<Point3d> oriPoints, 
            ref Dictionary<Polyline, List<Tuple<Point3d, Point3d>>> closeBorderLine)
        {
            foreach(var pl in polylines)
            {
                for(int i = 0; i < pl.NumberOfVertices; ++i)
                {
                    var pt = pl.GetPoint3dAt(i);
                    if (!oriPoints.Contains(pt))
                    {
                        oriPoints.Add(pt);
                    }
                }
            }

            var outline2BorderPts = PointsDealer.GetOutline2BorderPts(polylines, oriPoints);
            foreach (var dic in outline2BorderPts)
            {
                Polyline polyline = dic.Key;
                closeBorderLine.Add(polyline, new List<Tuple<Point3d, Point3d>>());
                List<Point3d> points = new List<Point3d>();
                int n = polyline.NumberOfVertices;
                for (int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach (var borderPt in dic.Value)
                    {
                        if (borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 700)
                        {
                            tmpPts.Add(borderPt);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                for (int i = 1; i <= points.Count; ++i)
                {
                    {
                        closeBorderLine[polyline].Add(new Tuple<Point3d, Point3d>(points[i % points.Count], points[(i + points.Count - 1) % points.Count]));
                    }
                }
            }
        }
    }
}
