using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.AutoCAD.Geometry;

namespace ThCADExtension
{
    public static class ThMatrix3dExtension
    {
        /// <summary>
        /// Gets the transformation matrix that transforms from the 
        /// given CoordinateSystem3d to the coordinate system of the 
        /// matrix which the method is invoked on.
        /// </summary>
        public static Matrix3d GetTransformFrom(this Matrix3d dest, CoordinateSystem3d origin)
        {
            CoordinateSystem3d to = dest.CoordinateSystem3d;
            if (to == origin)
                return dest;
            return Matrix3d.AlignCoordinateSystem(to.Origin, to.Xaxis, to.Yaxis, to.Zaxis,
               origin.Origin, origin.Xaxis, origin.Yaxis, origin.Zaxis);
        }

        /// <summary>
        /// Gets the transformation matrix that transforms from the 
        /// coordinate system of the given Matrix3d to the coordinate 
        /// system of the Matrix3d which the method is invoked on.
        /// </summary>
        public static Matrix3d GetTransformFrom(this Matrix3d dest, Matrix3d origin)
        {
            if (dest == origin)
                return dest;
            CoordinateSystem3d from = dest.CoordinateSystem3d;
            CoordinateSystem3d to = origin.CoordinateSystem3d;
            return Matrix3d.AlignCoordinateSystem(from.Origin, from.Xaxis, from.Yaxis, from.Zaxis,
               to.Origin, to.Xaxis, to.Yaxis, to.Zaxis);
        }

        /// <summary>
        /// Returns the Matrix3d that tranforms from the given coordinate
        /// system to the world coordinate system.
        /// </summary>
        public static Matrix3d ToWorld(this CoordinateSystem3d ucs)
        {
            return Matrix3d.Identity.GetTransformFrom(ucs);
        }

        /// <summary>
        /// Decompose the Matrix3d
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="scale"></param>
        /// <param name="rotate"></param>
        /// <param name="translate"></param>
        public static void Decompose(this Matrix3d mat, out Matrix3d scale, out Matrix3d rotate, out Matrix3d mirror, out Matrix3d translate)
        {
            // https://adndevblog.typepad.com/autocad/2014/08/decomposing-material-mapper-transform-matrix.html
            // Scale - assume always positive, and must be non-zero
            var v3dOne = new Vector3d(mat[0, 0], mat[0, 1], mat[0, 2]);
            var v3dTwo = new Vector3d(mat[1, 0], mat[1, 1], mat[1, 2]);
            var v3dThree = new Vector3d(mat[2, 0], mat[2, 1], mat[2, 2]);

            var lengthList = new List<double>
            {
                v3dOne.Length,
                v3dTwo.Length,
                v3dThree.Length,
            };

            var data = Matrix3d.Identity.ToArray();
            for (int i = 0; i < 3; i++)
            {
                data[i * 5] = data[i * 5] * lengthList[i];
            }
            scale = new Matrix3d(data);

            // Translation
            translate = Matrix3d.Displacement(mat.Translation);

            // Rotation – assume only rotation about the Z axis
            var acosValue = mat[0, 0] / lengthList[0];
            if (acosValue > 1.0)
            {
                acosValue = 1.0;
            }
            else if (acosValue < -1.0)
            {
                acosValue = -1.0;
            }
            var zAngle = Math.Acos(acosValue);
            Debug.Assert(0.0 <= zAngle && zAngle <= Math.PI);
            if (mat[0, 1] > 0.0)
            {
                zAngle = 2.0 * Math.PI - zAngle;
            }
            rotate = Matrix3d.Rotation(zAngle, Vector3d.ZAxis, Point3d.Origin);

            var identity = Matrix3d.Identity.ToArray();
            var mirrorTemp = mat.PreMultiplyBy(rotate.Inverse());
            identity[0] = Math.Sign(mirrorTemp[0, 0]);
            identity[5] = Math.Sign(mirrorTemp[1, 1]);
            mirror = new Matrix3d(identity);
        }
    }
}
