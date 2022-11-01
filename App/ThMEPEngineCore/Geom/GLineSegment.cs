using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Geom
{
    public struct GLineSegment
    {
        public class EqualityComparer : IEqualityComparer<GLineSegment>
        {
            double radius;
            public EqualityComparer(double radius)
            {
                this.radius = radius;
            }
            public bool Equals(GLineSegment x, GLineSegment y)
            {
                return x.StartPoint.GetDistanceTo(y.StartPoint) <= radius && x.EndPoint.GetDistanceTo(y.EndPoint) <= radius
                    || x.StartPoint.GetDistanceTo(y.EndPoint) <= radius && x.EndPoint.GetDistanceTo(y.StartPoint) <= radius;
            }

            public int GetHashCode(GLineSegment obj)
            {
                return 0;
            }
        }
        public GLineSegment Offset(Vector2d vec)
        {
            return new GLineSegment(StartPoint + vec, EndPoint + vec);
        }
        public GLineSegment Offset(double dx, double dy)
        {
            return new GLineSegment(StartPoint.OffsetXY(dx, dy), EndPoint.OffsetXY(dx, dy));
        }
        public GLineSegment TransformBy(Matrix2d leftSide)
        {
            return new GLineSegment(StartPoint.TransformBy(leftSide), EndPoint.TransformBy(leftSide));
        }
        public GLineSegment TransformBy(Matrix3d leftSide)
        {
            return new GLineSegment(StartPoint.ToPoint3d().TransformBy(leftSide), EndPoint.ToPoint3d().TransformBy(leftSide));
        }
        public GLineSegment TransformBy(ref Matrix2d leftSide)
        {
            return new GLineSegment(StartPoint.TransformBy(leftSide), EndPoint.TransformBy(leftSide));
        }
        public GLineSegment TransformBy(ref Matrix3d leftSide)
        {
            return new GLineSegment(StartPoint.ToPoint3d().TransformBy(leftSide), EndPoint.ToPoint3d().TransformBy(leftSide));
        }
        public bool IsNull => Equals(this, default(GLineSegment));
        public GLineSegment(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
        public GLineSegment(Point3d point1, Point3d point2) : this(point1.X, point1.Y, point2.X, point2.Y) { }
        public GLineSegment(Point2d point1, Point2d point2) : this(point1.X, point1.Y, point2.X, point2.Y) { }
        public bool IsValid => !(X1 == X2 && Y1 == Y2) && !double.IsNaN(X1) && !double.IsNaN(X2) && !double.IsNaN(Y1) && !double.IsNaN(Y2);
        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
        public Point2d StartPoint => new Point2d(X1, Y1);
        public Point2d EndPoint => new Point2d(X2, Y2);
        public Point2d Center => GeoAlgorithm.MidPoint(StartPoint, EndPoint);
        public double Length => StartPoint.GetDistanceTo(EndPoint);
        public double MinX => Math.Min(X1, X2);
        public double MaxX => Math.Max(X1, X2);
        public double MinY => Math.Min(Y1, Y2);
        public double MaxY => Math.Max(Y1, Y2);
        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public double AngleDegree
        {
            get
            {
                var dg = (EndPoint - StartPoint).Angle.AngleToDegree();
                if (dg < 0) dg += 360;
                if (dg >= 360) dg -= 360;
                return dg;
            }
        }
        public double Angle
        {
            get
            {
                var angle = (EndPoint - StartPoint).Angle;
                if (angle < 0) angle += Math.PI * 2;
                if (angle >= Math.PI * 2) angle -= Math.PI * 2;
                return angle;
            }
        }
        public double SingleAngleDegree
        {
            get
            {
                var dg = AngleDegree;
                if (dg >= 180) dg -= 180;
                return dg;
            }
        }
        public double SingleAngle
        {
            get
            {
                var angle = Angle;
                if (angle >= Math.PI) angle -= Math.PI;
                return angle;
            }
        }
        public bool IsVertical(double tollerance)
        {
            var dg = SingleAngleDegree;
            return 90 - tollerance <= dg && dg <= 90 + tollerance;
        }
        public bool IsHorizontalOrVertical(double tollerance) => IsHorizontal(tollerance) || IsVertical(tollerance);
        public bool IsHorizontal(double tollerance)
        {
            var dg = SingleAngleDegree;
            return dg <= tollerance || dg >= 180 - tollerance;
        }
        public GLineSegment Extend(double ext)
        {
            var vec = EndPoint - StartPoint;
            var len = vec.Length;
            if (len == 0) return this;
            var k = ext / len;
            var ep = EndPoint + vec * k;
            var sp = StartPoint + vec * (-k);
            return new GLineSegment(sp, ep);
        }
        public Vector2d ToVector2d()
        {
            return EndPoint - StartPoint;
        }
    }
}
