using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThCADCore.NTS;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
using Dreambuild.AutoCAD;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class PointsCenters
    {
        //获取点集分组后的中心
        public static List<Point3d> PointsCores(List<Point3d> points, double tolerance = 200)
        {
            Dictionary<Point3d, HashSet<Point3d>> limitedLines = new Dictionary<Point3d, HashSet<Point3d>>();
            DelaunayTriangulationLines(ref limitedLines, PointsDistinct(points), tolerance);

            List<List<Point3d>> pointsGroups = new List<List<Point3d>>();
            GetPointsGroups(ref pointsGroups, limitedLines);

            return GetPointsCores(pointsGroups);
        }

        //点集分组
        public static List<List<Point3d>> PointsGroups(List<Point3d> points, double tolerance = 200)
        {
            Dictionary<Point3d, HashSet<Point3d>> limitedLines = new Dictionary<Point3d, HashSet<Point3d>>();
            DelaunayTriangulationLines(ref limitedLines, PointsDistinct(points), tolerance);

            List<List<Point3d>> pointsGroups = new List<List<Point3d>>();
            GetPointsGroups(ref pointsGroups, limitedLines);
            return pointsGroups;
        }

        //获取点集生成的德劳内三角中长度在规定范围内的线段
        public static void DelaunayTriangulationLines(ref Dictionary<Point3d, HashSet<Point3d>> limitedLines, List<Point3d> points, double tolerance)
        {
            foreach(var pt in points)
            {
                AddLineTodicTuples(pt, pt, ref limitedLines);
            }
            var delaunayTriangulation = new DelaunayTriangulationBuilder();
            delaunayTriangulation.SetSites(ToNTSCoordinates(points));
            var triangles = delaunayTriangulation.GetTriangles(ThCADCoreNTSService.Instance.GeometryFactory);
            foreach (var geometry in triangles.Geometries)
            {
                if (geometry is Polygon polygon)
                {
                    Point3dCollection pts = polygon.Shell.Coordinates.ToAcGePoint3ds();
                    for (int i = 0; i < pts.Count - 1; ++i)
                    {
                        double curDis = pts[i].DistanceTo(pts[i + 1]);
                        if(curDis > 10 && curDis < tolerance)
                        {
                            AddLineTodicTuples(pts[i], pts[i + 1], ref limitedLines);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
        public static void AddLineTodicTuples(Point3d ptA, Point3d ptB, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(ptA))
            {
                dicTuples.Add(ptA, new HashSet<Point3d>());
            }
            if (!dicTuples[ptA].Contains(ptB))
            {
                dicTuples[ptA].Add(ptB);
            }
            if (!dicTuples.ContainsKey(ptB))
            {
                dicTuples.Add(ptB, new HashSet<Point3d>());
            }
            if (!dicTuples[ptB].Contains(ptA))
            {
                dicTuples[ptB].Add(ptA);
            }
        }
        public static Coordinate[] ToNTSCoordinates(List<Point3d> points)
        {
            var coordinates = new List<Coordinate>();
            foreach (Point3d pt in points)
            {
                coordinates.Add(pt.ToNTSCoordinate());
            }
            return coordinates.ToArray();
        }

        //生成点集组
        public static void GetPointsGroups(ref List<List<Point3d>> pointsGroups, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            Random rd = new Random();
            HashSet<Point3d> ptVisted = new HashSet<Point3d>();
            foreach(var curPt in dicTuples.Keys)
            {
                if (!ptVisted.Contains(curPt))
                {
                    List<Point3d> onePtsGroup = new List<Point3d>();
                    BFS(curPt, ref onePtsGroup, ref ptVisted, dicTuples, rd.Next(1, 10) % 6 + 1);
                    pointsGroups.Add(onePtsGroup);
                }
            }
        }

        //广度遍历
        public static void BFS(Point3d basePt, ref List<Point3d> onePtsGroup, ref HashSet<Point3d> ptVisted, Dictionary<Point3d, HashSet<Point3d>> dicTuples, int colorIndex)
        {
            Queue<Point3d> queue = new Queue<Point3d>();
            queue.Enqueue(basePt);
            onePtsGroup.Add(basePt);
            ptVisted.Add(basePt);
            while (queue.Count > 0)
            {
                Point3d topPt = queue.Dequeue();
                //ShowInfo.ShowPointAsO(topPt, colorIndex, 200);
                foreach(var pt in dicTuples[topPt])
                {
                    if (!ptVisted.Contains(pt))
                    {
                        ptVisted.Add(pt);
                        queue.Enqueue(pt);
                        onePtsGroup.Add(pt);
                        //ShowInfo.DrawLine(topPt, pt, colorIndex);
                    }
                }
            }
        }

        //各个点集中心点组成的点集
        public static List<Point3d> GetPointsCores(List<List<Point3d>> pointsGroups)
        {
            var centerPoints = new List<Point3d>();
            pointsGroups.ForEach(points => {
                centerPoints.Add(GetPointsCenter(points));
            });
            return centerPoints;
        }

        //获取点集中心点
        public static Point3d GetPointsCenter(List<Point3d> points)
        {
            int n = points.Count();
            double xSum = 0;
            double ySum = 0;
            points.ForEach(pt => {
                xSum += pt.X;
                ySum += pt.Y;
            });
            return new Point3d(xSum / n, ySum / n, 0);
        }

        //带误差的点集去重
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
    }
}
