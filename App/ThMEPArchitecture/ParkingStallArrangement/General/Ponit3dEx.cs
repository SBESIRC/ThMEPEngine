using Autodesk.AutoCAD.Geometry;
using System;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    public class Point3dEx : IEquatable<Point3dEx>
    {
        public double Tolerance; //mm
        public Point3d _pt;
        public double X;
        public double Y;
        public double Z;

        public Point3dEx(double tol = 1)
        {
            _pt = new Point3d();
            X = 0;
            Y = 0;
            Z = 0;
            Tolerance = tol;
        }
        public Point3dEx(Point3d pt, double tol = 1)
        {
            _pt = pt;
            X = pt.X;
            Y = pt.Y;
            Z = 0;
            Tolerance = tol;
        }

        public Point3dEx(double x, double y, double z, double tol = 1)
        {
            _pt = new Point3d(x, y, 0);
            X = x;
            Y = y;
            Z = 0;
            Tolerance = tol;
        }

        public override int GetHashCode()
        {
            return ((int)_pt.X / 1000).GetHashCode() ^ ((int)_pt.Y / 1000).GetHashCode();
        }

        public bool Equals(Point3dEx other)
        {
            return Math.Abs(other._pt.X - _pt.X) < Tolerance && Math.Abs(other._pt.Y - _pt.Y) < Tolerance;
        }
    }
}
