using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Geom
{
    public struct GRect
    {
        public class EqualityComparer : IEqualityComparer<GRect>
        {
            double tol;

            public EqualityComparer(double tollerence)
            {
                this.tol = tollerence;
            }

            public bool Equals(GRect x, GRect y)
            {
                return x.EqualsTo(y, tol);
            }

            public int GetHashCode(GRect obj)
            {
                return 0;
            }
        }
        public bool IsNull => Equals(this, default(GRect));
        public bool IsValid => Width > 0 && Height > 0;
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public Point2d LeftTop => new Point2d(MinX, MaxY);
        public Point2d LeftButtom => new Point2d(MinX, MinY);
        public Point2d RightButtom => new Point2d(MaxX, MinY);
        public Point2d RightTop => new Point2d(MaxX, MaxY);
        public Point2d Center => new Point2d(CenterX, CenterY);
        public GRect(double x1, double y1, double x2, double y2)
        {
            MinX = Math.Min(x1, x2);
            MinY = Math.Min(y1, y2);
            MaxX = Math.Max(x1, x2);
            MaxY = Math.Max(y1, y2);
        }
        public GRect OffsetXY(Vector2d v)
        {
            return this.OffsetXY(v.X, v.Y);
        }
        public GRect OffsetXY(double deltaX, double deltaY)
        {
            return new GRect(this.MinX + deltaX, this.MinY + deltaY, this.MaxX + deltaX, this.MaxY + deltaY);
        }
        public static GRect Create(double widht, double height)
        {
            return new GRect(0, 0, widht, height);
        }
        public static GRect Create(Point3dCollection points)
        {
            var pts = points.Cast<Point3d>();
            return new GRect(pts.Select(x => x.X).Min(), pts.Select(x => x.Y).Min(), pts.Select(x => x.X).Max(), pts.Select(x => x.Y).Max());
        }
        public static GRect Create(Extents3d ext)
        {
            return new GRect(ext.MinPoint.X, ext.MinPoint.Y, ext.MaxPoint.X, ext.MaxPoint.Y);
        }
        public static GRect Create(Point3d pt, double ext)
        {
            return new GRect(pt.X - ext, pt.Y - ext, pt.X + ext, pt.Y + ext);
        }
        public static GRect Create(Point2d pt, double extX, double extY)
        {
            return new GRect(pt.X - extX, pt.Y - extY, pt.X + extX, pt.Y + extY);
        }
        public static GRect Create(Point2d pt, double ext)
        {
            return new GRect(pt.X - ext, pt.Y - ext, pt.X + ext, pt.Y + ext);
        }
        public GRect(Point2d leftTop, double width, double height) : this(leftTop.X, leftTop.Y, leftTop.X + width, leftTop.Y - height)
        {
        }
        public GRect(Point3d p1, Point3d p2) : this(p1.ToPoint2d(), p2.ToPoint2d())
        {
        }
        public GRect(Point2d p1, Point2d p2) : this(p1.X, p1.Y, p2.X, p2.Y)
        {
        }
        public double Radius => Math.Sqrt(Math.Pow(Width / 2, 2) + Math.Pow(Height / 2, 2));
        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public double CenterX => (MinX + MaxX) / 2;
        public double CenterY => (MinY + MaxY) / 2;
        public double OuterRadius => (new Point2d(MinX, MinY)).GetDistanceTo(new Point2d(CenterX, CenterY));
        public double MiddleRadius => Math.Max(Width, Height) / 2;
        public double InnerRadius => Math.Min(Width, Height) / 2;
        public double Area => Width * Height;
        public Extents2d ToExtents2d() => new Extents2d(MinX, MinY, MaxX, MaxY);
        public GRect Expand(double thickness)
        {
            return new GRect(this.MinX - thickness, this.MinY - thickness, this.MaxX + thickness, this.MaxY + thickness);
        }
        public bool ContainsRect(GRect rect)
        {
            return rect.MinX > this.MinX && rect.MinY > this.MinY && rect.MaxX < this.MaxX && rect.MaxY < this.MaxY;
            //return (this.MinX - rect.MinX) * (this.MaxX - rect.MaxX) <= 0 && (this.MinY - rect.MinY) * (this.MaxY - rect.MaxY) < 0;
        }
        public bool ContainsPoint(Point2d point)
        {
            return MinX <= point.X && point.X <= MaxX && MinY <= point.Y && point.Y <= MaxY;
        }

        public bool EqualsTo(GRect other, double tollerance)
        {
            return Math.Abs(this.MinX - other.MinX) < tollerance && Math.Abs(this.MinY - other.MinY) < tollerance
                && Math.Abs(this.MaxX - other.MaxX) < tollerance && Math.Abs(this.MaxY - other.MaxY) < tollerance;
        }
        public static GRect Combine(IEnumerable<GRect> rs)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            var ok = false;
            foreach (var r in rs)
            {
                if (r.MinX < minX) minX = r.MinX;
                if (r.MinY < minY) minY = r.MinY;
                if (r.MaxX > maxX) maxX = r.MaxX;
                if (r.MaxY > maxY) maxY = r.MaxY;
                ok = true;
            }
            if (ok) return new GRect(minX, minY, maxX, maxY);
            return default;
        }
        public LinearRing ToLinearRing(Matrix3d matrix)
        {
            if (this.IsNull) return null;
            if (matrix == Matrix3d.Identity) return this.ToLinearRing();
            var p1 = this.LeftTop.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            var p2 = this.LeftButtom.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            var p3 = this.RightButtom.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            var p4 = this.RightTop.ToPoint3d().TransformBy(matrix).ToNTSCoordinate();
            return new LinearRing(new Coordinate[] { p1, p2, p3, p4, p1 });
        }
        public GRect TransformBy(Matrix3d matrix)
        {
            if (this.IsNull) return this;
            if (matrix == Matrix3d.Identity) return this;
            var o = new Extents2dCalculator();
            o.Update(this.LeftTop.ToPoint3d().TransformBy(matrix));
            o.Update(this.LeftButtom.ToPoint3d().TransformBy(matrix));
            o.Update(this.RightTop.ToPoint3d().TransformBy(matrix));
            o.Update(this.RightButtom.ToPoint3d().TransformBy(matrix));
            if (o.IsValid) return o.ToGRect();
            return default;
        }
    }
}
