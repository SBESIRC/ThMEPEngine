using System;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSLineExtension
    {
        public static bool IsCollinear(this Line line, Line other)
        {
            var p1 = line.StartPoint.ToNTSCoordinate();
            var p2 = line.EndPoint.ToNTSCoordinate();
            var q1 = other.StartPoint.ToNTSCoordinate();
            var q2 = other.EndPoint.ToNTSCoordinate();
            return Orientation.Index(p1, q1, q2) == OrientationIndex.Collinear
                && Orientation.Index(p2, q1, q2) == OrientationIndex.Collinear;
        }

        public static bool Overlaps(this Line line, Line other)
        {
            return line.ToNTSLineString().Overlaps(other.ToNTSLineString());
        }

        public static Point3d Intersection(this Line line, Polyline other)
        {
            var geometry = line.ToNTSLineString().Intersection(other.ToNTSLineString());
            if (geometry is Point point)
            {
                return point.ToAcGePoint3d();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
