using System;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm;

namespace ThMEPEngineCore.Algorithm
{
    public static class ThMEPNTSExtension
    {
        /// <summary>
        /// NTS固定精度下的共线
        /// </summary>
        /// <param name="firstSp"></param>
        /// <param name="firstEp"></param>
        /// <param name="secondSp"></param>
        /// <param name="secondEp"></param>
        /// <returns></returns>
        public static bool IsLooseCollinear(Point3d firstSp, Point3d firstEp,
            Point3d secondSp, Point3d secondEp)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                var p1 = firstSp.ToNTSCoordinate();
                var p2 = firstEp.ToNTSCoordinate();
                var q1 = secondSp.ToNTSCoordinate();
                var q2 = secondEp.ToNTSCoordinate();
                return Orientation.Index(p1, q1, q2) == OrientationIndex.Collinear
                    && Orientation.Index(p2, q1, q2) == OrientationIndex.Collinear;
            }
        }

        /// <summary>
        /// NTS固定精度下的重叠
        /// </summary>
        /// <param name="firstSp"></param>
        /// <param name="firstEp"></param>
        /// <param name="secondSp"></param>
        /// <param name="secondEp"></param>
        /// <returns></returns>
        public static bool IsLooseOverlap(Point3d firstSp, Point3d firstEp,
            Point3d secondSp, Point3d secondEp)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                var first = new Line(firstSp, firstEp);
                var second = new Line(secondSp, secondEp);
                return first.Overlaps(second);
            }
        }

        public static DBObjectCollection LooseUnion(this DBObjectCollection objs)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                return objs.UnionPolygons();
            }
        }
    }
}
