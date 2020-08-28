using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolylineExtension
    {
        public static Circle MinimumBoundingCircle(this Polyline polyline)
        {
            var mbc = new MinimumBoundingCircle(polyline.ToNTSLineString());
            return new Circle(mbc.GetCentre().ToAcGePoint3d(), Vector3d.ZAxis, mbc.GetRadius());
        }

        public static Polyline MinimumBoundingBox(this Polyline polyline)
        {
            var geometry = polyline.ToNTSLineString().Envelope;
            if (geometry is ILineString lineString)
            {
                return lineString.ToDbPolyline();
            }
            else if (geometry is ILinearRing linearRing)
            {
                return linearRing.ToDbPolyline();
            }
            else if (geometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline GetMinimumRectangle(this Polyline polyline)
        {
            var geom = polyline.ToNTSLineString();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geom);
            if (rectangle is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline ConvexHull(this Polyline polyline)
        {
            var convexHull = new ConvexHull(polyline.ToNTSLineString());
            var geometry = convexHull.GetConvexHull();
            if (geometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polyline GetOctagonalEnvelope(this Polyline polyline)
        {
            var geometry = OctagonalEnvelope.GetOctagonalEnvelope(polyline.ToNTSLineString());
            if (geometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static bool Contains(this Polyline thisPline, Point3d pt)
        {
            return thisPline.PointInPolygon(pt) == LocateStatus.Interior;
        }

        public static Polyline Intersect(this Polyline thisPolyline, Polyline polySec)
        {
            var polygonFir = thisPolyline.ToNTSPolygon();

            var polygonSec = polySec.ToNTSPolygon();

            if (polygonFir == null || polygonSec == null)
            {
                return null;
            }

            // 检查是否相交
            if (!polygonFir.Intersects(polygonSec))
            {
                return null;
            }

            // 若相交，则计算相交部分
            var rGeometry = polygonFir.Intersection(polygonSec);
            if (rGeometry is IPolygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }

            return null;
        }
    }
}
