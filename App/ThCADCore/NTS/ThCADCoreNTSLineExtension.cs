using System;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSLineExtension
    {
        public static bool CoveredBy(this Line line, Line other)
        {
            return line.ToNTSLineString().CoveredBy(other.ToNTSLineString());
        }

        public static bool CoveredBy(this Line line, Polyline other)
        {
            return line.ToNTSLineString().CoveredBy(other.ToNTSLineString());
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
