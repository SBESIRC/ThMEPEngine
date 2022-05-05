using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.UI.WpfServices
{
    public struct GArc
    {
        public double X;
        public double Y;
        public Point Center => new(X, Y);
        public double Radius;
        public double StartAngle;
        public double EndAngle;
        public bool IsClockWise;
        public GArc(Point center, double radius, double startAngle, double endAngle, bool isClockWise) : this(center.X, center.Y, radius, startAngle, endAngle, isClockWise) { }
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
        public Point LeftTop => new Point(MinX, MaxY);
        public Point LeftButtom => new Point(MinX, MinY);
        public Point RightButtom => new Point(MaxX, MinY);
        public Point RightTop => new Point(MaxX, MaxY);
        public Point Center => new Point(CenterX, CenterY);
        public GRect(double x1, double y1, double x2, double y2)
        {
            MinX = Math.Min(x1, x2);
            MinY = Math.Min(y1, y2);
            MaxX = Math.Max(x1, x2);
            MaxY = Math.Max(y1, y2);
        }
        public GRect OffsetXY(Vector v)
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
        public static GRect Create(Point pt, double extX, double extY)
        {
            return new GRect(pt.X - extX, pt.Y - extY, pt.X + extX, pt.Y + extY);
        }
        public static GRect Create(Point pt, double ext)
        {
            return new GRect(pt.X - ext, pt.Y - ext, pt.X + ext, pt.Y + ext);
        }
        public GRect(Point leftTop, double width, double height) : this(leftTop.X, leftTop.Y, leftTop.X + width, leftTop.Y - height)
        {
        }
        public GRect(Point p1, Point p2) : this(p1.X, p1.Y, p2.X, p2.Y)
        {
        }
        public double Radius => Math.Sqrt(Math.Pow(Width / 2, 2) + Math.Pow(Height / 2, 2));
        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public double CenterX => (MinX + MaxX) / 2;
        public double CenterY => (MinY + MaxY) / 2;
        public double OuterRadius => (new Point(MinX, MinY)).GetDistanceTo(new Point(CenterX, CenterY));
        public double MiddleRadius => Math.Max(Width, Height) / 2;
        public double InnerRadius => Math.Min(Width, Height) / 2;
        public GRect Expand(double thickness)
        {
            return new GRect(this.MinX - thickness, this.MinY - thickness, this.MaxX + thickness, this.MaxY + thickness);
        }
        public GRect Expand(double dx, double dy)
        {
            return new GRect(this.MinX - dx, this.MinY - dy, this.MaxX + dx, this.MaxY + dy);
        }
        public bool ContainsRect(GRect rect)
        {
            return rect.MinX > this.MinX && rect.MinY > this.MinY && rect.MaxX < this.MaxX && rect.MaxY < this.MaxY;
        }
        public bool ContainsPoint(Point point)
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
    }
    public struct GLineSegment
    {
        public GLineSegment(Point startPoint, Point endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
        public Point StartPoint { get; }
        public Point EndPoint { get; }
        public bool IsNull => Equals(this, default(GLineSegment));
        public bool IsValid => StartPoint != EndPoint;
        public double Length => StartPoint.GetDistanceTo(EndPoint);
        public GLineSegment Offset(Vector v)
        {
            return new GLineSegment(StartPoint + v, EndPoint + v);
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
        public GCircle(Point center, double radius) : this(center.X, center.Y, radius)
        {
        }
        public Point Center => new Point(X, Y);
        public GCircle OffsetXY(double dx, double dy)
        {
            return new GCircle(X + dx, Y + dy, Radius);
        }
    }
    public class LineInfo
    {
        public GLineSegment Line;
        public string LayerName;
        public double Thickness;
        public LineInfo(GLineSegment line, string layerName)
        {
            this.Line = line;
            this.LayerName = layerName;
        }
    }
    public class ArcInfo
    {
        public GArc Arc;
        public string LayerName;
        public ArcInfo(GArc arc, string layerName)
        {
            this.Arc = arc;
            this.LayerName = layerName;
        }
    }
    public class HatchInfo
    {
        public IList<Point> Points;
        public string LayerName;
        public HatchInfo(IList<Point> points, string layerName)
        {
            Points = points;
            LayerName = layerName;
        }
    }
    public class CircleInfo
    {
        public GCircle Circle;
        public string LayerName;
        public CircleInfo(Point center, double radius, string layerName) : this(new GCircle(center, radius), layerName)
        {
        }
        public CircleInfo(GCircle circle, string layerName)
        {
            this.Circle = circle;
            this.LayerName = layerName;
        }
    }
    public class BlockDefInfo
    {
        public string BlockName;
        public GRect Bounds;
        public BlockDefInfo(string blockName, GRect bounds)
        {
            BlockName = blockName;
            Bounds = bounds;
        }
    }
    public class BlockInfo
    {
        public string LayerName;
        public string BlockName;
        public Point BasePoint;
        public double Rotate;
        public double Scale;
        public Dictionary<string, string> PropDict;
        public Dictionary<string, object> DynaDict;
        public BlockInfo(string blockName, string layerName, Point basePoint)
        {
            this.LayerName = layerName;
            this.BlockName = blockName;
            this.BasePoint = basePoint;
            this.PropDict = new Dictionary<string, string>();
            this.DynaDict = new Dictionary<string, object>();
            this.Rotate = 0;
            this.Scale = 1;
        }
    }
    public class DBTextInfo
    {
        public string LayerName;
        public string TextStyle;
        public Point BasePoint;
        public string Text;
        public double Rotation;
        public double Height;
        public DBTextInfo(Point point, string text, string layerName, string textStyle)
        {
            text ??= "";
            this.LayerName = layerName;
            this.TextStyle = textStyle;
            this.BasePoint = point;
            this.Text = text;
        }
    }

    public class PDSItemInfo
    {
        public Guid Guid = Guid.NewGuid();
        public Point BasePoint;
        public List<LineInfo> lineInfos = new();
        public List<ArcInfo> arcInfos = new();
        public List<BlockInfo> brInfos = new();
        public List<DBTextInfo> textInfos = new();
        public List<CircleInfo> circleInfos = new();
        public List<HatchInfo> hatchInfos = new();
        const double EXPY = 0;
        public static List<BlockDefInfo> blockDefInfos = new(128)
        {
            new BlockDefInfo("CircuitBreaker", new GRect(0, -10, 50, 6)),
            new BlockDefInfo("Contactor", new GRect(0, -12, 50, 0)),
            new BlockDefInfo("ThermalRelay", new GRect(0, -10, 40, 10)),
            new BlockDefInfo("Isolator", new GRect(0, -10, 50, 6)),
            new BlockDefInfo("CPS", new GRect(0, -10, 50, 5)),
            new BlockDefInfo("RCD", new GRect(0, -10, 50, 6)),
            new BlockDefInfo("ATSE", new GRect(-55, -35, 0, 35)),
            new BlockDefInfo("TSE", new GRect(-55, -26, 0, 26)),
            new BlockDefInfo("SPD", new GRect(0, -10, 78, 5)),
            new BlockDefInfo("常规", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("漏电", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("SPD附件", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("接触器控制", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("热继电器保护", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("配电计量（上海CT）", new GRect(0, -24, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("配电计量（上海直接表）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("配电计量（CT表在前）", new GRect(0, -24, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("配电计量（直接表在前）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("配电计量（CT表在后）", new GRect(0, -24, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("配电计量（直接表在后）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("电动机（分立元件）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("电动机（CPS）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("电动机（分立元件星三角启动）", new GRect(0, -112, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("电动机（CPS星三角启动）", new GRect(0, -113, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("双速电动机（分立元件 D-YY）", new GRect(0, -113, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("双速电动机（分立元件 Y-Y）", new GRect(0, -77, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("双速电动机（CPS Y-Y）", new GRect(0, -77, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("双速电动机（CPS D-YY）", new GRect(0, -113, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("消防应急照明回路（WFEL）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("控制（从属接触器）", new GRect(144, -7, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("控制（从属CPS）", new GRect(46, -7, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("SPD附件", new GRect(0, -10, 78, 21).Expand(0, EXPY)),
            new BlockDefInfo("1路进线", new GRect(0, -140, 200, -2).Expand(0, EXPY)),
            new BlockDefInfo("2路进线ATSE", new GRect(0, -300, 200, -2).Expand(0, EXPY)),
            new BlockDefInfo("集中电源", new GRect(0, -320, 200, 0).Expand(0, EXPY)),
            new BlockDefInfo("设备自带控制箱", new GRect(0, -320, 200, 0).Expand(0, EXPY)),
            new BlockDefInfo("Motor", new GRect(-10, -10, 10, 10)),
            new BlockDefInfo("直接表", new GRect(0, -7, 50, 7)),
            new BlockDefInfo("间接表", new GRect(0, -7, 50, 7)),
            new BlockDefInfo("Meter", new GRect(-10, -4, 10, 10)),
            new BlockDefInfo("CT", new GRect(0, -8, 50, 18)),
            new BlockDefInfo("1路进线", new GRect(0, -140, 200, -2)),
            new BlockDefInfo("2路进线ATSE", new GRect(0, -300, 200, -2)),
            new BlockDefInfo("3路进线", new GRect(0, -300, 200, -2)),
            new BlockDefInfo("分支小母排", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("小母排分支", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("消防应急照明回路（WFEL）", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
            new BlockDefInfo("过欠电压保护器", new GRect(0, -10, 50, 10)),
        };
        public static BlockDefInfo GetBlockDefInfo(string blkName)
        {
            return PDSItemInfo.blockDefInfos.FirstOrDefault(x => x.BlockName == blkName);
        }
        public static PDSItemInfo Create(string name, Point px)
        {
            var r = new PDSItemInfo();
            r.BasePoint = px;
            switch (name)
            {
                case "直接表":
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(19.9599999999998, -5.90769230769251), "kWh", "E-UNIV-EL", "TH-STYLE3") { Height = 7.20000000000005, });
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(15, 0.999999999999318), px.OffsetXY(35, 1)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(14.9999999999998, -6.99999999999977), px.OffsetXY(34.9999999999998, -6.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(15, 6.99999999999977), px.OffsetXY(14.9999999999998, -6.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(15, 6.99999999999977), px.OffsetXY(35, 6.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(35, 6.99999999999977), px.OffsetXY(34.9999999999998, -6.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(34.9999999999998, 0), px.OffsetXY(50, 0)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(14.9999999999998, 0), px.OffsetXY(0, 0)), "E-UNIV-WIRE"));
                    break;
                case "过欠电压保护器":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(35.0000000000033, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(15.0000000000033, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(15.0000000000033, -9.99999999999727), px.OffsetXY(35.0000000000033, -9.99999999999727)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(35.0000000000033, -9.99999999999727), px.OffsetXY(35.0000000000033, 10.0000000000027)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(35.0000000000033, 10.0000000000027), px.OffsetXY(15.0000000000033, 10.0000000000027)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(15.0000000000033, 10.0000000000027), px.OffsetXY(15.0000000000033, -9.99999999999727)), "E-UNIV-Oppo"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(22.7249999998486, -5.50000000050886), "U", "E-UNIV-EL", "TH-STYLE3") { Height = 13, });
                    break;
                case "1路进线（带电表）":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("直接表", "E-UNIV-WIRE", px.OffsetXY(140, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(140, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(190, -40), px.OffsetXY(200, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(140, -30), "MT", "E-UNIV-NOTE", "TH-STYLE3"));

                    break;
                case "2路进线ATSE（带电表）":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                    r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(140, -60)));
                    r.brInfos.Add(new BlockInfo("直接表", "E-UNIV-WIRE", px.OffsetXY(140, -60)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(200, 0)), "内框"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, 0), px.OffsetXY(0, 0)), "内框"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(85, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(85, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(190, -60), px.OffsetXY(200, -60)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -30), "ATSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(140, -50), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "3路进线（带电表）":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                    r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(140, -60)));
                    r.brInfos.Add(new BlockInfo("TSE", "E-UNIV-Oppo", px.OffsetXY(140, -165)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -185)));
                    r.brInfos.Add(new BlockInfo("直接表", "E-UNIV-WIRE", px.OffsetXY(140, -165)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(200, 0)), "内框"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, 0), px.OffsetXY(0, 0)), "内框"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(85, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(85, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(140, -60), px.OffsetXY(175, -60)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -60), px.OffsetXY(175, -115)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -115), px.OffsetXY(72, -115)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72, -115), px.OffsetXY(72, -145)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72, -145), px.OffsetXY(85, -145)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -185), px.OffsetXY(0, -185)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -185), px.OffsetXY(85, -185)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(190, -165), px.OffsetXY(200, -165)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -175), "QL3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -208), "进线回路编号3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -30), "ATSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -135), "MTSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(140, -155), "MT", "E-UNIV-NOTE", "TH-STYLE3"));

                    break;
                case "1路进线（带过欠电压保护）":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("过欠电压保护器", "0", px.OffsetXY(140, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(155, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(190, -40), px.OffsetXY(200, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(140, -30), "过欠电压保护器", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "2路进线ATSE（带过欠电压保护）":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                    r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(140, -60)));
                    r.brInfos.Add(new BlockInfo("过欠电压保护器", "0", px.OffsetXY(140, -60)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(85, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(85, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(190, -60), px.OffsetXY(200, -60)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -30), "ATSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(140, -50), "过欠电压保护器", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "3路进线（带过欠电压保护）":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                    r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(140, -60)));
                    r.brInfos.Add(new BlockInfo("TSE", "E-UNIV-Oppo", px.OffsetXY(140, -165)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -185)));
                    r.brInfos.Add(new BlockInfo("过欠电压保护器", "0", px.OffsetXY(140, -165)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(85, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(85, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(140, -60), px.OffsetXY(175, -60)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -60), px.OffsetXY(175, -115)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -115), px.OffsetXY(72, -115)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72, -115), px.OffsetXY(72, -145)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72, -145), px.OffsetXY(85, -145)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -185), px.OffsetXY(0, -185)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -185), px.OffsetXY(85, -185)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(140, -165), px.OffsetXY(155, -165)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(190, -165), px.OffsetXY(200, -165)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -175), "QL3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -208), "进线回路编号3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -30), "ATSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -135), "MTSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(140, -155), "过欠电压保护器", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "1路进线":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(200, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "2路进线ATSE":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                    r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(140, -60)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(85, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(85, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(140, -60), px.OffsetXY(200, -60)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -30), "ATSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "3路进线":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                    r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(140, -60)));
                    r.brInfos.Add(new BlockInfo("TSE", "E-UNIV-Oppo", px.OffsetXY(140, -165)));
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -185)));

                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(85, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(85, -80)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(140, -60), px.OffsetXY(175, -60)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -60), px.OffsetXY(175, -115)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -115), px.OffsetXY(72, -115)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72, -115), px.OffsetXY(72, -145)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72, -145), px.OffsetXY(85, -145)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -185), px.OffsetXY(0, -185)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -185), px.OffsetXY(85, -185)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(140, -165), px.OffsetXY(200, -165)), "E-UNIV-WIRE"));

                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -175), "QL3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -208), "进线回路编号3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -30), "ATSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(85, -135), "MTSE", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "集中电源":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -240)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(95, 0), px.OffsetXY(200, 0)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, 0), px.OffsetXY(200, -320)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, -320), px.OffsetXY(95, -320)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(95, -320), px.OffsetXY(95, 0)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(87, -30), px.OffsetXY(95, -35)), "E-CTRL-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(95, -35), px.OffsetXY(87, -40)), "E-CTRL-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(87, -30), px.OffsetXY(20, -30)), "E-CTRL-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(87, -40), px.OffsetXY(20, -40)), "E-CTRL-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -30), px.OffsetXY(20, -20)), "E-CTRL-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -50), px.OffsetXY(20, -40)), "E-CTRL-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(95, -75), px.OffsetXY(0, -75)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -240), px.OffsetXY(0, -240)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -240), px.OffsetXY(95, -240)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(4, -234), "QL", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(4, -220), "进线回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(4, -16), "通讯线", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(4, -71), "市电监测线 ", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(103, -167), "A型应急照明集中电源", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(133, -180), "DC36V", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(97, -316), "本设备由厂家配套", "E-UNIV-NOTE", "TH-STYLE3"));
                    {
                        var vecs = new List<Vector> { new Vector(13.4824608598765, -0.033501501497426), new Vector(-2.56184213504093, 2.64906698011407), new Vector(9.72115787093117, -2.61556547861665), new Vector(-9.8127552088763, -2.6283232281271), new Vector(2.65343947298607, 2.59482172662968) };
                        var p = px.OffsetXY(74.3582, -20);
                        foreach (var v in vecs)
                        {
                            r.lineInfos.Add(new LineInfo(new GLineSegment(p, p + v), "E-CTRL-WIRE"));
                            p += v;
                            var lst = new List<Point>();
                            lst.Add(p);
                            for (int i = 1; i < vecs.Count; i++)
                            {
                                var vec = vecs[i];
                                p += vec;
                                lst.Add(p);
                            }
                            r.hatchInfos.Add(new HatchInfo(lst, "E-CTRL-WIRE"));
                            break;
                        }
                        for (int i = 0; i < vecs.Count; i++)
                        {
                            var v = vecs[i];
                            vecs[i] = new Vector(-v.X, v.Y);
                        }
                        p = px.OffsetXY(74.3582 + 20.6415, -20 - 27.8171);
                        foreach (var v in vecs)
                        {
                            r.lineInfos.Add(new LineInfo(new GLineSegment(p, p + v), "E-CTRL-WIRE"));
                            p += v;
                            var lst = new List<Point>();
                            lst.Add(p);
                            for (int i = 1; i < vecs.Count; i++)
                            {
                                var vec = vecs[i];
                                p += vec;
                                lst.Add(p);
                            }
                            r.hatchInfos.Add(new HatchInfo(lst, "E-CTRL-WIRE"));
                            break;
                        }
                    }
                    break;
                case "设备自带控制箱":
                    r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -75)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, 0), px.OffsetXY(95, 0)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -75), px.OffsetXY(0, -75)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -75), px.OffsetXY(95, -75)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(95, 0), px.OffsetXY(200, 0)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, 0), px.OffsetXY(200, -320)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(200, -320), px.OffsetXY(95, -320)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(95, -320), px.OffsetXY(95, 0)), "E-POWR-DEVC"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(116, 4), "本设备由厂家配套", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(125, -167), "设备控制箱", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(4, -69), "QL", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(4, -55), "进线回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(116, -316), "本设备由厂家配套", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "常规":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "漏电":
                    r.brInfos.Add(new BlockInfo("RCD", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "接触器控制":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(114, -30), "QAC", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -46), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -36), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -56), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -36), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -56), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "热继电器保护":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -46), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -36), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -56), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -36), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -56), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "KH", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "配电计量（上海CT）":
                    r.brInfos.Add(new BlockInfo("Meter", "E-POWR-DEVC", px.OffsetXY(155, -80)));
                    r.brInfos.Add(new BlockInfo("CT", "E-POWR-DEVC", px.OffsetXY(115, -60)) { Rotate = Math.PI / 2 });
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -60)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(215, -60)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -60), px.OffsetXY(15, -60)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -60), px.OffsetXY(115, -60)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -60), px.OffsetXY(215, -60)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -60), px.OffsetXY(485, -60)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -50), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(198, -50), "CB2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -50), "CB1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -66), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -56), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -76), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -56), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -76), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -50), "CT", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -82), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "配电计量（上海直接表）":
                    r.brInfos.Add(new BlockInfo("Meter", "E-POWR-DEVC", px.OffsetXY(140, -43)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(215, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(130, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(150, -40), px.OffsetXY(215, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(198, -30), "CB2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "配电计量（CT表在前）":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.brInfos.Add(new BlockInfo("Meter", "E-POWR-DEVC", px.OffsetXY(55, -60)));
                    r.brInfos.Add(new BlockInfo("CT", "E-POWR-DEVC", px.OffsetXY(15, -40)) { Rotate = Math.PI / 2 });
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CT", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -62), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "配电计量（直接表在前）":
                    r.brInfos.Add(new BlockInfo("Meter", "E-POWR-DEVC", px.OffsetXY(40, -43)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(30, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(50, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "配电计量（CT表在后）":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Meter", "E-POWR-DEVC", px.OffsetXY(155, -60)));
                    r.brInfos.Add(new BlockInfo("CT", "E-POWR-DEVC", px.OffsetXY(115, -40)) { Rotate = Math.PI / 2 });
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "CT", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -62), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "配电计量（直接表在后）":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Meter", "E-POWR-DEVC", px.OffsetXY(140, -43)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(130, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(150, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "MT", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "电动机（分立元件）":
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(215, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -40), px.OffsetXY(165, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "QAC", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(214, -30), "KH", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "电动机（CPS）":
                    r.brInfos.Add(new BlockInfo("CPS", "E-POWR-DEVC", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CPS", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "电动机（分立元件星三角启动）":
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -100)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -140)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(215, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -40), px.OffsetXY(165, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -100), px.OffsetXY(475, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(475, -100), px.OffsetXY(475, -50)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -100), px.OffsetXY(105, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -100), px.OffsetXY(105, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -140), px.OffsetXY(105, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -148), px.OffsetXY(105, -132)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -140), px.OffsetXY(276, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(276, -140), px.OffsetXY(276, -100)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "QAC1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(214, -30), "KH", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -130), "QAC3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -90), "Conductor2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -90), "QAC2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "电动机（CPS星三角启动）":
                    r.brInfos.Add(new BlockInfo("CPS", "E-POWR-DEVC", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -100)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -140)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -100), px.OffsetXY(475, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(475, -100), px.OffsetXY(475, -50)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -100), px.OffsetXY(105, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -100), px.OffsetXY(105, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -140), px.OffsetXY(105, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -148), px.OffsetXY(105, -132)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -140), px.OffsetXY(276, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(276, -140), px.OffsetXY(276, -100)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CPS", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -130), "QAC2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -90), "Conductor2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -90), "QAC1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -46), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -36), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -56), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -36), "功率", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -56), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "双速电动机（分立元件 D-YY）":
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -100)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -140)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(215, -40)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(215, -100)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -40), px.OffsetXY(165, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -100), px.OffsetXY(475, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(475, -100), px.OffsetXY(475, -50)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -100), px.OffsetXY(165, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -100), px.OffsetXY(105, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -100), px.OffsetXY(105, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -140), px.OffsetXY(105, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -148), px.OffsetXY(105, -132)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -140), px.OffsetXY(276, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(276, -140), px.OffsetXY(276, -100)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "QAC1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(214, -30), "KH1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -90), "QAC2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -90), "Conductor2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(214, -90), "KH2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -130), "QAC3", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -46), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率(低)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -97), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -117), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -97), "功率(高)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -117), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "双速电动机（分立元件 Y-Y）":
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -40)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -100)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(215, -40)));
                    r.brInfos.Add(new BlockInfo("ThermalRelay", "E-UNIV-Oppo", px.OffsetXY(215, -100)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(115, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -40), px.OffsetXY(165, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -100), px.OffsetXY(475, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(475, -100), px.OffsetXY(475, -50)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -100), px.OffsetXY(165, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -100), px.OffsetXY(105, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -100), px.OffsetXY(105, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -30), "QAC1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(214, -30), "KH1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -90), "QAC2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -90), "Conductor2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(214, -90), "KH2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率(低)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -97), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -117), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -97), "功率(高)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -117), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "双速电动机（CPS Y-Y）":
                    r.brInfos.Add(new BlockInfo("CPS", "E-POWR-DEVC", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("CPS", "E-POWR-DEVC", px.OffsetXY(15, -100)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -100), px.OffsetXY(475, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(475, -100), px.OffsetXY(475, -50)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -100), px.OffsetXY(15, -100)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CPS1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -90), "Conductor2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -90), "CPS2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -36), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -56), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -36), "功率(低)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -56), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -97), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -117), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -97), "功率(高)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -117), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "双速电动机（CPS D-YY）":
                    r.brInfos.Add(new BlockInfo("CPS", "E-POWR-DEVC", px.OffsetXY(15, -40)));
                    r.brInfos.Add(new BlockInfo("Motor", "E-POWR-EQPM", px.OffsetXY(475, -40)));
                    r.brInfos.Add(new BlockInfo("CPS", "E-POWR-DEVC", px.OffsetXY(15, -100)));
                    r.brInfos.Add(new BlockInfo("Contactor", "E-UNIV-Oppo", px.OffsetXY(115, -140)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(465, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -100), px.OffsetXY(475, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(475, -100), px.OffsetXY(475, -50)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -100), px.OffsetXY(15, -100)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(115, -140), px.OffsetXY(105, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(105, -148), px.OffsetXY(105, -132)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(165, -140), px.OffsetXY(276, -140)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(276, -140), px.OffsetXY(276, -100)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CPS1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor1", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -90), "Conductor2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -90), "CPS2", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(115, -130), "QAC", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -36), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -56), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -36), "功率(低)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -56), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -97), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -117), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -97), "功率(高)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -117), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "CircuitBreaker":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(24, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(24, 6), px.OffsetXY(24, -6)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(5.63603896932113, 6.36396103067909), px.OffsetXY(18.3639610306789, -6.36396103067909)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(18.3639610306789, 6.36396103067909), px.OffsetXY(5.63603896932113, -6.36396103067909)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(22.6794919243112, -10)), "E-UNIV-Oppo"));
                    break;
                case "RCD":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(24, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(24, 6), px.OffsetXY(24, -6)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(5.63603896932113, 6.36396103067909), px.OffsetXY(18.3639610306789, -6.36396103067909)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(18.3639610306789, 6.36396103067909), px.OffsetXY(5.63603896932113, -6.36396103067909)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(22.6794919243112, -10)), "E-UNIV-Oppo"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(31.3397459621556, -5), 4), "E-UNIV-Oppo"));
                    break;
                case "Contactor":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(20, 0)), "E-UNIV-Oppo"));
                    r.arcInfos.Add(new ArcInfo(new GArc(px.OffsetXY(15, 1.33974596215558), 5.17638090205041, 195.0.AngleFromDegree(), 345.0.AngleFromDegree(), false), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(18.3493649053889, -12.5)), "E-UNIV-Oppo"));
                    break;
                case "ThermalRelay":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(31.2500000000002, 10), px.OffsetXY(18.7500000000002, 10)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(18.7500000000002, 10), px.OffsetXY(18.7500000000002, -10)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(18.7500000000002, -10), px.OffsetXY(31.2500000000002, -10)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(31.2500000000002, -10), px.OffsetXY(31.2500000000002, 10)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(22.5000000000005, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(22.5000000000005, 0), px.OffsetXY(22.5000000000005, 6.25)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(22.5000000000005, 6.25), px.OffsetXY(27.5000000000005, 6.25)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(27.5000000000005, 6.25), px.OffsetXY(27.5000000000002, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(27.5000000000002, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    break;
                case "CT":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(25, 8), px.OffsetXY(25, 18)), "0"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(25, 0), 8), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(50, 0)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(21.6237162160389, 14.4736240109228), px.OffsetXY(27.1322197687448, 10.6934289129881)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(23.2438643197761, 15.8236936887565), px.OffsetXY(28.7523678724817, 12.0434985908219)), "0"));
                    break;
                case "Meter":
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(-5.03999999999996, -2.90769230769251), "kWh", "E-UNIV-EL", "TH-STYLE3") { Height = 7.20000000000005, });
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 3.99999999999932), px.OffsetXY(10, 4)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, -3.99999999999977), px.OffsetXY(10, -3.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 9.99999999999977), px.OffsetXY(-10, -3.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 9.99999999999977), px.OffsetXY(10, 9.99999999999977)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(10, 9.99999999999977), px.OffsetXY(10, -3.99999999999977)), "0")); break;
                case "Motor":
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(0, 0), 10), "0"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(-5, -10), "M", "E-UNIV-EL", "TH-STYLE3") { Height = 20, });
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(-1.74461538461583, -6.83076923076919), "~", "E-UNIV-EL", "TH-STYLE3") { Height = 7.20000000000005, });
                    break;
                case "CPS":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(24, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(16.5, 4.5), px.OffsetXY(16.5, -4.5)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(4.97702922699068, 4.77297077300909), px.OffsetXY(14.5229707730093, -4.77297077300909)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(14.5229707730093, 4.77297077300909), px.OffsetXY(4.97702922699068, -4.77297077300909)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(22.6794919243112, -10)), "E-UNIV-Oppo"));
                    r.arcInfos.Add(new ArcInfo(new GArc(px.OffsetXY(21, 0.803847577293254), 3.10582854123025, 195.0.AngleFromDegree(), 345.0.AngleFromDegree(), false), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(29.6076951545867, -6), px.OffsetXY(31.6076951545867, -9.46410161513768)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(31.6076951545867, -9.46410161513768), px.OffsetXY(35.0717967697244, -7.46410161513768)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(35.0717967697244, -7.46410161513768), px.OffsetXY(33.0717967697244, -4)), "E-UNIV-Oppo"));
                    break;
                case "Isolator":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(21, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(21, 6), px.OffsetXY(21, -6)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(50, 0)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(40, 0), px.OffsetXY(22.6794919243112, -10)), "E-UNIV-Oppo"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(24, 0), 3), "E-UNIV-Oppo"));
                    break;
                case "ATSE":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-55, 20), px.OffsetXY(-34, 20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-34, 14), px.OffsetXY(-34, 26)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-15, 20), px.OffsetXY(-5, 20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-15, 20), px.OffsetXY(-32.3205080756888, 30)), "E-UNIV-Oppo"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(-31, 20), 3), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-55, -20), px.OffsetXY(-34, -20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-34, -14), px.OffsetXY(-34, -26)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-15, -20), px.OffsetXY(-5, -20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-15, -20), px.OffsetXY(-32.3205080756888, -30)), "E-UNIV-Oppo"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(-31, -20), 3), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-5, 20), px.OffsetXY(-5, -20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-23.6602540378444, 25), px.OffsetXY(-23.6602540378444, 4.00000000001683)), "E-UNIV-NOTE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-23.6602540378444, -3.99999999998727), px.OffsetXY(-23.6602540378444, -25)), "E-UNIV-NOTE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-27.1243556529789, 6.00000000001501), px.OffsetXY(-27.1243556529789, -5.99999999998499)), "E-UNIV-NOTE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-27.1243556529789, -5.99999999998499), px.OffsetXY(-16.7320508075657, 1.50066625792533E-11)), "E-UNIV-NOTE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-16.7320508075657, 1.50066625792533E-11), px.OffsetXY(-27.1243556529789, 6.00000000001501)), "E-UNIV-NOTE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, 0), px.OffsetXY(-5, 0)), "E-UNIV-Oppo"));
                    break;
                case "TSE":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-55, 20), px.OffsetXY(-34, 20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-34, 14), px.OffsetXY(-34, 26)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 0), px.OffsetXY(0, 0)), "E-UNIV-Oppo"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(-31, 20), 3), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-55, -20), px.OffsetXY(-34, -20)), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-34, -14), px.OffsetXY(-34, -26)), "E-UNIV-Oppo"));
                    r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(-31, -20), 3), "E-UNIV-Oppo"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 0), px.OffsetXY(-44.5, 0)), "E-UNIV-Oppo"));
                    break;
                case "SPD附件":
                    r.brInfos.Add(new BlockInfo("SPD", "E-UNIV-Oppo", px.OffsetXY(0, -40)));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(3, -30), "SPD1", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "SPD":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72.9919784453705, -0.000226596953552871), px.OffsetXY(42.6156667589894, 6.43058558580378E-06)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(72.9919094777506, -4.50748979746368), px.OffsetXY(72.9919500743326, 4.50703660355987)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(75.4959511584293, -3.0487805263466), px.OffsetXY(75.4959786164418, 3.04830477905341)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(77.9999928391098, -1.59007125563107), px.OffsetXY(78.0000071585582, 1.58957295455036)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(67.9838433146178, -3.60601460357543), px.OffsetXY(67.9838757918806, 3.60560651724347)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(47.9515624231608, -3.60603524490614), px.OffsetXY(67.9838433141213, -3.60612545965478)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(18, 0), px.OffsetXY(0, 0)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(67.9839244612049, 3.60560651684523), px.OffsetXY(47.9516435702444, 3.60569673159375)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(67.9838433146178, -3.60601460357543), px.OffsetXY(67.9838757918806, 3.60560651724347)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(47.9516110929817, -3.60592438922151), px.OffsetXY(47.9516435702444, 3.60569673159375)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(67.983843314687, -3.60599873829165), px.OffsetXY(67.9838757919497, 3.60562238252726)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(47.951562423239, -3.60601937962235), px.OffsetXY(67.9838433141977, -3.6061095943744)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(67.9839244608775, 3.60553388676021), px.OffsetXY(47.951643569917, 3.60562410150885)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(67.983843314687, -3.60599873829165), px.OffsetXY(67.9838757919497, 3.60562238252726)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(47.9516110930526, -3.60590852394114), px.OffsetXY(47.9516435703154, 3.60571259687754)), "E-POWR-DEVC"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(24.578755982202, -1.45046977819334), px.OffsetXY(21.6777718340727, 1.45051436993208)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(21.6777718340727, -1.45046977819334), px.OffsetXY(24.578755982202, 1.45051436993208)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(38.5130556325494, 2.22958693711917E-05), px.OffsetXY(28.1256620542608, -5.99714218273562)), "0"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(42.615666759064, 2.22958693711917E-05), px.OffsetXY(29.1513148358881, -7.77362491144856)), "0"));
                    r.hatchInfos.Add(new HatchInfo(new Point[] { px.OffsetXY(27.1000092726281, -4.22065945402983), px.OffsetXY(30.1769676175099, -9.55010764015429), px.OffsetXY(24.0230509277371, -9.55010764015429), }, "E-POWR-DEVC"));
                    r.hatchInfos.Add(new HatchInfo(new Point[] { px.OffsetXY(54.620, -1.959), px.OffsetXY(63.704, 0), px.OffsetXY(54.620, 1.959), }, "E-POWR-DEVC"));
                    break;
                case "分支小母排":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(15, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(15, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(65, -40), px.OffsetXY(205, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(15, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "小母排分支":
                    r.brInfos.Add(new BlockInfo("CircuitBreaker", "E-UNIV-Oppo", px.OffsetXY(215, -40)));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(215, -40), px.OffsetXY(205, -40)), "E-UNIV-WIRE"));
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(265, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(215, -30), "CB", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率(低)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "消防应急照明回路（WFEL）":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(0, -40), px.OffsetXY(485, -40)), "E-UNIV-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(595, -37), "负载编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -57), "功能用途", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -37), "功率(低)", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(545, -57), "相序", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "控制（从属接触器）":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(144, -40), px.OffsetXY(485, -40)), "E-CTRL-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -47), "控制回路", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                case "控制（从属CPS）":
                    r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(46, -40), px.OffsetXY(485, -40)), "E-CTRL-WIRE"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(294, -30), "Conductor", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(594, -47), "控制回路", "E-UNIV-NOTE", "TH-STYLE3"));
                    r.textInfos.Add(new DBTextInfo(px.OffsetXY(495, -47), "回路编号", "E-UNIV-NOTE", "TH-STYLE3"));
                    break;
                default:
                    throw new NotSupportedException();
            }
            return r;
        }
    }
}
