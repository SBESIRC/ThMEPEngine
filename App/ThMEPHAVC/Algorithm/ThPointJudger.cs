using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPHVAC.Algorithm
{
    public class ThPointJudger
    {
        public static bool IsInPolyline(List<List<Point3d>> triangles, Point3d p)
        {
            foreach (var t in triangles)
            {
                if (ThBaryCentric.IsInTriangle(t[0], t[1], t[2], p))
                    return true;
            }
            return false;
        }
        public static bool IsInPolyline(Polyline pl, Point3d p)
        {
            var triangles = SplitWallRegion(pl.Vertices());
            foreach (var t in triangles)
            {
                if (ThBaryCentric.IsInTriangle(t[0], t[1], t[2], p))
                    return true;
            }
            return false;
        }
        public static List<List<Point3d>> SplitWallRegion(Point3dCollection points)
        {
            if (points.Count == 0)
                return new List<List<Point3d>>();
            points.Add(points[0]);
            var tTriangles = points.DelaunayTriangulation().Cast<Polyline>().ToList();
            var conCavePoint = RecordConCavePoint(points);
            var triangles = new List<List<Point3d>>();
            foreach (var triangle in tTriangles)
            {
                var triPoints = new List<Point3d>();
                int num = 0;
                for (int i = 0; i < 3; ++i)
                {
                    var p = RoundPoint(triangle.GetPoint3dAt(i), 6);
                    if (conCavePoint.Contains(p))
                        ++num;
                    triPoints.Add(p);
                }
                if (num != 2)
                    triangles.Add(triPoints);
            }
            tTriangles.Clear();
            return triangles;
        }
        private static HashSet<Point3d> RecordConCavePoint(Point3dCollection points)
        {
            // points按逆时针排列
            var pl = new Polyline();
            var set = new HashSet<Point3d>();
            pl.CreatePolyline(points);
            points.Insert(0, points[points.Count - 1]);
            for (int i = 1; i < points.Count; ++i)
            {
                var v1 = (points[i] - points[i - 1]).GetNormal();
                var v2 = (points[i + 1] - points[i]).GetNormal();
                var z = v1.CrossProduct(v2).Z;
                if (z < 0)
                {
                    // 整个图形法线向图纸外，所以points[i]为凹点，记录与它相邻的两点
                    set.Add(RoundPoint(points[i - 1], 6));
                    set.Add(RoundPoint(points[i + 1], 6));
                }
            }
            return set;
        }
        public static Point3d RoundPoint(Point3d p, int tailNum)
        {
            var X = Math.Abs(p.X) < 1e-3 ? 0 : p.X;
            var Y = Math.Abs(p.Y) < 1e-3 ? 0 : p.Y;
            return new Point3d(Math.Round(X, tailNum), Math.Round(Y, tailNum), 0);
        }
        //public void Test()
        //{
        //    var pl = new Polyline();
        //    var pts = new Point2dCollection() {
        //        new Point2d (0,0),
        //        new Point2d (1000,0),
        //        new Point2d (1000,1000),
        //        new Point2d (800,1000),
        //        new Point2d (800,500),
        //        new Point2d (400,500),
        //        new Point2d (400,1000),
        //        new Point2d (0,1000),
        //    };
        //    pl.CreatePolyline(pts);
        //    var t = DateTime.Now;
        //    var tris = ThPointJudger.SplitWallRegion(pl.Vertices());
        //    var T = 1000000;
        //    var pp = new Point3d(50, 50, 0);
        //    for (int i = 0; i < T; ++i)
        //        ThPointJudger.IsInPolyline(tris, pp);
        //    var t1 = DateTime.Now;
        //    ThMEPHVACService.PromptMsg((t1 - t).ToString() + "\n");
        //    t1 = DateTime.Now;
        //    for (int i = 0; i < T; ++i)
        //        pl.IsPointIn(pp);
        //    var t2 = DateTime.Now;
        //    ThMEPHVACService.PromptMsg((t2 - t1).ToString());
        //}
    }
}