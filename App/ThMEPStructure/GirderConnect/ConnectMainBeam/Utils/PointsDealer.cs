using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using AcHelper;
using ThCADExtension;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class PointsDealer
    {
        /// <summary>
        /// Classify points on polyline by in or out
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="PointClass"></param>
        /// <returns></returns>
        public static void PointClassify(Polyline polyline, Dictionary<Point3d, int> PointClass)
        {
            int n = polyline.NumberOfVertices;
            for (int i = 0; i < n; ++i)
            {
                var prePoint = polyline.GetPoint3dAt((i + n - 1) % n);
                var curPoint = polyline.GetPoint3dAt(i);
                var nxtPoint = polyline.GetPoint3dAt((i + 1) % n);
                if (!PointClass.ContainsKey(curPoint))
                {
                    if (DirectionCompair(prePoint, curPoint, nxtPoint) < 0)
                    {
                        PointClass.Add(curPoint, 1);
                    }
                    else
                    {
                        PointClass.Add(curPoint, 2);
                    }
                }
            }
        }

        /// <summary>
        /// Compair the relation about two lines(right judge)
        /// </summary>
        /// <param name="prePoint">line A start</param>
        /// <param name="curPoint">line A end，line B start mean time</param>
        /// <param name="nxtPoint">line B end</param>
        /// <returns></returns>
        public static double DirectionCompair(Point3d prePoint, Point3d curPoint, Point3d nxtPoint)
        {
            return (curPoint.X - prePoint.X) * (nxtPoint.Y - curPoint.Y) - (nxtPoint.X - curPoint.X) * (curPoint.Y - prePoint.Y);
        }

        /// <summary>
        /// Get Out Points of a Polyline
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point3d> OutPoints(Polyline polyline)
        {
            int ptType = 0;
            Dictionary<Point3d, int> PointClass = new Dictionary<Point3d, int>();
            PointClassify(polyline, PointClass);
            var points = Algorithms.GetConvexHull(PointClass.Keys.ToList());
            foreach (var point in points)
            {
                if (PointClass.ContainsKey(point))
                {
                    ptType = PointClass[point];
                    break;
                }
            }
            List<Point3d> outPoints = new List<Point3d>();
            if (ptType == 0)
            {
                return null;
            }
            else
            {
                foreach (var point in PointClass)
                {
                    if (point.Value == ptType)
                    {
                        outPoints.Add(point.Key);
                    }
                }
                return outPoints;
            }
        }

        /// <summary>
        /// Get near points of a Polyline
        /// </summary>
        /// <param name="poly2points">outlines with their near points</param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Point3d> NearPoints(Dictionary<Polyline, Point3dCollection> poly2points, Point3dCollection points)
        {
            BorderConnectToNear.VoronoiDiagramNearPoints(points, ref poly2points);

            List<Point3d> ansPts = new List<Point3d>();
            foreach (var pts in poly2points.Values)
            {
                foreach (var pt in pts)
                {
                    if (pt is Point3d ptt)
                    {
                        ansPts.Add(ptt);
                    }
                }
            }
            return ansPts;
        }

        /// <summary>
        /// Get Near Points By ConformingDelaunayTriangulation
        /// </summary>
        /// <param name="points"></param>
        /// <param name="poly2points"></param>
        public static void ConformingDelaunayTriangulationNearPoints(Point3dCollection points, Tuple<Polyline, Point3dCollection> poly2points)
        {
            var builder = new ConformingDelaunayTriangulationBuilder();
            builder.SetSites(points.ToNTSGeometry());
            builder.Constraints = poly2points.Item1.ToNTSLineString();

            foreach (var geometry in builder.GetTriangles(ThCADCoreNTSService.Instance.GeometryFactory).Geometries)
            {
                if (geometry is Polygon polygon)
                {
                    if (polygon.IsEmpty)
                    {
                        continue;
                    }
                    var polyline = polygon.Shell.ToDbPolyline();
                    if (poly2points.Item1.Intersects(polyline))
                    {
                        foreach (var obj in polyline.Vertices())
                        {
                            if (obj is Point3d pt)
                            {
                                if (pt.DistanceTo(poly2points.Item1.GetClosePoint(pt)) > 2)
                                {
                                    poly2points.Item2.Add(pt);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove Points in a Point set whose distance to a polyline out of a certain range
        /// </summary>
        /// <param name="points"></param>
        /// <param name="outline"></param>
        /// <param name="maxDis"></param>
        public static void RemovePointsFarFromOutline(ref List<Point3d> points, Polyline outline, double maxDis = 600)
        {
            List<Point3d> tmpPoints = new List<Point3d>();
            tmpPoints.AddRange(points);
            foreach (var pt in tmpPoints)
            {
                double curDis = pt.DistanceTo(outline.GetClosePoint(pt));
                if (curDis > maxDis)
                {
                    points.Remove(pt);
                }
            }
        }

        private static List<Line> GetCenterLines(Polyline pl)
        {
            var lines = ThMEPPolygonService.CenterLine(pl.ToNTSPolygon().ToDbMPolygon());
            int n = pl.NumberOfVertices;
            for (int j = 0; j < n; ++j)
            {
                if (lines.Count != 0)
                {
                    break;
                }
                //pl.ReverseCurve();
                var pl2 = new Polyline();
                for (int i = 1; i < n; ++i)
                {
                    pl2.SetPointAt(i - 1, pl.GetPoint2dAt(i));
                }
                pl2.SetPointAt(n - 1, pl.GetPoint2dAt(0));
                pl2.Closed = true;
                //pl = pl.Buffer(2)[0] as Polyline;
                lines = ThMEPPolygonService.CenterLine(pl2.ToNTSPolygon().ToDbMPolygon());
            }
            return lines;
        }

        /// <summary>
        /// 获得墙的拐角点和边界点
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="fstPts"></param>
        /// <param name="SndPts"></param>
        public static void WallCrossPoint(Polyline pline, ref List<Point3d> fstPts, ref List<Point3d> SndPts)
        {
            fstPts.Clear();
            SndPts.Clear();
            //首先找出中心线
            //var lines = ThMEPPolygonService.CenterLine(polygon.ToDbMPolygon());
            var lines = GetCenterLines(pline);
            Dictionary<Point3d, HashSet<Point3d>> pt2Pts = TypeConvertor.Lines2Tuples(lines);

            //对块进行分割
            var walls = new DBObjectCollection();
            var columns = new DBObjectCollection();
            ThVStructuralElementSimplifier.Classify(pline.ToNTSPolygon().ToDbCollection(), columns, walls);
            List<Point3d> zeroPts = new List<Point3d>();
            foreach (var ent in columns)
            {
                if (ent is Polyline polyline)
                {
                    zeroPts.Add(polyline.GetCentroidPoint());
                    fstPts.Add(polyline.GetCentroidPoint());
                }
            }
            foreach (var pt2Pt in pt2Pts)
            {
                var pt = pt2Pt.Key;
                foreach (var ent in walls)
                {
                    if (ent is Polyline polyline)
                    {
                        if (polyline.ContainsOrOnBoundary(pt))
                        {
                            bool flag = false;
                            foreach (var zeroPt in zeroPts)
                            {
                                if (pt.DistanceTo(zeroPt) < 300)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if (flag == true)
                            {
                                continue;
                            }
                            if (IsCrossPt(pt, pt2Pts))
                            {
                                fstPts.Add(pt);
                            }
                            else
                            {
                                SndPts.Add(pt);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 寻找窄条形状的多边形的折角点
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="tolerence"></param>
        /// <returns></returns>
        public static List<Point3d> CrossPointsOnPolyline(Polyline polyline, double tolerence = 100)
        {
            List<Point3d> ansList = new List<Point3d>();
            var lines = ThMEPPolygonService.CenterLine(polyline.ToNTSPolygon().ToDbMPolygon());
            foreach (var line in lines)
            {
                ansList.Add(line.StartPoint);
                ansList.Add(line.EndPoint);
            }
            ansList = PointsDistinct(ansList, tolerence);
            return ansList;
        }

        /// <summary>
        /// Judge wether a point is a cross point
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="pt2Pts"></param>
        /// <returns></returns>
        public static bool IsCrossPt(Point3d pt, Dictionary<Point3d, HashSet<Point3d>> pt2Pts)
        {
            if (!pt2Pts.ContainsKey(pt))
            {
                return false;
            }
            foreach (var ptA in pt2Pts[pt])
            {
                var vecA = ptA - pt;
                foreach (var ptB in pt2Pts[pt])
                {
                    var vecB = ptB - pt;
                    var angel = vecA.GetAngleTo(vecB);
                    if (angel > Math.PI / 6 && angel < Math.PI / 6 * 5)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// find pt intersect with columns
        /// </summary>
        /// <param name="clumnPts"></param>
        /// <param name="outlineWalls"></param>
        /// <returns></returns>
        public static HashSet<Point3d> FindIntersectBorderPt(List<Polyline> outlines, HashSet<Point3d> points, double bufferLength = 500)
        {
            var borderPts = new HashSet<Point3d>();
            var dbPoints = points.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            foreach (var outline in outlines)
            {
                var innerPoints = outline.Buffer(bufferLength).OfType<Polyline>()
                    .Where(o => o.Area > 1.0)
                    .SelectMany(p => spatialIndex.SelectWindowPolygon(p)
                    .OfType<DBPoint>()
                    .Select(d => d.Position)).Distinct().ToList();
                innerPoints.ForEach(o => borderPts.Add(o));
            }
            return borderPts;
        }

        /// <summary>
        /// 生成一种数据结构，可以通过外框线找到其包含的边界点
        /// </summary>
        /// <param name="outlines"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, HashSet<Point3d>> GetOutline2BorderPts(List<Polyline> outlines, List<Point3d> points, double bufferLength = 500)
        {
            var outline2BorderPts = new Dictionary<Polyline, HashSet<Point3d>>();
            var dbPoints = points.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            foreach (var outline in outlines)
            {
                var innerPoints = outline.Buffer(bufferLength).OfType<Polyline>()
                    .Where(o => o.Area > 1.0)
                    .SelectMany(p => spatialIndex.SelectWindowPolygon(p)
                    .OfType<DBPoint>()
                    .Select(d => d.Position)).Distinct().ToList();
                outline2BorderPts.Add(outline, innerPoints.ToHashSet());
            }
            return outline2BorderPts;
        }

        /// <summary>
        /// 获取outline2BorderNearPts
        /// </summary>
        /// <param name="dicTuples">注：此时的dicTuples不应包含close border lines</param>
        /// <param name="polylines"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> CreateOutline2BorderNearPts(Dictionary<Point3d, HashSet<Point3d>> dicTuples, List<Polyline> polylines)
        {
            var outline2BorderPts = GetOutline2BorderPts(polylines, dicTuples.Keys.ToList());
            var outline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
            foreach (var outline in outline2BorderPts.Keys)
            {
                if (!outline2BorderNearPts.ContainsKey(outline))
                {
                    outline2BorderNearPts.Add(outline, new Dictionary<Point3d, HashSet<Point3d>>());
                }
                foreach (var borderPt in outline2BorderPts[outline])
                {
                    if (!outline2BorderNearPts[outline].ContainsKey(borderPt))
                    {
                        outline2BorderNearPts[outline].Add(borderPt, new HashSet<Point3d>());
                    }
                    if (dicTuples.ContainsKey(borderPt))
                    {
                        foreach (var nearPt in dicTuples[borderPt])
                        {
                            if (!outline2BorderNearPts[outline][borderPt].Contains(nearPt))
                            {
                                outline2BorderNearPts[outline][borderPt].Add(nearPt);
                            }
                        }
                    }
                }
            }
            return outline2BorderNearPts;
        }

        public static Point3dCollection PointsDistinct(Point3dCollection pts, double deviation = 1.0)
        {
            Point3dCollection ansPts = new Point3dCollection();
            var kdTree = new ThCADCoreNTSKdTree(deviation);
            foreach (Point3d pt in pts)
            {
                kdTree.InsertPoint(pt);
            }
            kdTree.Nodes.ForEach(o =>
            {
                ansPts.Add(o.Key.Coordinate.ToAcGePoint3d());
            });
            return ansPts;
        }
        public static List<Point3d> PointsDistinct(List<Point3d> pts, double deviation = 1.0)
        {
            List<Point3d> ansPts = new List<Point3d>();
            var kdTree = new ThCADCoreNTSKdTree(deviation);
            foreach (Point3d pt in pts)
            {
                kdTree.InsertPoint(pt);
            }
            kdTree.Nodes.ForEach(o =>
            {
                ansPts.Add(o.Key.Coordinate.ToAcGePoint3d());
            });
            return ansPts;
        }
        public static HashSet<Point3d> PointsDistinct(HashSet<Point3d> pts, double deviation = 1.0)
        {
            HashSet<Point3d> ansPts = new HashSet<Point3d>();
            var kdTree = new ThCADCoreNTSKdTree(deviation);
            foreach (Point3d pt in pts)
            {
                kdTree.InsertPoint(pt);
            }
            kdTree.Nodes.ForEach(o =>
            {
                ansPts.Add(o.Key.Coordinate.ToAcGePoint3d());
            });
            return ansPts;
        }
    }
}
