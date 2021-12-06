using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.TowerSeparation.DBScan;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;
using NFox.Cad;
using GeometryExtensions;
using ThMEPEngineCore.CAD;
using System.Linq;
using DotNetARX;

namespace ThMEPWSS.TowerSeparation.TowerExtract
{
    public class TowerExtractor
    {
        private Dictionary<Point3d, Polyline> CenterPointForShearwall { get; set; } = new Dictionary<Point3d, Polyline>();
        private Dictionary<Point3d, List<Point3d>> ShearwallPoints { get; set; } = new Dictionary<Point3d, List<Point3d>>();
        private DBScanCluster ClusterWorker { get; set; } = new DBScanCluster();
        private List<Polyline> ConvexHull { get; set; } = new List<Polyline>();
        public List<Polyline> Extractor(List<Polyline> shearWallList , Polyline fireZone)
        {
            if(shearWallList.Count ==0)
            {
                return new List<Polyline>();
            }
            Point3dCollection shearWalls = FindCenterForPolygon(shearWallList);
            List<Polyline> result = new List<Polyline>();
            double epsilon = 8000;
            int minPts = 2;
            List<Point3dCollection> clusters = ClusterWorker.getClusters(shearWalls, epsilon , minPts);
            List<List<Point3d>> clusterPoints = getShearwallClusterPoints(clusters);
            foreach (List<Point3d> cluster in clusterPoints)
            {
                //Extents3d temp = Point3dCollectionExtensions.ToExtents3d(cluster);
                //List<Point2d> temp = new List<Point2d>();
                //cluster.ForEach(o => temp.Add(Point3dExtension.ToPoint2D(o)));
                //temp.GetConvexHull();
                //List<Point3d> convexPoint = new List<Point3d>();
                //temp.ForEach(o => convexPoint.Add(o.ToPoint3d()));
                Polyline extentRec = GetConvexHull(cluster);

                //DotNetARX.PolylineTools.CreatePolyline(extentRec, GeometryEx.GetConvexHull(cluster));
                //extentRec.CreatePolyline(temp.GetConvexHull());
                //Polyline extentRec = new Polyline();
                //DotNetARX.PolylineTools.CreatePolyline(extentRec,FindConvexHull(cluster));

                //if (!extentRec.Closed)
                //{
                //    extentRec.AddVertexAt(extentRec.NumberOfVertices, extentRec.GetPoint2dAt(0), 0, 0, 0);
                //}

                //MakeValid(extentRec);
                //DBObjectCollection intersection = new DBObjectCollection();
                //if (!ThGeometryTool.IsContains(fireZone, extentRec))
                //{
                //    intersection = ThCADCoreNTSEntityExtension.Intersection(extentRec, fireZone, false);
                //}
                //else
                //{
                //    intersection.Add(extentRec);
                //}
                //var intersection  = Algorithms.Intersect(extentRec, fireZone,0);
                //ConvexHull.Add(PolylineTools.CreatePolyline(intersection));
                ConvexHull.Add(extentRec);

                //foreach(Polyline l in intersection)
                //{
                //    ConvexHull.Add(l);
                //}

            }
            foreach (Polyline pl in ConvexHull)
            {
                result.Add(ThMEPOrientedBoundingBox.CalObb(pl));
            }
            return result;
            //return ConvexHull;
        }

        //返回剪力墙外包盒中心点
        private Point3dCollection FindCenterForPolygon(List<Polyline> shearWallList)
        {
            Point3dCollection result = new Point3dCollection();
            foreach(Polyline wall in shearWallList)
            {
                Extents3d temp = new Extents3d();
                List<Point3d> wallPoints= new List<Point3d>();
                for(int i = 0; i < wall.NumberOfVertices; ++i)
                {
                    temp.AddPoint(wall.GetPoint3dAt(i));
                    wallPoints.Add(wall.GetPoint3dAt(i));
                }
                result.Add(temp.CenterPoint());
                ShearwallPoints[temp.CenterPoint()] = wallPoints;
                CenterPointForShearwall[temp.CenterPoint()] = wall;
            }
            return result;
        }

