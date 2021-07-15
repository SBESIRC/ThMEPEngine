using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPoint3dCollectionExtensions
    { 
        public static Geometry ToNTSGeometry(this Point3dCollection collection)
        {
            var geomFactory = ThCADCoreNTSService.Instance.GeometryFactory;
            return geomFactory.CreateMultiPointFromCoords(collection.ToNTSCoordinates());
        }

        public static Geometry ConvexHull(this Point3dCollection collection)
        {
            var geomFactory = ThCADCoreNTSService.Instance.GeometryFactory;
            var convexHull = new ConvexHull(collection.ToNTSCoordinates(), geomFactory);
            return convexHull.GetConvexHull();
        }

        public static Circle MinimumBoundingCircle(this Point3dCollection collection)
        {
            var mbc = new MinimumBoundingCircle(collection.ToNTSGeometry());
            return new Circle(mbc.GetCentre().ToAcGePoint3d(), Vector3d.ZAxis, mbc.GetRadius());
        }
    }
}
