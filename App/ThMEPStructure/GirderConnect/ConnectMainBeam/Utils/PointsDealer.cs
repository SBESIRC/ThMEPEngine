using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using AcHelper;
using DotNetARX;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using NFox.Cad;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Model;
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
                        //ShowInfo.ShowPointAsO(curPoint, 130); // debug
                        PointClass.Add(curPoint, 1);
                    }
                    else
                    {
                        //ShowInfo.ShowPointAsO(curPoint, 210); // debug
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
            foreach (var point in points) //actually O(1) time
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
                        //ShowInfo.ShowPointAsO(point.Key, 130);
                        outPoints.Add(point.Key);
                    }
                    //else // debug: do not delete
                    //{
                    //    ShowInfo.ShowPointAsO(point.Key, 210);
                    //}
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
            //StructureBuilder.VoronoiDiagramConnect(points, poly2points);
            VoronoiDiagramNearPoints(points, poly2points);

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
        /// Get Near Points By VoronoiDiagram
        /// </summary>
        /// <param name="points"></param>
        /// <param name="poly2points"></param>
        public static void VoronoiDiagramNearPoints(Point3dCollection points, Dictionary<Polyline, Point3dCollection> poly2points)
        {
            var voronoiDiagram = new VoronoiDiagramBuilder();
            voronoiDiagram.SetSites(points.ToNTSGeometry());

            foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
            {
                if (polygon.IsEmpty)
                {
                    continue;
                }
                var polyline = polygon.ToDbPolylines().First();
                foreach (Point3d pt in points)
                {
                    if (polyline.Contains(pt))
                    {
                        if (poly2points != null)
                        {
                            foreach (var pl2pts in poly2points)
                            {
                                if (!pl2pts.Value.Contains(pt) && pl2pts.Key.Intersects(polyline))
                                {
                                    pl2pts.Value.Add(pt);
                                    ShowInfo.ShowPointAsX(pt, 2, 500);
                                }
                            }
                        }
                        break;
                    }
                }
            }
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
        /// Get the best connect point by conform point set
        /// </summary>
        /// <param name="nearPt"></param>
        /// <param name="priotityConnectPt"></param>
        /// <param name="outPts"></param>
        /// <returns>The best connect point</returns>
        public static Point3d BestConnect(Point3d nearPt, List<Point3d> priotityConnectPt, List<Point3d> outPts)
        {
            double minArea = double.MaxValue;
            double minDis = double.MaxValue;
            double curArea;
            double curDis;
            Point3d ansPt = new Point3d();
            foreach (var priCntPt in priotityConnectPt)
            {
                foreach (var outPt in outPts)
                {
                    curDis = priCntPt.DistanceTo(outPt);
                    minDis = curDis < minDis ? curDis : minDis;
                }
                minDis = minDis < 600 ? minDis : 600; //prevent mistake by big minDis
                curArea = nearPt.DistanceTo(priCntPt) * minDis;
                if (curArea < minArea)
                {
                    minArea = curArea;
                    ansPt = priCntPt;
                }
            }
            //return ansPt;
            return priotityConnectPt[0];
        }

        /// <summary>
        /// Remove Points in this point set who is very close to the points in another point set 
        /// </summary>
        /// <param name="basePoints"></param>
        /// <param name="removePoints"></param>
        /// <param name="deviation"></param>
        /// <returns></returns>
        public static Point3dCollection RemoveSimmilerPoint(Point3dCollection basePoints, HashSet<Point3d> removePoints, double deviation = 0.001)
        {
            Point3dCollection ansPoints = new Point3dCollection();
            foreach (Point3d basePt in basePoints)
            {
                ansPoints.Add(basePt);
            }
            foreach (Point3d basePoint in basePoints)
            {
                foreach (Point3d removePoint in removePoints)
                {
                    if (basePoint.DistanceTo(removePoint) < deviation)
                    {
                        if (basePoints.Contains(removePoint))
                        {
                            ansPoints.Remove(removePoint);
                        }
                    }
                }
            }
            return ansPoints;
        }
        public static Point3dCollection RemoveSimmilerPoint(Point3dCollection basePoints, Point3dCollection removePoints, double deviation = 0.001)
        {
            Point3dCollection ansPoints = new Point3dCollection();
            foreach (Point3d basePt in basePoints)
            {
                ansPoints.Add(basePt);
            }
            foreach (Point3d basePoint in basePoints)
            {
                foreach (Point3d removePoint in removePoints)
                {
                    if (basePoint.DistanceTo(removePoint) < deviation)
                    {
                        if (ansPoints.Contains(removePoint) && basePoints.Contains(removePoint))
                        {
                            ansPoints.Remove(removePoint);
                        }
                    }
                }
            }
            return ansPoints;
        }
        public static List<Point3d> RemoveSimmilerPoint(List<Point3d> points, double deviation = 1)
        {
            var ansPts = new List<Point3d>();
            Dictionary<Point3d, bool> visitPt = new Dictionary<Point3d, bool>();
            foreach(var point in points)
            {
                if (!visitPt.ContainsKey(point))
                {
                    visitPt.Add(point, false);
                }
            }
            foreach(var ptA in points)
            {
                if(visitPt[ptA] == false)
                {
                    visitPt[ptA] = true;
                    ansPts.Add(ptA);
                    foreach(var ptB in points)
                    {
                        if (visitPt[ptB] == false && ptA.DistanceTo(ptB) < deviation)
                        {
                            visitPt[ptB] = true;
                        }
                    }
                }
            }
            return ansPts;
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
                    ShowInfo.ShowPointAsU(pt, 2);
                }
            }
        }

        /// <summary>
        /// 获得墙的拐角点和边界点
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="fstPts"></param>
        /// <param name="SndPts"></param>
        public static void WallCrossPoint(Polygon polygon, ref List<Point3d> fstPts, ref List<Point3d> SndPts)//, ref HashSet<Point3d> zeroPts)
        {
            fstPts.Clear();
            SndPts.Clear();
            //首先找出中心线
            var lines = ThMEPPolygonService.CenterLine(polygon.ToDbMPolygon());
            Dictionary<Point3d, HashSet<Point3d>> pt2Pts = LinesToTuples(lines);

            //对块进行分割
            var walls = new DBObjectCollection();
            var columns = new DBObjectCollection();
            ThVStructuralElementSimplifier.Classify(polygon.ToDbCollection(), columns, walls);
            List<Point3d> zeroPts = new List<Point3d>();
            foreach (var ent in columns)
            {
                if (ent is Polyline polyline)
                {
                    zeroPts.Add(polyline.GetCentroidPoint());
                    fstPts.Add(polyline.GetCentroidPoint());
                    ShowInfo.ShowPointAsO(polyline.GetCentroidPoint(), 3, 500);
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
                            foreach(var zeroPt in zeroPts)
                            {
                                if(pt.DistanceTo(zeroPt) < 300)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if(flag == true)
                            {
                                continue;
                            }
                            if (IsCrossPt(pt, pt2Pts))
                            {
                                fstPts.Add(pt);
                                ShowInfo.ShowPointAsO(pt, 1);
                            }
                            else
                            {
                                SndPts.Add(pt);
                                ShowInfo.ShowPointAsO(pt, 5);
                            }
                        }
                    }
                }
            }
            //fstPts = RemoveSimmilerPoint(fstPts);
            //SndPts = RemoveSimmilerPoint(SndPts);
        }

        public static Dictionary<Point3d, HashSet<Point3d>> LinesToTuples(List<Line> lines)
        {
            Dictionary<Point3d, HashSet<Point3d>> pt2Pts = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var line in lines)
            {
                var stPt = line.StartPoint;
                var edPt = line.EndPoint;
                if (!pt2Pts.ContainsKey(stPt))
                {
                    pt2Pts.Add(stPt, new HashSet<Point3d>());
                }
                if (!pt2Pts[stPt].Contains(edPt))
                {
                    pt2Pts[stPt].Add(edPt);
                }
                if (!pt2Pts.ContainsKey(edPt))
                {
                    pt2Pts.Add(edPt, new HashSet<Point3d>());
                }
                if (!pt2Pts[edPt].Contains(stPt))
                {
                    pt2Pts[edPt].Add(stPt);
                }
            }
            return pt2Pts;
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
            ansList = RemoveSimmilerPoint(ansList, tolerence);
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
        public static HashSet<Point3d> FindIntersectNearPt(Point3dCollection clumnPts, Dictionary<Polyline, HashSet<Polyline>> outlineWalls, 
            ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            var ansPts = new HashSet<Point3d>();
            foreach(Polyline polyline in outlineWalls.Keys)
            {
                Polyline pl = polyline.Buffer(500)[0] as Polyline;
                foreach (Point3d pt in clumnPts)
                {
                    if (pl.ContainsOrOnBoundary(pt) && !ansPts.Contains(pt))
                    {
                        if (outline2BorderNearPts.ContainsKey(polyline))
                        {
                            if (!outline2BorderNearPts[polyline].ContainsKey(pt))
                            {
                                outline2BorderNearPts[polyline].Add(pt, new HashSet<Point3d>());
                            }
                        }
                        ansPts.Add(pt);
                    }
                }
            }
            return ansPts;
        }

        /// <summary>
        /// Update structure Outline2BorderNearPts
        /// </summary>
        /// <param name="outline2BorderNearPts"></param>
        /// <param name="allConnects"></param>
        public static void UpdateOutline2BorderNearPts(ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, Dictionary<Point3d, HashSet<Point3d>> connects)
        {
            foreach(var outline2BorderNearPt in outline2BorderNearPts)
            {
                var outline = outline2BorderNearPt.Key;
                foreach(var border2NearPts in outline2BorderNearPt.Value)
                {
                    var borderPt = border2NearPts.Key;
                    if (connects.ContainsKey(borderPt))
                    {
                        foreach(var pt in connects[borderPt])
                        {
                            if (!outline2BorderNearPt.Value.ContainsKey(pt) && !border2NearPts.Value.Contains(pt))
                            {
                                border2NearPts.Value.Add(pt);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 生成一种数据结构，可以通过外框线找到其包含的边界点
        /// </summary>
        /// <param name="outlines"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, HashSet<Point3d>> GetOutline2BorderPts(HashSet<Polyline> outlines, List<Point3d> points)
        {
            var outline2BorderPts = new Dictionary<Polyline, HashSet<Point3d>>();
            foreach(var outline in outlines)
            {
                if (!outline2BorderPts.ContainsKey(outline))
                {
                    outline2BorderPts.Add(outline, new HashSet<Point3d>());
                }
                foreach(var point in points)
                {
                    Polyline newOutline = outline.Buffer(500)[0] as Polyline;
                    if (!outline2BorderPts[outline].Contains(point) && newOutline.ContainsOrOnBoundary(point))
                    {
                        outline2BorderPts[outline].Add(point);
                    }
                }
            }
            return outline2BorderPts;
        }
    }
}
