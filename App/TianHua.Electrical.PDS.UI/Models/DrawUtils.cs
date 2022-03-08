using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;


namespace TianHua.Electrical.PDS.UI.Models
{
    public class _DrawingTransaction : IDisposable
    {
        public DBText dBText;
        public readonly Dictionary<string, ObjectId> TextStyleIdDict = new Dictionary<string, ObjectId>();
        public bool NoDraw;
        public bool? AbleToDraw = null;
        public static _DrawingTransaction Current { get; private set; }
        public AcadDatabase adb { get; private set; }
        public _DrawingTransaction(AcadDatabase adb) : this()
        {
            this.adb = adb;
        }
        public _DrawingTransaction(AcadDatabase adb, bool noDraw) : this()
        {
            this.adb = adb;
            this.NoDraw = noDraw;
        }
        public _DrawingTransaction()
        {
            DrawUtils.DrawingQueue.Clear();
            Current = this;
        }
        public void Dispose()
        {
            try
            {
                dBText?.Dispose();
                if (!NoDraw)
                {
                    if (AbleToDraw != false)
                    {
                        if (adb != null)
                        {
                            DrawUtils.FlushDQ(adb);
                        }
                        else
                        {
                            DrawUtils.FlushDQ();
                        }
                    }
                }
            }
            finally
            {
                Current = null;
                DrawUtils.Dispose();
            }
        }

