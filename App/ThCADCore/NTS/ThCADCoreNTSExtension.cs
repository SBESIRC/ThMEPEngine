using GeoAPI.Geometries;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSExtension
    {
        public static IPolygon ToPolygon(this ILinearRing linearRing)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(linearRing);
        }

        public static IMultiLineString ToNTSMultiLineString(this IPolygon polygon)
        {
            if (polygon.NumInteriorRings == 0)
            {
                var lineStrings = new ILineString[] { polygon.ExteriorRing };
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiLineString(lineStrings);
            }
            else
            {
                return polygon.Boundary as IMultiLineString;
            }
        }
    }
}
