using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Geometries;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class StructureDealer
    {
        /// <summary>
        /// Connect 4 dege diagram by VoronoiDiagram way
        /// </summary>
        /// <param name="points"></param>
        public static HashSet<Tuple<Point3d, Point3d>> VoronoiDiagramConnect(Point3dCollection points, Dictionary<Polyline, Point3dCollection> poly2points = null)
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
            HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in connectLines)
            {
                if (connectLines.Contains(new Tuple<Point3d, Point3d>(line.Item2, line.Item1))) //judge double edge
                {
                    tuples.Add(line);
                }
            }
            return tuples;
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
        /// Connect 4 dege diagram by ConformingDelaunayTriangulation way
        /// </summary>
        /// <param name="points"></param>
        /// <param name="polylines">constrain</param>
        public static void ConformingDelaunayTriangulationConnect(Point3dCollection points, MultiLineString polylines)
        {
            var objs = new DBObjectCollection();
            var builder = new ConformingDelaunayTriangulationBuilder();
            var sites = ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPointFromCoords(points.ToNTSCoordinates());
            builder.SetSites(sites);
            builder.Constraints = polylines;
            //builder.Tolerance = 500;
            var triangles = builder.GetTriangles(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach (var geometry in triangles.Geometries)
            {
                if (geometry is Polygon polygon)
                {
                    objs.Add(polygon.Shell.ToDbPolyline());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            HashSet<Line> lines = new HashSet<Line>();
            Dictionary<Tuple<Point3d, Point3d>, int> linesType = new Dictionary<Tuple<Point3d, Point3d>, int>(); //0：init 1：lognest line 
            foreach (Entity diagram in objs)
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
            foreach (var line in linesType.Keys)
            {
                //if (linesType[line] == 0 )//&& linesType.ContainsKey(new Tuple<Point3d, Point3d>(line.Item2, line.Item1)) && linesType[new Tuple<Point3d, Point3d>(line.Item2, line.Item1)] == 0)
                {
                    ShowInfo.DrawLine(line.Item1, line.Item2, 130);
                }
            }
        }

        /// <summary>
        /// reduce degree up to 4 for each point(删除最不符合90度的那个)
        /// 删除的时候，如果删除点的对点是外边线上的并且只有这一个连线，则不能删掉，其他都可删
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outline2BorderNearPts"></param>
        public static void DeleteConnectUpToFourB(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!dicTuples.ContainsKey(key))
                {
                    continue;
                }
                var value = dicTuples[key];
                int n = value.Count;
                while (n > 4)
                {
                    List<Point3d> cntPts = value.ToList();
                    Vector3d baseVec = cntPts[0] - key;
                    cntPts = cntPts.OrderBy(pt => (pt - key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        curDegree = (cntPts[i % n] - key).GetAngleTo(cntPts[i - 1] - key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    Point3d rmPt = new Point3d();
                    if (!IsCoreLine(key, minDegreePairPt.Item1, dicTuples))
                    {
                        rmPt = minDegreePairPt.Item1;
                    }
                    else if (!IsCoreLine(key, minDegreePairPt.Item2, dicTuples))
                    {
                        rmPt = minDegreePairPt.Item2;
                    }
                    --n;
                    if (rmPt == new Point3d() || !dicTuples.ContainsKey(rmPt))
                    {
                        continue;
                    }
                    bool flag = false;
                    foreach (var outline2BorderNearPt in outline2BorderNearPts)
                    {
                        if (outline2BorderNearPt.Value.ContainsKey(rmPt) && outline2BorderNearPt.Value[rmPt].Count == 1)
                        {
                            flag = true;
                            break;
                        }
                    }
                    //if (flag == true)
                    if (flag == true || dicTuples[rmPt].Count <= 2)
                    {
                        continue;
                    }
                    value.Remove(rmPt);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(key) && borderPt2NearPts[key].Contains(rmPt))
                        {
                            borderPt2NearPts[key].Remove(rmPt);
                            if (borderPt2NearPts[key].Count == 0)
                            {
                                borderPt2NearPts.Remove(key);
                            }
                        }
                    }
                    DicTuplesDealer.DeleteFromDicTuples(rmPt, key, ref dicTuples);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(rmPt) && borderPt2NearPts[rmPt].Contains(key))
                        {
                            borderPt2NearPts[rmPt].Remove(key);
                            if (borderPt2NearPts[rmPt].Count == 0)
                            {
                                borderPt2NearPts.Remove(key);
                            }
                        }
                    }
                }
            }
        }
        public static bool IsCoreLine(Point3d baseFromPt, Point3d baseToPt, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(baseFromPt) || !dicTuples[baseFromPt].Contains(baseToPt))
            {
                return false;
            }
            var baseVec = baseToPt - baseFromPt;
            int cnt = 0;
            foreach (var pt in dicTuples[baseFromPt])
            {
                var curAngel = (pt - baseFromPt).GetAngleTo(baseVec);
                if (Math.Abs(curAngel - Math.PI / 2) < Math.PI / 15 || (Math.Abs(curAngel - Math.PI) < Math.PI / 15))
                {
                    cnt++;
                }
            }
            if (cnt >= 2)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Close a polyline by its border points
        /// 注意：要考虑最外边框和包含型边框的区别
        /// </summary>
        public static HashSet<Tuple<Point3d, Point3d>> CloseBorderA(List<Polyline> polylines, List<Point3d> oriPoints)
        {
            var outline2BorderPts = PointsDealer.GetOutline2BorderPts(polylines, oriPoints);
            HashSet<Point3d> ptVisit = new HashSet<Point3d>();
            HashSet<Tuple<Point3d, Point3d>> ansTuple = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var dic in outline2BorderPts)
            {
                Polyline polyline = dic.Key;
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
                for (int i = 1; i <= points.Count; i++)
                {
                    if (!ptVisit.Contains(points[i % points.Count]))
                    {
                        ptVisit.Add(points[i % points.Count]);
                        ansTuple.Add(new Tuple<Point3d, Point3d>(points[i % points.Count], points[i - 1]));
                    }
                }
            }
            return ansTuple;
        }

        /// <summary>
        /// 在线集中删除和指定类型线相交的线
        /// </summary>
        public static void RemoveLinesInterSectWithCloseBorderLines(List<Tuple<Point3d, Point3d>> closebdLines, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var tmpTuples = LineDealer.UnifyTuples(dicTuples);
            var tuple2deduce = new Dictionary<Tuple<Point3d, Point3d>, Tuple<Point3d, Point3d>>();
            tmpTuples.ForEach(t => tuple2deduce.Add(t, LineDealer.ReduceTuple(t, 200)));
            foreach (var tup in closebdLines)
            {
                if (tup.Item1.DistanceTo(tup.Item2) > 20000)
                {
                    continue;
                }
                var tupA = LineDealer.ReduceTuple(tup, 200);
                foreach (var tmpTuple in tmpTuples)
                {
                    if (LineDealer.IsIntersect(tupA.Item1, tupA.Item2, tuple2deduce[tmpTuple].Item1, tuple2deduce[tmpTuple].Item2))
                    {
                        DicTuplesDealer.DeleteFromDicTuples(tmpTuple.Item1, tmpTuple.Item2, ref dicTuples);
                    }
                }
            }
        }
        public static void RemoveLinesInterSectWithImportantLines(Dictionary<Point3d, HashSet<Point3d>> importantLines, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var tmpTuples = LineDealer.UnifyTuples(dicTuples);
            var tuple2deduce = new Dictionary<Tuple<Point3d, Point3d>, Tuple<Point3d, Point3d>>();
            tmpTuples.ForEach(t => tuple2deduce.Add(t, LineDealer.ReduceTuple(t, 200)));
            foreach (var dicLines in importantLines)
            {
                foreach(var pt in dicLines.Value)
                {
                    var tup = new Tuple<Point3d, Point3d>(dicLines.Key, pt);
                    if (tup.Item1.DistanceTo(tup.Item2) > 20000)
                    {
                        continue;
                    }
                    var tupA = LineDealer.ReduceTuple(tup, 200);
                    foreach (var tmpTuple in tmpTuples)
                    {
                        if (LineDealer.IsIntersect(tupA.Item1, tupA.Item2, tuple2deduce[tmpTuple].Item1, tuple2deduce[tmpTuple].Item2))
                        {
                            DicTuplesDealer.DeleteFromDicTuples(tmpTuple.Item1, tmpTuple.Item2, ref dicTuples);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在线集中删除和多边形相交的线
        /// </summary>
        public static void RemoveLinesInterSectWithOutlines(List<Polyline> outlines, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var tmpTuples = LineDealer.UnifyTuples(dicTuples);
            var tuple2reduce = new Dictionary<Tuple<Point3d, Point3d>, Tuple<Point3d, Point3d>>();
            tmpTuples.ForEach(t => { if(t.Item1.DistanceTo(t.Item2) > 2300) tuple2reduce.Add(t, LineDealer.ReduceTuple(t, 800)); });
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
                            DicTuplesDealer.DeleteFromDicTuples(tmpTuple.Item1, tmpTuple.Item2, ref dicTuples);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除掉和外边框偏转角度30度内的多边形
        /// </summary>
        public static void RemoveLinesNearOutlines(List<Polyline> outlines, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double redius = 1000)
        {
            var pt1s = dicTuples.Keys.ToList();
            var redius2 = redius * 1.6;
            var redius4 = redius * 4;

            foreach (var pt1 in pt1s)
            {
                if (dicTuples.ContainsKey(pt1))
                {
                    var pt2s = dicTuples[pt1].ToList();
                    foreach(var pt2 in pt2s)
                    {
                        if (dicTuples[pt1].Contains(pt2) && pt1.DistanceTo(pt2) > redius4)
                        {
                            var dir = (pt2 - pt1).GetNormal();
                            Circle circle1 = new Circle(pt1 + dir * redius2, Vector3d.ZAxis, redius);
                            Circle circle2 = new Circle(pt2 - dir * redius2, Vector3d.ZAxis, redius);
                            foreach(var pl in outlines)
                            {
                                if (pl.Intersects(circle1) || pl.Intersects(circle2))
                                {
                                    DicTuplesDealer.DeleteFromDicTuples(pt1, pt2, ref dicTuples);
                                    //ShowInfo.DrawLine(pt1, pt2, 6);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static int ContainLines(List<Tuple<Point3d, Point3d>> oriTuples, Dictionary<Point3d, Point3d> closeBorderLines)
        {
            int cnt = 0;
            foreach (var oriTuple in oriTuples)
            {
                if (closeBorderLines.ContainsKey(oriTuple.Item1) && closeBorderLines[oriTuple.Item1] == oriTuple.Item2)
                {
                    ++cnt;
                }
            }
            return cnt;
        }
    }
}
