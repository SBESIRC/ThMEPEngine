using QuikGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HandyControl.Controls;
using TianHua.Electrical.PDS.UI.Models;
namespace TianHua.Electrical.PDS.UI.WpfServices
{
    public class PDSCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        readonly Action cb;
        public PDSCommand(Action cb)
        {
            this.cb = cb;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }
        public void Execute(object parameter)
        {
            cb();
        }
    }
    public class EnumConverter : IValueConverter
    {
        public readonly Type Type;
        readonly Dictionary<string, object> d1 = new();
        readonly Dictionary<object, string> d2 = new();
        public EnumConverter(Type type)
        {
            if (!type.IsEnum) throw new ArgumentException();
            Type = type;
            var values = Enum.GetValues(type);
            var names = Enum.GetNames(type);
            if (values.Length != names.Length) throw new ArgumentException();
            string TryGetDescription(object enumerationValue)
            {
                var memberInfo = type.GetMember(enumerationValue.ToString());
                if (memberInfo != null && memberInfo.Length > 0)
                {
                    object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        return ((DescriptionAttribute)attrs[0]).Description;
                    }
                }
                return null;
            }
            for (int i = 0; i < names.Length; i++)
            {
                var v = values.GetValue(i);
                d1[names[i]] = v;
                var description = TryGetDescription(v);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    d1[description] = v;
                    d2[v] = description;
                }
                else
                {
                    d2[v] = names[i];
                }
            }
            ItemsSource = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                ItemsSource[i] = Convert(values.GetValue(i));
            }
        }
        public readonly string[] ItemsSource;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return d2[value];
        }
        public string Convert(object value) => d2[value];
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return d1[(string)value];
        }
        public object ConvertBack(string value) => d1[value];
    }
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
    public static class GeoExtensions
    {
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static double AngleFromDegree(this double degree)
        {
            return degree * Math.PI / 180;
        }
        public static Rect OffsetXY(this Rect r, double dx, double dy)
        {
            var r2 = r;
            r2.Offset(dx, dy);
            return r2;
        }
        public static Rect ToWpfRect(this GRect r) => new(r.LeftTop, r.RightButtom);
        public static Point OffsetXY(this Point point, double dx, double dy)
        {
            point.Offset(dx, dy);
            return point;
        }
        public static Point OffsetY(this Point point, double dy)
        {
            point.Offset(0, dy);
            return point;
        }
        public static Point OffsetX(this Point point, double dx)
        {
            point.Offset(dx, 0);
            return point;
        }
        public static double GetDistanceTo(this Point pt1, Point pt2)
        {
            var v1 = pt1.X - pt2.X;
            var v2 = pt1.Y - pt2.Y;
            return Math.Sqrt(v1 * v1 + v2 * v2);
        }
    }
    public class ThPDSVertex
    {
        public TianHua.Electrical.PDS.Project.Module.NodeDetails Detail;
        public TianHua.Electrical.PDS.Model.PDSNodeType Type;
    }
    public class ThPDSContext
    {
        public List<ThPDSVertex> Vertices;
        public List<int> Souces;
        public List<int> Targets;
        public List<TianHua.Electrical.PDS.Model.ThPDSCircuit> Circuits;
        public List<TianHua.Electrical.PDS.Project.Module.CircuitDetails> Details;
    }
    public class ThPDSDistributionPanelService
    {
        public UserContorls.ThPDSDistributionPanel Panel;
        public TreeView TreeView;
        public Canvas Canvas;
        public ThPDSContext Context;
        public PropertyGrid propertyGrid;
        public AdjacencyGraph<PDS.Project.Module.ThPDSProjectGraphNode, PDS.Project.Module.ThPDSProjectGraphEdge<PDS.Project.Module.ThPDSProjectGraphNode>> Graph;
        public void Init()
        {
            var graph = TianHua.Electrical.PDS.UI.Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
            var vertices = graph.Vertices.Select(x => new ThPDSVertex { Detail = x.Details, Type = x.Type }).ToList();
            var srcLst = graph.Edges.Select(x => graph.Vertices.ToList().IndexOf(x.Source)).ToList();
            var dstLst = graph.Edges.Select(x => graph.Vertices.ToList().IndexOf(x.Target)).ToList();
            var circuitLst = graph.Edges.Select(x => x.Circuit).ToList();
            var details = graph.Edges.Select(x => x.Details).ToList();
            Context = new ThPDSContext() { Vertices = vertices, Souces = srcLst, Targets = dstLst, Circuits = circuitLst, Details = details };
            var canvas = Canvas;
            canvas.Background = Brushes.Transparent;
            canvas.Width = 2000;
            canvas.Height = 2000;
            Action clear = null;
            {
                var builder = new ViewModels.ThPDSCircuitGraphTreeBuilder();
                this.TreeView.DataContext = builder.Build(graph);
            }
            Project.Module.Component.ThPDSDistributionBoxModel boxVM = null;
            this.TreeView.SelectedItemChanged += (s, e) =>
            {
                if (this.TreeView.SelectedItem is ThPDSCircuitGraphTreeModel sel)
                {
                    var vertice = graph.Vertices.ToList()[sel.Id];
                    boxVM = new Project.Module.Component.ThPDSDistributionBoxModel(vertice);
                    UpdatePropertyGrid(boxVM);
                }
                else
                {
                    UpdatePropertyGrid(null);
                }
                UpdateCanvas();
            };
            void UpdatePropertyGrid(object vm)
            {
                propertyGrid.SelectedObject = vm;
            }
            void UpdateCanvas()
            {
                canvas.Children.Clear();
                clear?.Invoke();
                if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                var rightTemplates = new List<KeyValuePair<TextBlock, int>>();
                var leftTemplates = new List<TextBlock>();
                Action render = null;
                var left = ThCADExtension.ThEnumExtension.GetDescription(graph.Vertices.ToList()[sel.Id].Details.CircuitFormType.CircuitFormType) ?? "1路进线";
                var v = graph.Vertices.ToList()[sel.Id];
                var rights = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).Select(eg => ThCADExtension.ThEnumExtension.GetDescription(eg.Details.CircuitForm.CircuitFormType) ?? "常规").Select(x => x.Replace("(", "（").Replace(")", "）")).ToList();
                FrameworkElement selElement = null;
                Rect selArea = default;
                Point busStart, busEnd;
                var trans = new ScaleTransform(1, -1, 0, 0);
                {
                    var item = PDSItemInfo.Create(left, default);
                    DrawGeos(canvas, trans, item);
                    void DrawGeos(Canvas canvas, Transform trans, PDSItemInfo item, Brush strockBrush = null)
                    {
                        strockBrush ??= Brushes.Black;
                        foreach (var info in item.lineInfos)
                        {
                            var st = info.Line.StartPoint;
                            var ed = info.Line.EndPoint;
                            DrawLine(canvas, trans, strockBrush, st, ed);
                        }
                        {
                            var path = new Path();
                            var geo = new PathGeometry();
                            path.Stroke = strockBrush;
                            path.Data = geo;
                            path.RenderTransform = trans;
                            foreach (var info in item.arcInfos)
                            {
                                var figure = new PathFigure();
                                figure.StartPoint = info.Arc.Center.OffsetXY(info.Arc.Radius * Math.Cos(info.Arc.StartAngle), info.Arc.Radius * Math.Sin(info.Arc.StartAngle));
                                var arcSeg = new ArcSegment(info.Arc.Center.OffsetXY(info.Arc.Radius * Math.Cos(info.Arc.EndAngle), info.Arc.Radius * Math.Sin(info.Arc.EndAngle)),
                                  new Size(info.Arc.Radius, info.Arc.Radius), 0, true, SweepDirection.Clockwise, true);
                                figure.Segments.Add(arcSeg);
                                geo.Figures.Add(figure);
                            }
                            canvas.Children.Add(path);
                        }
                        foreach (var info in item.circleInfos)
                        {
                            var path = new Path();
                            var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                            path.Stroke = strockBrush;
                            path.Data = geo;
                            path.RenderTransform = trans;
                            canvas.Children.Add(path);
                        }
                        foreach (var info in item.textInfos)
                        {
                            var tbk = new TextBlock() { Text = info.Text, FontSize = 13, };
                            leftTemplates.Add(tbk);
                            if (info.Height > 0)
                            {
                                tbk.FontSize = info.Height;
                            }
                            tbk.RenderTransform = new ScaleTransform(.7, 1);
                            tbk.Foreground = strockBrush;
                            Canvas.SetLeft(tbk, info.BasePoint.X);
                            Canvas.SetTop(tbk, -info.BasePoint.Y - tbk.FontSize);
                            canvas.Children.Add(tbk);
                        }
                        foreach (var info in item.brInfos)
                        {
                            DrawGeos(canvas, trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red);
                            var _info = PDSItemInfo.GetBlockDefInfo(info.BlockName);
                            if (_info != null)
                            {
                                var r = _info.Bounds.ToWpfRect().OffsetXY(info.BasePoint.X, info.BasePoint.Y);
                                {
                                    var tr = new TranslateTransform(r.X, -r.Y - r.Height);
                                    var cvs = new Canvas
                                    {
                                        Width = r.Width,
                                        Height = r.Height,
                                        Background = Brushes.Transparent,
                                        RenderTransform = tr
                                    };
                                    cvs.MouseEnter += (s, e) =>
                                    {
                                        cvs.Background = LightBlue3;
                                    };
                                    cvs.MouseLeave += (s, e) =>
                                    {
                                        cvs.Background = Brushes.Transparent;
                                    };
                                    Action cb = null;
                                    render += () =>
                                    {
                                        var vertice = graph.Vertices.ToList()[sel.Id];
                                        {
                                            var item = leftTemplates.FirstOrDefault(x => x.Text == "进线回路编号");
                                            if (item != null)
                                            {
                                                var v = ThCADExtension.ThEnumExtension.GetDescription(vertice.Details.CircuitFormType.CircuitFormType);
                                                if (!string.IsNullOrEmpty(v))
                                                {
                                                    item.Text = v;
                                                }
                                            }
                                        }
                                        {
                                            var item = leftTemplates.FirstOrDefault(x => x.Text == "QL");
                                            if (item != null)
                                            {
                                                if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneway)
                                                {
                                                    var isolatingSwitch = oneway.isolatingSwitch;
                                                    if (isolatingSwitch != null)
                                                    {
                                                        var vm = new Project.Module.Component.ThPDSIsolatingSwitchModel(isolatingSwitch);
                                                        var bd = new Binding() { Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.SetBinding(TextBlock.TextProperty, bd);
                                                        cb += () => UpdatePropertyGrid(vm);
                                                    }
                                                    else
                                                    {
                                                        cb += () => UpdatePropertyGrid(null);
                                                    }
                                                }
                                            }
                                        }
                                    };
                                    cvs.MouseUp += (s, e) =>
                                    {
                                        if (e.ChangedButton != MouseButton.Left) return;
                                        setSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                        cb?.Invoke();
                                        e.Handled = true;
                                    };
                                    cvs.Cursor = Cursors.Hand;
                                    canvas.Children.Add(cvs);
                                }
                            }
                        }
                    }
                    busStart = new Point(PDSItemInfo.GetBlockDefInfo(left).Bounds.Width, 0);
                    busEnd = busStart;
                }
                var dy = .0;
                var insertGaps = new List<GLineSegment>();
                insertGaps.Add(new GLineSegment(busEnd, busEnd.OffsetXY(500, 0)));
                foreach (var i in Enumerable.Range(0, rights.Count))
                {
                    var name = rights[i];
                    var item = PDSItemInfo.Create(name, new Point(busStart.X, dy));
                    var vertice = graph.Vertices.ToList()[sel.Id];
                    var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                    var circuitVM = new Project.Module.Component.ThPDSCircuitModel(vertice, edge);
                    render += () =>
                    {
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "Conductor");
                            if (item.Key != null)
                            {
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "回路编号");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitId)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(TextBlock.TextProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "功率");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Power)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(TextBlock.TextProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "相序");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Phase)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(TextBlock.TextProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "负载编号");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LoadId)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(TextBlock.TextProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "功能用途");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Description)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(TextBlock.TextProperty, bd);
                            }
                        }
                    };
                    {
                        var _info = PDSItemInfo.GetBlockDefInfo(name);
                        if (_info != null)
                        {
                            dy -= _info.Bounds.Height;
                            busEnd = busEnd.OffsetXY(0, _info.Bounds.Height);
                            insertGaps.Add(new GLineSegment(busEnd, busEnd.OffsetXY(500, 0)));
                        }
                    }
                    if (item is null)
                    {
                        continue;
                    }
                    void DrawGeos(Canvas canvas, Transform trans, PDSItemInfo item, Brush strockBrush = null)
                    {
                        strockBrush ??= Brushes.Black;
                        foreach (var info in item.lineInfos)
                        {
                            var st = info.Line.StartPoint;
                            var ed = info.Line.EndPoint;
                            DrawLine(canvas, trans, strockBrush, st, ed);
                        }
                        {
                            var path = new Path();
                            var geo = new PathGeometry();
                            path.Stroke = strockBrush;
                            path.Data = geo;
                            path.RenderTransform = trans;
                            foreach (var info in item.arcInfos)
                            {
                                var figure = new PathFigure();
                                figure.StartPoint = info.Arc.Center.OffsetXY(info.Arc.Radius * Math.Cos(info.Arc.StartAngle), info.Arc.Radius * Math.Sin(info.Arc.StartAngle));
                                var arcSeg = new ArcSegment(info.Arc.Center.OffsetXY(info.Arc.Radius * Math.Cos(info.Arc.EndAngle), info.Arc.Radius * Math.Sin(info.Arc.EndAngle)),
                                  new Size(info.Arc.Radius, info.Arc.Radius), 0, true, SweepDirection.Clockwise, true);
                                figure.Segments.Add(arcSeg);
                                geo.Figures.Add(figure);
                            }
                            canvas.Children.Add(path);
                        }
                        foreach (var info in item.circleInfos)
                        {
                            var path = new Path();
                            var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                            path.Stroke = strockBrush;
                            path.Data = geo;
                            path.RenderTransform = trans;
                            canvas.Children.Add(path);
                        }
                        foreach (var info in item.textInfos)
                        {
                            var tbk = new TextBlock() { Text = info.Text, FontSize = 13, };
                            if (info.Height > 0)
                            {
                                tbk.FontSize = info.Height;
                            }
                            tbk.RenderTransform = new ScaleTransform(.7, 1);
                            rightTemplates.Add(new KeyValuePair<TextBlock, int>(tbk, i));
                            tbk.Foreground = strockBrush;
                            Canvas.SetLeft(tbk, info.BasePoint.X);
                            Canvas.SetTop(tbk, -info.BasePoint.Y - tbk.FontSize);
                            canvas.Children.Add(tbk);
                        }
                        foreach (var info in item.brInfos)
                        {
                            DrawGeos(canvas, trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red);
                            var _info = PDSItemInfo.GetBlockDefInfo(info.BlockName);
                            if (_info != null)
                            {
                                var r = _info.Bounds.ToWpfRect().OffsetXY(info.BasePoint.X, info.BasePoint.Y);
                                {
                                    var tr = new TranslateTransform(r.X, -r.Y - r.Height);
                                    var cvs = new Canvas
                                    {
                                        Width = r.Width,
                                        Height = r.Height,
                                        Background = Brushes.Transparent,
                                        RenderTransform = tr
                                    };
                                    cvs.MouseEnter += (s, e) =>
                                    {
                                        cvs.Background = LightBlue3;
                                    };
                                    cvs.MouseLeave += (s, e) =>
                                    {
                                        cvs.Background = Brushes.Transparent;
                                    };
                                    {
                                        Action cb = null;
                                        if (info.BlockName == "CircuitBreaker")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            PDS.Project.Module.Component.Breaker breaker = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.RegularCircuit regularCircuit)
                                            {
                                                breaker = regularCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                                breaker = motorCircuit_DiscreteComponents.breaker;
                                            }
                                            if (breaker != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSBreakerModel(breaker);
                                                cb += () => UpdatePropertyGrid(vm);
                                                render += () =>
                                                {
                                                    var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "CB");
                                                    if (item.Key != null)
                                                    {
                                                        var bd = new Binding() { Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.Key.SetBinding(TextBlock.TextProperty, bd);
                                                    }
                                                };
                                            }
                                            else
                                            {
                                                cb += () => UpdatePropertyGrid(null);
                                            }
                                        }
                                        else if (info.BlockName == "Contactor")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            PDS.Project.Module.Component.Contactor contactor = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                                contactor = motorCircuit_DiscreteComponents.contactor;
                                            }
                                            if (contactor != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSContactorModel(contactor);
                                                cb += () => UpdatePropertyGrid(vm);
                                                render += () =>
                                                {
                                                    var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "QAC");
                                                    if (item.Key != null)
                                                    {
                                                        var bd = new Binding() { Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.Key.SetBinding(TextBlock.TextProperty, bd);
                                                        item.Key.DataContext = vm;
                                                    }
                                                };
                                            }
                                            else
                                            {
                                                cb += () => UpdatePropertyGrid(null);
                                            }
                                        }
                                        else if (info.BlockName == "ThermalRelay")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            PDS.Project.Module.Component.ThermalRelay thermalRelay = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                                thermalRelay = motorCircuit_DiscreteComponents.thermalRelay;
                                            }
                                            if (thermalRelay != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSThermalRelayModel(thermalRelay);
                                                cb += () => UpdatePropertyGrid(vm);
                                                render += () =>
                                                {
                                                    var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.Text == "KH");
                                                    if (item.Key != null)
                                                    {
                                                        var bd = new Binding() { Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.Key.SetBinding(TextBlock.TextProperty, bd);
                                                        item.Key.DataContext = vm;
                                                    }
                                                };
                                            }
                                            else
                                            {
                                                cb += () => UpdatePropertyGrid(null);
                                            }
                                        }
                                        else if (info.BlockName == "Motor")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            cb += () => UpdatePropertyGrid(null);
                                        }
                                        else
                                        {
                                            cb += () => UpdatePropertyGrid(null);
                                        }
                                        cvs.MouseUp += (s, e) =>
                                        {
                                            if (e.ChangedButton != MouseButton.Left) return;
                                            setSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                            cb?.Invoke();
                                            e.Handled = true;
                                        };
                                    }
                                    cvs.Cursor = Cursors.Hand;
                                    canvas.Children.Add(cvs);
                                }
                            }
                        }
                    }
                    var before = new HashSet<FrameworkElement>(canvas.Children.Count);
                    foreach (var ui in canvas.Children)
                    {
                        if (ui is Path || ui is TextBlock) before.Add((FrameworkElement)ui);
                    }
                    DrawGeos(canvas, trans, item);
                    var after = new HashSet<FrameworkElement>(canvas.Children.Count - before.Count);
                    foreach (var ui in canvas.Children)
                    {
                        if (ui is Path || ui is TextBlock)
                        {
                            var fe = (FrameworkElement)ui;
                            if (!before.Contains(fe))
                            {
                                after.Add(fe);
                            }
                        }
                    }
                    var isLocked = false;
                    void SetLockStyle(bool @lock)
                    {
                        if (@lock)
                        {
                            foreach (var fe in after)
                            {
                                fe.Opacity = .6;
                            }
                        }
                        else
                        {
                            foreach (var fe in after)
                            {
                                fe.Opacity = 1;
                            }
                        }
                    }
                    SetLockStyle(isLocked);
                    {
                        var w1 = 485.0;
                        var w2 = 500.0;
                        var h = PDSItemInfo.GetBlockDefInfo(name).Bounds.Height;
                        var cvs = new Canvas
                        {
                            Width = w2,
                            Height = h,
                            Background = Brushes.Transparent,
                        };
                        {
                            var menu = new ContextMenu();
                            cvs.ContextMenu = menu;
                            {
                                var mi = new MenuItem();
                                menu.Items.Add(mi);
                                mi.Header = "切换回路形式";
                                {
                                    {
                                        var m = new MenuItem();
                                        mi.Items.Add(m);
                                        m.Header = "1路进线";
                                        m.Command = new PDSCommand(() =>
                                        {
                                            PDS.Project.Module.ThPDSProjectGraphService.UpdateFormInType(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormInType.一路进线);
                                        });
                                    }
                                    {
                                        var m = new MenuItem();
                                        mi.Items.Add(m);
                                        m.Header = "2路进线ATSE";
                                        m.Command = new PDSCommand(() =>
                                        {
                                            PDS.Project.Module.ThPDSProjectGraphService.UpdateFormInType(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormInType.二路进线ATSE);
                                        });
                                    }
                                    {
                                        var m = new MenuItem();
                                        mi.Items.Add(m);
                                        m.Header = "3路进线TSE";
                                        m.Command = new PDSCommand(() =>
                                        {
                                            PDS.Project.Module.ThPDSProjectGraphService.UpdateFormInType(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormInType.三路进线);
                                        });
                                    }
                                }
                            }
                            {
                                var m = new MenuItem();
                                menu.Items.Add(m);
                                m.Header = isLocked ? "解锁回路数据" : "锁定回路数据";
                                m.Command = new PDSCommand(() =>
                                {
                                    isLocked = !isLocked;
                                    m.Header = isLocked ? "解锁回路数据" : "锁定回路数据";
                                    SetLockStyle(isLocked);
                                });
                            }
                            {
                                var m = new MenuItem();
                                menu.Items.Add(m);
                                m.Header = "分配负载";
                                m.Command = new PDSCommand(() =>
                                {
                                });
                            }
                            {
                                var m = new MenuItem();
                                menu.Items.Add(m);
                                m.Header = "删除";
                                m.Command = new PDSCommand(() =>
                                {
                                    var vertice = graph.Vertices.ToList()[sel.Id];
                                    PDS.Project.Module.ThPDSProjectGraphService.Delete(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, vertice);
                                });
                            }
                        }
                        Canvas.SetLeft(cvs, item.BasePoint.X + w1);
                        Canvas.SetTop(cvs, -item.BasePoint.Y);
                        var cvs2 = new Canvas
                        {
                            Width = w1 + w2,
                            Height = h,
                            Background = Brushes.Transparent,
                            IsHitTestVisible = false,
                        };
                        Canvas.SetLeft(cvs2, item.BasePoint.X);
                        Canvas.SetTop(cvs2, -item.BasePoint.Y);
                        cvs.MouseEnter += (s, e) =>
                        {
                            cvs2.Background = LightBlue3;
                        };
                        cvs.MouseLeave += (s, e) =>
                        {
                            cvs2.Background = Brushes.Transparent;
                        };
                        cvs.MouseUp += (s, e) =>
                        {
                            if (e.ChangedButton != MouseButton.Left) return;
                            setSel(new Rect(Canvas.GetLeft(cvs2), Canvas.GetTop(cvs2), cvs2.Width, cvs2.Height));
                            if (this.TreeView.SelectedItem is ThPDSCircuitGraphTreeModel sel)
                            {
                                var vertice = graph.Vertices.ToList()[sel.Id];
                                var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                if (edge != null)
                                {
                                    UpdatePropertyGrid(circuitVM);
                                }
                                else
                                {
                                    UpdatePropertyGrid(null);
                                }
                            }
                            else
                            {
                                UpdatePropertyGrid(null);
                            }
                            e.Handled = true;
                        };
                        cvs.Cursor = Cursors.Hand;
                        canvas.Children.Add(cvs);
                        canvas.Children.Add(cvs2);
                    }
                }
                foreach (var gap in insertGaps)
                {
                }
                void setSel(Rect r)
                {
                    selArea = r;
                    selElement.Width = r.Width;
                    selElement.Height = r.Height;
                    Canvas.SetLeft(selElement, r.X);
                    Canvas.SetTop(selElement, r.Y);
                }
                {
                    var cvs = new Canvas
                    {
                        Width = 0,
                        Height = 0,
                        IsHitTestVisible = false,
                        Background = LightBlue3,
                    };
                    selElement = cvs;
                    canvas.Children.Add(selElement);
                }
                setSel(default);
                {
                    var width = 20.0;
                    var thickness = 5.0;
                    var path = DrawLine(canvas, null, Brushes.Black, busStart, busEnd);
                    path.StrokeThickness = thickness;
                    var cvs = new Canvas
                    {
                        Width = width,
                        Height = Math.Abs(busStart.Y - busEnd.Y),
                        Background = Brushes.Transparent,
                    };
                    Canvas.SetLeft(cvs, busStart.X - (width - thickness / 2) / 2);
                    Canvas.SetTop(cvs, busStart.Y);
                    cvs.MouseEnter += (s, e) =>
                    {
                        cvs.Background = LightBlue3;
                    };
                    cvs.MouseLeave += (s, e) =>
                    {
                        cvs.Background = Brushes.Transparent;
                    };
                    cvs.MouseUp += (s, e) =>
                    {
                        if (e.ChangedButton != MouseButton.Left) return;
                        setSel(new Rect(Canvas.GetLeft(cvs), Canvas.GetTop(cvs), cvs.Width, cvs.Height));
                        UpdatePropertyGrid(boxVM);
                        e.Handled = true;
                    };
                    {
                        var menu = new ContextMenu();
                        cvs.ContextMenu = menu;
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "新建回路";
                            {
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "常规";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.AddCircuit(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormOutType.常规);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "漏电";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.AddCircuit(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormOutType.漏电);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "电动机（分立）";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.AddCircuit(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormOutType.电动机_分立元件);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "电动机（CPS）";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.AddCircuit(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormOutType.电动机_CPS);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "电动机（三角/Y）";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.AddCircuit(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormOutType.双速电动机_CPSdetailYY);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "电动机（双速）";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.AddCircuit(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormOutType.双速电动机_分立元件detailYY);
                                    });
                                }
                            }
                        }
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "切换回路形式";
                            {
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "1路进线";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.UpdateFormInType(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormInType.一路进线);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "2路进线ATSE";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.UpdateFormInType(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormInType.二路进线ATSE);
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = "3路进线TSE";
                                    m.Command = new PDSCommand(() =>
                                    {
                                        PDS.Project.Module.ThPDSProjectGraphService.UpdateFormInType(new PDS.Project.Module.ThPDSProjectGraph() { Graph = graph }, PDS.Project.Module.CircuitFormInType.三路进线);
                                    });
                                }
                            }
                        }
                    }
                    cvs.Cursor = Cursors.Hand;
                    canvas.Children.Add(cvs);
                }
                void f(object sender, MouseButtonEventArgs e)
                {
                    if (e.ChangedButton != MouseButton.Left) return;
                    setSel(default);
                    UpdatePropertyGrid(boxVM);
                    e.Handled = true;
                }
                canvas.MouseUp += f;
                clear += () => { canvas.MouseUp -= f; };
                clear += () => { clear = null; };
                render?.Invoke();
            }
            UpdateCanvas();
        }
        private static Path DrawLine(Canvas canvas, Transform trans, Brush strockBrush, Point st, Point ed)
        {
            var geo = new LineGeometry(st, ed);
            var path = new Path
            {
                Stroke = strockBrush,
                Fill = Brushes.Black,
                Data = geo,
                RenderTransform = trans,
            };
            canvas.Children.Add(path);
            return path;
        }
        static readonly SolidColorBrush LightBlue3 = new SolidColorBrush(Color.FromArgb(50, 0, 0, 150));
        public class PDSItemInfo
        {
            public Guid Guid = Guid.NewGuid();
            public Point BasePoint;
            public List<LineInfo> lineInfos = new();
            public List<ArcInfo> arcInfos = new();
            public List<BlockInfo> brInfos = new();
            public List<DBTextInfo> textInfos = new();
            public List<CircleInfo> circleInfos = new();
            const double EXPY = 12;
            public static List<BlockDefInfo> blockDefInfos = new(128)
            {
                new BlockDefInfo("CircuitBreaker", new GRect(0, -10, 50, 6)),
                new BlockDefInfo("Contactor", new GRect(0, -12, 50, 0)),
                new BlockDefInfo("ThermalRelay", new GRect(0, -10, 40, 10)),
                new BlockDefInfo("Isolator", new GRect(0, -10, 50, 6)),
                new BlockDefInfo("CPS", new GRect(0, -10, 50, 5)),
                new BlockDefInfo("RCD", new GRect(0, -10, 50, 6)),
                new BlockDefInfo("ATSE", new GRect(-55, -35, 0, 35)),
                new BlockDefInfo("TSE", new GRect(-50, -26, 0, 26)),
                new BlockDefInfo("SPD", new GRect(0, -10, 78, 5)),
                new BlockDefInfo("常规", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("漏电", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
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
                new BlockDefInfo("双速电动机（CPS D-YY）", new GRect(0, -77, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("双速电动机（CPS Y-Y）", new GRect(0, -113, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("消防应急照明回路（WFEL）", new GRect(0, -17, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("控制（从属接触器）", new GRect(144, -7, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("控制（从属CPS）", new GRect(46, -7, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("SPD附件", new GRect(0, -10, 78, 21).Expand(0, EXPY)),
                new BlockDefInfo("1路进线", new GRect(0, -140, 200, -2).Expand(0, EXPY)),
                new BlockDefInfo("2路进线ATSE", new GRect(0, -300, 200, -2).Expand(0, EXPY)),
                new BlockDefInfo("集中电源", new GRect(0, -320, 200, 0).Expand(0, EXPY)),
                new BlockDefInfo("设备自带控制箱", new GRect(0, -320, 200, 0).Expand(0, EXPY)),
                new BlockDefInfo("Motor", new GRect(-10, -10, 10, 10)),
                new BlockDefInfo("Meter", new GRect(-10, -4, 10, 10)),
                new BlockDefInfo("CT", new GRect(0, -8, 50, 18)),
                new BlockDefInfo("1路进线", new GRect(0, -140, 200, -2)),
                new BlockDefInfo("2路进线ATSE", new GRect(0, -300, 200, -2)),
                new BlockDefInfo("3路进线", new GRect(0, -300, 200, -2)),
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
                        r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(150, -60)));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(95, -40)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(95, -80)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(150, -60), px.OffsetXY(200, -60)), "E-UNIV-WIRE"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
                        break;
                    case "3路进线":
                        r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -40)));
                        r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -80)));
                        r.brInfos.Add(new BlockInfo("ATSE", "E-UNIV-Oppo", px.OffsetXY(150, -60)));
                        r.brInfos.Add(new BlockInfo("TSE", "E-UNIV-Oppo", px.OffsetXY(145, -165)));
                        r.brInfos.Add(new BlockInfo("Isolator", "E-UNIV-Oppo", px.OffsetXY(20, -185)));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -40), px.OffsetXY(0, -40)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -80), px.OffsetXY(0, -80)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -40), px.OffsetXY(95, -40)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -80), px.OffsetXY(95, -80)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(150, -60), px.OffsetXY(175, -60)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -60), px.OffsetXY(175, -115)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(175, -115), px.OffsetXY(82, -115)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(82, -115), px.OffsetXY(82, -145)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(82, -145), px.OffsetXY(95, -145)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(20, -185), px.OffsetXY(0, -185)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(70, -185), px.OffsetXY(95, -185)), "E-UNIV-WIRE"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(145, -165), px.OffsetXY(200, -165)), "E-UNIV-WIRE"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -30), "QL1", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -70), "QL2", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -175), "QL3", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -208), "进线回路编号3", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -103), "进线回路编号2", "E-UNIV-NOTE", "TH-STYLE3"));
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(20, -15), "进线回路编号1", "E-UNIV-NOTE", "TH-STYLE3"));
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
                    case "双速电动机（CPS D-YY）":
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
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(-1.55076923076922, -0.907692307692514), "M", "E-UNIV-EL", "TH-STYLE3") { Height = 7.20000000000005, });
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
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-50, 20), px.OffsetXY(-29, 20)), "E-UNIV-Oppo"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-29, 14), px.OffsetXY(-29, 26)), "E-UNIV-Oppo"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 0), px.OffsetXY(0, 0)), "E-UNIV-Oppo"));
                        r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(-26, 20), 3), "E-UNIV-Oppo"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-50, -20), px.OffsetXY(-29, -20)), "E-UNIV-Oppo"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-29, -14), px.OffsetXY(-29, -26)), "E-UNIV-Oppo"));
                        r.circleInfos.Add(new CircleInfo(new GCircle(px.OffsetXY(-26, -20), 3), "E-UNIV-Oppo"));
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-10, 0), px.OffsetXY(-39.5, 0)), "E-UNIV-Oppo"));
                        break;
                    default:
                        return null;
                }
                return r;
            }
        }
    }
}