using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Catel.Collections;
using DA = Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Uitl
{
  namespace ExtensionsNs
  {
    public static class PointExtensions
    {
      public static Point3d OffsetX(this Point3d point, double delta)
      {
        return new Point3d(point.X + delta, point.Y, 0);
      }
      public static Point3d OffsetY(this Point3d point, double delta)
      {
        return new Point3d(point.X, point.Y + delta, 0);
      }
      public static Point3d OffsetXY(this Point3d point, double deltaX, double deltaY)
      {
        return new Point3d(point.X + deltaX, point.Y + deltaY, 0);
      }
    }
  }
  namespace DebugNs
  {
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using Linq2Acad;

    public static class DebugTool
    {
      public static Editor Editor => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
      public static Document MdiActiveDocument => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
      public static Database WorkingDatabase => HostApplicationServices.WorkingDatabase;
      public static AcadDatabase AcadDatabase => AcadDatabase.Active();
      public static Transaction Transaction => WorkingDatabase.TransactionManager.StartTransaction();
      static Database db => WorkingDatabase;
      public static void DrawBoundary(params Entity[] ents)
      {
        DrawBoundary(db, ents);
      }
      public static void DrawBoundary(Database db, params Entity[] ents)
      {
        var r = GeoAlgorithm.GetBoundaryRect(ents);
        var pt1 = new Point2d(r.LeftTop.X, r.LeftTop.Y);
        var pt2 = new Point2d(r.RightButtom.X, r.RightButtom.Y);
        DrawRect(db, pt1, pt2);
      }
      public static void DrawCircle(Point3d center, double radius)
      {
        DrawCircle(db, center, radius);
      }
      public static void DrawLine(Point3d startPoint, Point3d endPoint)
      {
        var line = new Line() { StartPoint = startPoint, EndPoint = endPoint };
        using (var trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        {
          var btr = GetBlockTableRecord(db, trans);
          line.Thickness = 5;
          btr.AppendEntity(line);
          trans.AddNewlyCreatedDBObject(line, true);
          line.ColorIndex = 1;
          trans.Commit();
        }
      }
      public static void DrawCircle(Database db, Point3d center, double radius)
      {
        var circle = new Circle() { Center = center, Radius = radius };
        using (var trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        {
          var btr = GetBlockTableRecord(db, trans);
          circle.Thickness = 5;
          btr.AppendEntity(circle);
          trans.AddNewlyCreatedDBObject(circle, true);
          circle.ColorIndex = 32;
          trans.Commit();
        }
      }
      public static void DrawCircle(Database db, Point3d pt1, Point3d pt2, Point3d pt3)
      {
        using (var trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        {
          var btr = GetBlockTableRecord(db, trans);
          var circle = CreateCircle(pt1, pt2, pt3);
          circle.Thickness = 5;
          btr.AppendEntity(circle);
          trans.AddNewlyCreatedDBObject(circle, true);
          circle.ColorIndex = 32;
          trans.Commit();
        }
      }
      public static Circle CreateCircle(Point3d pt1, Point3d pt2, Point3d pt3)
      {
        var va = pt1.GetVectorTo(pt2);
        var vb = pt1.GetVectorTo(pt3);
        var angle = va.GetAngleTo(vb);
        if (angle == 0 || angle == Math.PI)
        {
          return null;
        }
        else
        {
          var circle = new Circle();
          var geArc = new CircularArc3d(pt1, pt2, pt3);
          circle.Center = geArc.Center;
          circle.Radius = geArc.Radius;
          return circle;
        }
      }
      public static void DrawRect(ThWGRect rect)
      {
        DrawRect(db, rect.LeftTop, rect.RightButtom);
      }
      public static void DrawRect(Database db, Point2d leftTop, Point2d rightButtom)
      {
        using (var trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        {
          var btr = GetBlockTableRecord(db, trans);
          var pline = CreateRectangle(leftTop, rightButtom);
          btr.AppendEntity(pline);
          trans.AddNewlyCreatedDBObject(pline, true);
          pline.ConstantWidth = 2;
          pline.ColorIndex = 32;
          trans.Commit();
        }
      }
      public static void DrawText(Database db, Point2d pt, string text)
      {
        using (var trans = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        {
          var btr = GetBlockTableRecord(db, trans);
          var t = new DBText() { Position = pt.ToPoint3d(), TextString = text, Height = 350, Thickness = 10, };
          btr.AppendEntity(t);
          trans.AddNewlyCreatedDBObject(t, true);
          trans.Commit();
        }
      }
      private static BlockTableRecord GetBlockTableRecord(Database db, Transaction trans)
      {
        var bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
        var btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        return btr;
      }
      public static Polyline CreateRectangle(Point2d pt1, Point2d pt2)
      {
        var minX = Math.Min(pt1.X, pt2.X);
        var maxX = Math.Max(pt1.X, pt2.X);
        var minY = Math.Min(pt1.Y, pt2.Y);
        var maxY = Math.Max(pt1.Y, pt2.Y);
        var pts = new Point2dCollection
                {
                    new Point2d(minX, minY),
                    new Point2d(minX, maxY),
                    new Point2d(maxX, maxY),
                    new Point2d(maxX, minY)
                };
        var pline = CreatePolyline(pts);
        pline.Closed = true;
        return pline;
      }
      public static Polyline CreatePolygon(Point2d centerPoint, int num, double radius)
      {
        var pts = new Point2dCollection(num);
        double angle = 2 * Math.PI / num;
        for (int i = 0; i < num; i++)
        {
          var pt = new Point2d(centerPoint.X + radius * Math.Cos(i * angle),
              centerPoint.Y + radius * Math.Sin(i * angle));
          pts.Add(pt);
        }
        var pline = CreatePolyline(pts);
        pline.Closed = true;
        return pline;
      }
      public static Polyline CreatePolyline(Point2dCollection pts)
      {
        var pline = new Polyline();
        for (int i = 0; i < pts.Count; i++)
        {
          pline.AddVertexAt(i, pts[i], 0, 0, 0);
        }
        return pline;
      }
    }
  }

  public struct ThWGRect
  {
    double x1;
    double y1;
    double x2;
    double y2;
    public double MinX { get => Math.Min(x1, x2); }
    public double MinY { get => Math.Min(y1, y2); }
    public double MaxX { get => Math.Max(x1, x2); }
    public double MaxY { get => Math.Max(y1, y2); }
    public Point2d LeftTop => new Point2d(MinX, MaxY);
    public Point2d RightButtom => new Point2d(MaxX, MinY);
    public Point2d Center => new Point2d(CenterX, CenterY);
    public ThWGRect(double x1, double y1, double x2, double y2)
    {
      this.x1 = Math.Min(x1, x2);
      this.y2 = Math.Min(y1, y2);
      this.x2 = Math.Max(x1, x2);
      this.y1 = Math.Max(y1, y2);
    }
    public ThWGRect(Point2d leftTop, double width, double height) : this(leftTop.X, leftTop.Y, leftTop.X + width, leftTop.Y - height)
    {
    }
    public ThWGRect(Point2d leftTop, Point2d rightButtom) : this(leftTop.X, leftTop.Y, rightButtom.X, rightButtom.Y)
    {
    }
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double CenterX => (MinX + MaxX) / 2;
    public double CenterY => (MinY + MaxY) / 2;
    public double OuterRadius
    {
      get
      {
        return GeoAlgorithm.Distance(new Point2d(MinX, MinY), new Point2d(CenterX, CenterY));
      }
    }
    public double InnerRadius
    {
      get
      {
        return Math.Min(Width, Height) / 2;
      }
    }
    public ThWGRect Expand(double thickness)
    {
      return new ThWGRect(this.MinX - thickness, this.MinY - thickness, this.MaxX + thickness, this.MaxY + thickness);
    }
    public bool ContainsRect(ThWGRect rect)
    {
      return (this.MinX - rect.MinX) * (this.MaxX - rect.MaxX) <= 0 && (this.MinY - rect.MinY) * (this.MaxY - rect.MaxY) < 0;
    }
    public bool ContainsPoint(Point2d point)
    {
      return MinX <= point.X && point.X <= MaxX && MinY <= point.Y && point.Y <= MaxY;
    }
  }
  public struct ThWGLineSegment
  {
    public ThWGLineSegment(double x1, double y1, double x2, double y2)
    {
      X1 = x1;
      Y1 = y1;
      X2 = x2;
      Y2 = y2;
    }
    public ThWGLineSegment(Point2d point1, Point2d point2) : this(point1.X, point1.Y, point2.X, point2.Y)
    {
    }
    public bool IsValid => !(X1 == X2 && Y1 == Y2);
    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public Point2d Point1 { get => new Point2d(X1, Y1); }
    public Point2d Point2 { get => new Point2d(X2, Y2); }
    public double Length => GeoAlgorithm.Distance(Point1, Point2);
    public double MinX => Math.Min(X1, X2);
    public double MaxX => Math.Max(X1, X2);
    public double MinY => Math.Min(Y1, Y2);
    public double MaxY => Math.Max(Y1, Y2);
    public ThWGLine Line
    {
      get
      {
        if (!IsValid) return default;
        if (X1 == X2) return new ThWGLine(1, 0, -X1);
        var k = (Y1 - Y2) / (X1 - X2);
        var b = Y1 - k * X1;
        return new ThWGLine(k, b);
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
      return Math.Max(Point1.GetDistanceTo(point), Point2.GetDistanceTo(point));
    }
  }
  public struct ThWGLine
  {
    public double A;
    public double B;
    public double C;
    public bool IsNull => object.Equals(this, default);
    public bool IsValid => !IsNull;
    public ThWGLine(double a, double b, double c)
    {
      A = a;
      B = b;
      C = c;
    }
    public ThWGLine(double k, double b) : this(k, -1, b)
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
    public bool IsParallel(ThWGLine line2, double tollerance)
    {
      if (this.IsVertical(tollerance) && line2.IsVertical(tollerance)) return true;
      if (Math.Abs(this.k - line2.k) < tollerance) return true;
      return false;
    }
    public Point2d GetCrossPoint(ThWGLine line2)
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
    public ThWGLine GetPedalLine(Point2d point)
    {
      if (IsPointOnMe(point, double.Epsilon)) return default;
      return _GetPedalLine(point);
    }

    private ThWGLine _GetPedalLine(Point2d point)
    {
      return new ThWGLine(B, -A, A * point.Y - B * point.X);
    }
  }
  public struct ThWGCircle
  {
    public double X;
    public double Y;
    public double radius;

    public ThWGCircle(double x, double y, double radius)
    {
      X = x;
      Y = y;
      this.radius = radius;
    }
    public ThWGCircle(Point2d center, double radius) : this(center.X, center.Y, radius)
    {
    }
    public Point2d Center => new Point2d(X, Y);
    public bool IsPointInMe(Point2d point)
    {
      return GeoAlgorithm.Distance(point, Center) < radius;
    }
    public bool IsPointOutOfMe(Point2d point)
    {
      return GeoAlgorithm.Distance(point, Center) > radius;
    }
    public bool IsPointOnMe(Point2d point, double tollerance)
    {
      return Math.Abs(GeoAlgorithm.Distance(point, Center) - radius) < tollerance;
    }
  }
  public struct ThWGTriangle
  {
    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public double X3 { get; }
    public double Y3 { get; }

    public ThWGTriangle(double x1, double y1, double x2, double y2, double x3, double y3)
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
    public ThWGLineSegment LineC => new ThWGLineSegment(A, B);
    public ThWGLineSegment LineA => new ThWGLineSegment(B, C);
    public ThWGLineSegment LineB => new ThWGLineSegment(C, A);
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



  public static class GeoAlgorithm
  {
    public static double AngleToDegree(double angle)
    {
      return angle * 180 / Math.PI;
    }
    public static double AngleFromDegree(double degree)
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
    public static bool IsRectCross(ThWGRect r1, ThWGRect r2)
    {
      return IsRectCross(r1.MinX, r1.MinY, r1.MaxX, r1.MaxY, r2.MinX, r2.MinY, r2.MaxX, r2.MaxY);
    }
    public static bool IsRectCross(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
    {
      return Math.Max(x1, x3) <= Math.Min(x2, x4) && Math.Max(y1, y3) <= Math.Min(y2, y4);
    }
    public static ThWGRect ExpandRect(ThWGRect rect, double width)
    {
      var x1 = rect.LeftTop.X - width;
      var y1 = rect.LeftTop.Y + width;
      var x2 = rect.RightButtom.X + width;
      var y2 = rect.RightButtom.Y - width;
      return new ThWGRect(new Point2d(Math.Min(x1, x2), Math.Max(y1, y2)),
        new Point2d(Math.Max(x1, x2), Math.Min(y1, y2)));
    }
    public static ThWGRect GetBoundaryRect(params Entity[] ents)
    {
      if (!SystemDiagramUtils.GetBoundaryRect(out double minX, out double minY, out double maxX, out double maxY, ents)) return default;
      return new ThWGRect(minX, minY, maxX, maxY);
    }
  }
  public class SystemDiagramUtils
  {
    const string ROOF_RAIN_PIPE_PREFIX = "Y1";
    const string BALCONY_PIPE_PREFIX = "Y2";
    const string CONDENSE_PIPE_PREFIX = "N1";
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
    static public List<string> GetCondenseVerticalPipeNotes(Point3dCollection pts)
    {
      var vpTexts = GetDBText(pts);

      return vpTexts.Where(t => t.StartsWith(CONDENSE_PIPE_PREFIX)).ToList();
    }

    static public List<string> GetBalconyVerticalPipeNotes(Point3dCollection pts)
    {
      var vpTexts = GetDBText(pts);

      return vpTexts.Where(t => t.StartsWith(BALCONY_PIPE_PREFIX)).ToList();
    }

    static public List<string> GetRoofVerticalPipeNotes(Point3dCollection pts)
    {
      var vpTexts = GetDBText(pts);

      return vpTexts.Where(t => t.StartsWith(ROOF_RAIN_PIPE_PREFIX)).ToList();
    }

    static public List<string> GetVerticalPipeNotes(Point3dCollection pts)
    {
      var vpTexts = GetDBText(pts);

      return vpTexts.Where(t => t.StartsWith(ROOF_RAIN_PIPE_PREFIX) || t.StartsWith(BALCONY_PIPE_PREFIX) || t.StartsWith(CONDENSE_PIPE_PREFIX)).ToList();
    }
    static public List<string> GetDBText(Point3dCollection pts)
    {
      var textEntities = GetDBTextEntities(pts);
      var texts = textEntities.Select(e => (e as DBText).TextString);
      return texts.ToList();
    }
    static public List<Entity> GetDBTextEntities(Point3dCollection pts)
    {
      using (var db = Linq2Acad.AcadDatabase.Active())
      {
        var rst = new List<Entity>();

        var tvs = new List<TypedValue>();
        tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(DBText)).DxfName + "," + RXClass.GetClass(typeof(MText)).DxfName));
        tvs.Add(new TypedValue((int)DxfCode.LayerName, ThWPipeCommon.W_RAIN_NOTE));
        var sf = new SelectionFilter(tvs.ToArray());

        var psr = Active.Editor.SelectAll(sf);
        if (psr.Status == PromptStatus.OK)
        {
          foreach (var id in psr.Value.GetObjectIds())
            rst.Add(db.Element<Entity>(id));
        }

        if (pts.Count >= 3)
        {
          var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(rst.ToCollection());
          rst = spatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ToList();
        }

        return rst;
      }
    }
  }
}
