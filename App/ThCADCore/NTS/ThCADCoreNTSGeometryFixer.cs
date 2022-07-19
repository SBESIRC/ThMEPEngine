using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries.Utilities;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSGeometryFixer
    {
        public static DBObjectCollection Fix(this DBObjectCollection polygons)
        {
            return GeometryFixer.Fix(polygons.ToNTSMultiPolygon()).ToDbCollection();
        }

        public static DBObjectCollection Fix(this AcPolygon polygon)
        {
            return GeometryFixer.Fix(polygon.ToNTSPolygon()).ToDbCollection(true);
        }
    }
}