        HashSet<string> visibleLayers = new HashSet<string>();
        HashSet<string> invisibleLayers = new HashSet<string>();
        public bool IsLayerVisible(string layer)
        {
            if (visibleLayers.Contains(layer)) return true;
            if (invisibleLayers.Contains(layer)) return false;
            var ly = adb.Layers.ElementOrDefault(layer);
            if (ly != null && !ly.IsFrozen && !ly.IsOff && !ly.IsHidden)
            {
                visibleLayers.Add(layer);
                return true;
            }
            else
            {
                invisibleLayers.Add(layer);
                return false;
            }
        }

    }
    public static class DrawUtils
    {
        public static DocumentLock DocLock => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument();
        public static _DrawingTransaction DrawingTransaction => new _DrawingTransaction();
        public static Queue<Action<AcadDatabase>> DrawingQueue { get; } = new Queue<Action<AcadDatabase>>(4096);
        public static void DrawEntityLazy(Entity ent)
        {
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(ent));
        }
        public static void Dispose()
        {
            DrawingQueue.Clear();
        }
        public static void FlushDQ()
        {
            if (DrawingQueue.Count == 0) return;
            using var adb = AcadDatabase.Active();
            FlushDQ(adb);
        }
        public static Circle DrawCircleLazy(Point2d center, double radius)
        {
            return DrawCircleLazy(center.ToPoint3d(), radius);
        }
        public static Circle DrawCircleLazy(Point3d center, double radius)
        {
            if (radius <= 0) radius = 1;
            var circle = new Circle() { Center = center, Radius = radius };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(circle);
            });
            return circle;
        }
        public static DBText DrawTextLazy(string text, double height, Point2d position) => DrawTextLazy(text, height, position.ToPoint3d());
        public static DBText DrawTextLazy(string text, Point2d position)
        {
            return DrawTextLazy(text, position.ToPoint3d());
        }
        public static DBText DrawTextLazy(string text, Point3d position)
        {
            return DrawTextLazy(text, 100, position);
        }
        public static void FlushDQ(AcadDatabase adb)
        {
            try
            {
                while (DrawingQueue.Count > 0)
                {
                    DrawingQueue.Dequeue()(adb);
                }
            }
            finally
            {
                if (DrawingQueue.Count > 0) DrawingQueue.Clear();
            }
        }
        public static DBText DrawTextLazy(string text, double height, Point3d position, Action<DBText> cb = null)
        {
            var dbText = new DBText
            {
                TextString = text,
                Position = position,
                Height = height,
            };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(dbText);
                cb?.Invoke(dbText);
            });
            return dbText;
        }
        public static void DrawEntitiesLazy<T>(IList<T> ents) where T : Entity
        {
            DrawingQueue.Enqueue(adb => ents.ForEach(ent => adb.ModelSpace.Add(ent)));
        }
        public static Line DrawLineSegmentLazy(GLineSegment seg, string layer)
        {
            var line = DrawLineSegmentLazy(seg);
            line.ColorIndex = 256;
            line.Layer = layer;
            return line;
        }
        public static void ByLayer(Entity line)
        {
            line.ColorIndex = 256;
            line.LineWeight = LineWeight.ByLayer;
            line.Linetype = "ByLayer";
        }
        public static Line DrawLineSegmentLazy(GLineSegment seg)
        {
            return DrawLineLazy(seg.StartPoint, seg.EndPoint);
        }
        public static Line DrawLineLazy(double x1, double y1, double x2, double y2)
        {
            return DrawLineLazy(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
        }
        public static Line DrawLineLazy(Point2d start, Point2d end)
        {
            return DrawLineLazy(start.ToPoint3d(), end.ToPoint3d());
        }
        public static Line DrawLineLazy(Point3d start, Point3d end)
        {
            var line = new Line() { StartPoint = start, EndPoint = end };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(line);
            });
            return line;
        }
        public static List<Line> DrawLineSegmentsLazy(IEnumerable<GLineSegment> segs)
        {
            var lines = segs.Select(seg => new Line() { StartPoint = seg.StartPoint.ToPoint3d(), EndPoint = seg.EndPoint.ToPoint3d() }).ToList();
            DrawingQueue.Enqueue(adb =>
            {
                foreach (var line in lines)
                {
                    adb.ModelSpace.Add(line);
                }
            });
            return lines;
        }
        public static Polyline DrawLineSegmentLazy(GLineSegment seg, double width)
        {
            var pl = DrawPolyLineLazy(new Point2d[] { seg.StartPoint, seg.EndPoint });
            pl.ConstantWidth = width;
            return pl;
        }
        public static Polyline DrawPolyLineLazy(params Point2d[] pts)
        {
            var c = new Point2dCollection();
            foreach (var pt in pts)
            {
                c.Add(pt);
            }
            var pl = new Polyline();
            CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
        static void CreatePolyline(Polyline pline, Point2dCollection pts)
        {
            for (int i = 0; i < pts.Count; i++)
            {
                pline.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
            }
        }
        public static Polyline DrawPolyLineLazy(params Point3d[] pts)
        {
            var c = new Point2dCollection();
            foreach (var pt in pts)
            {
                c.Add(pt.ToPoint2d());
            }
            var pl = new Polyline();
            CreatePolyline(pl, c);
            DrawingQueue.Enqueue(adb => adb.ModelSpace.Add(pl));
            return pl;
        }
    }
    public static class GeoAlgorithm
    {
        public static GCircle ToGCircle(this Circle x)
        {
            return new GCircle(x.Center.X, x.Center.Y, x.Radius);
        }
        public static GArc ToGArc(this Arc arc) => new(arc.Center.X, arc.Center.Y, arc.Radius, arc.StartAngle, arc.EndAngle, arc.Normal.DotProduct(Vector3d.ZAxis) < 0);
        public static GLineSegment ToGLineSegment(this Line line)
        {
            return new GLineSegment(line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D());
        }
        public static GRect ToGRect(this Extents3d? extents3D)
        {
            if (extents3D.HasValue) return new GRect(extents3D.Value.MinPoint, extents3D.Value.MaxPoint);
            return default;
        }
        public static Point2d MidPoint(Point2d pt1, Point2d pt2)
        {
            Point2d midPoint = new Point2d((pt1.X + pt2.X) / 2.0,
                                        (pt1.Y + pt2.Y) / 2.0);
            return midPoint;
        }
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static double AngleFromDegree(this double degree)
        {
            return degree * Math.PI / 180;
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
        public bool EqualsTo(GRect other, double tollerance)
        {
            return Math.Abs(this.MinX - other.MinX) < tollerance && Math.Abs(this.MinY - other.MinY) < tollerance
                && Math.Abs(this.MaxX - other.MaxX) < tollerance && Math.Abs(this.MaxY - other.MaxY) < tollerance;
        }
    }
    public static class Extensions
    {
        public static DBObjectCollection ExplodeToDBObjectCollection(this Entity ent)
        {
            var entitySet = new DBObjectCollection();
            ent.Explode(entitySet);
            return entitySet;
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
    public class CText
    {
        public GRect Boundary;
        public string Text;
        public CText OffsetXY(double dx, double dy)
        {
            return new CText() { Text = Text, Boundary = Boundary.OffsetXY(dx, dy) };
        }
    }
}
