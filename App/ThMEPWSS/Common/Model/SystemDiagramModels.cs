using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Service;
using System.Collections;
using PolylineTools = ThMEPWSS.Pipe.Service.PolylineTools;
using ThMEPWSS.CADExtensionsNs;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.ApplicationServices;
using Linq2Acad;
using ThCADExtension;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Assistant;

namespace ThMEPWSS.Uitl
{
    public class SPoint
    {
        public double X;
        public double Y;
        public double Z;
        public Point3d ToPoint3d()
        {
            return new Point3d(X, Y, Z);
        }
        public Point2d ToPoint2d()
        {
            return new Point2d(X, Y);
        }
    }
    public class SCircle
    {
        public SPoint Center;
        public double Radius;
        public Circle ToCircle()
        {
            return new Circle() { Center = Center.ToPoint3d(), Radius = Radius };
        }
    }
    public class SLine
    {
        public SPoint StartPoint;
        public SPoint EndPoint;
        public Line ToLine()
        {
            return new Line() { StartPoint = StartPoint.ToPoint3d(), EndPoint = EndPoint.ToPoint3d() };
        }
    }
    public class SPolyline
    {
        public List<SPoint> Points;
        public bool Closed;
        public Polyline ToPolyline()
        {
            var pline = new Polyline();
            for (int i = 0; i < Points.Count; i++)
            {
                pline.AddVertexAt(i, Points[i].ToPoint2d(), 0, 0, 0);
            }
            return pline;
        }
    }
    public class SRect
    {
        public double MinX;
        public double MinY;
        public double MaxX;
        public double MaxY;
        public GRect ToGRect()
        {
            return new GRect(MinX, MinY, MaxX, MaxY);
        }
        public Point3dCollection ToPoint3dCollection()
        {
            return ToGRect().ToPt3dCollection();
        }
        public Polyline ToPolyline()
        {
            return PolylineTools.CreatePolyline(ToPoint3dCollection().Cast<Point3d>().ToList());
        }
    }
    namespace ExtensionsNs
    {
        public static class SGeoConverters
        {
            public static SPoint ToSPoint(this Point2d pt) => new SPoint() { X = pt.X, Y = pt.Y };
            public static SPoint ToSPoint(this Point3d pt) => new SPoint() { X = pt.X, Y = pt.Y, Z = pt.Z };
            public static SCircle ToSCircle(this Circle circle) => new SCircle() { Center = circle.Center.ToSPoint(), Radius = circle.Radius };
            public static SRect ToSRect(this Point3dCollection pts)
            {
                GeoAlgorithm.GetCornerCoodinate(pts, out double minX, out double minY, out double maxX, out double maxY);
                return new SRect() { MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY };
            }
            public static SRect ToSRect(this GRect r) => new SRect() { MinX = r.MinX, MinY = r.MinY, MaxX = r.MaxX, MaxY = r.MaxY };
            public static SLine ToSLine(this GLineSegment seg) => new SLine() { StartPoint = seg.StartPoint.ToSPoint(), EndPoint = seg.EndPoint.ToSPoint() };
        }
        public class CircleDraw
        {
            public List<Vector2d> Vector2ds = new List<Vector2d>();
            public IEnumerable<Line> GetLines(Point3d basePt)
            {
                foreach (var v in Vector2ds)
                {
                    var endPt = basePt + v.ToVector3d();
                    yield return new Line() { StartPoint = basePt, EndPoint = endPt };
                }
            }
            public void Rotate(double radius, double degree)
            {
                var phi = GeoAlgorithm.AngleFromDegree(degree);
                var dx = radius * Math.Cos(phi); var dy = radius * Math.Sin(phi);
                Vector2ds.Add(new Vector2d(dx, dy));
            }
            public void OffsetX(double delta)
            {
                Vector2ds.Add(new Vector2d(delta, 0));
            }
            public void OffsetY(double delta)
            {
                Vector2ds.Add(new Vector2d(0, delta));
            }
            public void OffsetXY(double deltaX, double deltaY)
            {
                Vector2ds.Add(new Vector2d(deltaX, deltaY));
            }
        }
        public class YesDraw
        {
            public static List<Point3d> FixLines(List<Point3d> pts)
            {
                if (pts.Count < 2) return pts;
                var ret = new List<Point3d>(pts.Count);
                Point3d p1 = pts[0], p2 = pts[1];
                for (int i = 2; i < pts.Count; i++)
                {
                    var p3 = pts[i];
                    var v1 = p2.ToPoint2D() - p1.ToPoint2D();
                    var v2 = p3.ToPoint2D() - p2.ToPoint2D();
                    if (
                        (Math.Abs(GeoAlgorithm.AngleToDegree(v1.Angle) - GeoAlgorithm.AngleToDegree(v2.Angle)) < 1)
                        ||
                        (GeoAlgorithm.Distance(p1, p2) < 1)
                        ||
                        (GeoAlgorithm.Distance(p2, p3) < 1)
                        )
                    {
                    }
                    else
                    {
                        ret.Add(p1);
                        p1 = p2;
                    }
                    p2 = p3;
                }
                ret.Add(p1);
                ret.Add(p2);
                return ret;
            }
            public struct Context
            {
                public double DeltaX;
                public double DeltaY;
            }
            public List<Context> ctxs = new List<Context>();
            double curX;
            double curY;
            double minX;
            double minY;
            double maxX;
            double maxY;
            public double GetDeltaX() => maxX - minX;
            public double GetDeltaY() => maxY - minY;
            public IEnumerable<Point3d> GetPoint3ds(Point3d basePt)
            {
                yield return basePt;
                double sdx = 0, sdy = 0;
                foreach (var ctx in ctxs)
                {
                    sdx += ctx.DeltaX;
                    sdy += ctx.DeltaY;
                    yield return new Point3d(basePt.X + sdx, basePt.Y + sdy, 0);
                }
            }
            public void Rotate(double radius, double degree)
            {
                var phi = GeoAlgorithm.AngleFromDegree(degree);
                var dx = radius * Math.Cos(phi); var dy = radius * Math.Sin(phi);
                curX += dx; curY += dy;
                ctxs.Add(new Context() { DeltaX = dx, DeltaY = dy });
                fixData();
            }
            public void OffsetX(double delta)
            {
                curX += delta;
                ctxs.Add(new Context() { DeltaX = delta });
                fixData();
            }
            public void OffsetY(double delta)
            {
                curY += delta;
                ctxs.Add(new Context() { DeltaY = delta });
                fixData();
            }
            public void OffsetXY(double deltaX, double deltaY)
            {
                curX += deltaX; curY += deltaY;
                ctxs.Add(new Context() { DeltaX = deltaX, DeltaY = deltaY });
                fixData();
            }
            public void GoX(double x)
            {
                OffsetX(x - curX);
            }
            public void GoY(double y)
            {
                OffsetY(y - curY);
            }
            public void GoXY(double x, double y)
            {
                OffsetXY(x - curX, y - curY);
            }
            public Point3d GetFromRightTop(Point3d basePt)
            {
                var delX = maxX - minX;
                var delY = maxY - minY;
                return new Point3d(basePt.X - delX, basePt.Y - delY, 0);
            }
            public double GetCurX() => curX;
            public double GetCurY() => curY;
            public double GetOffsetX() => maxX - minX;
            public double GetOffsetY() => maxY - minY;
            public GRect GetGRect(Point3d basePt, bool bsPtIncluded)
            {
                List<Point3d> pts = GetPoint3ds(basePt, bsPtIncluded);
                return GeoAlgorithm.ToGRect(pts);
            }
            private List<Point3d> GetPoint3ds(Point3d basePt, bool bsPtIncluded)
            {
                var q = GetPoint3ds(basePt);
                if (!bsPtIncluded) q = q.Skip(1);
                var pts = q.ToList();
                return pts;
            }
            void fixData()
            {
                if (minX > curX) minX = curX;
                if (minY > curY) minY = curY;
                if (maxX < curX) maxX = curX;
                if (maxY < curY) maxY = curY;
            }
        }
        public static class PointExtensions
        {
            public static Point3d Rotate(this Point3d point, double radius, double degree)
            {
                var phi = GeoAlgorithm.AngleFromDegree(degree);
                return point.OffsetXY(radius * Math.Cos(phi), radius * Math.Sin(phi));
            }
            public static Point2d Rotate(this Point2d point, double radius, double degree)
            {
                var phi = GeoAlgorithm.AngleFromDegree(degree);
                return point.OffsetXY(radius * Math.Cos(phi), radius * Math.Sin(phi));
            }
            public static Point3d OffsetX(this Point3d point, double delta) => new(point.X + delta, point.Y, point.Z);
            public static Point3d OffsetY(this Point3d point, double delta) => new(point.X, point.Y + delta, point.Z);
            public static Point3d OffsetXY(this Point3d point, double deltaX, double deltaY) => new(point.X + deltaX, point.Y + deltaY, point.Z);
            public static Point3d ReplaceX(this Point3d point, double x) => new(x, point.Y, point.Z);
            public static Point3d ReplaceY(this Point3d point, double y) => new(point.X, y, point.Z);
            public static Point2d OffsetX(this Point2d point, double delta) => new(point.X + delta, point.Y);
            public static Point2d OffsetY(this Point2d point, double delta) => new(point.X, point.Y + delta);
            public static Point2d OffsetXY(this Point2d point, double deltaX, double deltaY) => new(point.X + deltaX, point.Y + deltaY);
            public static Point2d ReplaceX(this Point2d point, double x) => new(x, point.Y);
            public static Point2d ReplaceY(this Point2d point, double y) => new(point.X, y);
            public static Point2d Offset(this Point2d point, Vector2d v) => new Point2d(point.X + v.X, point.Y + v.Y);
            public static Point3d Offset(this Point3d point, Vector3d v) => new Point3d(point.X + v.X, point.Y + v.Y, point.Z + v.Z);
        }
    }
    public class ComparableCollection<T> : List<T>, IEquatable<ComparableCollection<T>>
    {
        public bool Equals(ComparableCollection<T> other)
        {
            if (this.Count != other.Count) return false;
            for (int i = 0; i < this.Count; i++)
            {
                if (!Equals(this[i], other[i])) return false;
            }
            return true;
        }
    }
    public class GuidDict : Dictionary<object, string>
    {
        public GuidDict() : base() { }
        public GuidDict(int cap) : base(cap) { }
        public string AddObj(object obj)
        {
            if (!this.TryGetValue(obj, out string ret))
            {
                ret = Guid.NewGuid().ToString("N");
                this[obj] = ret;
            }
            return ret;
        }
        public void AddObjs(IEnumerable objs)
        {
            foreach (var o in objs) AddObj(o);
        }
    }
    public class CText
    {
        public GRect Boundary;
        public string Text;
        public CText OffsetXY(double dx,double dy)
        {
            return new CText() { Text = Text, Boundary = Boundary.OffsetXY(dx, dy) };
        }
        public Polygon ToPolygon()
        {
            var pl = Boundary.ToPolygon();
            pl.UserData = Text;
            return pl;
        }
    }
    public struct GVector
    {
        public bool IsNull => Equals(this, default(GVector));
        public bool IsValid => Length > 0;
        public Point2d StartPoint;
        public Vector2d Vector;
        public Point2d EndPoint => StartPoint + Vector;
        public double Length => Vector.Length;
        public GVector(Point2d startPoint, Vector2d vector)
        {
            StartPoint = startPoint;
            Vector = vector;
        }
        public static GVector Create(Point2d startPoint, Vector2d vec, double len)
        {
            if (vec.Length > 0) return new GVector(startPoint, vec.GetNormal() * len);
            else return new GVector(startPoint, default);
        }
        public GVector Scale(double ratio)
        {
            if (!IsValid) return this;
            return new GVector(StartPoint, Vector * ratio);
        }
        public GVector Extend(double ext)
        {
            if (!IsValid) return this;
            return new GVector(StartPoint, Vector + Vector.GetNormal() * ext);
        }
        public GVector OffsetXY(double dx, double dy)
        {
            return new GVector(StartPoint.OffsetXY(dx, dy), Vector);
        }
        public GVector Create(double len)
        {
            if (!IsValid) return this;
            return new GVector(StartPoint, Vector.GetNormal() * len);
        }
    }
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
            //var pl = this.ToCadPolyline();
            //pl.TransformBy(matrix);
            //return pl.Bounds.ToGRect();
        }
        public GRect TransformBy(ref Matrix3d matrix)
        {
            if (this.IsNull) return this;
            if (matrix == Matrix3d.Identity) return this;
            var pl = this.ToCadPolyline();
            pl.TransformBy(matrix);
            return pl.Bounds.ToGRect();
        }
    }
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
        public GLine Line
        {
            get
            {
                if (!IsValid) return default;
                if (X1 == X2) return new GLine(1, 0, -X1);
                var k = (Y1 - Y2) / (X1 - X2);
                var b = Y1 - k * X1;
                return new GLine(k, b);
            }
        }
        public bool IsPointOnMe(Point2d point, double tollerance)
        {
            if (!this.Line.IsPointOnMe(point, tollerance)) return false;
            return MinX <= point.X && point.X <= MaxX && MinY <= point.Y && point.Y <= MaxY;
        }
        public double GetDistanceTo(Point2d point)
        {
            if (IsPointOnMe(point, double.Epsilon)) return 0;
            var pedalPt = this.Line.GetPedalPoint(point);
            if ((MinX <= pedalPt.X && pedalPt.X <= MaxX) || (MinY <= pedalPt.Y && pedalPt.Y <= MaxY))
            {
                return this.Line.GetDistance(point);
            }
            return Math.Max(StartPoint.GetDistanceTo(point), EndPoint.GetDistanceTo(point));
        }
        public Vector2d ToVector2d()
        {
            return EndPoint - StartPoint;
        }
    }
    public struct GLine
    {
        public double A;
        public double B;
        public double C;
        public bool IsNull => object.Equals(this, default);
        public bool IsValid => !IsNull;
        public GLine(double a, double b, double c)
        {
            A = a;
            B = b;
            C = c;
        }
        public GLine(double k, double b) : this(k, -1, b)
        {
        }
        public double k => -A / B;
        public double b => -C / B;
        public double Angle
        {
            get
            {
                if (IsVertical()) return Math.PI;
                return GeoAlgorithm.AngleFromDegree(Math.Atan(k));
            }
        }
        public bool IsVertical()
        {
            return k == double.PositiveInfinity || k == double.NegativeInfinity;
        }
        public bool IsVertical(double tollerance)
        {
            if (IsVertical()) return true;
            if (k > 0) return (double.MaxValue - k) < tollerance;
            if (k < 0) return Math.Abs((-double.MinValue - (-k))) < tollerance;
            return false;
        }
        public bool IsHorizontal(double tollerance)
        {
            return Math.Abs(k) < tollerance;
        }
        public bool IsParallel(GLine line2, double tollerance)
        {
            if (this.IsVertical(tollerance) && line2.IsVertical(tollerance)) return true;
            if (Math.Abs(this.k - line2.k) < tollerance) return true;
            return false;
        }
        public Point2d GetCrossPoint(GLine line2)
        {
            var x = (line2.C / line2.B - this.C / this.B) / (this.A / this.B - line2.A / line2.B);
            var y = -this.A / this.B * x - this.C / this.B;
            return new Point2d(x, y);
        }
        public double GetDistance(Point2d point)
        {
            return Math.Abs(A * point.X + B * point.Y + C) / Math.Sqrt(A * A + B * B);
        }
        public bool IsPointOnMe(Point2d point, double tollerance)
        {
            return this.GetDistance(point) < tollerance;
        }
        public Point2d GetPedalPoint(Point2d point)
        {
            if (IsPointOnMe(point, double.Epsilon)) return point;
            return GetCrossPoint(_GetPedalLine(point));
        }
        public GLine GetPedalLine(Point2d point)
        {
            if (IsPointOnMe(point, double.Epsilon)) return default;
            return _GetPedalLine(point);
        }

        private GLine _GetPedalLine(Point2d point)
        {
            return new GLine(B, -A, A * point.Y - B * point.X);
        }
    }
    public struct GArc
    {
        public double X;
        public double Y;
        public Point2d Center => new(X, Y);
        public double Radius;
        public double StartAngle;
        public double EndAngle;
        public bool IsClockWise;
        public GArc(Point2d center, double radius, double startAngle, double endAngle, bool isClockWise) : this(center.X, center.Y, radius, startAngle, endAngle, isClockWise) { }
        public GArc(Point3d center, double radius, double startAngle, double endAngle, bool isClockWise) : this(center.X, center.Y, radius, startAngle, endAngle, isClockWise) { }
        public GArc(double x, double y, double radius, double startAngle, double endAngle, bool isClockWise)
        {
            X = x;
            Y = y;
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
            IsClockWise = isClockWise;
        }

        public GCircle ToGCircle() => new(X, Y, Radius);
        public GArc OffsetXY(double dx, double dy)
        {
            return new GArc(X + dx, Y + dy, Radius, StartAngle, EndAngle, IsClockWise);
        }
    }
    public struct GCircle
    {
        public double X;
        public double Y;
        public double Radius;
        public bool IsValid => Radius > 0 && !double.IsNaN(X) && !double.IsNaN(Y);
        public GCircle(double x, double y, double radius)
        {
            X = x;
            Y = y;
            this.Radius = radius;
        }
        public GCircle(Point2d center, double radius) : this(center.X, center.Y, radius)
        {
        }
        public Point2d Center => new Point2d(X, Y);
        public GCircle OffsetXY(double dx, double dy)
        {
            return new GCircle(X + dx, Y + dy, Radius);
        }
        public bool IsPointInMe(Point2d point)
        {
            return GeoAlgorithm.Distance(point, Center) < Radius;
        }
        public bool IsPointOutOfMe(Point2d point)
        {
            return GeoAlgorithm.Distance(point, Center) > Radius;
        }
        public bool IsPointOnMe(Point2d point, double tollerance)
        {
            return Math.Abs(GeoAlgorithm.Distance(point, Center) - Radius) < tollerance;
        }
    }
    public struct GTriangle
    {
        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
        public double X3 { get; }
        public double Y3 { get; }

        public GTriangle(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            X3 = x3;
            Y3 = y3;
        }
        public Point2d A => new Point2d(X1, Y1);
        public Point2d B => new Point2d(X2, Y2);
        public Point2d C => new Point2d(X2, Y3);
        public GLineSegment LineC => new GLineSegment(A, B);
        public GLineSegment LineA => new GLineSegment(B, C);
        public GLineSegment LineB => new GLineSegment(C, A);
        public double c => LineC.Length;
        public double a => LineA.Length;
        public double b => LineB.Length;
        public double Perimeter => a + b + c;
        public double AreaSize
        {
            get
            {
                var p = Perimeter / 2;
                return Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }
        }
        public bool IsValid
        {
            get
            {
                var p = Perimeter / 2;
                return (p - a) > 0 && (p - b) > 0 && (p - c) > 0;
            }
        }
        public double AngleA => GeoAlgorithm.AngleFromDegree(AngleADegree);

        private double AngleADegree => Math.Acos((b * b + c * c - a * a) / 2 * b * c);
        public double AngleB => GeoAlgorithm.AngleFromDegree(AngleBDegree);

        private double AngleBDegree => Math.Acos((a * a + c * c - b * b) / 2 * a * c);

        public double AngleC => GeoAlgorithm.AngleFromDegree(AngleCDegree);

        private double AngleCDegree => Math.Acos((a * a + b * b - c * c) / 2 * a * b);
        public bool IsObtuse => Math.Max(Math.Max(AngleADegree, AngleBDegree), AngleCDegree) > 90;
        public bool IsSharp => Math.Max(Math.Max(AngleADegree, AngleBDegree), AngleCDegree) < 90;
        public bool IsPerpendicular(double tollerance)
        {
            return Math.Abs(Math.Max(Math.Max(AngleADegree, AngleBDegree), AngleCDegree) - 90) < tollerance;
        }
    }
    public enum GOrientation
    {
        Horizontal,
        Vertical,
    }
    public static class LinqAlgorithm
    {
        public static IEnumerable<V> SelectNotNull<T, V>(this IEnumerable<T> source, Func<T, V> f) where V : class
        {
            foreach (var item in source)
            {
                var v = f(item);
                if (v != null) yield return v;
            }
        }
        public static IEnumerable<T> SelectNotNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where(x => x != null);
        }
        public static T GetLastOrDefault<T>(this IList<T> source, int i)
        {
            return source.GetAt(source.Count - i);
        }
        public static T GetAt<T>(this IList<T> source, int i)
        {
            if (i >= 0 && i < source.Count) return source[i];
            return default;
        }
        public static bool EqualsDefault<T>(this T valueObj) where T : struct
        {
            return Equals(valueObj, default(T));
        }
        public static IEnumerable<T> YieldBefore<T>(this IEnumerable<T> souce, T item)
        {
            yield return item;
            foreach (var m in souce)
            {
                yield return m;
            }
        }
        public static IEnumerable<T> Yield<T>(this IEnumerable<T> source)
        {
            if (source == null) return Enumerable.Empty<T>();
            return source;
        }
        public static IEnumerable<T> YieldAfter<T>(this IEnumerable<T> souce, T item)
        {
            foreach (var m in souce)
            {
                yield return m;
            }
            yield return item;
        }
        public static Action Once(Action f)
        {
            bool ok = false;
            return new Action(() =>
            {
                if (!ok)
                {
                    f?.Invoke();
                    ok = true;
                }
            });
        }
        public static T GetLast<T>(this IList<T> source, int n)
        {
            return source[source.Count - n];
        }
        public static T FirstOrDefault<T>(Func<T, bool> f, params T[] source)
        {
            return source.FirstOrDefault(f);
        }
        public static IEnumerable<KeyValuePair<T, T>> YieldPairsByGroup<T>(IList<IList<T>> gs)
        {
            foreach (var g in gs)
            {
                foreach (var r in YieldPairs(g))
                {
                    yield return r;
                }
            }
        }
        public static IEnumerable<KeyValuePair<T, T>> YieldPairs<T>(IList<T> g)
        {
            for (int i = 0; i < g.Count; i++)
            {
                for (int j = i + 1; j < g.Count; j++)
                {
                    yield return new KeyValuePair<T, T>(g[i], g[j]);
                }
            }
        }
        public static IEnumerable<KeyValuePair<T, T>> YieldPairs<T>(IList<T> l1, IList<T> l2)
        {
            foreach (var m1 in l1)
            {
                foreach (var m2 in l2)
                {
                    yield return new KeyValuePair<T, T>(m1, m2);
                }
            }
        }
        public static IEnumerable<KeyValuePair<T, T>> YieldPairs<T>(List<List<T>> groups)
        {
            foreach (var g in groups)
            {
                for (int i = 0; i < g.Count; i++)
                {
                    for (int j = i + 1; j < g.Count; j++)
                    {
                        yield return new KeyValuePair<T, T>(g[i], g[j]);
                    }
                }
            }
        }
    }
    public static class Matrix2dFac
    {
        public static Matrix2d XAxisMirroring = Matrix2d.Mirroring(new Line2d(new Point2d(0, 0), new Point2d(1, 0)));
        public static Matrix2d YAxisMirroring = Matrix2d.Mirroring(new Line2d(new Point2d(0, 0), new Point2d(0, 1)));
    }
    public static class EntityFactory
    {
        public static Point2d GetLastPoint(this IEnumerable<Vector2d> vector2Ds, Point3d basePt) => vector2Ds.GetLastPoint(basePt.ToPoint2D());
        public static Point2d GetLastPoint(this IEnumerable<Vector2d> vector2Ds, Point2d basePt)
        {
            var p1 = basePt;
            Point2d p2 = p1;
            foreach (var vec in vector2Ds)
            {
                p2 = p1 + vec;
                p1 = p2;
            }
            return p2;
        }
        public static List<Point2d> ToPoint2ds(this List<Vector2d> vector2Ds, Point2d basePt)
        {
            var p1 = basePt;
            Point2d p2;
            var points = new List<Point2d>(vector2Ds.Count + 1) { p1 };
            foreach (var vec in vector2Ds)
            {
                p2 = p1 + vec;
                points.Add(p2);
                p1 = p2;
            }
            return points;
        }
        public static List<GLineSegment> ToGLineSegments(this List<Vector2d> vector2Ds, Point3d basePt) => vector2Ds.ToGLineSegments(basePt.ToPoint2D());
        public static List<GLineSegment> ToGLineSegments(this List<Vector2d> vector2Ds, Point2d basePt)
        {
            var p1 = basePt;
            Point2d p2;
            var segs = new List<GLineSegment>(vector2Ds.Count);
            foreach (var vec in vector2Ds)
            {
                p2 = p1 + vec;
                segs.Add(new GLineSegment(p1, p2));
                p1 = p2;
            }
            return segs;
        }
        public static List<Vector3d> ToVector3ds(this Point3d[] points)
        {
            if (points.Length <= 1) return new List<Vector3d>();
            var vecs = new List<Vector3d>(points.Length - 1);
            for (int i = 0; i < points.Length - 1; i++)
            {
                vecs.Add(points[i + 1] - points[i]);
            }
            return vecs;
        }
        public static List<Vector2d> ToVector2ds(this Point2d[] points)
        {
            if (points.Length <= 1) return new List<Vector2d>();
            var vecs = new List<Vector2d>(points.Length - 1);
            for (int i = 0; i < points.Length - 1; i++)
            {
                vecs.Add(points[i + 1] - points[i]);
            }
            return vecs;
        }
        public static Point2d[] MoveXY(this Point2d[] points, double dx, double dy)
        {
            var m = Matrix2d.Displacement(new Vector2d(dx, dy));
            return points.Select(x => x.TransformBy(m)).ToArray();
        }
        public static List<GLineSegment> ToGLineSegments(this Point2d[] points, Point3d basePt, Matrix2d matrix2D)
        {
            if (points.Length <= 1) return new List<GLineSegment>();
            points = points.Select(pt => pt.TransformBy(matrix2D)).ToArray();
            return ToGLineSegments(points, basePt);
        }
        public static Point2d[] GetYAxisMirror(this Point2d[] points)
        {
            return points.Select(pt => new Point2d(-pt.X, pt.Y)).ToArray();
        }
        public static List<Vector2d> GetYAxisMirror(this List<Vector2d> vecs)
        {
            return vecs.Select(v => new Vector2d(-v.X, v.Y)).ToList();
        }
        public static List<GLineSegment> ToGLineSegments(this Point2d[] points, Point3d basePt)
        {
            if (points.Length <= 1) return new List<GLineSegment>();
            var segs = new List<GLineSegment>(points.Length - 1);
            var vec = basePt.ToPoint2d() - points[0];
            for (int i = 0; i < points.Length - 1; i++)
            {
                segs.Add(new GLineSegment(points[i] + vec, points[i + 1] + vec));
            }
            return segs;
        }
        public static List<GLineSegment> ToGLineSegments(this Point2d[] points)
        {
            if (points.Length <= 1) return new List<GLineSegment>();
            var segs = new List<GLineSegment>(points.Length - 1);
            for (int i = 0; i < points.Length - 1; i++)
            {
                segs.Add(new GLineSegment(points[i], points[i + 1]));
            }
            return segs;
        }
        public static List<GLineSegment> ToGLineSegments(this List<Point2d> points)
        {
            if (points.Count <= 1) return new List<GLineSegment>();
            var segs = new List<GLineSegment>(points.Count - 1);
            for (int i = 0; i < points.Count - 1; i++)
            {
                segs.Add(new GLineSegment(points[i], points[i + 1]));
            }
            return segs;
        }
        public static Polyline CreatePolyline(this Point3dCollection pts)
        {
            var pl = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }
            return pl;
        }
        public static Polyline CreatePolyline(this IList<Point3d> pts)
        {
            var pl = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }
            return pl;
        }
        public static Polyline ToObb(this BlockReference br)
        {
            return br.ToOBB(br.BlockTransform);
        }
    }
    public static class GeoAlgorithm
    {
        public static double Round(this double num, double prec)
        {
            return prec == 0.0 ? num : Math.Floor(num / prec + 0.5) * prec;
        }
        public static GRect ToGRect(this Point3dCollection pts)
        {
            if (pts.Count == 0)
            {
                return default;
            }
            var minX = pts.Cast<Point3d>().Select(p => p.X).Min();
            var maxX = pts.Cast<Point3d>().Select(p => p.X).Max();
            var minY = pts.Cast<Point3d>().Select(p => p.Y).Min();
            var maxY = pts.Cast<Point3d>().Select(p => p.Y).Max();
            return new GRect(minX, minY, maxX, maxY);
        }
        public static Vector3d ToVector3d(this Point3d pt) => new Vector3d(pt.X, pt.Y, pt.Z);
        public static Vector2d ToVector2d(this Point2d pt) => new Vector2d(pt.X, pt.Y);
        public static Point2d ToLongPoint2d(this Point2d pt)
        {
            return new Point2d(Convert.ToInt64(pt.X), Convert.ToInt64(pt.Y));
        }
        public static Point3d ToPoint3d(this Vector3d v)
        {
            return new Point3d(v.X, v.Y, v.Z);
        }
        public static Point2d ToPoint2d(this Vector2d v)
        {
            return new Point2d(v.X, v.Y);
        }
        public static bool IsParallelTo(this GLineSegment first, GLineSegment second, double angleTol)
        {
            var v1 = first.EndPoint - first.StartPoint;
            var v2 = second.EndPoint - second.StartPoint;
            var angle = v1.GetAngleTo(v2).AngleToDegree();
            return angle.EqualsTo(.0, angleTol) || angle.EqualsTo(180.0, angleTol);
        }
        public static bool EqualsTo(this double value1, double value2, double tollerance)
        {
            return value2 - tollerance <= value1 && value1 <= value2 + tollerance;
        }
        public static bool InRange(this double value, double std, double tollerance)
        {
            return std - tollerance <= value && value <= std + tollerance;
        }
        public static IEnumerable<KeyValuePair<Point2d, Point2d>> YieldPoints(GLineSegment seg1, GLineSegment seg2)
        {
            yield return new KeyValuePair<Point2d, Point2d>(seg1.StartPoint, seg2.StartPoint);
            yield return new KeyValuePair<Point2d, Point2d>(seg1.StartPoint, seg2.EndPoint);
            yield return new KeyValuePair<Point2d, Point2d>(seg1.EndPoint, seg2.StartPoint);
            yield return new KeyValuePair<Point2d, Point2d>(seg1.EndPoint, seg2.EndPoint);
        }
        public static Point2d MidPoint(Point2d pt1, Point2d pt2)
        {
            Point2d midPoint = new Point2d((pt1.X + pt2.X) / 2.0,
                                        (pt1.Y + pt2.Y) / 2.0);
            return midPoint;
        }
        public static Point3d MidPoint(Point3d pt1, Point3d pt2)
        {
            Point3d midPoint = new Point3d((pt1.X + pt2.X) / 2.0,
                                        (pt1.Y + pt2.Y) / 2.0,
                                        (pt1.Z + pt2.Z) / 2.0);
            return midPoint;
        }
        public static GRect ToGRect
            (IList<Point3d> pts)
        {
            if (pts.Count == 0) return default;
            GetCornerCoodinate(pts, out double minX, out double minY, out double maxX, out double maxY);
            return new GRect(minX, minY, maxX, maxY);
        }
        public static void GetCornerCoodinate(Point3dCollection pts, out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = pts.Cast<Point3d>().Select(pt => pt.X).Min();
            minY = pts.Cast<Point3d>().Select(pt => pt.Y).Min();
            maxX = pts.Cast<Point3d>().Select(pt => pt.X).Max();
            maxY = pts.Cast<Point3d>().Select(pt => pt.Y).Max();
        }
        public static void GetCornerCoodinate(IList<Point3d> pts, out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = pts.Select(pt => pt.X).Min();
            minY = pts.Select(pt => pt.Y).Min();
            maxX = pts.Select(pt => pt.X).Max();
            maxY = pts.Select(pt => pt.Y).Max();
        }
        public static bool CanConnect(GLineSegment seg1, GLineSegment seg2, double angleTol, double dis)
        {
            return GetMinConnectionDistance(seg1, seg2) <= dis && Math.Abs(seg1.AngleDegree - seg2.AngleDegree) <= angleTol;
        }
        public static double mult(Point2d p0, Point2d p1, Point2d p2) //叉积计算,p0为公用节点
        {
            return (p0.X - p1.X) * (p0.Y - p2.Y) - (p0.Y - p1.Y) * (p0.X - p2.X);
        }
        //判断线段是否相交
        public static bool IsCross(GLineSegment seg1, GLineSegment seg2)
        {
            var p1 = seg1.StartPoint;
            var p2 = seg1.EndPoint;
            var p3 = seg2.StartPoint;
            var p4 = seg2.EndPoint;
            //先判断两个形成的矩形是否不相交，不相交那线段肯定不相交
            if (Math.Max(p1.X, p2.X) < Math.Min(p3.X, p4.X)) return false;
            if (Math.Max(p1.Y, p2.Y) < Math.Min(p3.Y, p4.Y)) return false;
            if (Math.Max(p3.X, p4.X) < Math.Min(p1.X, p2.X)) return false;
            if (Math.Max(p3.Y, p4.Y) < Math.Min(p1.Y, p2.Y)) return false;
            //现在已经满足快速排斥实验，那么后面就是跨立实验内容(叉积判断两个线段是否相交)
            //正确的话也就是p1,p2要在p3或者p4的两边
            if (IsDiffSign(mult(p1, p3, p2), mult(p1, p2, p4))) return false;
            if (IsDiffSign(mult(p3, p1, p4), mult(p3, p4, p2))) return false;
            return true;
        }
        public static bool IsDiffSign(double v1, double v2)
        {
            return v1 < 0 && v2 > 0 || v1 > 0 && v2 < 0;
        }
        public static Point3dCollection GetPoint3dCollection(Point3d pt1, Point3d pt2)
        {
            var points = new Point3dCollection();
            points.Add(pt1);
            points.Add(new Point3d(pt1.X, pt2.Y, 0));
            points.Add(pt2);
            points.Add(new Point3d(pt2.X, pt1.Y, 0));
            return points;
        }
        public static GCircle ToGCircle(this Circle x)
        {
            return new GCircle(x.Center.X, x.Center.Y, x.Radius);
        }
        public static void Erase(this Entity ent, AcadDatabase adb)
        {
            adb.Element<Entity>(ent.ObjectId, true).Erase();
        }
        public static LineString ToLineString(this Line line)
        {
            return new LineString(new Coordinate[] { line.StartPoint.ToNTSCoordinate(), line.EndPoint.ToNTSCoordinate() });
        }
        public static LineString ToLineString(this Polyline line)
        {
            if (line.NumberOfVertices == 0) return new LineString(new Coordinate[] { });
            if (line.Closed)
            {
                var cords = line.Vertices().OfType<Point3d>().Select(x => x.ToNTSCoordinate()).ToList();
                cords.Add(cords[0]);
                return new LinearRing(cords.ToArray());
            }
            else
            {
                return new LineString(line.Vertices().OfType<Point3d>().Select(x => x.ToNTSCoordinate()).ToArray());
            }
        }
        public static GLineSegment ToGLineSegment(this Line line)
        {
            return new GLineSegment(line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D());
        }
        public static Point2d[] GetPoint2ds(this Polyline pline)
        {
            var rt = new Point2d[pline.NumberOfVertices];
            for (int i = 0; i < rt.Length; i++)
            {
                rt[i] = pline.GetPoint2dAt(i);
            }
            return rt;
        }
        public static double GetMinConnectionDistance(GLineSegment seg1, GLineSegment seg2)
        {
            var p1 = seg1.StartPoint;
            var p2 = seg1.EndPoint;
            var p3 = seg2.StartPoint;
            var p4 = seg2.EndPoint;
            var v1 = p3 - p1;
            var v2 = p4 - p1;
            var v3 = p3 - p2;
            var v4 = p4 - p2;
            return new Vector2d[] { v1, v2, v3, v4 }.Select(v => v.Length).Min();
        }
        public static bool TryConvertToLineSegment(Entity ent, out GLineSegment lineSeg)
        {
            if (ent is Line line)
            {
                lineSeg = new GLineSegment(line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D());
                return true;
            }
            {
                var p1 = ent.GetType().GetProperty("StartPoint");
                if (p1 == null)
                {
                    lineSeg = default;
                    return false;
                }
                var p2 = ent.GetType().GetProperty("EndPoint");
                if (p2 == null)
                {
                    lineSeg = default;
                    return false;
                }
                if (p1.PropertyType != typeof(Point3d))
                {
                    lineSeg = default;
                    return false;
                }
                if (p2.PropertyType != typeof(Point3d))
                {
                    lineSeg = default;
                    return false;
                }
                var pt1 = (Point3d)p1.GetValue(ent);
                var pt2 = (Point3d)p2.GetValue(ent);
                lineSeg = new GLineSegment(pt1.ToPoint2D(), pt2.ToPoint2D());
                return true;
            }
        }
        public static bool IsLineConnected(Entity e1, Entity e2, double tollerance = 10)
        {
            var p1 = e1.GetType().GetProperty("StartPoint");
            if (p1 == null) return false;
            var p2 = e2.GetType().GetProperty("StartPoint");
            if (p2 == null) return false;
            var p3 = e1.GetType().GetProperty("EndPoint");
            if (p3 == null) return false;
            var p4 = e2.GetType().GetProperty("EndPoint");
            if (p4 == null) return false;
            if (p1.PropertyType != typeof(Point3d) || p2.PropertyType != typeof(Point3d) || p3.PropertyType != typeof(Point3d) || p4.PropertyType != typeof(Point3d)) return false;
            var pt1 = (Point3d)p1.GetValue(e1);
            var pt2 = (Point3d)p2.GetValue(e2);
            var pt3 = (Point3d)p3.GetValue(e1);
            var pt4 = (Point3d)p4.GetValue(e2);
            return IsLineConnected(pt1, pt2, pt3, pt4, tollerance);
        }
        public static bool IsLineConnected(GLineSegment seg1, GLineSegment seg2, double tollerance)
        {
            return IsLineConnected(seg1.StartPoint, seg2.StartPoint, seg1.EndPoint, seg2.EndPoint, tollerance);
        }
        private static bool IsLineConnected(Point2d pt1, Point2d pt2, Point2d pt3, Point2d pt4, double tollerance)
        {
            return ((GeoAlgorithm.Distance(pt1, pt2) < tollerance) || (GeoAlgorithm.Distance(pt1, pt4) < tollerance) || (GeoAlgorithm.Distance(pt3, pt2) < tollerance) || (GeoAlgorithm.Distance(pt3, pt4) < tollerance));
        }
        private static bool IsLineConnected(Point3d pt1, Point3d pt2, Point3d pt3, Point3d pt4, double tollerance)
        {
            return ((GeoAlgorithm.Distance(pt1, pt2) < tollerance) || (GeoAlgorithm.Distance(pt1, pt4) < tollerance) || (GeoAlgorithm.Distance(pt3, pt2) < tollerance) || (GeoAlgorithm.Distance(pt3, pt4) < tollerance));
        }
        public static Tuple<double, double> GetXY(double radius, double degree)
        {
            var phi = AngleFromDegree(degree);
            return new Tuple<double, double>(radius * Math.Cos(phi), radius * Math.Sin(phi));
        }
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static double AngleFromDegree(this double degree)
        {
            return degree * Math.PI / 180;
        }
        public static double Distance(Point2d pt1, Point2d pt2)
        {
            return (pt1 - pt2).Length;
        }
        public static double Distance(Point3d pt1, Point3d pt2)
        {
            return (pt1 - pt2).Length;
        }
        public static bool PointInCircle(Point3d center, double radius, Point3d targetPoint)
        {
            return Distance(center, targetPoint) < radius;
        }
        public static bool PointOnCircle(Point3d center, double radius, Point3d targetPoint, double tolerance)
        {
            return Math.Abs((Distance(center, targetPoint) - radius)) < tolerance;
        }
        public static bool PointOutOfCircle(Point3d center, double radius, Point3d targetPoint)
        {
            return Distance(center, targetPoint) > radius;
        }
        public static bool IsRectCross(GRect r1, GRect r2)
        {
            return IsRectCross(r1.MinX, r1.MinY, r1.MaxX, r1.MaxY, r2.MinX, r2.MinY, r2.MaxX, r2.MaxY);
        }
        public static bool IsRectCross(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            return Math.Max(x1, x3) <= Math.Min(x2, x4) && Math.Max(y1, y3) <= Math.Min(y2, y4);
        }
        public static GRect ExpandRect(GRect rect, double width)
        {
            var x1 = rect.LeftTop.X - width;
            var y1 = rect.LeftTop.Y + width;
            var x2 = rect.RightButtom.X + width;
            var y2 = rect.RightButtom.Y - width;
            return new GRect(new Point2d(Math.Min(x1, x2), Math.Max(y1, y2)),
              new Point2d(Math.Max(x1, x2), Math.Min(y1, y2)));
        }
        public static GRect GetEntitiesBoundaryRect(IEnumerable<Entity> ents)
        {
            if (!SystemDiagramUtils.GetBoundaryRect(out double minX, out double minY, out double maxX, out double maxY, ents.ToArray())) return default;
            return new GRect(minX, minY, maxX, maxY);
        }
        public static GRect GetBoundaryRect(params Entity[] ents)
        {
            if (!SystemDiagramUtils.GetBoundaryRect(out double minX, out double minY, out double maxX, out double maxY, ents)) return default;
            return new GRect(minX, minY, maxX, maxY);
        }
    }
    public class SystemDiagramUtils
    {
        private static Tuple<Point3d, Point3d> SelectPoints()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }

        public static List<string> GetFloorListDatas()
        {
            using (var doclock = Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取楼层框线图块
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择楼层框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return new List<string>();
                }
                var selectedIds = result.Value.GetObjectIds();
                List<string> storeyInfoList = GetStoreyInfoList(acadDatabase, selectedIds);
                return storeyInfoList;
            }
        }

        public static List<string> GetStoreyInfoList(AcadDatabase acadDatabase, ObjectId[] selectedIds)
        {
            var storeys = selectedIds
                .Select(o => acadDatabase.Element<BlockReference>(o))
                .Where(o => o.GetEffectiveName() == ThWPipeCommon.STOREY_BLOCK_NAME)
                .Select(o => o.ObjectId)
                .ToObjectIdCollection();

            // 获取楼层名称
            var service = new ThReadStoreyInformationService();
            service.Read(storeys);
            var storeyInfoList = service.StoreyNames.Select(o => o.Item2).ToList();
            return storeyInfoList;
        }

        public static bool GetBoundaryRect(out double minX, out double minY, out double maxX, out double maxY, params Entity[] ents)
        {
            var pts = new Point3dCollection();
            foreach (var ent in ents)
            {
                if (ent?.Bounds is Extents3d bd)
                {
                    pts.Add(bd.MaxPoint);
                    pts.Add(bd.MinPoint);
                }
            }
            return GetExtend2d(out minX, out minY, out maxX, out maxY, pts);
        }

        public static bool GetExtend2d(out double minX, out double minY, out double maxX, out double maxY, Point3dCollection pts)
        {
            if (pts.Count == 0)
            {
                minX = minY = maxX = maxY = default;
                return false;
            }
            minX = pts.Cast<Point3d>().Select(p => p.X).Min();
            maxX = pts.Cast<Point3d>().Select(p => p.X).Max();
            minY = pts.Cast<Point3d>().Select(p => p.Y).Min();
            maxY = pts.Cast<Point3d>().Select(p => p.Y).Max();
            return true;
        }

        static public void DrawLine(Point3d startPt, Point3d endPt, string layerName)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var line = new Line(startPt, endPt);
                line.Layer = layerName;
                db.ModelSpace.Add(line);
            }
        }
        static public Pipe.Model.WaterBucketEnum GetRelatedWaterBucket(Database db, Point3dCollection range, Point3d centerOfPipe)
        {
            var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
            gravityBucketEngine.Recognize(db, range);
            var gravities = gravityBucketEngine.Elements;

            var gravitiesExtents = gravities.Select(g => g.Outline.GeometricExtents);
            foreach (var e in gravitiesExtents)
            {
                if (e.IsPointIn(centerOfPipe))
                {
                    return WaterBucketEnum.Gravity;
                }
            }

            var sidebucketEngine = new ThWSideEntryWaterBucketRecognitionEngine();
            sidebucketEngine.Recognize(db, range);
            var sides = sidebucketEngine.Elements;
            var sidesExtents = sides.Select(g => g.Outline.GeometricExtents);
            foreach (var e in sidesExtents)
            {
                if (e.IsPointIn(centerOfPipe))
                {
                    return WaterBucketEnum.Side;
                }
            }

            return Pipe.Model.WaterBucketEnum.None;
        }

    }
}
