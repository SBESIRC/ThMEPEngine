using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using Microsoft.Toolkit.Mvvm.Input;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Helpers;
using TianHua.Electrical.PDS.UI.Services;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.UI.Converters;
using ThCADExtension;
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
        public NodeDetails Detail;
        public PDSNodeType Type;
    }
    public class ThPDSContext
    {
        public List<ThPDSVertex> Vertices;
        public List<int> Souces;
        public List<int> Targets;
        public List<ThPDSCircuit> Circuits;
        public List<CircuitDetails> Details;
    }
    public class ThPDSDistributionPanelService
    {
        public void Init(UserContorls.ThPDSDistributionPanel panel)
        {
            var graph = Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
            var vertices = graph.Vertices.Select(x => new ThPDSVertex { Detail = x.Details, Type = x.Type }).ToList();
            var srcLst = graph.Edges.Select(x => graph.Vertices.ToList().IndexOf(x.Source)).ToList();
            var dstLst = graph.Edges.Select(x => graph.Vertices.ToList().IndexOf(x.Target)).ToList();
            var circuitLst = graph.Edges.Select(x => x.Circuit).ToList();
            var details = graph.Edges.Select(x => x.Details).ToList();
            var ctx = new ThPDSContext() { Vertices = vertices, Souces = srcLst, Targets = dstLst, Circuits = circuitLst, Details = details };
            var tv = panel.tv;
            var treeCMenu = tv.ContextMenu;
            var config = new ThPDSDistributionPanelConfig();
            treeCMenu.DataContext = config;
            var builder = new ViewModels.ThPDSCircuitGraphTreeBuilder();
            var tree = builder.Build(graph);
            static string FixString(string text)
            {
                if (string.IsNullOrEmpty(text)) return " ";
                return text;
            }
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
            var batchGenCmd = new RelayCommand(() =>
            {
                UI.ElecSandboxUI.TryGetCurrentWindow()?.Hide();
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
                    if (checkeddVertices.Count == 0) return;
                    var drawCmd = new Command.ThPDSSystemDiagramCommand(graph, checkeddVertices);
                    drawCmd.Execute();
                    AcHelper.Active.Editor.Regen();
                }
                finally
                {
                    UI.ElecSandboxUI.TryGetCurrentWindow()?.Show();
                }
            });
            var treeCmenu = new ContextMenu()
            {
                ItemsSource = new MenuItem[] {
                    new MenuItem()
                    {
                        Header="批量生成",
                        Command=batchGenCmd,
                    },
                },
            };
            tv.ContextMenu = treeCmenu;
            var canvas = panel.canvas;
            canvas.Background = Brushes.Transparent;
            canvas.Width = 2000;
            canvas.Height = 2000;
            var fontUri = new Uri(System.IO.Path.Combine(Environment.GetEnvironmentVariable("windir"), @"Fonts\simHei.ttf"));
            var glyphsUnicodeStrinConverter = new GlyphsUnicodeStringConverter();
            Action clear = null;
            {
                var menu = new MenuItem()
                {
                    Header = "平衡相序",
                    Command = new RelayCommand(() =>
                    {
                        ThPDSProjectGraphService.BalancedPhaseSequence(graph, GetCurrentVertice());
                    }),
                };
                treeCMenu.Items.Add(menu);
            }
            tv.DataContext = tree;
            Action<DrawingContext> dccbs;
            var cbDict = new Dictionary<Rect, Action>(4096);
            var br = Brushes.Black;
            var pen = new Pen(br, 1);
            tv.SelectedItemChanged += (s, e) =>
            {
                var vertice = GetCurrentVertice();
                if (vertice is not null)
                {
                    if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                    {
                        var cm = new ContextMenu();
                        tv.ContextMenu = cm;
                        cm.Items.Add(new MenuItem()
                        {
                            Header = "平衡相序",
                            Command = new RelayCommand(() =>
                            {
                                ThPDSProjectGraphService.BalancedPhaseSequence(graph, GetCurrentVertice());
                            }),
                        });
                        cm.Items.Add(new MenuItem()
                        {
                            Header = "批量生成",
                            Command = batchGenCmd,
                        });
                    }
                    else
                    {
                        tv.ContextMenu = treeCMenu;
                    }
                    var boxVM = new Project.Module.Component.ThPDSDistributionBoxModel(vertice);
                    UpdatePropertyGrid(boxVM);
                }
                else
                {
                    tv.ContextMenu = treeCmenu;
                    UpdatePropertyGrid(null);
                }
                UpdateCanvas();
            };
            void UpdatePropertyGrid(object vm)
            {
                var pg = panel.propertyGrid;
                if (vm is Project.Module.Component.ThPDSBreakerModel breaker)
                {
                    if (breaker.ComponentType == ComponentType.CB)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSBreakerModel>("RCDType", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSBreakerModel>("ResidualCurrent", false);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSBreakerModel>("RCDType", true);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSBreakerModel>("ResidualCurrent", true);
                    }
                    if (breaker.ComponentType == ComponentType.组合式RCD)
                    {
                        ThPDSPropertyDescriptorHelper.SetReadOnlyProperty<Project.Module.Component.ThPDSBreakerModel>("Appendix", true);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetReadOnlyProperty<Project.Module.Component.ThPDSBreakerModel>("Appendix", false);
                    }
                }
                if (vm is Project.Module.Component.ThPDSCircuitModel circuit)
                {
                    if (circuit.IsDualPower)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSCircuitModel>("Power", false);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSCircuitModel>("LowPower", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSCircuitModel>("HighPower", false);
                    }
                }
                if (vm is Project.Module.Component.ThPDSConductorModel conductor)
                {
                    if (conductor.ComponentType == ComponentType.Conductor)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSConductorModel>("ConductorCount", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSConductorModel>("ControlConductorCrossSectionalArea", false);
                    }
                    else if (conductor.ComponentType == ComponentType.ControlConductor)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSConductorModel>("NumberOfPhaseWire", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<Project.Module.Component.ThPDSConductorModel>("ConductorCrossSectionalArea", false);
                    }
                }
                if (vm is Project.Module.Component.ThPDSCircuitModel circuitVM)
                {
                    pg.SetBinding(UIElement.IsEnabledProperty, new Binding() { Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitLock)), Converter = new NotConverter() });
                }
                else
                {
                    BindingOperations.ClearBinding(pg, UIElement.IsEnabledProperty);
                }
                pg.SelectedObject = vm ?? new object();
            }
            ThPDSProjectGraphNode GetCurrentVertice()
            {
                var id = tv.SelectedItem is ThPDSCircuitGraphTreeModel sel1 ? sel1.Id : (tv.SelectedItem is ThPDSGraphTreeModel sel2 ? sel2.Id : -1);
                if (id < 0) return null;
                return graph.Vertices.ToList()[id];
            }
            void UpdateCanvas()
            {
                canvas.Children.Clear();
                clear?.Invoke();
                dccbs = null;
                canvas.ContextMenu = null;
                var vertice = GetCurrentVertice();
                if (vertice is null) return;
                config.Current = new(vertice);
                config.BatchGenerate = batchGenCmd;
                {
                    var cmenu = new ContextMenu();
                    cmenu.Items.Add(new MenuItem()
                    {
                        Header = "单独生成",
                        Command = new RelayCommand(() =>
                        {
                            UI.ElecSandboxUI.TryGetCurrentWindow()?.Hide();
                            try
                            {
                                var drawCmd = new Command.ThPDSSystemDiagramCommand(graph, new List<ThPDSProjectGraphNode>() { vertice, });
                                drawCmd.Execute();
                                AcHelper.Active.Editor.Regen();
                            }
                            finally
                            {
                                UI.ElecSandboxUI.TryGetCurrentWindow()?.Show();
                            }
                        }),
                    });
                    canvas.ContextMenu = cmenu;
                }
                var hoverDict = new Dictionary<object, object>();
                var dv = new DrawingVisual();
                var circuitInType = vertice.Details.CircuitFormType.CircuitFormType;
                var left = ThCADExtension.ThEnumExtension.GetDescription(circuitInType) ?? "1路进线";
                FrameworkElement selElement = null;
                Rect selArea = default;
                Point busStart, busEnd;
                var trans = new ScaleTransform(1, -1, 0, 0);
                OUVP GetInputOUVP()
                {
                    if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneWayInCircuit)
                    {
                        return oneWayInCircuit.reservedComponent as OUVP;
                    }
                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.TwoWayInCircuit twoWayInCircuit)
                    {
                        return twoWayInCircuit.reservedComponent as OUVP;
                    }
                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.ThreeWayInCircuit threeWayInCircuit)
                    {
                        return threeWayInCircuit.reservedComponent as OUVP;
                    }
                    else
                    {
                        return null;
                    }
                }
                Meter GetInputMeter()
                {
                    if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneWayInCircuit)
                    {
                        return oneWayInCircuit.reservedComponent as Meter;
                    }
                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.TwoWayInCircuit twoWayInCircuit)
                    {
                        return twoWayInCircuit.reservedComponent as Meter;
                    }
                    else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.ThreeWayInCircuit threeWayInCircuit)
                    {
                        return threeWayInCircuit.reservedComponent as Meter;
                    }
                    else
                    {
                        return null;
                    }
                }
                var boxVM = new Project.Module.Component.ThPDSDistributionBoxModel(vertice);
                UpdatePropertyGrid(boxVM);
                {
                    var leftTemplates = new List<Glyphs>();
                    PDSItemInfo item;
                    if (GetInputOUVP() != null)
                    {
                        item = PDSItemInfo.Create(left.Contains("进线") ? left + "（带过欠电压保护）" : left, default);
                    }
                    else if (GetInputMeter() != null)
                    {
                        item = PDSItemInfo.Create(left.Contains("进线") ? left + "（带电表）" : left, default);
                    }
                    else
                    {
                        item = PDSItemInfo.Create(left, default);
                    }
                    if (item is null) throw new NotSupportedException(left);
                    foreach (var fe in CreateDrawingObjects(canvas, trans, item))
                    {
                        canvas.Children.Add(fe);
                    }
                    IEnumerable<FrameworkElement> CreateDrawingObjects(Canvas canvas, Transform trans, PDSItemInfo item, Brush strockBrush = null)
                    {
                        strockBrush ??= Brushes.Black;
                        foreach (var info in item.lineInfos)
                        {
                            var st = info.Line.StartPoint;
                            var ed = info.Line.EndPoint;
                            yield return CreateLine(trans, strockBrush, st, ed);
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
                            yield return path;
                        }
                        foreach (var info in item.circleInfos)
                        {
                            var path = new Path();
                            var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                            path.Stroke = strockBrush;
                            path.Data = geo;
                            path.RenderTransform = trans;
                            yield return path;
                        }
                        foreach (var info in item.textInfos)
                        {
                            var glyph = new Glyphs() { UnicodeString = FixString(info.Text), Tag = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                            leftTemplates.Add(glyph);
                            if (info.Height > 0)
                            {
                                glyph.FontRenderingEmSize = info.Height;
                            }
                            Canvas.SetLeft(glyph, info.BasePoint.X);
                            Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                            yield return glyph;
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
                            yield return path;
                        }
                        {
                            {
                                var circuitNumbers = Service.ThPDSCircuitNumberSeacher.Seach(vertice, graph);
                                var str = string.Join(",", circuitNumbers);
                                {
                                    var m = leftTemplates.FirstOrDefault(x => x.Tag as string == "进线回路编号");
                                    if (m != null)
                                    {
                                        m.UnicodeString = FixString(str);
                                    }
                                }
                                {
                                    var m = leftTemplates.FirstOrDefault(x => x.Tag as string == "进线回路编号1");
                                    if (m != null)
                                    {
                                        var s = circuitNumbers.Count >= 1 ? circuitNumbers[0] : str;
                                        m.UnicodeString = FixString(s);
                                    }
                                }
                                {
                                    var m = leftTemplates.FirstOrDefault(x => x.Tag as string == "进线回路编号2");
                                    if (m != null)
                                    {
                                        var s = circuitNumbers.Count >= 2 ? circuitNumbers[1] : str;
                                        m.UnicodeString = FixString(s);
                                    }
                                }
                                {
                                    var m = leftTemplates.FirstOrDefault(x => x.Tag as string == "进线回路编号3");
                                    if (m != null)
                                    {
                                        var s = circuitNumbers.Count >= 3 ? circuitNumbers[2] : str;
                                        m.UnicodeString = FixString(s);
                                    }
                                }
                            }
                        }
                        foreach (var info in item.brInfos)
                        {
                            foreach (var el in CreateDrawingObjects(canvas, trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red))
                            {
                                yield return el;
                            }
                            var _info = PDSItemInfo.GetBlockDefInfo(info.BlockName);
                            if (_info is null) continue;
                            var r = _info.Bounds.ToWpfRect().OffsetXY(info.BasePoint.X, info.BasePoint.Y);
                            var tr = new TranslateTransform(r.X, -r.Y - r.Height);
                            var cvs = new Canvas
                            {
                                Width = r.Width,
                                Height = r.Height,
                                Background = Brushes.Transparent,
                                RenderTransform = tr
                            };
                            Action cb = null;
                            IEnumerable<MenuItem> getInputMenus()
                            {
                                if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                                {
                                    yield break;
                                }
                                if (GetInputOUVP() == null && GetInputMeter() == null)
                                {
                                    yield return new MenuItem()
                                    {
                                        Header = "增加过欠电压保护",
                                        Command = new RelayCommand(() =>
                                        {
                                            ThPDSProjectGraphService.InsertUndervoltageProtector(new ThPDSProjectGraph(graph), vertice);
                                            UpdateCanvas();
                                        }),
                                    };
                                    yield return new MenuItem()
                                    {
                                        Header = "增加电能表",
                                        Command = new RelayCommand(() =>
                                        {
                                            ThPDSProjectGraphService.InsertEnergyMeter(new ThPDSProjectGraph(graph), vertice);
                                            UpdateCanvas();
                                        }),
                                    };
                                }
                                else
                                {
                                    yield return new MenuItem()
                                    {
                                        Header = "还原为标准样式",
                                        Command = new RelayCommand(() =>
                                        {
                                            ThPDSProjectGraphService.RemoveUndervoltageProtector(new ThPDSProjectGraph(graph), vertice);
                                            ThPDSProjectGraphService.RemoveEnergyMeter(new ThPDSProjectGraph(graph), vertice);
                                            UpdateCanvas();
                                        }),
                                    };
                                }
                            }
                            cvs.MouseEnter += (s, e) => { cvs.Background = LightBlue3; };
                            cvs.MouseLeave += (s, e) => { cvs.Background = Brushes.Transparent; };
                            if (info.IsOUVP())
                            {
                                cb += () => UpdatePropertyGrid(new { Type = "过欠电压保护器", });
                                var ouvp = GetInputOUVP();
                                if (ouvp != null)
                                {
                                    var vm = new Project.Module.Component.ThPDSOUVPModel(ouvp);
                                    cb += () => UpdatePropertyGrid(vm);
                                    {
                                        var m = leftTemplates.FirstOrDefault(x => x.Tag as string == "过欠电压保护器");
                                        if (m != null)
                                        {
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                        }
                                    }
                                }
                                else
                                {
                                    cb += () => UpdatePropertyGrid(null);
                                }
                            }
                            else if (info.IsIsolator())
                            {
                                void reg(IsolatingSwitch isolatingSwitch, string templateStr)
                                {
                                    var vm = new Project.Module.Component.ThPDSIsolatingSwitchModel(isolatingSwitch);
                                    cb += () => UpdatePropertyGrid(vm);
                                    {
                                        var m = leftTemplates.FirstOrDefault(x => x.Tag as string == templateStr);
                                        if (m != null)
                                        {
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                        }
                                    }
                                }
                                var isolatingSwitches = item.brInfos.Where(x => x.BlockName == info.BlockName).ToList();
                                var idx = isolatingSwitches.IndexOf(info);
                                IsolatingSwitch isolatingSwitch = null, isolatingSwitch1 = null, isolatingSwitch2 = null, isolatingSwitch3 = null;
                                if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneway)
                                {
                                    isolatingSwitch = oneway.isolatingSwitch;
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.TwoWayInCircuit twoWayInCircuit)
                                {
                                    isolatingSwitch1 = twoWayInCircuit.isolatingSwitch1;
                                    isolatingSwitch2 = twoWayInCircuit.isolatingSwitch2;
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.ThreeWayInCircuit threeWayInCircuit)
                                {
                                    isolatingSwitch1 = threeWayInCircuit.isolatingSwitch1;
                                    isolatingSwitch2 = threeWayInCircuit.isolatingSwitch2;
                                    isolatingSwitch3 = threeWayInCircuit.isolatingSwitch3;
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                                {
                                    isolatingSwitch = centralizedPowerCircuit.isolatingSwitch;
                                }
                                if (isolatingSwitch != null)
                                {
                                    reg(isolatingSwitch, "QL");
                                }
                                else if (isolatingSwitches.Count > 1)
                                {
                                    isolatingSwitch = idx == 0 ? isolatingSwitch1 : (idx == 1 ? isolatingSwitch2 : isolatingSwitch3);
                                    if (isolatingSwitch != null)
                                    {
                                        reg(isolatingSwitch, "QL" + (idx + 1));
                                    }
                                    else
                                    {
                                        cb += () => UpdatePropertyGrid(null);
                                    }
                                }
                                else
                                {
                                    cb += () => UpdatePropertyGrid(null);
                                }
                            }
                            else if (info.IsATSE())
                            {
                                if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneway)
                                {
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.TwoWayInCircuit twoWayInCircuit)
                                {
                                    var sw = twoWayInCircuit.transferSwitch;
                                    if (sw != null)
                                    {
                                        var vm = new Project.Module.Component.ThPDSATSEModel(sw);
                                        cb += () => UpdatePropertyGrid(vm);
                                        {
                                            var m = leftTemplates.FirstOrDefault(x => x.UnicodeString is "ATSE");
                                            if (m != null)
                                            {
                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        cb += () => UpdatePropertyGrid(null);
                                    }
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.ThreeWayInCircuit threeWayInCircuit)
                                {
                                    {
                                        var sw = threeWayInCircuit.transferSwitch1;
                                        if (sw != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSATSEModel(sw);
                                            cb += () => UpdatePropertyGrid(vm);
                                            {
                                                var m = leftTemplates.FirstOrDefault(x => x.UnicodeString is "ATSE");
                                                if (m != null)
                                                {
                                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cb += () => UpdatePropertyGrid(null);
                                        }
                                    }
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                                {
                                }
                            }
                            else if (info.IsMTSE())
                            {
                                if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.OneWayInCircuit oneway)
                                {
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.TwoWayInCircuit twoWayInCircuit)
                                {
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.ThreeWayInCircuit threeWayInCircuit)
                                {
                                    var sw = threeWayInCircuit.transferSwitch2;
                                    if (sw != null)
                                    {
                                        var vm = new Project.Module.Component.ThPDSMTSEModel(sw);
                                        cb += () => UpdatePropertyGrid(vm);
                                        {
                                            var m = leftTemplates.FirstOrDefault(x => x.UnicodeString is "MTSE");
                                            if (m != null)
                                            {
                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        cb += () => UpdatePropertyGrid(null);
                                    }
                                }
                                else if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                                {
                                }
                            }
                            else if (info.IsMeter())
                            {
                                var meter = GetInputMeter();
                                if (meter != null)
                                {
                                    IEnumerable<MenuItem> getMeterMenus(Meter meter)
                                    {
                                        if (meter is CurrentTransformer)
                                        {
                                            yield return new MenuItem()
                                            {
                                                Header = "切换为直接表",
                                                Command = new RelayCommand(() =>
                                                {
                                                    ThPDSProjectGraphService.ComponentSwitching(vertice, meter, ComponentType.MT);
                                                    UpdateCanvas();
                                                }),
                                            };
                                        }
                                        else if (meter is MeterTransformer)
                                        {
                                            yield return new MenuItem()
                                            {
                                                Header = "切换为间接表",
                                                Command = new RelayCommand(() =>
                                                {
                                                    ThPDSProjectGraphService.ComponentSwitching(vertice, meter, ComponentType.CT);
                                                    UpdateCanvas();
                                                }),
                                            };
                                        }
                                    }
                                    object vm = null;
                                    if (meter is MeterTransformer meterTransformer)
                                    {
                                        var o = new Project.Module.Component.ThPDSMeterTransformerModel(meterTransformer); ;
                                        vm = o;
                                        {
                                            var m = leftTemplates.FirstOrDefault(x => x.UnicodeString is "MT" or "CT");
                                            if (m != null)
                                            {
                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(o.ContentMT)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                        }
                                    }
                                    else if (meter is CurrentTransformer currentTransformer)
                                    {
                                        var o = new Project.Module.Component.ThPDSCurrentTransformerModel(currentTransformer);
                                        vm = o;
                                        {
                                            var m = leftTemplates.FirstOrDefault(x => x.UnicodeString is "MT" or "CT");
                                            if (m != null)
                                            {
                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(o.ContentCT)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                        }
                                    }
                                    cb += () => UpdatePropertyGrid(vm);
                                    var cmenu = new ContextMenu();
                                    cvs.ContextMenu = cmenu;
                                    foreach (var menu in getMeterMenus(meter))
                                    {
                                        cmenu.Items.Add(menu);
                                    }
                                }
                                else
                                {
                                    cb += () => UpdatePropertyGrid(null);
                                    var cmenu = new ContextMenu();
                                    cvs.ContextMenu = cmenu;
                                }
                            }
                            {
                                var cmenu = cvs.ContextMenu ?? new ContextMenu();
                                foreach (var m in getInputMenus())
                                {
                                    cmenu.Items.Add(m);
                                }
                                cvs.ContextMenu = cmenu;
                            }
                            cvs.MouseUp += (s, e) =>
                            {
                                void Update()
                                {
                                    SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                    cb?.Invoke();
                                }
                                if (e.ChangedButton != MouseButton.Left)
                                {
                                    if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                    return;
                                }
                                Update();
                                e.Handled = true;
                            };
                            cvs.Cursor = Cursors.Hand;
                            canvas.Children.Add(cvs);
                        }
                    }
                    busStart = new Point(PDSItemInfo.GetBlockDefInfo(left).Bounds.Width, 0);
                    busEnd = busStart;
                }
                var dy = .0;
                var insertGaps = new List<GLineSegment>();
                insertGaps.Add(new GLineSegment(busEnd, busEnd.OffsetXY(500, 0)));
                void DrawEdge(ThPDSProjectGraphEdge edge)
                {
                    {
                        var edgeName = (ThCADExtension.ThEnumExtension.GetDescription(edge.Details.CircuitForm?.CircuitFormType ?? default) ?? "常规").Replace("(", "（").Replace(")", "）");
                        var item = PDSItemInfo.Create(edgeName, new Point(busStart.X, dy + 10));
                        if (item is null) throw new NotSupportedException(edgeName);
                        var circuitVM = new Project.Module.Component.ThPDSCircuitModel(edge);
                        var glyphs = new List<Glyphs>();
                        {
                            var _info = PDSItemInfo.GetBlockDefInfo(edgeName);
                            if (_info != null)
                            {
                                dy -= _info.Bounds.Height;
                                busEnd = busEnd.OffsetXY(0, _info.Bounds.Height);
                                insertGaps.Add(new GLineSegment(busEnd, busEnd.OffsetXY(500, 0)));
                            }
                        }
                        IEnumerable<FrameworkElement> CreateDrawingObjects(Transform trans, PDSItemInfo item, Brush strockBrush = null)
                        {
                            strockBrush ??= Brushes.Black;
                            foreach (var info in item.lineInfos)
                            {
                                var st = info.Line.StartPoint;
                                var ed = info.Line.EndPoint;
                                yield return CreateLine(trans, strockBrush, st, ed);
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
                                yield return path;
                            }
                            foreach (var info in item.circleInfos)
                            {
                                var path = new Path();
                                var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                                path.Stroke = strockBrush;
                                path.Data = geo;
                                path.RenderTransform = trans;
                                yield return path;
                            }
                            foreach (var info in item.textInfos)
                            {
                                var glyph = new Glyphs() { UnicodeString = FixString(info.Text), Tag = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                                if (info.Height > 0)
                                {
                                    glyph.FontRenderingEmSize = info.Height;
                                }
                                glyphs.Add(glyph);
                                Canvas.SetLeft(glyph, info.BasePoint.X);
                                Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                                yield return glyph;
                            }
                            foreach (var info in item.brInfos)
                            {
                                if (info.IsBreaker())
                                {
                                    var breakers = item.brInfos.Where(x => x.IsBreaker()).ToList();
                                    Breaker breaker = null, breaker1 = null, breaker2 = null, breaker3 = null;
                                    var blkVm = new ThPDSBlockViewModel();
                                    Project.Module.Component.ThPDSBreakerModel vm;
                                    void UpdateBreakerViewModel()
                                    {
                                        void reg(Breaker breaker, string templateStr)
                                        {
                                            vm = new(breaker);
                                            blkVm.UpdatePropertyGridCommand = new RelayCommand(() => { UpdatePropertyGrid(vm); });
                                            var m = glyphs.FirstOrDefault(x => x.Tag as string == templateStr);
                                            if (m != null && vm != null)
                                            {
                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                        }
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
                                            breaker1 = distributionMetering_ShanghaiCTCircuit.breaker1;
                                            breaker2 = distributionMetering_ShanghaiCTCircuit.breaker2;
                                        }
                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                        {
                                            breaker1 = distributionMetering_ShanghaiMTCircuit.breaker1;
                                            breaker2 = distributionMetering_ShanghaiMTCircuit.breaker2;
                                        }
                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                        {
                                            throw new NotSupportedException();
                                        }
                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                        {
                                            breaker = leakageCircuit.breaker;
                                        }
                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                        {
                                            throw new NotSupportedException();
                                        }
                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                        {
                                            throw new NotSupportedException();
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
                                            throw new NotSupportedException();
                                        }
                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                        {
                                            throw new NotSupportedException();
                                        }
                                        var idx = breakers.IndexOf(info);
                                        if (breakers.Count > 1)
                                        {
                                            breaker = idx == 0 ? breaker1 : (idx == 1 ? breaker2 : breaker3);
                                            if (breaker != null)
                                            {
                                                reg(breaker, "CB" + (idx + 1));
                                            }
                                        }
                                        else if (breaker != null)
                                        {
                                            reg(breaker, "CB");
                                        }
                                        if (breaker == null)
                                        {
                                            throw new NotSupportedException($"{edge?.Details?.CircuitForm?.CircuitFormType} {edge?.Details?.CircuitForm?.GetType()}");
                                        }
                                        blkVm.BlockName = GetBreakerBlockName(breaker.ComponentType);
                                        blkVm.ContextMenuItems = GetBreakerMenus(breaker);
                                        blkVm.RaisePropertyChangedEvent();
                                    }
                                    var names = new string[] { "CircuitBreaker", "RCD" };
                                    string GetBreakerBlockName(ComponentType type)
                                    {
                                        if (type == ComponentType.CB)
                                        {
                                            return "CircuitBreaker";
                                        }
                                        else
                                        {
                                            return "RCD";
                                        }
                                    }
                                    foreach (var name in names)
                                    {
                                        foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(name, info.BasePoint), Brushes.Red))
                                        {
                                            el.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(blkVm.BlockName)) { Source = blkVm, Converter = new NormalValueConverter(v => (string)v == name ? Visibility.Visible : Visibility.Collapsed), });
                                            yield return el;
                                        }
                                    }
                                    IEnumerable<MenuItem> GetBreakerMenus(Breaker breaker)
                                    {
                                        var types = new ComponentType[] { ComponentType.CB, ComponentType.一体式RCD, ComponentType.组合式RCD, };
                                        foreach (var type in types)
                                        {
                                            if (breaker.ComponentType != type)
                                            {
                                                yield return new MenuItem()
                                                {
                                                    Header = "切换为" + ThCADExtension.ThEnumExtension.GetDescription(type),
                                                    Command = new RelayCommand(() =>
                                                    {
                                                        breaker.SetBreakerType(type);
                                                        UpdateBreakerViewModel();
                                                        blkVm.UpdatePropertyGrid();
                                                    }),
                                                };
                                            }
                                        }
                                    }
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
                                            var cm = new ContextMenu();
                                            cvs.ContextMenu = cm;
                                            cm.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(blkVm.ContextMenuItems)) { Source = blkVm, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                            cvs.MouseUp += (s, e) =>
                                            {
                                                void Update()
                                                {
                                                    SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                    blkVm.UpdatePropertyGrid();
                                                }
                                                if (e.ChangedButton != MouseButton.Left)
                                                {
                                                    if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                    return;
                                                }
                                                Update();
                                                e.Handled = true;
                                            };
                                            cvs.Cursor = Cursors.Hand;
                                            canvas.Children.Add(cvs);
                                        }
                                    }
                                    UpdateBreakerViewModel();
                                    continue;
                                }
                                {
                                    foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red))
                                    {
                                        yield return el;
                                    }
                                    if (info.IsMotor()) continue;
                                    {
                                        var _info = PDSItemInfo.GetBlockDefInfo(info.BlockName);
                                        if (_info != null)
                                        {
                                            void AddSecondaryCircuitMenus(ContextMenu cm)
                                            {
                                                foreach (var scinfo in ThPDSProjectGraphService.GetSecondaryCircuitInfos(edge))
                                                {
                                                    cm.Items.Add(new MenuItem()
                                                    {
                                                        Header = "新建控制回路" + scinfo.SecondaryCircuitCode,
                                                        Command = new RelayCommand(() =>
                                                        {
                                                            ThPDSProjectGraphService.AddControlCircuit(graph, edge, scinfo);
                                                            UpdateCanvas();
                                                        }),
                                                    });
                                                }
                                            }
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
                                                    IEnumerable<MenuItem> getMeterMenus(Meter meter)
                                                    {
                                                        if (meter is CurrentTransformer)
                                                        {
                                                            yield return new MenuItem()
                                                            {
                                                                Header = "切换为直接表",
                                                                Command = new RelayCommand(() =>
                                                                {
                                                                    ThPDSProjectGraphService.ComponentSwitching(edge, meter, ComponentType.MT);
                                                                    UpdateCanvas();
                                                                }),
                                                            };
                                                        }
                                                        else if (meter is MeterTransformer)
                                                        {
                                                            yield return new MenuItem()
                                                            {
                                                                Header = "切换为间接表",
                                                                Command = new RelayCommand(() =>
                                                                {
                                                                    ThPDSProjectGraphService.ComponentSwitching(edge, meter, ComponentType.CT);
                                                                    UpdateCanvas();
                                                                }),
                                                            };
                                                        }
                                                    }
                                                    if (info.IsContactor())
                                                    {
                                                        var contactors = item.brInfos.Where(x => x.BlockName == "Contactor").ToList();
                                                        var idx = contactors.IndexOf(info);
                                                        Contactor contactor = null, contactor1 = null, contactor2 = null, contactor3 = null;
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
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                                        {
                                                            contactor = motor_DiscreteComponentsCircuit.contactor;
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                                        {
                                                            contactor = twoSpeedMotor_CPSDYYCircuit.contactor;
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        if (contactor != null)
                                                        {
                                                            var vm = new Project.Module.Component.ThPDSContactorModel(contactor);
                                                            cb += () => UpdatePropertyGrid(vm);
                                                            {
                                                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "QAC");
                                                                if (m != null)
                                                                {
                                                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                                }
                                                            }
                                                        }
                                                        else if (contactors.Count > 1)
                                                        {
                                                            contactor = idx == 0 ? contactor1 : (idx == 1 ? contactor2 : contactor3);
                                                            if (contactor != null)
                                                            {
                                                                var vm = new Project.Module.Component.ThPDSContactorModel(contactor);
                                                                cb += () => UpdatePropertyGrid(vm);
                                                                {
                                                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "QAC" + (idx + 1));
                                                                    if (m != null)
                                                                    {
                                                                        var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                        m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            throw new ArgumentNullException();
                                                        }
                                                        var cm = new ContextMenu();
                                                        cvs.ContextMenu = cm;
                                                        AddSecondaryCircuitMenus(cm);
                                                    }
                                                    else if (info.IsThermalRelay())
                                                    {
                                                        var thermalRelays = item.brInfos.Where(x => x.IsThermalRelay()).ToList();
                                                        ThermalRelay thermalRelay = null, thermalRelay1 = null, thermalRelay2 = null, thermalRelay3 = null;
                                                        void reg(ThermalRelay thermalRelay, string templateStr)
                                                        {
                                                            var vm = new Project.Module.Component.ThPDSThermalRelayModel(thermalRelay);
                                                            var m = glyphs.FirstOrDefault(x => x.Tag as string == templateStr);
                                                            if (m != null && vm != null)
                                                            {
                                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                            }
                                                            cb += () => { UpdatePropertyGrid(vm); };
                                                        }
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
                                                            thermalRelay1 = twoSpeedMotor_DiscreteComponentsDYYCircuit.thermalRelay1;
                                                            thermalRelay2 = twoSpeedMotor_DiscreteComponentsDYYCircuit.thermalRelay2;
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                                        {
                                                            thermalRelay1 = twoSpeedMotor_DiscreteComponentsYYCircuit.thermalRelay1;
                                                            thermalRelay2 = twoSpeedMotor_DiscreteComponentsYYCircuit.thermalRelay2;
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                                        {
                                                            throw new NotSupportedException();
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
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        var idx = thermalRelays.IndexOf(info);
                                                        if (thermalRelays.Count > 1)
                                                        {
                                                            thermalRelay = idx == 0 ? thermalRelay1 : (idx == 1 ? thermalRelay2 : thermalRelay3);
                                                            if (thermalRelay != null)
                                                            {
                                                                reg(thermalRelay, "KH" + (idx + 1));
                                                            }
                                                        }
                                                        else if (thermalRelay != null)
                                                        {
                                                            reg(thermalRelay, "KH");
                                                        }
                                                        if (thermalRelay == null)
                                                        {
                                                            throw new NotSupportedException($"{edge?.Details?.CircuitForm?.CircuitFormType} {edge?.Details?.CircuitForm?.GetType()}");
                                                        }
                                                    }
                                                    else if (info.IsCPS())
                                                    {
                                                        var cpss = item.brInfos.Where(x => x.IsCPS()).ToList();
                                                        CPS cps = null, cps1 = null, cps2 = null, cps3 = null;
                                                        if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInBehindCircuit distributionMetering_MTInBehindCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInFrontCircuit distributionMetering_CTInFrontCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_CTInBehindCircuit distributionMetering_CTInBehindCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_MTInFrontCircuit distributionMetering_MTInFrontCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiCTCircuit distributionMetering_ShanghaiCTCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.DistributionMetering_ShanghaiMTCircuit distributionMetering_ShanghaiMTCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.FireEmergencyLighting fireEmergencyLighting)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                                        {
                                                            throw new NotSupportedException();
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
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                                        {
                                                            cps1 = twoSpeedMotor_CPSDYYCircuit.cps1;
                                                            cps2 = twoSpeedMotor_CPSDYYCircuit.cps2;
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                                        {
                                                            cps1 = twoSpeedMotor_CPSYYCircuit.cps1;
                                                            cps2 = twoSpeedMotor_CPSYYCircuit.cps2;
                                                        }
                                                        void reg(CPS cps, string templateStr)
                                                        {
                                                            var vm = new Project.Module.Component.ThPDSCPSModel(cps);
                                                            var m = glyphs.FirstOrDefault(x => x.Tag as string == templateStr);
                                                            if (m != null && vm != null)
                                                            {
                                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                            }
                                                            cb += () => { UpdatePropertyGrid(vm); };
                                                        }
                                                        if (cpss.Count > 1)
                                                        {
                                                            var idx = cpss.IndexOf(info);
                                                            cps = idx == 0 ? cps1 : (idx == 1 ? cps2 : cps3);
                                                            if (cps != null)
                                                            {
                                                                reg(cps, "CPS" + (idx + 1));
                                                            }
                                                        }
                                                        else if (cps != null)
                                                        {
                                                            reg(cps, "CPS");
                                                        }
                                                        else
                                                        {
                                                            throw new ArgumentNullException();
                                                        }
                                                        var cm = new ContextMenu();
                                                        cvs.ContextMenu = cm;
                                                        AddSecondaryCircuitMenus(cm);
                                                    }
                                                    else if (info.IsMeter())
                                                    {
                                                        Meter meter = null;
                                                        if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motorCircuit_DiscreteComponents)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsStarTriangleStartCircuit motor_DiscreteComponentsStarTriangleStartCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsDYYCircuit twoSpeedMotor_DiscreteComponentsDYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ContactorControlCircuit contactorControlCircuit)
                                                        {
                                                            throw new NotSupportedException();
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
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.LeakageCircuit leakageCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSCircuit motor_CPSCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_CPSStarTriangleStartCircuit motor_CPSStarTriangleStartCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.Motor_DiscreteComponentsCircuit motor_DiscreteComponentsCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.ThermalRelayProtectionCircuit thermalRelayProtectionCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSDYYCircuit twoSpeedMotor_CPSDYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                                        {
                                                            throw new NotSupportedException();
                                                        }
                                                        if (meter != null)
                                                        {
                                                            object vm = null;
                                                            if (meter is MeterTransformer meterTransformer)
                                                            {
                                                                var o = new Project.Module.Component.ThPDSMeterTransformerModel(meterTransformer); ;
                                                                vm = o;
                                                                {
                                                                    var m = glyphs.FirstOrDefault(x => x.Tag as string is "MT" or "CT");
                                                                    if (m != null)
                                                                    {
                                                                        var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(o.ContentMT)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                        m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                                    }
                                                                }
                                                            }
                                                            else if (meter is CurrentTransformer currentTransformer)
                                                            {
                                                                var o = new Project.Module.Component.ThPDSCurrentTransformerModel(currentTransformer);
                                                                vm = o;
                                                                {
                                                                    var m = glyphs.FirstOrDefault(x => x.Tag as string is "MT" or "CT");
                                                                    if (m != null)
                                                                    {
                                                                        var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(o.ContentCT)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                                        m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                                    }
                                                                }
                                                            }
                                                            cb += () => UpdatePropertyGrid(vm);
                                                            var cmenu = new ContextMenu();
                                                            cvs.ContextMenu = cmenu;
                                                            foreach (var menu in getMeterMenus(meter))
                                                            {
                                                                cmenu.Items.Add(menu);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            cb += () => UpdatePropertyGrid(null);
                                                            var cmenu = new ContextMenu();
                                                            cvs.ContextMenu = cmenu;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        cb += () => UpdatePropertyGrid(null);
                                                    }
                                                    cvs.MouseUp += (s, e) =>
                                                    {
                                                        void Update()
                                                        {
                                                            SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                            cb?.Invoke();
                                                        }
                                                        if (e.ChangedButton != MouseButton.Left)
                                                        {
                                                            if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                            return;
                                                        }
                                                        Update();
                                                        e.Handled = true;
                                                    };
                                                }
                                                cvs.ContextMenu ??= new();
                                                cvs.Cursor = Cursors.Hand;
                                                canvas.Children.Add(cvs);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var cvt = new NormalValueConverter(v => (bool)v ? .6 : 1.0);
                            var bd = new Binding() { Converter = cvt, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitLock)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                            foreach (var fe in CreateDrawingObjects(trans, item))
                            {
                                canvas.Children.Add(fe);
                                if (fe is Path or Glyphs or TextBlock)
                                {
                                    fe.SetBinding(UIElement.OpacityProperty, bd);
                                }
                            }
                        }
                        {
                            {
                                Conductor conductor = null, conductor1 = null, conductor2 = null;
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
                                    conductor1 = twoSpeedMotor_DiscreteComponentsDYYCircuit.conductor1;
                                    conductor2 = twoSpeedMotor_DiscreteComponentsDYYCircuit.conductor2;
                                }
                                else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                {
                                    conductor1 = twoSpeedMotor_DiscreteComponentsYYCircuit.conductor1;
                                    conductor2 = twoSpeedMotor_DiscreteComponentsYYCircuit.conductor2;
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
                                    conductor = fireEmergencyLighting.Conductor;
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
                                    conductor1 = twoSpeedMotor_CPSDYYCircuit.conductor1;
                                    conductor2 = twoSpeedMotor_CPSDYYCircuit.conductor2;
                                }
                                else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                {
                                    conductor1 = twoSpeedMotor_CPSYYCircuit.conductor1;
                                    conductor2 = twoSpeedMotor_CPSYYCircuit.conductor2;
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
                                        void Update()
                                        {
                                            UpdatePropertyGrid(vm);
                                            SetSel(gr.ToWpfRect());
                                        }
                                        if (e.ChangedButton != MouseButton.Left)
                                        {
                                            if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                            return;
                                        }
                                        Update();
                                        e.Handled = true;
                                    };
                                    cvs.ContextMenu = new();
                                }
                                {
                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor");
                                    if (m != null)
                                    {
                                        if (conductor != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSConductorModel(conductor);
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                            reg(r, vm);
                                        }
                                    }
                                }
                                {
                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor1");
                                    if (m != null)
                                    {
                                        if (conductor1 != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSConductorModel(conductor1);
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                            reg(r, vm);
                                        }
                                    }
                                }
                                {
                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor2");
                                    if (m != null)
                                    {
                                        if (conductor2 != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSConductorModel(conductor2);
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                            reg(r, vm);
                                        }
                                    }
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "回路编号");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitID)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功率");
                                if (m != null)
                                {
                                    m.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Power)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                    m.SetBinding(UIElement.VisibilityProperty, new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? Visibility.Collapsed : Visibility.Visible), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Power)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功率(低)");
                                if (m != null)
                                {
                                    m.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LowPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                    m.SetBinding(UIElement.VisibilityProperty, new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? Visibility.Collapsed : Visibility.Visible), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LowPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功率(高)");
                                if (m != null)
                                {
                                    m.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.HighPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                    m.SetBinding(UIElement.VisibilityProperty, new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? Visibility.Collapsed : Visibility.Visible), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.HighPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                }
                            }
                            {
                                foreach (var m in glyphs.Where(x => x.Tag as string == "相序"))
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.PhaseSequence)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                foreach (var m in glyphs.Where(x => x.Tag as string == "负载编号"))
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LoadId)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                foreach (var m in glyphs.Where(x => x.Tag as string == "功能用途"))
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Description)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                        }
                        {
                            var w1 = 485.0;
                            var w2 = 500.0;
                            var h = PDSItemInfo.GetBlockDefInfo(edgeName).Bounds.Height;
                            var cvs = new Canvas
                            {
                                Width = w2,
                                Height = h,
                                Background = Brushes.Transparent,
                            };
                            {
                                var menu = new ContextMenu();
                                cvs.ContextMenu = menu;
                                if (vertice.Details.CircuitFormType is not PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit)
                                {
                                    var mi = new MenuItem();
                                    menu.Items.Add(mi);
                                    mi.Header = "切换回路样式";
                                    var sw = new CircuitFormOutSwitcher(edge);
                                    var outTypes = sw.AvailableTypes();
                                    foreach (var outType in outTypes)
                                    {
                                        var m = new MenuItem();
                                        mi.Items.Add(m);
                                        m.Header = outType;
                                        m.Command = new RelayCommand(() =>
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
                                    m.Command = new RelayCommand(() =>
                                    {
                                        circuitVM.CircuitLock = !circuitVM.CircuitLock;
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    menu.Items.Add(m);
                                    m.Header = "分配负载";
                                    m.Command = new RelayCommand(() =>
                                    {
                                        var w = new Window() { Title = "分类负载", Width = 400, Height = 300, Topmost = true, WindowStartupLocation = WindowStartupLocation.CenterScreen, };
                                        var ctrl = new UserContorls.ThPDSLoadDistribution();
                                        ctrl.btnYes.Command = new RelayCommand(() =>
                                        {
                                            w.Close();
                                        });
                                        w.Content = ctrl;
                                        w.ShowDialog();
                                        UpdateCanvas();
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    menu.Items.Add(m);
                                    m.Header = "删除";
                                    m.Command = new RelayCommand(() =>
                                    {
                                        var r = MessageBox.Show("是否需要自动选型？\n注：已锁定的设备不会重新选型。", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                        if (r == MessageBoxResult.Cancel) return;
                                        ThPDSProjectGraphService.DeleteCircuit(graph, edge);
                                        UpdateCanvas();
                                    });
                                }
                                {
                                    var m = new MenuItem();
                                    menu.Items.Add(m);
                                    m.Header = "查看回路类型";
                                    m.Command = new RelayCommand(() =>
                                    {
                                        MessageBox.Show(edge.Details.CircuitForm?.CircuitFormType.ToString() ?? "");
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
                            cvs.MouseEnter += (s, e) => { cvs2.Background = LightBlue3; };
                            cvs.MouseLeave += (s, e) => { cvs2.Background = Brushes.Transparent; };
                            var rect = new Rect(Canvas.GetLeft(cvs2), Canvas.GetTop(cvs2), cvs2.Width, cvs2.Height);
                            cvs.MouseUp += (s, e) =>
                            {
                                void Update()
                                {
                                    SetSel(rect);
                                    UpdatePropertyGrid(circuitVM);
                                }
                                if (e.ChangedButton != MouseButton.Left)
                                {
                                    if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                    return;
                                }
                                Update();
                                e.Handled = true;
                            };
                            cvs.Cursor = Cursors.Hand;
                            canvas.Children.Add(cvs);
                            canvas.Children.Add(cvs2);
                        }
                    }
                }
                var circuitIDSortNames = "WPE、WP、WLE、WL、WS、WFEL".Split('、').ToList();
                IEnumerable<ThPDSProjectGraphEdge> GetSortedEdges(IEnumerable<ThPDSProjectGraphEdge> edges)
                {
                    return from edge in edges
                           where edge.Source == vertice
                           let circuitVM = new Project.Module.Component.ThPDSCircuitModel(edge)
                           let id = circuitVM.CircuitID ?? ""
                           orderby id.Length == 0 ? 1 : 0 ascending, circuitIDSortNames.IndexOf(circuitIDSortNames.FirstOrDefault(x => id.ToUpper().StartsWith(x))) + id ascending
                           select edge;
                }
                {
                    var edges = ThPDSProjectGraphService.GetOrdinaryCircuit(graph, vertice);
                    foreach (var edge in GetSortedEdges(edges))
                    {
                        DrawEdge(edge);
                    }
                }
                if (vertice.Details.CircuitFormType is PDS.Project.Module.Circuit.IncomingCircuit.CentralizedPowerCircuit centralizedPowerCircuit)
                {
                    var cvs = new Canvas() { Width = 100, Height = 320, Background = Brushes.Transparent, };
                    Canvas.SetLeft(cvs, 98);
                    canvas.Children.Add(cvs);
                    hoverDict[cvs] = cvs;
                    var cm = new ContextMenu();
                    var edges = ThPDSProjectGraphService.GetOrdinaryCircuit(graph, vertice);
                    if (edges.Count < 8)
                    {
                        cm.Items.Add(new MenuItem()
                        {
                            Header = "增加消防应急照明回路（WFEL）",
                            Command = new RelayCommand(() =>
                            {
                                ThPDSProjectGraphService.AddCircuit(graph, vertice, "消防应急照明回路（WFEL）");
                                UpdateCanvas();
                            }),
                        });
                    }
                    cvs.ContextMenu = cm;
                    cvs.MouseUp += (s, e) =>
                    {
                        void Update()
                        {
                            SetSel(new Rect(98, 0, cvs.Width, cvs.Height));
                            UpdatePropertyGrid(null);
                        }
                        if (e.ChangedButton != MouseButton.Left)
                        {
                            if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                            return;
                        }
                        Update();
                        e.Handled = true;
                    };
                }
                foreach (var kv in vertice.Details.MiniBusbars)
                {
                    var mbb = kv.Key;
                    var edges = GetSortedEdges(ThPDSProjectGraphService.GetSmallBusbarCircuit(graph, vertice, mbb));
                    var start = new Point(busStart.X + 205, -dy - 10);
                    var end = start.OffsetY(10);
                    busEnd = end;
                    dy -= end.Y - start.Y;
                    {
                        var item = PDSItemInfo.Create("分支小母排", new Point(start.X - 205, -start.Y));
                        var glyphs = new List<Glyphs>();
                        foreach (var fe in CreateDrawingObjects(trans, item))
                        {
                            if (fe is Glyphs g) glyphs.Add(g);
                            canvas.Children.Add(fe);
                        }
                        IEnumerable<FrameworkElement> CreateDrawingObjects(Transform trans, PDSItemInfo item, Brush strockBrush = null)
                        {
                            strockBrush ??= Brushes.Black;
                            foreach (var info in item.lineInfos)
                            {
                                var st = info.Line.StartPoint;
                                var ed = info.Line.EndPoint;
                                yield return CreateLine(trans, strockBrush, st, ed);
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
                                yield return path;
                            }
                            foreach (var info in item.circleInfos)
                            {
                                var path = new Path();
                                var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                                path.Stroke = strockBrush;
                                path.Data = geo;
                                path.RenderTransform = trans;
                                yield return path;
                            }
                            foreach (var info in item.textInfos)
                            {
                                var glyph = new Glyphs() { UnicodeString = FixString(info.Text), Tag = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                                if (info.Height > 0)
                                {
                                    glyph.FontRenderingEmSize = info.Height;
                                }
                                Canvas.SetLeft(glyph, info.BasePoint.X);
                                Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                                yield return glyph;
                            }
                            foreach (var info in item.brInfos)
                            {
                                if (info.IsBreaker())
                                {
                                    var breakers = item.brInfos.Where(x => x.IsBreaker()).ToList();
                                    Breaker breaker = null, breaker1 = null, breaker2 = null, breaker3 = null;
                                    breaker = mbb.Breaker;
                                    var blkVm = new ThPDSBlockViewModel();
                                    Project.Module.Component.ThPDSBreakerModel vm;
                                    void UpdateBreakerViewModel()
                                    {
                                        void reg(Breaker breaker, string templateStr)
                                        {
                                            vm = new(breaker);
                                            blkVm.UpdatePropertyGridCommand = new RelayCommand(() => { UpdatePropertyGrid(vm); });
                                            var m = glyphs.FirstOrDefault(x => x.Tag as string == templateStr);
                                            if (m != null && vm != null)
                                            {
                                                var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            }
                                        }
                                        var idx = breakers.IndexOf(info);
                                        if (breakers.Count > 1)
                                        {
                                            breaker = idx == 0 ? breaker1 : (idx == 1 ? breaker2 : breaker3);
                                            if (breaker != null)
                                            {
                                                reg(breaker, "CB" + (idx + 1));
                                            }
                                        }
                                        else if (breaker != null)
                                        {
                                            reg(breaker, "CB");
                                        }
                                        if (breaker == null)
                                        {
                                            throw new ArgumentNullException();
                                        }
                                        blkVm.BlockName = GetBreakerBlockName(breaker.ComponentType);
                                        blkVm.ContextMenuItems = GetBreakerMenus(breaker);
                                        blkVm.RaisePropertyChangedEvent();
                                    }
                                    var names = new string[] { "CircuitBreaker", "RCD" };
                                    string GetBreakerBlockName(ComponentType type)
                                    {
                                        if (type == ComponentType.CB)
                                        {
                                            return "CircuitBreaker";
                                        }
                                        else
                                        {
                                            return "RCD";
                                        }
                                    }
                                    foreach (var name in names)
                                    {
                                        foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(name, info.BasePoint), Brushes.Red))
                                        {
                                            el.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(blkVm.BlockName)) { Source = blkVm, Converter = new NormalValueConverter(v => (string)v == name ? Visibility.Visible : Visibility.Collapsed), });
                                            yield return el;
                                        }
                                    }
                                    IEnumerable<MenuItem> GetBreakerMenus(Breaker breaker)
                                    {
                                        var types = new ComponentType[] { ComponentType.CB, ComponentType.一体式RCD, ComponentType.组合式RCD, };
                                        foreach (var type in types)
                                        {
                                            if (breaker.ComponentType != type)
                                            {
                                                yield return new MenuItem()
                                                {
                                                    Header = "切换为" + ThCADExtension.ThEnumExtension.GetDescription(type),
                                                    Command = new RelayCommand(() =>
                                                    {
                                                        breaker.SetBreakerType(type);
                                                        UpdateBreakerViewModel();
                                                        blkVm.UpdatePropertyGrid();
                                                    }),
                                                };
                                            }
                                        }
                                    }
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
                                            var cm = new ContextMenu();
                                            cvs.ContextMenu = cm;
                                            cm.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(blkVm.ContextMenuItems)) { Source = blkVm, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                            cvs.MouseUp += (s, e) =>
                                            {
                                                void Update()
                                                {
                                                    SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                    blkVm.UpdatePropertyGrid();
                                                }
                                                if (e.ChangedButton != MouseButton.Left)
                                                {
                                                    if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                    return;
                                                }
                                                Update();
                                                e.Handled = true;
                                            };
                                            cvs.Cursor = Cursors.Hand;
                                            canvas.Children.Add(cvs);
                                        }
                                    }
                                    UpdateBreakerViewModel();
                                    continue;
                                }
                                {
                                    foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red))
                                    {
                                        yield return el;
                                    }
                                    if (info.IsMotor()) continue;
                                    {
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
                                                cvs.MouseEnter += (s, e) => { cvs.Background = LightBlue3; };
                                                cvs.MouseLeave += (s, e) => { cvs.Background = Brushes.Transparent; };
                                                cvs.MouseUp += (s, e) =>
                                                {
                                                    void Update()
                                                    {
                                                        SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                    }
                                                    if (e.ChangedButton != MouseButton.Left)
                                                    {
                                                        if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                        return;
                                                    }
                                                    Update();
                                                    e.Handled = true;
                                                };
                                                cvs.ContextMenu ??= new();
                                                cvs.Cursor = Cursors.Hand;
                                                canvas.Children.Add(cvs);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    var bsPt = start;
                    foreach (var edge in edges)
                    {
                        var name = "小母排分支";
                        var item = PDSItemInfo.Create(name, new Point(bsPt.X - 205, -bsPt.Y));
                        var circuitVM = new Project.Module.Component.ThPDSCircuitModel(edge);
                        var glyphs = new List<Glyphs>();
                        foreach (var fe in CreateDrawingObjects(trans, item))
                        {
                            if (fe is Glyphs g) glyphs.Add(g);
                            canvas.Children.Add(fe);
                        }
                        var _info = PDSItemInfo.GetBlockDefInfo(name);
                        bsPt = bsPt.OffsetXY(0, _info.Bounds.Height);
                        IEnumerable<FrameworkElement> CreateDrawingObjects(Transform trans, PDSItemInfo item, Brush strockBrush = null)
                        {
                            strockBrush ??= Brushes.Black;
                            foreach (var info in item.lineInfos)
                            {
                                var st = info.Line.StartPoint;
                                var ed = info.Line.EndPoint;
                                yield return CreateLine(trans, strockBrush, st, ed);
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
                                yield return path;
                            }
                            foreach (var info in item.circleInfos)
                            {
                                var path = new Path();
                                var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                                path.Stroke = strockBrush;
                                path.Data = geo;
                                path.RenderTransform = trans;
                                yield return path;
                            }
                            foreach (var info in item.textInfos)
                            {
                                var glyph = new Glyphs() { UnicodeString = FixString(info.Text), Tag = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                                if (info.Height > 0)
                                {
                                    glyph.FontRenderingEmSize = info.Height;
                                }
                                Canvas.SetLeft(glyph, info.BasePoint.X);
                                Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                                yield return glyph;
                            }
                            foreach (var info in item.brInfos)
                            {
                                {
                                    foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red))
                                    {
                                        yield return el;
                                    }
                                    if (info.IsMotor()) continue;
                                    if (info.IsBreaker())
                                    {
                                        var breakers = item.brInfos.Where(x => x.IsBreaker()).ToList();
                                        Breaker breaker = null, breaker1 = null, breaker2 = null, breaker3 = null;
                                        breaker = mbb.Breaker;
                                        var blkVm = new ThPDSBlockViewModel();
                                        Project.Module.Component.ThPDSBreakerModel vm;
                                        void UpdateBreakerViewModel()
                                        {
                                            void reg(Breaker breaker, string templateStr)
                                            {
                                                vm = new(breaker);
                                                blkVm.UpdatePropertyGridCommand = new RelayCommand(() => { UpdatePropertyGrid(vm); });
                                                var m = glyphs.FirstOrDefault(x => x.Tag as string == templateStr);
                                                if (m != null && vm != null)
                                                {
                                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                                }
                                            }
                                            var idx = breakers.IndexOf(info);
                                            if (breakers.Count > 1)
                                            {
                                                breaker = idx == 0 ? breaker1 : (idx == 1 ? breaker2 : breaker3);
                                                if (breaker != null)
                                                {
                                                    reg(breaker, "CB" + (idx + 1));
                                                }
                                            }
                                            else if (breaker != null)
                                            {
                                                reg(breaker, "CB");
                                            }
                                            if (breaker == null)
                                            {
                                                throw new ArgumentNullException();
                                            }
                                            blkVm.BlockName = GetBreakerBlockName(breaker.ComponentType);
                                            blkVm.ContextMenuItems = GetBreakerMenus(breaker);
                                            blkVm.RaisePropertyChangedEvent();
                                        }
                                        var names = new string[] { "CircuitBreaker", "RCD" };
                                        string GetBreakerBlockName(ComponentType type)
                                        {
                                            if (type == ComponentType.CB)
                                            {
                                                return "CircuitBreaker";
                                            }
                                            else
                                            {
                                                return "RCD";
                                            }
                                        }
                                        foreach (var name in names)
                                        {
                                            foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(name, info.BasePoint), Brushes.Red))
                                            {
                                                el.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(blkVm.BlockName)) { Source = blkVm, Converter = new NormalValueConverter(v => (string)v == name ? Visibility.Visible : Visibility.Collapsed), });
                                                yield return el;
                                            }
                                        }
                                        IEnumerable<MenuItem> GetBreakerMenus(Breaker breaker)
                                        {
                                            var types = new ComponentType[] { ComponentType.CB, ComponentType.一体式RCD, ComponentType.组合式RCD, };
                                            foreach (var type in types)
                                            {
                                                if (breaker.ComponentType != type)
                                                {
                                                    yield return new MenuItem()
                                                    {
                                                        Header = "切换为" + ThCADExtension.ThEnumExtension.GetDescription(type),
                                                        Command = new RelayCommand(() =>
                                                        {
                                                            breaker.SetBreakerType(type);
                                                            UpdateBreakerViewModel();
                                                            blkVm.UpdatePropertyGrid();
                                                        }),
                                                    };
                                                }
                                            }
                                        }
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
                                                var cm = new ContextMenu();
                                                cvs.ContextMenu = cm;
                                                cm.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(blkVm.ContextMenuItems)) { Source = blkVm, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                                cvs.MouseUp += (s, e) =>
                                                {
                                                    void Update()
                                                    {
                                                        SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                        blkVm.UpdatePropertyGrid();
                                                    }
                                                    if (e.ChangedButton != MouseButton.Left)
                                                    {
                                                        if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                        return;
                                                    }
                                                    Update();
                                                    e.Handled = true;
                                                };
                                                cvs.Cursor = Cursors.Hand;
                                                canvas.Children.Add(cvs);
                                            }
                                        }
                                        UpdateBreakerViewModel();
                                        continue;
                                    }
                                    {
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
                                                cvs.MouseEnter += (s, e) => { cvs.Background = LightBlue3; };
                                                cvs.MouseLeave += (s, e) => { cvs.Background = Brushes.Transparent; };
                                                cvs.MouseUp += (s, e) =>
                                                {
                                                    void Update()
                                                    {
                                                        SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                    }
                                                    if (e.ChangedButton != MouseButton.Left)
                                                    {
                                                        if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                        return;
                                                    }
                                                    Update();
                                                    e.Handled = true;
                                                };
                                                cvs.ContextMenu ??= new();
                                                cvs.Cursor = Cursors.Hand;
                                                canvas.Children.Add(cvs);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        {
                            {
                                Conductor conductor = null, conductor1 = null, conductor2 = null;
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
                                    conductor1 = twoSpeedMotor_DiscreteComponentsDYYCircuit.conductor1;
                                    conductor2 = twoSpeedMotor_DiscreteComponentsDYYCircuit.conductor2;
                                }
                                else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_DiscreteComponentsYYCircuit twoSpeedMotor_DiscreteComponentsYYCircuit)
                                {
                                    conductor1 = twoSpeedMotor_DiscreteComponentsYYCircuit.conductor1;
                                    conductor2 = twoSpeedMotor_DiscreteComponentsYYCircuit.conductor2;
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
                                    throw new NotSupportedException();
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
                                    throw new NotSupportedException();
                                }
                                else if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.TwoSpeedMotor_CPSYYCircuit twoSpeedMotor_CPSYYCircuit)
                                {
                                    throw new NotSupportedException();
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
                                        void Update()
                                        {
                                            UpdatePropertyGrid(vm);
                                            SetSel(gr.ToWpfRect());
                                        }
                                        if (e.ChangedButton != MouseButton.Left)
                                        {
                                            if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                            return;
                                        }
                                        Update();
                                        e.Handled = true;
                                    };
                                    cvs.ContextMenu = new();
                                }
                                {
                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor");
                                    if (m != null)
                                    {
                                        if (conductor != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSConductorModel(conductor);
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                            reg(r, vm);
                                        }
                                    }
                                }
                                {
                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor1");
                                    if (m != null)
                                    {
                                        if (conductor1 != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSConductorModel(conductor1);
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                            reg(r, vm);
                                        }
                                    }
                                }
                                {
                                    var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor2");
                                    if (m != null)
                                    {
                                        if (conductor2 != null)
                                        {
                                            var vm = new Project.Module.Component.ThPDSConductorModel(conductor2);
                                            var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                            m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                            var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                            reg(r, vm);
                                        }
                                    }
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "回路编号");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.CircuitID)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功率");
                                if (m != null)
                                {
                                    m.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Power)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                    m.SetBinding(UIElement.VisibilityProperty, new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? Visibility.Collapsed : Visibility.Visible), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Power)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "相序");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.PhaseSequence)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "负载编号");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LoadId)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功能用途");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.Description)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功率(低)");
                                if (m != null)
                                {
                                    m.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LowPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                    m.SetBinding(UIElement.VisibilityProperty, new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? Visibility.Collapsed : Visibility.Visible), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.LowPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "功率(高)");
                                if (m != null)
                                {
                                    m.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Converter = glyphsUnicodeStrinConverter, Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.HighPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                    m.SetBinding(UIElement.VisibilityProperty, new Binding() { Converter = new NormalValueConverter(v => Convert.ToDouble(v) == 0 ? Visibility.Collapsed : Visibility.Visible), Source = circuitVM, Path = new PropertyPath(nameof(circuitVM.HighPower)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, });
                                }
                            }
                        }
                        var w1 = 485.0;
                        var w2 = 500.0;
                        var h = PDSItemInfo.GetBlockDefInfo(name).Bounds.Height;
                        var cvs = new Canvas
                        {
                            Width = w2,
                            Height = h,
                            Background = Brushes.Transparent,
                        };
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
                        cvs.MouseEnter += (s, e) => { cvs2.Background = LightBlue3; };
                        cvs.MouseLeave += (s, e) => { cvs2.Background = Brushes.Transparent; };
                        {
                            var cmenu = new ContextMenu();
                            cvs.ContextMenu = cmenu;
                            {
                                var mi = new MenuItem();
                                cmenu.Items.Add(mi);
                                mi.Header = "删除";
                                mi.Command = new RelayCommand(() =>
                                {
                                    UpdateCanvas();
                                });
                            }
                        }
                        var rect = new Rect(Canvas.GetLeft(cvs2), Canvas.GetTop(cvs2), cvs2.Width, cvs2.Height);
                        cvs.MouseUp += (s, e) =>
                        {
                            void Update()
                            {
                                SetSel(rect);
                                UpdatePropertyGrid(circuitVM);
                            }
                            if (e.ChangedButton != MouseButton.Left)
                            {
                                if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                return;
                            }
                            Update();
                            e.Handled = true;
                        };
                        cvs.Cursor = Cursors.Hand;
                        canvas.Children.Add(cvs);
                        canvas.Children.Add(cvs2);
                    }
                    end = bsPt;
                    dy -= end.Y - start.Y;
                    busEnd = end.OffsetY(20);
                    {
                        var width = 20.0;
                        var thickness = 5.0;
                        var offsetY = 20.0;
                        var ln = CreateLine(null, Brushes.Black, start.OffsetY(offsetY), end.OffsetY(offsetY));
                        ln.StrokeThickness = thickness;
                        canvas.Children.Add(ln);
                        var cvs = new Canvas
                        {
                            Width = width,
                            Height = Math.Abs(start.Y - end.Y),
                            Background = Brushes.Transparent,
                        };
                        Canvas.SetLeft(cvs, start.X - (width - thickness / 2) / 2);
                        Canvas.SetTop(cvs, start.Y + offsetY);
                        cvs.MouseEnter += (s, e) => { cvs.Background = LightBlue3; };
                        cvs.MouseLeave += (s, e) => { cvs.Background = Brushes.Transparent; };
                        cvs.MouseUp += (s, e) =>
                        {
                            void Update()
                            {
                                SetSel(new Rect(Canvas.GetLeft(cvs), Canvas.GetTop(cvs), cvs.Width, cvs.Height));
                                var vm = new PDS.UI.Project.Module.ThPDSMiniBusbarModel(mbb);
                                UpdatePropertyGrid(vm);
                            }
                            if (e.ChangedButton != MouseButton.Left)
                            {
                                if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                return;
                            }
                            Update();
                            e.Handled = true;
                        };
                        var menu = new ContextMenu();
                        cvs.ContextMenu = menu;
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "添加已有回路";
                            mi.Command = new RelayCommand(() =>
                            {
                                var node = new ThPDSCircuitGraphTreeModel() { DataList = new(), };
                                var edges = ThPDSProjectGraphService.GetCircuit(graph, vertice).Where(x => x.Details.CircuitForm.CircuitFormType == CircuitFormOutType.常规).ToList();
                                for (int i = 0; i < edges.Count; i++)
                                {
                                    var edge = edges[i];
                                    node.DataList.Add(new ThPDSCircuitGraphTreeModel() { Id = i, Name = edge.Circuit.ID.CircuitID.LastOrDefault(), });
                                }
                                var w = new UserContorls.ThPDSAssignCircuit2SmallBusbar();
                                w.ctl.DataContext = node;
                                var r = w.ShowDialog();
                                if (r == true)
                                {
                                    foreach (ThPDSCircuitGraphTreeModel m in w.ctl.SelectedItems)
                                    {
                                        ThPDSProjectGraphService.AssignCircuit2SmallBusbar(vertice, mbb, edges[m.Id]);
                                    }
                                    UpdateCanvas();
                                }
                            });
                        }
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "新建分支回路";
                            mi.Command = new RelayCommand(() =>
                            {
                                ThPDSProjectGraphService.SmallBusbarAddCircuit(graph, vertice, mbb);
                                UpdateCanvas();
                            });
                        }
                        cvs.Cursor = Cursors.Hand;
                        canvas.Children.Add(cvs);
                    }
                }
                foreach (var kv in vertice.Details.SecondaryCircuits)
                {
                    var sc = kv.Key;
                    var scVm = new PDS.UI.Project.Module.ThPDSSecondaryCircuitModel(sc);
                    var edges = ThPDSProjectGraphService.GetControlCircuit(graph, vertice, sc);
                    foreach (var edge in GetSortedEdges(edges))
                    {
                        DrawEdge(edge);
                    }
                    var start = new Point(busStart.X + 205, -dy - 10);
                    var end = start.OffsetY(40);
                    busEnd = end;
                    dy -= end.Y - start.Y;
                    var pt = new Point(start.X - 205, start.Y);
                    var cvt1 = new DoubleCollectionConverter();
                    var dashArrBORDERBorder = (DoubleCollection)cvt1.ConvertFrom("12.7, 6.35, 12.7, 6.35, 1, 6.35 ");
                    var dashArrBORDER2Border5x = (DoubleCollection)cvt1.ConvertFrom("6.35, 3.175, 6.35, 3.175, 1, 3.175 ");
                    var dashArrBORDERX2Border2x = (DoubleCollection)cvt1.ConvertFrom("25.4, 12.7, 25.4, 12.7, 1, 12.7 ");
                    var currentDashArr = new DoubleCollection(new double[] { 12.7, 6.35, 12.7, 6.35, 1, 6.35 }.Select(x => x * .5));
                    var hasCPS = edges.Any(x => x.Details.CircuitForm.CircuitFormType.GetDescription().Contains("CPS"));
                    {
                        var st = new Point(pt.X + (hasCPS ? 46 : 144), pt.Y + 10 - 38 * (edges.Count - 1));
                        var ed = st;
                        ed.Y = pt.Y + 40;
                        var ln = CreateLine(null, Brushes.Black, st, ed);
                        ln.StrokeDashCap = PenLineCap.Square;
                        ln.StrokeDashArray = currentDashArr;
                        canvas.Children.Add(ln);
                    }
                    pt.Y = -pt.Y;
                    var item = PDSItemInfo.Create(hasCPS ? "控制（从属CPS）" : "控制（从属接触器）", pt);
                    {
                        var glyphs = new List<Glyphs>();
                        foreach (var fe in CreateDrawingObjects(trans, item, false))
                        {
                            if (fe is Glyphs g) glyphs.Add(g);
                            canvas.Children.Add(fe);
                        }
                        {
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
                                    void Update()
                                    {
                                        UpdatePropertyGrid(vm);
                                        SetSel(gr.ToWpfRect());
                                    }
                                    if (e.ChangedButton != MouseButton.Left)
                                    {
                                        if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                        return;
                                    }
                                    Update();
                                    e.Handled = true;
                                };
                                cvs.ContextMenu = new();
                            }
                            {
                                Conductor conductor = sc.Conductor;
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "Conductor");
                                if (m != null)
                                {
                                    if (conductor != null)
                                    {
                                        var vm = new Project.Module.Component.ThPDSConductorModel(conductor);
                                        var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = vm, Path = new PropertyPath(nameof(vm.Content)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                        m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                        var r = new Rect(Canvas.GetLeft(m), Canvas.GetTop(m), w, m.FontRenderingEmSize);
                                        reg(r, vm);
                                    }
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "回路编号");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = scVm, Path = new PropertyPath(nameof(scVm.CircuitID)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                            {
                                var m = glyphs.FirstOrDefault(x => x.Tag as string == "控制回路");
                                if (m != null)
                                {
                                    var bd = new Binding() { Converter = glyphsUnicodeStrinConverter, Source = scVm, Path = new PropertyPath(nameof(scVm.CircuitDescription)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                                    m.SetBinding(Glyphs.UnicodeStringProperty, bd);
                                }
                            }
                        }
                        {
                            var w1 = 485.0;
                            var w2 = 500.0;
                            var h = 40.0;
                            var cvs = new Canvas
                            {
                                Width = w2,
                                Height = h,
                                Background = Brushes.Transparent,
                            };
                            pt.Y = -pt.Y;
                            var offsetY = -20.0;
                            Canvas.SetLeft(cvs, pt.X + w1);
                            Canvas.SetTop(cvs, pt.Y - offsetY);
                            var cvs2 = new Canvas
                            {
                                Width = w1 + w2,
                                Height = h,
                                IsHitTestVisible = false,
                            };
                            Canvas.SetLeft(cvs2, pt.X);
                            Canvas.SetTop(cvs2, pt.Y - offsetY);
                            cvs.MouseEnter += (s, e) => { cvs2.Background = LightBlue3; };
                            cvs.MouseLeave += (s, e) => { cvs2.Background = Brushes.Transparent; };
                            var rect = new Rect(Canvas.GetLeft(cvs2), Canvas.GetTop(cvs2), cvs2.Width, cvs2.Height);
                            cvs.MouseUp += (s, e) =>
                            {
                                void Update()
                                {
                                    SetSel(rect);
                                    UpdatePropertyGrid(scVm);
                                }
                                if (e.ChangedButton != MouseButton.Left)
                                {
                                    if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                    return;
                                }
                                Update();
                                e.Handled = true;
                            };
                            cvs.Cursor = Cursors.Hand;
                            canvas.Children.Add(cvs);
                            canvas.Children.Add(cvs2);
                        }
                        IEnumerable<FrameworkElement> CreateDrawingObjects(Transform trans, PDSItemInfo item, bool isBlock, Brush strockBrush = null)
                        {
                            strockBrush ??= Brushes.Black;
                            foreach (var info in item.lineInfos)
                            {
                                var st = info.Line.StartPoint;
                                var ed = info.Line.EndPoint;
                                var ln = CreateLine(trans, strockBrush, st, ed);
                                if (!isBlock) ln.StrokeDashArray = currentDashArr;
                                yield return ln;
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
                                yield return path;
                            }
                            foreach (var info in item.circleInfos)
                            {
                                var path = new Path();
                                var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                                path.Stroke = strockBrush;
                                path.Data = geo;
                                path.RenderTransform = trans;
                                yield return path;
                            }
                            foreach (var info in item.textInfos)
                            {
                                var glyph = new Glyphs() { UnicodeString = FixString(info.Text), Tag = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                                if (info.Height > 0)
                                {
                                    glyph.FontRenderingEmSize = info.Height;
                                }
                                Canvas.SetLeft(glyph, info.BasePoint.X);
                                Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                                yield return glyph;
                            }
                            foreach (var info in item.brInfos)
                            {
                                {
                                    foreach (var el in CreateDrawingObjects(trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), true, Brushes.Red))
                                    {
                                        yield return el;
                                    }
                                    if (info.IsMotor()) continue;
                                    {
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
                                                cvs.MouseEnter += (s, e) => { cvs.Background = LightBlue3; };
                                                cvs.MouseLeave += (s, e) => { cvs.Background = Brushes.Transparent; };
                                                cvs.MouseUp += (s, e) =>
                                                {
                                                    void Update()
                                                    {
                                                        SetSel(new Rect(r.X, -r.Y - r.Height, cvs.Width, cvs.Height));
                                                    }
                                                    if (e.ChangedButton != MouseButton.Left)
                                                    {
                                                        if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                                                        return;
                                                    }
                                                    Update();
                                                    e.Handled = true;
                                                };
                                                cvs.ContextMenu ??= new();
                                                cvs.Cursor = Cursors.Hand;
                                                canvas.Children.Add(cvs);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                void SetSel(Rect r)
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
                SetSel(default);
                {
                    {
                        {
                            var name = "SPD附件";
                            var item = PDSItemInfo.Create(name, new Point(busStart.X, dy + 20));
                            if (item is null) throw new NotSupportedException(name);
                            foreach (var fe in CreateDrawingObjects(canvas, trans, item))
                            {
                                fe.SetBinding(UIElement.VisibilityProperty, new Binding() { Source = config.Current, Path = new PropertyPath(nameof(config.Current.SurgeProtection)), Converter = new EqualsThenNotVisibeConverter(PDS.Project.Module.SurgeProtectionDeviceType.None), });
                                if (fe is Glyphs g)
                                {
                                    g.SetBinding(Glyphs.UnicodeStringProperty, new Binding() { Source = config.Current, Converter = glyphsUnicodeStrinConverter, Path = new PropertyPath(nameof(config.Current.SurgeProtection)), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                                }
                                canvas.Children.Add(fe);
                            }
                            var _info = PDSItemInfo.GetBlockDefInfo(name);
                            if (_info != null)
                            {
                                dy -= _info.Bounds.Height;
                                busEnd = busEnd.OffsetXY(0, _info.Bounds.Height);
                                insertGaps.Add(new GLineSegment(busEnd, busEnd.OffsetXY(500, 0)));
                            }
                            IEnumerable<FrameworkElement> CreateDrawingObjects(Canvas canvas, Transform trans, PDSItemInfo item, Brush strockBrush = null)
                            {
                                strockBrush ??= Brushes.Black;
                                foreach (var info in item.lineInfos)
                                {
                                    var st = info.Line.StartPoint;
                                    var ed = info.Line.EndPoint;
                                    yield return CreateLine(trans, strockBrush, st, ed);
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
                                    yield return path;
                                }
                                foreach (var info in item.circleInfos)
                                {
                                    var path = new Path();
                                    var geo = new EllipseGeometry(info.Circle.Center, info.Circle.Radius, info.Circle.Radius);
                                    path.Stroke = strockBrush;
                                    path.Data = geo;
                                    path.RenderTransform = trans;
                                    yield return path;
                                }
                                foreach (var info in item.textInfos)
                                {
                                    var glyph = new Glyphs() { UnicodeString = FixString(info.Text), Tag = info.Text, FontRenderingEmSize = 13, Fill = strockBrush, FontUri = fontUri, };
                                    if (info.Height > 0)
                                    {
                                        glyph.FontRenderingEmSize = info.Height;
                                    }
                                    Canvas.SetLeft(glyph, info.BasePoint.X);
                                    Canvas.SetTop(glyph, -info.BasePoint.Y - glyph.FontRenderingEmSize);
                                    yield return glyph;
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
                                    yield return path;
                                }
                                foreach (var info in item.brInfos)
                                {
                                    foreach (var r in CreateDrawingObjects(canvas, trans, PDSItemInfo.Create(info.BlockName, info.BasePoint), Brushes.Red))
                                    {
                                        yield return r;
                                    }
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
                        config.Current.BusLength = len;
                        var path = CreateLine(null, Brushes.Black, busStart, busEnd);
                        canvas.Children.Add(path);
                        path.StrokeThickness = thickness;
                        BindingOperations.SetBinding(path, Path.DataProperty, new Binding() { Source = config.Current, Path = new PropertyPath(nameof(config.Current.BusLength)), Converter = new NormalValueConverter(v => new LineGeometry(busStart, busStart.OffsetXY(0, (double)v))), });
                    }
                    var cvs = new Canvas
                    {
                        Width = width,
                        Background = Brushes.Transparent,
                    };
                    BindingOperations.SetBinding(cvs, FrameworkElement.HeightProperty, new Binding() { Source = config.Current, Path = new PropertyPath(nameof(config.Current.BusLength)) });
                    Canvas.SetLeft(cvs, busStart.X - (width - thickness / 2) / 2);
                    Canvas.SetTop(cvs, busStart.Y);
                    hoverDict[cvs] = cvs;
                    cvs.MouseUp += (s, e) =>
                    {
                        void Update()
                        {
                            SetSel(new Rect(Canvas.GetLeft(cvs), Canvas.GetTop(cvs), cvs.Width, cvs.Height));
                            UpdatePropertyGrid(boxVM);
                        }
                        if (e.ChangedButton != MouseButton.Left)
                        {
                            if (e.ChangedButton == MouseButton.Right && e.OriginalSource == cvs) Update();
                            return;
                        }
                        Update();
                        e.Handled = true;
                    };
                    {
                        var menu = new ContextMenu();
                        cvs.ContextMenu = menu;
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "新建回路";
                            var outTypes = ThPDSProjectGraphService.AvailableTypes();
                            foreach (var outType in outTypes)
                            {
                                var m = new MenuItem();
                                mi.Items.Add(m);
                                m.Header = outType;
                                m.Command = new RelayCommand(() =>
                                {
                                    ThPDSProjectGraphService.AddCircuit(graph, vertice, outType);
                                    UpdateCanvas();
                                });
                            }
                            {
                                var m = new MenuItem();
                                mi.Items.Add(m);
                                m.Header = "分支母排";
                                m.Command = new RelayCommand(() =>
                                {
                                    ThPDSProjectGraphService.AddSmallBusbar(graph, vertice);
                                    UpdateCanvas();
                                });
                            }
                        }
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "切换进线形式";
                            var sw = new CircuitFormInSwitcher(vertice);
                            var inTypes = sw.AvailableTypes();
                            foreach (var inType in inTypes)
                            {
                                var m = new MenuItem();
                                mi.Items.Add(m);
                                m.Header = inType;
                                m.Command = new RelayCommand(() =>
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
                        if (e.ChangedButton != MouseButton.Left)
                        {
                            if (e.ChangedButton == MouseButton.Right && e.OriginalSource == canvas) SetSel(default);
                            return;
                        }
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
                                var drawingCollection = drawingGroup?.Children;
                                if (drawingCollection is null) return;
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
                            SetSel(default);
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
            }
            UpdateCanvas();
        }
        private static Path CreateLine(Transform trans, Brush strockBrush, Point st, Point ed)
        {
            var geo = new LineGeometry(st, ed);
            var path = new Path
            {
                Stroke = strockBrush,
                Fill = Brushes.Black,
                Data = geo,
                RenderTransform = trans,
            };
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
}