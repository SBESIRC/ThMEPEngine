using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public static class ThLightingCrossPointService
    {
        public static Point3dCollection GetNoRepeatedPoints(DBObjectCollection lines, double disTolerance)
        {
            var results = new Point3dCollection();
            lines.OfType<Line>().ForEach(l =>
            {
                if (!IsExist(l.StartPoint, results, disTolerance))
                {
                    results.Add(l.StartPoint);
                }
                if (!IsExist(l.EndPoint, results, disTolerance))
                {
                    results.Add(l.EndPoint);
                }
            });
            return results;
        }

        public static Point3dCollection FilterByDegree(Point3dCollection pts,
            DBObjectCollection nodedLines, int minimumDegree, int maximumDegree, double pointTolerance)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(nodedLines);
            return pts.OfType<Point3d>().Where(p =>
            {
                var degree = Query(p, spatialIndex, pointTolerance).Count;
                return degree >= minimumDegree && degree <= maximumDegree;
            }).ToCollection();
        }

        public static DBObjectCollection Query(Point3d pt, ThCADCoreNTSSpatialIndex spatialIndex, double pointRange)
        {
            var outline = pt.CreateSquare(pointRange);
            var objs = spatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return objs;
        }

        private static bool IsExist(Point3d pt, Point3dCollection pts, double disTolerance)
        {
            if (disTolerance < 0)
            {
                disTolerance = 1e-4;
            }
            return pts
                .OfType<Point3d>()
                .Where(p => p.DistanceTo(pt) <= disTolerance)
                .Any();
        }
    }
}
