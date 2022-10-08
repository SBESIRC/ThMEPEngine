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

        public static XbimPoint3D ToXbimPoint3D(this Point3D pt)
        {
            return new XbimPoint3D(pt.X, pt.Y, pt.Z);
        }

        public static XbimVector3D ToXbimVector3D(this Vector3D v)
        {
            return new XbimVector3D(v.X, v.Y, v.Z);
        }
    }
}
