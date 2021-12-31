using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Algorithm
{
    public class ThBaryCentric
    {
        public static bool IsInTriangle(Point3d A, Point3d B, Point3d C, Point3d P)
        {
            var bcCoor = GetBaryCentric(A, B, C, P);
            return !(bcCoor.X < 0 || bcCoor.Y < 0 || bcCoor.Z < 0);
        }
        private static Point3d GetBaryCentric(Point3d A, Point3d B, Point3d C, Point3d P)
        {
            var vB = B - A;
            var vC = C - A;
            var vP = A - P;
            var v1 = new Vector3d(vB.X, vC.X, vP.X);
            var v2 = new Vector3d(vB.Y, vC.Y, vP.Y);
            var u = v1.CrossProduct(v2);
            if (Math.Abs(u.Z) > 0)
                return new Point3d(1.0 - (u.X + u.Y) / u.Z, u.Y / u.Z, u.X / u.Z);
            return new Point3d(-1.0, 1.0, 1.0);
        }
    }
}