        private List<List<Point3d>> getShearwallClusterPoints(List<Point3dCollection> clusters)
        {
            List<List<Point3d>> result = new List<List<Point3d>>();
            foreach(Point3dCollection cluster in clusters)
            {
                List<Point3d> temp = new List<Point3d>();
                foreach(Point3d pt in cluster)
                {
                    temp.AddRange(ShearwallPoints[pt]);
                }
                result.Add(temp);
            }
            return result;
        }
        //////private Polyline ToPolyline(Extents3d ex)
        //////{
        //////    Polyline rectangle = new Polyline();
        //////    rectangle.AddVertexAt(0, ex.MinPoint.ToPoint2D(), 0, 0, 0);
        //////    rectangle.AddVertexAt(1, new Point2d(ex.MaxPoint.X, ex.MinPoint.Y), 0, 0, 0);
        //////    rectangle.AddVertexAt(2, ex.MinPoint.ToPoint2D(), 0, 0, 0);
        //////    rectangle.AddVertexAt(3, new Point2d(ex.MinPoint.X, ex.MaxPoint.Y), 0, 0, 0);
        //////    return rectangle;
        //////}
        //////private List<Point3d> ToPoint3d(Point3dCollection set)
        //////{
        //////    List<Point3d> result = new List<Point3d>();
        //////    foreach(Point3d pt in set)
        //////    {
        //////        result.Add(pt);
        //////    }
        //////    return result;
        //////}
        //////public static Polyline MakeValid(Polyline polygon)
        //////{
        //////    //处理自交
        //////    var objs = polygon.MakeValid();
        //////    return objs.Count > 0 ? objs.Cast<Polyline>().OrderByDescending(p => p.Area).First() : new Polyline();
        //////}
        //////private Point3dCollection FindConvexHull(List<Point3d> Pts)
        //////{
        //////    Point3dCollection result = new Point3dCollection();
        //////    if (Pts.Count < 3)
        //////    {
        //////        Pts.ForEach(o => result.Add(o));
        //////        return result;
        //////    }
        //////    Pts.Sort((p1, p2) => (p1.Y - p2.Y == 0) ? (int)(p1.X - p2.X) : (int)(p1.Y - p2.Y));
            
        //////    result.Add(Pts[0]);
        //////    Pts.RemoveAt(0);
        //////    Point3d pt0 = result[0];
        //////    //Pts.Sort((p1, p2) => (GeTools.AngleFromXAxis(p1, pt0) - GeTools.AngleFromXAxis(p2, pt0)==0) 
        //////    //? (int)(pt0.DistanceTo(p1)-pt0.DistanceTo(p2))
        //////    //:(int)(GeTools.AngleFromXAxis(p1, pt0) - GeTools.AngleFromXAxis(p2, pt0)));
        //////    Pts = SortByAngle(Pts, pt0);
        //////    List<Point3d> stack = new List<Point3d>();
        //////    stack.Add(pt0);
        //////    stack.Add(Pts[0]);
        //////    //stack.Add(Pts[1]);
        //////    //Pts.RemoveAt(1);
        //////    Pts.RemoveAt(0);
        //////    foreach (Point3d pt in Pts)
        //////    {
        //////        while (stack.Count > 2 && IsNonLeftTurn(stack[stack.Count - 2], stack[stack.Count - 1], pt))
        //////        {
        //////            stack.RemoveAt(stack.Count - 1);
        //////        }
        //////        stack.Add(pt);
        //////        //Pts.RemoveAt(0);
        //////    }
        //////    stack.ForEach(x => result.Add(x));
        //////    return result;
        //////}

        //////private bool IsNonLeftTurn(Point3d pt0, Point3d pt1, Point3d pt2)
        //////{
        //////    Vector3d vec1 = pt1 - pt0;
        //////    Vector3d vec2 = pt2 - pt0;
        //////    double num = vec1.X * vec2.Y - vec1.Y * vec2.X;
        //////    //double num = ((pt2.Y - pt1.Y) * (pt1.X - pt0.X)) - ((pt1.Y - pt0.Y) * (pt2.X - pt1.X));
        //////    if (num > 0)
        //////        return false;
        //////    else
        //////        return true;
        //////}
        ////private List<Point3d> SortByAngle(List<Point3d> points, Point3d start)
        ////{
        ////    Dictionary<Point3d, double> angleMap= new Dictionary<Point3d, double>();
        ////    foreach(Point3d pt in points)
        ////    {
        ////        if (!angleMap.ContainsKey(pt))
        ////        {
        ////            angleMap.Add(pt, GeTools.AngleFromXAxis(pt, start));
        ////        }
        ////    }
        ////    //points.Sort((p1, p2) => (angleMap[p1] - angleMap[p2] == 0)
        ////    //? (int)(start.DistanceTo(p1) - start.DistanceTo(p2))
        ////    //: (int)(angleMap[p1] - angleMap[p2]));
        ////    points.Sort((p1, p2) => (int)(angleMap[p1] - angleMap[p2]));
        ////    for(int i = 1; i< points.Count; ++i)
        ////    {
        ////        if(angleMap[points[i]] == angleMap[points[i-1]])
        ////        {
        ////            int temp = start.DistanceTo(points[i]) - start.DistanceTo(points[i - 1]) < 0 ? i : i-1;
        ////            points.RemoveAt(temp);
        ////            --i;
        ////        }
        ////    }
        ////    return points;
        ////}

        private static Polyline GetConvexHull(List<Point3d> pts)
        {
            var convexPl = new Polyline();
            var netI2d = pts.Select(x => x.ToPoint2d()).ToList();

            var convex = netI2d.GetConvexHull();

            for (int j = 0; j < convex.Count; j++)
            {
                convexPl.AddVertexAt(convexPl.NumberOfVertices, convex.ElementAt(j), 0, 0, 0);
            }
            convexPl.Closed = true;

            if (convexPl.Area < 1.0)
            {
                var newPts = pts.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
                var maxLine = new Line(pts.First(), pts[pts.Count - 1]);
                return maxLine.Buffer(1.0);
            }

            return convexPl;
        }
    }
}