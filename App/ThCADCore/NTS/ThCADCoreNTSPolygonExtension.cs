using GeoAPI.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

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

        public static bool Contains(this Polyline thisPline, Polyline otherPline)
        {
            // Geometry A contains Geometry B if no points of B lie in the exterior of A, 
            // and at least one point of the interior of B lies in the interior of A
            return thisPline.ToNTSPolygon().Contains(otherPline.ToNTSPolygon());
        }

        public static bool Covers(this Polyline thisPline, Polyline otherPline)
        {
            // Geometry A covers Geometry B if no points of B lie in the exterior of A
            return thisPline.ToNTSPolygon().Covers(otherPline.ToNTSPolygon());
        }

        public static bool Overlaps(this Polyline thisPline, Polyline otherPline)
        {
            return thisPline.ToNTSPolygon().Overlaps(otherPline.ToNTSPolygon());
        }
    }
}
