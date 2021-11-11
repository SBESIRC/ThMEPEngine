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
using ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess;

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
                        //ShowInfo.DrawLine(prePoint, curPoint, 130);
                        //ShowInfo.DrawLine(curPoint, nxtPoint, 130);
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
                        ansPts.Add(ptt);
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
                                    //ShowInfo.ShowPointAsO(pt, 230, 300);
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
                        if (basePoints.Contains(removePoint))
                        {
                            ansPoints.Remove(removePoint);
                        }
                    }
                }
            }
            return ansPoints;
        }

        /// <summary>
        /// Remove Points in a Point set whose distance to a polyline out of a certain range
        /// </summary>
        /// <param name="points"></param>
        /// <param name="outline"></param>
        /// <param name="maxDis"></param>
        public static void RemovePointsFarFromOutline(List<Point3d> points, Polyline outline, double maxDis = 600)
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
    }
}
