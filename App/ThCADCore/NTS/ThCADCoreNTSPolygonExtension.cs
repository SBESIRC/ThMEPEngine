using GeoAPI.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Predicate;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using AcPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolygonExtension
    {
        public static DBObjectCollection Difference(this Polyline region, DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            var geometry = region.ToNTSPolygon().Difference(curves.UnionGeometries());
            if (geometry.IsEmpty)
            {
                return objs;
            }
            if (geometry is IPolygon polygon)
            {
                objs.Add(polygon.Shell.ToDbPolyline());
                foreach (var hole in polygon.Holes)
                {
                    objs.Add(hole.ToDbPolyline());
                }
            }
            else if (geometry is IMultiPolygon mPolygon)
            {
                foreach (IPolygon subPolygon in mPolygon.Geometries)
                {
                    objs.Add(subPolygon.Shell.ToDbPolyline());
                    foreach (var hole in subPolygon.Holes)
                    {
                        objs.Add(hole.ToDbPolyline());
                    }
                }
            }
            return objs;
        }

        public static bool Contains(this AcPolygon polygon, AcPolyline other)
        {
            return RectangleContains.Contains(polygon.ToNTSPolygon(), other.ToNTSLineString());
        }

        public static bool Intersects(this AcPolygon polygon, AcPolyline other)
        {
            return RectangleIntersects.Intersects(polygon.ToNTSPolygon(), other.ToNTSLineString());
        }
    }
}
