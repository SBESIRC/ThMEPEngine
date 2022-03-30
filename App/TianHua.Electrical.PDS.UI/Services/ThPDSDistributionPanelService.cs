using System;
using QuikGraph;
using System.Linq;
using ThCADExtension;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Converters;
using System.Windows.Controls.Primitives;
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
    public class COR
    {
        readonly Dictionary<string, Action> dict = new();
        public void Register(string key, Action cb)
        {
            if (cb is null) return;
            if (!dict.ContainsKey(key))
            {
                dict[key] = cb;
            }
            else
            {
                dict[key] += cb;
            }
        }
        bool hasExecuted;
        public void Execute(string key)
        {
            if (dict.TryGetValue(key, out Action cb))
            {
                cb();
                hasExecuted = true;
            }
        }
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
        public class ThPDSDistributionPanelConfig : NotifyPropertyChangedBase
        {
            PDSCommand _BatchGenerate;
            public PDSCommand BatchGenerate
            {
                get => _BatchGenerate;
                set
                {
                    if (value != _BatchGenerate)
                    {
                        _BatchGenerate = value;
                        OnPropertyChanged(nameof(BatchGenerate));
                    }
                }
            }
            ThPDSDistributionPanelConfigState _Current;
            public ThPDSDistributionPanelConfigState Current
            {
                get => _Current;
                set
                {
                    if (value != _Current)
                    {
                        _Current = value;
                        OnPropertyChanged(nameof(Current));
                    }
                }
            }
        }
        readonly ThPDSDistributionPanelConfig Config = new();
        public class ThPDSDistributionPanelConfigState : NotifyPropertyChangedBase
        {
            PDS.Project.Module.ThPDSProjectGraphNode vertice;
            public ThPDSDistributionPanelConfigState(PDS.Project.Module.ThPDSProjectGraphNode vertice)
            {
                this.vertice = vertice;
            }
            public bool FirePowerMonitoring
            {
                get => vertice.Details.FirePowerMonitoring;
                set
                {
                    if (value != FirePowerMonitoring)
                    {
                        vertice.Details.FirePowerMonitoring = value;
                        OnPropertyChanged(nameof(FirePowerMonitoring));
                    }
                }
            }
            public bool ElectricalFireMonitoring
            {
                get => vertice.Details.ElectricalFireMonitoring;
                set
                {
                    if (value != ElectricalFireMonitoring)
                    {
                        vertice.Details.ElectricalFireMonitoring = value;
                        OnPropertyChanged(nameof(ElectricalFireMonitoring));
                    }
                }
            }
            public Array SurgeProtectionTypes => Enum.GetValues(typeof(PDS.Project.Module.SurgeProtectionDeviceType));
            public PDS.Project.Module.SurgeProtectionDeviceType SurgeProtection
            {
                get => vertice.Details.SurgeProtection;
                set
                {
                    if (value != SurgeProtection)
                    {
                        vertice.Details.SurgeProtection = value;
                        OnPropertyChanged(nameof(SurgeProtection));
                    }
                }
            }
            double _busLength;
            public double BusLength
            {
                get => _busLength;
                set
                {
                    if (value != _busLength)
                    {
                        _busLength = value;
                        OnPropertyChanged(nameof(BusLength));
                    }
                }
            }
        }
        public UserContorls.ThPDSDistributionPanel Panel;
        public TreeView TreeView;
        public Canvas Canvas;
        public ThPDSContext Context;
        public dynamic propertyGrid;
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
            TreeView.ContextMenu.DataContext = Config;
            TreeView.ContextMenu.SetBinding(UIElement.VisibilityProperty, new Binding() { Source = TreeView, Path = new PropertyPath(nameof(TreeView.SelectedItem)), Converter = new EqualsThenNotVisibeConverter(null), }); ;
            var canvas = Canvas;
            canvas.Background = Brushes.Transparent;
            canvas.Width = 2000;
            canvas.Height = 2000;
            var fontUri = new Uri(System.IO.Path.Combine(Environment.GetEnvironmentVariable("windir"), @"Fonts\simHei.ttf"));
            var cvt = new GlyphsUnicodeStringConverter();
            Action clear = null;
            {
                var builder = new ViewModels.ThPDSCircuitGraphTreeBuilder();
                this.TreeView.DataContext = builder.Build(graph);
            }
            Project.Module.Component.ThPDSDistributionBoxModel boxVM = null;
            Action<DrawingContext> dccbs;
            var cbDict = new Dictionary<Rect, Action>(4096);
            var br = Brushes.Black;
            var pen = new Pen(br, 1);
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
                propertyGrid.SelectedObject = vm ?? new object();
                if (vm is Project.Module.Component.ThPDSCircuitModel circuitVM)
                {
                    propertyGrid.SetBinding(UIElement.IsEnabledProperty, new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitLock)), Converter = new NotConverter() });
                }
                else
                {
                    BindingOperations.ClearBinding(propertyGrid, UIElement.IsEnabledProperty);
                }
            }
            void UpdateCanvas()
            {
                canvas.Children.Clear();
                clear?.Invoke();
                dccbs = null;
                if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                Config.Current = new(graph.Vertices.ToList()[sel.Id]);
                Config.BatchGenerate = new PDSCommand(() =>
                {
                    var ctrl = new UserContorls.ThPDSBatchGenerate();
                    var builder = new ViewModels.ThPDSCircuitGraphTreeBuilder();
                    var tree = builder.Build(graph);
                    {
                        void dfs(ThPDSCircuitGraphTreeModel node)
                        {
                            foreach (var n in node.DataList)
                            {
                                n.Parent = node;
                                n.Root = tree;
                                dfs(n);
                            }
                        }
                        dfs(tree);
                    }
                    ctrl.treeView.DataContext = tree;
                    {
                        ctrl.cbxCheckAll.SetBinding(ToggleButton.IsCheckedProperty, new Binding() { Source = tree, Path = new PropertyPath(nameof(tree.IsChecked)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                    }
                    var w = new Window() { Width = 400, Height = 300, Topmost = true, WindowStartupLocation = WindowStartupLocation.CenterScreen, };
                    ctrl.btnGen.Command = new PDSCommand(() =>
                    {
                        w.Hide();
                        UI.ElecSandboxUI.TryGetCurrentWindow().Hide();
                        try
                        {
                            var vertices = graph.Vertices.ToList();
                            var checkeddVertices = new List<PDS.Project.Module.ThPDSProjectGraphNode>();
                            void dfs(ThPDSCircuitGraphTreeModel node)
                            {
                                if (node.IsChecked == true) checkeddVertices.Add(vertices[node.Id]);
                                foreach (var n in node.DataList) dfs(n);
                            }
                            dfs(tree);
                            foreach (var vertice in checkeddVertices)
                            {
                                var drawCmd = new Command.ThPDSSystemDiagramCommand(graph, vertice);
                                drawCmd.Execute();
                            }
                            AcHelper.Active.Editor.Regen();
                        }
                        finally
                        {
                            UI.ElecSandboxUI.TryGetCurrentWindow().Show();
                            w.Show();
                        }
                    });
                    ctrl.cbxCheckAll.Unchecked += (s, e) => { };
                    w.Content = ctrl;
                    w.Show();
                });
                {
                    var cmenu = new ContextMenu();
                    cmenu.Items.Add(new MenuItem()
                    {
                        Header = "单独生成",
                        Command = new PDSCommand(() =>
                        {
                            if (TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                            var vertices = graph.Vertices.ToList();
                            var drawCmd = new Command.ThPDSSystemDiagramCommand(graph, vertices[sel.Id]);
                            drawCmd.Execute();
                            AcHelper.Active.Editor.Regen();
                        }),
                    });
                    canvas.ContextMenu = cmenu;
                }
                var hoverDict = new Dictionary<object, object>();
                var rightTemplates = new List<KeyValuePair<Glyphs, int>>();
                var leftTemplates = new List<Glyphs>();
                Action render = null;
                var dv = new DrawingVisual();
                var circuitInType = graph.Vertices.ToList()[sel.Id].Details.CircuitFormType.CircuitFormType;
                var left = ThCADExtension.ThEnumExtension.GetDescription(circuitInType) ?? "1路进线";
                var v = graph.Vertices.ToList()[sel.Id];
                var rights = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).Select(eg => ThCADExtension.ThEnumExtension.GetDescription(eg.Details.CircuitForm?.CircuitFormType ?? default) ?? "常规").Select(x => x.Replace("(", "（").Replace(")", "）")).ToList();
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
                            var glyph = new Glyphs() { UnicodeString = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                            leftTemplates.Add(glyph);
                            if (info.Height > 0)
                            {
                                glyph.FontRenderingEmSize = info.Height;
                            }
                            Canvas.SetLeft(glyph, info.BasePoint.X);
                            Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                            canvas.Children.Add(glyph);
                        }
                        foreach (var hatch in item.hatchInfos)
                        {
                            if (hatch.Points.Count < 3) continue;
                            var geo = new PathGeometry();
                            var path = new Path
                            {
                                Fill = strockBrush,
                                Stroke = strockBrush,
                                Data = geo,
                                RenderTransform = trans
                            };
                            var figure = new PathFigure
                            {
                                StartPoint = hatch.Points[0]
                            };
                            for (int i = 1; i < hatch.Points.Count; i++)
                            {
                                figure.Segments.Add(new LineSegment(hatch.Points[i], false));
                            }
                            geo.Figures.Add(figure);
                            canvas.Children.Add(path);
                        }
                        var vertice = graph.Vertices.ToList()[sel.Id];
                        var cor = new COR();
                        render += () =>
                        {
                            {
                                var circuitNumbers = Service.ThPDSCircuitNumberSeacher.Seach(vertice, graph);
                                var str = string.Join(",", circuitNumbers);
                                {
                                    var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "进线回路编号");
                                    if (item != null)
                                    {
                                        if (string.IsNullOrEmpty(str)) str = " ";
                                        item.UnicodeString = str;
                                    }
                                }
                                {
                                    var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "进线回路编号1");
                                    if (item != null)
                                    {
                                        var s = circuitNumbers.Count >= 1 ? circuitNumbers[0] : str;
                                        if (string.IsNullOrEmpty(s)) s = " ";
                                        item.UnicodeString = s;
                                    }
                                }
                                {
                                    var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "进线回路编号2");
                                    if (item != null)
                                    {
                                        var s = circuitNumbers.Count >= 2 ? circuitNumbers[1] : str;
                                        if (string.IsNullOrEmpty(s)) s = " ";
                                        item.UnicodeString = s;
                                    }
                                }
                                {
                                    var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "进线回路编号3");
                                    if (item != null)
                                    {
                                        var s = circuitNumbers.Count >= 3 ? circuitNumbers[2] : str;
                                        if (string.IsNullOrEmpty(s)) s = " ";
                                        item.UnicodeString = s;
                                    }
                                }
                                {
                                    if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneway)
                                    {
                                        var isolatingSwitch = oneway.isolatingSwitch;
                                        if (isolatingSwitch != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSIsolatingSwitchModel(isolatingSwitch);
                                            var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "QL");
                                            if (item != null)
                                            {
                                                var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                item.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                            cor.Register("Isolator", () => UpdatePropertyGrid(vm));
                                        }
                                        else
                                        {
                                            cor.Register("Isolator", () => UpdatePropertyGrid(null));
                                        }
                                    }
                                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.TwoWayInCircuit twoWayInCircuit)
                                    {
                                        {
                                            var isolatingSwitch = twoWayInCircuit.isolatingSwitch1;
                                            if (isolatingSwitch != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSIsolatingSwitchModel(isolatingSwitch);
                                                var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "QL");
                                                if (item != null)
                                                {
                                                    var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                    item.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                }
                                                cor.Register("Isolator", () => UpdatePropertyGrid(vm));
                                            }
                                            else
                                            {
                                                cor.Register("Isolator", () => UpdatePropertyGrid(null));
                                            }
                                        }
                                        {
                                            var sw = twoWayInCircuit.transferSwitch;
                                            if (sw != null)
                                            {
                                                var vm = new Project.Module.Component.ThATSEModel(sw);
                                                cor.Register("ATSE", () => UpdatePropertyGrid(vm));
                                            }
                                            else
                                            {
                                                cor.Register("ATSE", () => UpdatePropertyGrid(null));
                                            }
                                        }
                                    }
                                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.ThreeWayInCircuit threeWayInCircuit)
                                    {
                                        {
                                            var isolatingSwitch = threeWayInCircuit.isolatingSwitch1;
                                            if (isolatingSwitch != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSIsolatingSwitchModel(isolatingSwitch);
                                                var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "QL");
                                                if (item != null)
                                                {
                                                    var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                    item.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                }
                                                cor.Register("Isolator", () => UpdatePropertyGrid(vm));
                                            }
                                            else
                                            {
                                                cor.Register("Isolator", () => UpdatePropertyGrid(null));
                                            }
                                        }
                                        {
                                            var sw = threeWayInCircuit.transferSwitch1;
                                            if (sw != null)
                                            {
                                                var vm = new Project.Module.Component.ThATSEModel(sw);
                                                cor.Register("ATSE", () => UpdatePropertyGrid(vm));
                                            }
                                            else
                                            {
                                                cor.Register("ATSE", () => UpdatePropertyGrid(null));
                                            }
                                        }
                                        {
                                            var sw = threeWayInCircuit.transferSwitch2;
                                            if (sw != null)
                                            {
                                                var vm = new Project.Module.Component.ThMTSEModel(sw);
                                                cor.Register("TSE", () => UpdatePropertyGrid(vm));
                                            }
                                            else
                                            {
                                                cor.Register("TSE", () => UpdatePropertyGrid(null));
                                            }
                                        }
                                    }
                                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                                    {
                                        var isolatingSwitch = centralizedPowerCircuit.isolatingSwitch;
                                        if (isolatingSwitch != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSIsolatingSwitchModel(isolatingSwitch);
                                            var item = leftTemplates.FirstOrDefault(x => x.UnicodeString == "QL");
                                            if (item != null)
                                            {
                                                var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                item.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                            cor.Register("Isolator", () => UpdatePropertyGrid(vm));
                                        }
                                    }
                                }
                            }
                        };
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
                                    hoverDict[cvs] = cvs;
                                    dccbs += dc =>
                                    {
                                        var rect = new Rect(r.X, -r.Y - r.Height, r.Width, r.Height);
                                        dc.DrawRectangle(br, pen, rect);
                                        cbDict[rect] = () =>
                                        {
                                            setSel(rect);
                                            cor.Execute(info.BlockName);
                                        };
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
                    var item = PDSItemInfo.Create(name, new Point(busStart.X, dy + 10));
                    var vertice = graph.Vertices.ToList()[sel.Id];
                    var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                    var circuitVM = new Project.Module.Component.ThPDSCircuitModel(edge);
                    render += () =>
                    {
                        {
                            PDS.Project.Module.Component.Conductor conductor = null, conductor1 = null, conductor2 = null;
                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.RegularCircuit regularCircuit)
                            {
                                conductor = regularCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                            {
                                conductor = motorCircuit_DiscreteComponents.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                            {
                                conductor1 = motor_DiscreteComponentsStarTriangleStartCircuit.Conductor1;
                                conductor2 = motor_DiscreteComponentsStarTriangleStartCircuit.Conductor2;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                            {
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                            {
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                            {
                                conductor = contactorControlCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                            {
                                conductor = distributionMetering_MTInBehindCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                            {
                                conductor = distributionMetering_CTInFrontCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                            {
                                conductor = distributionMetering_CTInBehindCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                            {
                                conductor = distributionMetering_MTInFrontCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                            {
                                conductor = distributionMetering_ShanghaiCTCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                            {
                                conductor = distributionMetering_ShanghaiMTCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                            {
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                            {
                                conductor = leakageCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                            {
                                conductor = motor_CPSCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                            {
                                conductor1 = motor_CPSStarTriangleStartCircuit.Conductor1;
                                conductor2 = motor_CPSStarTriangleStartCircuit.Conductor2;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                            {
                                conductor = motor_DiscreteComponentsCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                            {
                                conductor = thermalRelayProtectionCircuit.Conductor;
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                            {
                            }
                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                            {
                            }
                            var w = 200.0;
                            void reg(Rect r, object vm)
                            {
                                GRect gr = new GRect(r.TopLeft, r.BottomRight);
                                gr = gr.Expand(5);
                                var cvs = new Canvas
                                {
                                    Width = gr.Width,
                                    Height = gr.Height,
                                    Background = Brushes.Transparent,
                                };
                                Canvas.SetLeft(cvs, gr.MinX);
                                Canvas.SetTop(cvs, gr.MinY);
                                canvas.Children.Add(cvs);
                                cvs.MouseEnter += (s, e) => { cvs.Background = LightBlue3; };
                                cvs.MouseLeave += (s, e) => { cvs.Background = Brushes.Transparent; };
                                cvs.Cursor = Cursors.Hand;
                                cvs.MouseUp += (s, e) =>
                                {
                                    if (e.ChangedButton != MouseButton.Left) return;
                                    UpdatePropertyGrid(vm);
                                    setSel(gr.ToWpfRect());
                                    e.Handled = true;
                                };
                            }
                            {
                                var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "Conductor");
                                if (item.Key != null)
                                {
                                    if (conductor != null)
                                    {
                                        var vm = new Project.Module.Component.ThPDSConductorModel(conductor);
                                        var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                        item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                        var r = new Rect(Canvas.GetLeft(item.Key), Canvas.GetTop(item.Key), w, item.Key.FontRenderingEmSize);
                                        reg(r, vm);
                                    }
                                    else
                                    {
                                        item.Key.UnicodeString = " ";
                                    }
                                }
                            }
                            {
                                var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "Conductor1");
                                if (item.Key != null)
                                {
                                    if (conductor1 != null)
                                    {
                                        var vm = new Project.Module.Component.ThPDSConductorModel(conductor1);
                                        var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                        item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                        var r = new Rect(Canvas.GetLeft(item.Key), Canvas.GetTop(item.Key), w, item.Key.FontRenderingEmSize);
                                        reg(r, vm);
                                    }
                                    else
                                    {
                                        item.Key.UnicodeString = " ";
                                    }
                                }
                            }
                            {
                                var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "Conductor2");
                                if (item.Key != null)
                                {
                                    if (conductor2 != null)
                                    {
                                        var vm = new Project.Module.Component.ThPDSConductorModel(conductor2);
                                        var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                        item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                        var r = new Rect(Canvas.GetLeft(item.Key), Canvas.GetTop(item.Key), w, item.Key.FontRenderingEmSize);
                                        reg(r, vm);
                                    }
                                    else
                                    {
                                        item.Key.UnicodeString = " ";
                                    }
                                }
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "回路编号");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitNumber)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "功率");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? " " : v.ToString()), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Power)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "相序");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.PhaseSequence)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "负载编号");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LoadId)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                            }
                        }
                        {
                            var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "功能用途");
                            if (item.Key != null)
                            {
                                var bd = new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Description)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
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
                            var glyph = new Glyphs() { UnicodeString = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                            if (info.Height > 0)
                            {
                                glyph.FontRenderingEmSize = info.Height;
                            }
                            rightTemplates.Add(new KeyValuePair<Glyphs, int>(glyph, i));
                            Canvas.SetLeft(glyph, info.BasePoint.X);
                            Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                            canvas.Children.Add(glyph);
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
                                    hoverDict[cvs] = cvs;
                                    {
                                        Action cb = null;
                                        if (info.BlockName == "CircuitBreaker")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            PDS.Project.Module.Component.BreakerBaseComponent breaker = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.RegularCircuit regularCircuit)
                                            {
                                                breaker = regularCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                                breaker = motorCircuit_DiscreteComponents.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                            {
                                                breaker = motor_DiscreteComponentsStarTriangleStartCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                            {
                                                breaker = twoSpeedMotor_DiscreteComponentsDYYCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                            {
                                                breaker = twoSpeedMotor_DiscreteComponentsYYCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                            {
                                                breaker = contactorControlCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                            {
                                                breaker = distributionMetering_MTInBehindCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                            {
                                                breaker = distributionMetering_CTInFrontCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                            {
                                                breaker = distributionMetering_CTInBehindCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                            {
                                                breaker = distributionMetering_MTInFrontCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                            {
                                                breaker = distributionMetering_ShanghaiCTCircuit.breaker1;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                            {
                                                breaker = distributionMetering_ShanghaiMTCircuit.breaker1;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                            {
                                                breaker = leakageCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                            {
                                                breaker = motor_DiscreteComponentsCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                            {
                                                breaker = thermalRelayProtectionCircuit.breaker;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                            {
                                                breaker = twoSpeedMotor_CPSDYYCircuit.breaker1;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                            {
                                                breaker = twoSpeedMotor_CPSYYCircuit.breaker1;
                                            }
                                            if (breaker != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSBreakerModel(breaker);
                                                cb += () => UpdatePropertyGrid(vm);
                                                render += () =>
                                                {
                                                    var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "CB");
                                                    if (item.Key != null)
                                                    {
                                                        var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                    }
                                                };
                                                var cmenu = new ContextMenu();
                                                cvs.ContextMenu = cmenu;
                                                cmenu.Items.Add(new MenuItem()
                                                {
                                                    Header = "切换断路器",
                                                    Command = new PDSCommand(() =>
                                                    {
                                                        ThPDSProjectGraphService.ComponentSwitching(edge, breaker, PDS.Project.Module.Component.ComponentType.CB);
                                                        UpdateCanvas();
                                                    }),
                                                });
                                            }
                                            else
                                            {
                                                cb += () => UpdatePropertyGrid(null);
                                            }
                                        }
                                        else if (info.BlockName == "RCD")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                            {
                                                void handleLeakageCircuit()
                                                {
                                                    {
                                                        var breaker = leakageCircuit.breaker as PDS.Project.Module.Component.Breaker;
                                                        if (breaker != null)
                                                        {
                                                            var vm = new Project.Module.Component.ThPDSBreakerModel(breaker);
                                                            cb += () => UpdatePropertyGrid(vm);
                                                            render += () =>
                                                            {
                                                                var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "CB");
                                                                if (item.Key != null)
                                                                {
                                                                    var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                    item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                                }
                                                            };
                                                            var cmenu = new ContextMenu();
                                                            cvs.ContextMenu = cmenu;
                                                            cmenu.Items.Add(new MenuItem()
                                                            {
                                                                Header = "切换断路器",
                                                                Command = new PDSCommand(() =>
                                                                {
                                                                    ThPDSProjectGraphService.ComponentSwitching(edge, breaker, PDS.Project.Module.Component.ComponentType.CB);
                                                                    UpdateCanvas();
                                                                }),
                                                            });
                                                            return;
                                                        }
                                                    }
                                                    {
                                                        var breaker = leakageCircuit.breaker as PDS.Project.Module.Component.ResidualCurrentBreaker;
                                                        if (breaker != null)
                                                        {
                                                            var vm = new Project.Module.Component.ThPDSResidualCurrentBreakerModel(breaker);
                                                            cb += () => UpdatePropertyGrid(vm);
                                                            render += () =>
                                                            {
                                                                var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "CB");
                                                                if (item.Key != null)
                                                                {
                                                                    var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                    item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                                }
                                                            };
                                                            var cmenu = new ContextMenu();
                                                            cvs.ContextMenu = cmenu;
                                                            cmenu.Items.Add(new MenuItem()
                                                            {
                                                                Header = "切换断路器",
                                                                Command = new PDSCommand(() =>
                                                                {
                                                                    ThPDSProjectGraphService.ComponentSwitching(edge, breaker, PDS.Project.Module.Component.ComponentType.CB);
                                                                    UpdateCanvas();
                                                                }),
                                                            });
                                                            return;
                                                        }
                                                    }
                                                    cb += () => UpdatePropertyGrid(null);
                                                    return;
                                                }
                                                handleLeakageCircuit();
                                            }
                                        }
                                        else if (info.BlockName == "Contactor")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            var contactors = item.brInfos.Where(x => x.BlockName == "Contactor").ToList();
                                            var idx = contactors.IndexOf(info);
                                            PDS.Project.Module.Component.Contactor contactor = null, contactor1 = null, contactor2 = null, contactor3 = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                                contactor = motorCircuit_DiscreteComponents.contactor;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                            {
                                                contactor1 = motor_DiscreteComponentsStarTriangleStartCircuit.contactor1;
                                                contactor2 = motor_DiscreteComponentsStarTriangleStartCircuit.contactor2;
                                                contactor3 = motor_DiscreteComponentsStarTriangleStartCircuit.contactor3;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                            {
                                                contactor1 = twoSpeedMotor_DiscreteComponentsDYYCircuit.contactor1;
                                                contactor2 = twoSpeedMotor_DiscreteComponentsDYYCircuit.contactor2;
                                                contactor3 = twoSpeedMotor_DiscreteComponentsDYYCircuit.contactor3;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                            {
                                                contactor1 = twoSpeedMotor_DiscreteComponentsYYCircuit.contactor1;
                                                contactor2 = twoSpeedMotor_DiscreteComponentsYYCircuit.contactor2;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                            {
                                                contactor = contactorControlCircuit.contactor;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                            {
                                                contactor = motor_DiscreteComponentsCircuit.contactor;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                            {
                                                contactor1 = twoSpeedMotor_CPSDYYCircuit.contactor1;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                            {
                                            }
                                            if (contactor != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSContactorModel(contactor);
                                                cb += () => UpdatePropertyGrid(vm);
                                                render += () =>
                                                {
                                                    var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "QAC");
                                                    if (item.Key != null)
                                                    {
                                                        var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                    }
                                                };
                                            }
                                            else if (contactors.Count > 1)
                                            {
                                                contactor = idx == 0 ? contactor1 : (idx == 1 ? contactor2 : contactor3);
                                                if (contactor != null)
                                                {
                                                    var vm = new Project.Module.Component.ThPDSContactorModel(contactor);
                                                    cb += () => UpdatePropertyGrid(vm);
                                                    render += () =>
                                                    {
                                                        var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "QAC" + (idx + 1));
                                                        if (item.Key != null)
                                                        {
                                                            var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                            item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                        }
                                                    };
                                                }
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
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                            {
                                                thermalRelay = motor_DiscreteComponentsStarTriangleStartCircuit.thermalRelay;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                            {
                                                thermalRelay = twoSpeedMotor_DiscreteComponentsDYYCircuit.thermalRelay1;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                            {
                                                thermalRelay = twoSpeedMotor_DiscreteComponentsYYCircuit.thermalRelay1;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                            {
                                                thermalRelay = motor_DiscreteComponentsCircuit.thermalRelay;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                            {
                                                thermalRelay = thermalRelayProtectionCircuit.thermalRelay;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                            {
                                            }
                                            if (thermalRelay != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSThermalRelayModel(thermalRelay);
                                                cb += () => UpdatePropertyGrid(vm);
                                                render += () =>
                                                {
                                                    var item = rightTemplates.FirstOrDefault(x => x.Value == i && x.Key.UnicodeString == "KH");
                                                    if (item.Key != null)
                                                    {
                                                        var bd = new Binding() { Converter = cvt, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                        item.Key.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                    }
                                                };
                                            }
                                            else
                                            {
                                                cb += () => UpdatePropertyGrid(null);
                                            }
                                        }
                                        else if (info.BlockName == "CPS")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            PDS.Project.Module.Component.CPS cps = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                            {
                                                cps = motor_CPSCircuit.cps;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                            {
                                                cps = motor_CPSStarTriangleStartCircuit.cps;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                            {
                                            }
                                            if (cps != null)
                                            {
                                                var vm = new Project.Module.Component.ThPDSCPSModel(cps);
                                                cb += () => UpdatePropertyGrid(vm);
                                            }
                                            else
                                            {
                                                cb += () => UpdatePropertyGrid(null);
                                            }
                                        }
                                        else if (info.BlockName == "Meter")
                                        {
                                            if (this.TreeView.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
                                            var vertice = graph.Vertices.ToList()[sel.Id];
                                            var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                            PDS.Project.Module.Component.Meter meter = null;
                                            if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                            {
                                                meter = distributionMetering_MTInBehindCircuit.meter;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                            {
                                                meter = distributionMetering_CTInFrontCircuit.meter;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                            {
                                                meter = distributionMetering_CTInBehindCircuit.meter;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                            {
                                                meter = distributionMetering_MTInFrontCircuit.meter;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                            {
                                                meter = distributionMetering_ShanghaiCTCircuit.meter;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                            {
                                                meter = distributionMetering_ShanghaiMTCircuit.meter;
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                            {
                                            }
                                            else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                            {
                                            }
                                            if (meter != null)
                                            {
                                                object vm = null;
                                                if (meter is PDS.Project.Module.Component.MeterTransformer meterTransformer)
                                                {
                                                    vm = new Project.Module.Component.ThPDSMeterTransformerModel(meterTransformer);
                                                }
                                                else if (meter is PDS.Project.Module.Component.CurrentTransformer currentTransformer)
                                                {
                                                    vm = new Project.Module.Component.ThPDSCurrentTransformerModel(currentTransformer);
                                                }
                                                cb += () => UpdatePropertyGrid(vm);
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
                                        dccbs += dc =>
                                        {
                                            var rect = new Rect(r.X, -r.Y - r.Height, r.Width, r.Height);
                                            dc.DrawRectangle(br, pen, rect);
                                            cbDict[rect] = () =>
                                            {
                                                setSel(rect);
                                                cb?.Invoke();
                                            };
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
                        if (ui is Path or Glyphs or TextBlock) before.Add((FrameworkElement)ui);
                    }
                    DrawGeos(canvas, trans, item);
                    var after = new HashSet<FrameworkElement>(canvas.Children.Count - before.Count);
                    foreach (var ui in canvas.Children)
                    {
                        if (ui is Path or Glyphs or TextBlock)
                        {
                            var fe = (FrameworkElement)ui;
                            if (!before.Contains(fe))
                            {
                                after.Add(fe);
                            }
                        }
                    }
                    {
                        var cvt = new NormalValueConverter(v => (bool)v ? .6 : 1.0);
                        var bd = new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitLock)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                        foreach (var fe in after)
                        {
                            fe.SetBinding(UIElement.OpacityProperty, bd);
                        }
                    }
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
                                mi.Header = "切换回路样式";
                                var outTypes = new CircuitFormOutType[]
                                {
                                    CircuitFormOutType.常规,
                                    CircuitFormOutType.漏电,
                                    CircuitFormOutType.电动机_分立元件,
                                    CircuitFormOutType.电动机_CPS,
                                    CircuitFormOutType.双速电动机_CPSdetailYY,
                                    CircuitFormOutType.双速电动机_分立元件detailYY,
                                    CircuitFormOutType.双速电动机_分立元件YY,
                                };
                                foreach (var outType in outTypes)
                                {
                                    var m = new MenuItem();
                                    mi.Items.Add(m);
                                    m.Header = outType.GetEnumDescription();
                                    m.Command = new PDSCommand(() =>
                                    {
                                        ThPDSProjectGraphService.SwitchFormOutType(edge, outType);
                                        UpdateCanvas();
                                    });
                                }
                            }
                            {
                                var m = new MenuItem();
                                menu.Items.Add(m);
                                var cvt = new NormalValueConverter(v => (bool)v ? "解锁回路数据" : "锁定回路数据");
                                m.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitLock)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                m.Command = new PDSCommand(() =>
                                {
                                    circuitVM.CircuitLock = !circuitVM.CircuitLock;
                                });
                            }
                            {
                                var m = new MenuItem();
                                menu.Items.Add(m);
                                m.Header = "分配负载";
                                m.Command = new PDSCommand(() =>
                                {
                                    UpdateCanvas();
                                });
                            }
                            {
                                var m = new MenuItem();
                                menu.Items.Add(m);
                                m.Header = "删除";
                                m.Command = new PDSCommand(() =>
                                {
                                    var vertice = graph.Vertices.ToList()[sel.Id];
                                    ThPDSProjectGraphService.DeleteCircuit(graph, edge);
                                    UpdateCanvas();
                                });
                            }
                        }
                        const double offsetY = -20;
                        Canvas.SetLeft(cvs, item.BasePoint.X + w1);
                        Canvas.SetTop(cvs, -item.BasePoint.Y - offsetY);
                        var cvs2 = new Canvas
                        {
                            Width = w1 + w2,
                            Height = h,
                            Background = Brushes.Transparent,
                            IsHitTestVisible = false,
                        };
                        Canvas.SetLeft(cvs2, item.BasePoint.X);
                        Canvas.SetTop(cvs2, -item.BasePoint.Y - offsetY);
                        hoverDict[cvs] = cvs2;
                        dccbs += dc =>
                        {
                            var rect = new Rect(item.BasePoint.X + w1, -item.BasePoint.Y - offsetY, w2, h);
                            dc.DrawRectangle(br, pen, rect);
                            cbDict[rect] = () =>
                            {
                                setSel(new Rect(Canvas.GetLeft(cvs2), Canvas.GetTop(cvs2), cvs2.Width, cvs2.Height));
                                var ok = false;
                                if (this.TreeView.SelectedItem is ThPDSCircuitGraphTreeModel sel)
                                {
                                    var vertice = graph.Vertices.ToList()[sel.Id];
                                    var edge = graph.Edges.Where(eg => eg.Source == graph.Vertices.ToList()[sel.Id]).ToList()[i];
                                    if (edge != null)
                                    {
                                        ok = true;
                                        UpdatePropertyGrid(circuitVM);
                                    }
                                }
                                if (!ok)
                                {
                                    UpdatePropertyGrid(null);
                                }
                            };
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
                    {
                        {
                            var name = "SPD附件";
                            var item = PDSItemInfo.Create(name, new Point(busStart.X, dy + 20));
                            var before = new HashSet<FrameworkElement>(canvas.Children.Count);
                            foreach (var ui in canvas.Children)
                            {
                                if (ui is Path or Glyphs or TextBlock) before.Add((FrameworkElement)ui);
                            }
                            DrawGeos(canvas, trans, item);
                            var after = new HashSet<FrameworkElement>(canvas.Children.Count - before.Count);
                            foreach (var ui in canvas.Children)
                            {
                                if (ui is Path or Glyphs or TextBlock)
                                {
                                    var fe = (FrameworkElement)ui;
                                    if (!before.Contains(fe))
                                    {
                                        after.Add(fe);
                                    }
                                }
                            }
                            foreach (var fe in after)
                            {
                                fe.SetBinding(UIElement.VisibilityProperty, new Binding() { Source = Config.Current, Path = new PropertyPath(nameof(Config.Current.SurgeProtection)), Converter = new EqualsThenNotVisibeConverter(PDS.Project.Module.SurgeProtectionDeviceType.None), });
                                if (fe is Glyphs g)
                                {
                                    g.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Source = Config.Current, Converter = cvt, Path = new PropertyPath(nameof(Config.Current.SurgeProtection)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                                }
                            }
                            var _info = PDSItemInfo.GetBlockDefInfo(name);
                            if (_info != null)
                            {
                                dy -= _info.Bounds.Height;
                                busEnd = busEnd.OffsetXY(0, _info.Bounds.Height);
                                insertGaps.Add(new GLineSegment(busEnd, busEnd.OffsetXY(500, 0)));
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
                                    var glyph = new Glyphs() { UnicodeString = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                                    leftTemplates.Add(glyph);
                                    if (info.Height > 0)
                                    {
                                        glyph.FontRenderingEmSize = info.Height;
                                    }
                                    Canvas.SetLeft(glyph, info.BasePoint.X);
                                    Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                                    canvas.Children.Add(glyph);
                                }
                                foreach (var hatch in item.hatchInfos)
                                {
                                    if (hatch.Points.Count < 3) continue;
                                    var geo = new PathGeometry();
                                    var path = new Path
                                    {
                                        Fill = strockBrush,
                                        Stroke = strockBrush,
                                        Data = geo,
                                        RenderTransform = trans
                                    };
                                    var figure = new PathFigure
                                    {
                                        StartPoint = hatch.Points[0]
                                    };
                                    for (int i = 1; i < hatch.Points.Count; i++)
                                    {
                                        figure.Segments.Add(new LineSegment(hatch.Points[i], false));
                                    }
                                    geo.Figures.Add(figure);
                                    canvas.Children.Add(path);
                                }
                                foreach (var info in item.brInfos)
                                {
                                    DrawGeos(canvas, trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red);
                                }
                            }
                        }
                    }
                    var shouldDrawBusLine = left.Contains("进线");
                    var width = 20.0;
                    var thickness = 5.0;
                    if (shouldDrawBusLine)
                    {
                        var len = busEnd.Y - busStart.Y;
                        var minLen = circuitInType == CircuitFormInType.三路进线 ? 200.0 : 100.0;
                        if (len < minLen) len = minLen;
                        Config.Current.BusLength = len;
                        var path = DrawLine(canvas, null, Brushes.Black, busStart, busEnd);
                        path.StrokeThickness = thickness;
                        BindingOperations.SetBinding(path, Path.DataProperty, new Binding() { Source = Config.Current, Path = new PropertyPath(nameof(Config.Current.BusLength)), Converter = new NormalValueConverter(v => new LineGeometry(busStart, busStart.OffsetXY(0, (double)v))), });
                    }
                    var cvs = new Canvas
                    {
                        Width = width,
                        Background = Brushes.Transparent,
                    };
                    BindingOperations.SetBinding(cvs, FrameworkElement.HeightProperty, new Binding() { Source = Config.Current, Path = new PropertyPath(nameof(Config.Current.BusLength)) });
                    Canvas.SetLeft(cvs, busStart.X - (width - thickness / 2) / 2);
                    Canvas.SetTop(cvs, busStart.Y);
                    hoverDict[cvs] = cvs;
                    cvs.MouseUp += (s, e) =>
                    {
                        if (e.ChangedButton != MouseButton.Left) return;
                        setSel(new Rect(Canvas.GetLeft(cvs), Canvas.GetTop(cvs), cvs.Width, cvs.Height));
                        UpdatePropertyGrid(boxVM);
                        e.Handled = true;
                    };
                    {
                        var menu = new ContextMenu();
                        var vertice = graph.Vertices.ToList()[sel.Id];
                        cvs.ContextMenu = menu;
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "新建回路";
                            var outTypes = new CircuitFormOutType[]
                            {
                                CircuitFormOutType.常规,
                                CircuitFormOutType.漏电,
                                CircuitFormOutType.电动机_分立元件,
                                CircuitFormOutType.电动机_CPS,
                                CircuitFormOutType.双速电动机_CPSdetailYY,
                                CircuitFormOutType.双速电动机_分立元件detailYY,
                                CircuitFormOutType.双速电动机_分立元件YY,
                            };
                            foreach (var outType in outTypes)
                            {
                                var m = new MenuItem();
                                mi.Items.Add(m);
                                m.Header = outType.GetDescription();
                                m.Command = new PDSCommand(() =>
                                {
                                    ThPDSProjectGraphService.AddCircuit(graph, vertice, outType);
                                    UpdateCanvas();
                                });
                            }
                        }
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "切换进线形式";
                            var inTypes = new CircuitFormInType[] {
                                CircuitFormInType.一路进线,
                                CircuitFormInType.二路进线ATSE,
                                CircuitFormInType.三路进线,
                                CircuitFormInType.集中电源,
                            };
                            foreach (var inType in inTypes)
                            {
                                var m = new MenuItem();
                                mi.Items.Add(m);
                                m.Header = inType.GetDescription();
                                m.Command = new PDSCommand(() =>
                                {
                                    ThPDSProjectGraphService.UpdateFormInType(graph, vertice, inType);
                                    UpdateCanvas();
                                });
                            }
                        }
                    }
                    cvs.Cursor = Cursors.Hand;
                    canvas.Children.Add(cvs);
                }
                {
                    void f(object sender, MouseButtonEventArgs e)
                    {
                        if (e.ChangedButton != MouseButton.Left) return;
                        cbDict.Clear();
                        using (var dc = dv.RenderOpen())
                        {
                            dccbs?.Invoke(dc);
                        }
                        var ok = false;
                        {
                            var pt = e.GetPosition(canvas);
                            var rects = new List<Rect>();
                            EnumDrawingGroup(VisualTreeHelper.GetDrawing(dv));
                            void EnumDrawingGroup(DrawingGroup drawingGroup)
                            {
                                var drawingCollection = drawingGroup.Children;
                                foreach (Drawing drawing in drawingCollection)
                                {
                                    if (drawing is DrawingGroup group)
                                    {
                                        EnumDrawingGroup(group);
                                    }
                                    else if (drawing is GeometryDrawing geometryDrawing)
                                    {
                                        var geo = geometryDrawing.Geometry;
                                        if (geo.FillContains(pt))
                                        {
                                            if (geo is RectangleGeometry rectangle)
                                            {
                                                rects.Add(rectangle.Rect);
                                            }
                                        }
                                    }
                                }
                            }
                            var rect = rects.Where(x => cbDict.ContainsKey(x)).OrderByDescending(x => x.Width * x.Height).FirstOrDefault();
                            if (rect.Width * rect.Height > 0)
                            {
                                ok = true;
                                cbDict[rect]();
                            }
                        }
                        if (!ok)
                        {
                            setSel(default);
                            UpdatePropertyGrid(boxVM);
                        }
                        e.Handled = true;
                    }
                    canvas.MouseUp += f;
                    clear += () => { canvas.MouseUp -= f; };
                    foreach (var kv in hoverDict)
                    {
                        ((Panel)kv.Key).MouseEnter += (s, e) =>
                        {
                            ((Panel)kv.Value).Background = LightBlue3;
                        };
                        ((Panel)kv.Key).MouseLeave += (s, e) =>
                        {
                            ((Panel)kv.Value).Background = Brushes.Transparent;
                        };
                    }
                }
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
                new BlockDefInfo("TSE", new GRect(-50, -26, 0, 26)),
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
                new BlockDefInfo("Meter", new GRect(-10, -4, 10, 10)),
                new BlockDefInfo("CT", new GRect(0, -8, 50, 18)),
                new BlockDefInfo("1路进线", new GRect(0, -140, 200, -2)),
                new BlockDefInfo("2路进线ATSE", new GRect(0, -300, 200, -2)),
                new BlockDefInfo("3路进线", new GRect(0, -300, 200, -2)),
                new BlockDefInfo("分支小母排", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("小母排分支", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
                new BlockDefInfo("消防应急照明回路（WFEL）", new GRect(0, -16, 628, 21).Expand(0, EXPY)),
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
                        r.textInfos.Add(new DBTextInfo(px.OffsetXY(116, -316), "本设备由厂家配套", "E-UNIV-NOTE", "TH-STYLE3"));
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
                        r.lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(205, 0), px.OffsetXY(205, -80)), "E-UNIV-WIRE"));
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
                    default:
                        return null;
                }
                return r;
            }
        }
    }
}