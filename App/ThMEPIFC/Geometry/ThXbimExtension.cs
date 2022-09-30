using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;

namespace ThMEPIFC.Geometry
{
    public static class ThXbimExtension
    {
        public static Point3D ToPoint3D(this XbimPoint3D pt)
        {
            return new Point3D(pt.X, pt.Y, pt.Z);
        }

        public static UnitVector3D ToUnitVector3D(this XbimVector3D v)
        {
            return UnitVector3D.Create(v.X, v.Y, v.Z, ThXbimCommon.TOLERANCE);
        }
    }
}
