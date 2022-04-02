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

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class PointsDealer
    {
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


        /// <summary>
        /// 生成一种数据结构，可以通过外框线找到其包含的边界点
        /// </summary>
        /// <param name="outlines"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, HashSet<Point3d>> GetOutline2BorderPts(List<Polyline> outlines, HashSet<Point3d> points, double bufferLength = 500)
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
                    .Select(d => d.Position)).Distinct().ToHashSet();
                outline2BorderPts.Add(outline, innerPoints);
            }
            return outline2BorderPts;
        }

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

        public static Point3d GetLeftDownPt(HashSet<Point3d> points) //目前的方向算法并不适合LeftDown， 而是
        {
            var ansPt = points.First();
            double minSum = double.MaxValue;
            double curSum;
            foreach(var curPt in points)
            {
                curSum = curPt.X + curPt.Y;
                if(curSum < minSum)
                {
                    minSum = curSum;
                    ansPt = curPt;
                }
            }
            return ansPt;
        }
    }
}
