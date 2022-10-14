using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Units;

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

        public static XbimMatrix3D MultipleTransformFroms(double scale, XbimVector3D xDir, XbimVector3D move)
        {
            var scaleM = CoordinateSystem.CreateIdentity(4).Multiply(scale);
            var rotation = xDir.Angle(new XbimVector3D(1, 0, 0));
            var rotationM = CoordinateSystem.Rotation(Angle.FromRadians(rotation), new Vector3D(0, 0, 1));
            var displacementM = CoordinateSystem.Translation(new Vector3D(move.X, move.Y, move.Z));
            var matrix = displacementM.Multiply(rotationM).Multiply(scaleM);
            return ToXbimMatrix3D(matrix);
        }

        private static XbimMatrix3D ToXbimMatrix3D(Matrix<double> m)
        {
            var data = m.ToColumnMajorArray();
            return new XbimMatrix3D(
                data[0],
                data[1],
                data[2],
                data[3],
                data[4],
                data[5],
                data[6],
                data[7],
                data[8],
                data[9],
                data[10],
                data[11],
                data[12],
                data[13],
                data[14],
                data[15]);
        }
    }
}
