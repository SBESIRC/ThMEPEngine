using Autodesk.AutoCAD.Geometry;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSPoint3dService
    {
        public static ThPDSPoint3d ToPDSPoint3d(this Point3d point)
        {
            return new ThPDSPoint3d(point.X, point.Y);
        }

        public static Point3d PDSPoint3dToPoint3d(this ThPDSPoint3d point)
        {
            return new Point3d(point.X, point.Y, 0.0);
        }
    }
}
