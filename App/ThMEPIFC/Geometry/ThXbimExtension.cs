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

        public static XbimPoint3D ToXbimPoint3D(this Point3D pt)
        {
            return new XbimPoint3D(pt.X, pt.Y, pt.Z);
        }

        public static UnitVector3D ToUnitVector3D(this XbimVector3D v)
        {
            return UnitVector3D.Create(v.X, v.Y, v.Z, ThXbimCommon.TOLERANCE);
        }

        public static XbimVector3D ToXbimVector3D(this Vector3D v)
        {
            return new XbimVector3D(v.X, v.Y, v.Z);
        }

        public static XbimMatrix3D ToXbimMatrix3D(this ThTCHMatrix3d matrix3D)
        {
            return new XbimMatrix3D(matrix3D.Data11, matrix3D.Data12, matrix3D.Data13, matrix3D.Data14,
                matrix3D.Data21, matrix3D.Data22, matrix3D.Data23, matrix3D.Data24,
                matrix3D.Data31, matrix3D.Data32, matrix3D.Data33, matrix3D.Data34,
                matrix3D.Data41, matrix3D.Data42, matrix3D.Data43, matrix3D.Data44);
        }
    }
}
