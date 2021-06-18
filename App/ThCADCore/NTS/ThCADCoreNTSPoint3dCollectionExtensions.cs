using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPoint3dCollectionExtensions
    {
        public static Geometry ConvexHull(this Point3dCollection collection)
        {
            var geomFactory = ThCADCoreNTSService.Instance.GeometryFactory;
            var convexHull = new ConvexHull(collection.ToNTSCoordinates(), geomFactory);
            return convexHull.GetConvexHull();
        }
    }
}
