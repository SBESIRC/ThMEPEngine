﻿using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThChangeCoordinateService
    {
        public static Matrix3d GetCoordinateTransformer(Point3d fromOrigin, Vector3d fromXAxis, Point3d toOrigin, Vector3d toXAxis)
        {

            var rotationangle = fromXAxis.GetAngleTo(toXAxis, Vector3d.ZAxis);
            var matrix = Matrix3d.Displacement(toOrigin - fromOrigin) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

            return matrix.Inverse();

        }

        public static long GetValue(Point3d pt, bool isXAxis)
        {
            if (isXAxis)
                return (long)Math.Round(pt.X / 45);
            else
                return (long)Math.Round(pt.Y / 45);
        }


        public static double GetOriginallyValue(Point3d pt, bool isXAxis)
        {
            if (isXAxis)
                return pt.X;
            else
                return pt.Y;
        }

    }
}