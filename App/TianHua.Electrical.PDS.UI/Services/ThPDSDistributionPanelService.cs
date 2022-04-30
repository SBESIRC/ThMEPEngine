using System;
using System.Linq;
using System.Windows;
using ThCADExtension;
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
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Helpers;
using TianHua.Electrical.PDS.UI.Services;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.UI.Converters;
using TianHua.Electrical.PDS.UI.Project.Module;
using TianHua.Electrical.PDS.UI.Project.Module.Component;
using Microsoft.Toolkit.Mvvm.Input;

namespace TianHua.Electrical.PDS.UI.WpfServices
{
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
        ContextMenu treeCMenu;
        QuikGraph.BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph => Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
        public void Init(UserContorls.ThPDSDistributionPanel panel)
        {
            if (graph is null) return;
            var vertices = graph.Vertices.Select(x => new ThPDSVertex { Detail = x.Details, Type = x.Type }).ToList();
            var srcLst = graph.Edges.Select(x => graph.Vertices.ToList().IndexOf(x.Source)).ToList();
            var dstLst = graph.Edges.Select(x => graph.Vertices.ToList().IndexOf(x.Target)).ToList();
            var circuitLst = graph.Edges.Select(x => x.Circuit).ToList();
            var details = graph.Edges.Select(x => x.Details).ToList();
            var ctx = new ThPDSContext() { Vertices = vertices, Souces = srcLst, Targets = dstLst, Circuits = circuitLst, Details = details };
            var tv = panel.tv;
            treeCMenu ??= tv.ContextMenu;
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
            var selectAllCmd = new RelayCommand(() =>
           {
               if (tv.DataContext is not ThPDSCircuitGraphTreeModel tree) return;
               void dfs(ThPDSCircuitGraphTreeModel node)
               {
                   node.IsChecked = true;
                   foreach (var n in node.DataList)
                   {
                       dfs(n);
                   }
               }
               dfs(tree);
           });
            var batchGenCmd = new RelayCommand(() =>
            {
                UI.ElecSandboxUI.TryGetCurrentWindow()?.Hide();
                try
                {
                    if (tv.DataContext is not ThPDSCircuitGraphTreeModel tree) return;
                    var vertices = graph.Vertices.ToList();
                    var nodes = new List<PDS.Project.Module.ThPDSProjectGraphNode>();
                    void dfs(ThPDSCircuitGraphTreeModel node)
                    {
                        if (node.IsChecked == true)
                        {
                            var nd = vertices[node.Id];
                            if (!nodes.Any(x => new ThPDSDistributionBoxModel(x).ID == new ThPDSDistributionBoxModel(nd).ID))
                            {
                                nodes.Add(nd);
                            }
                        }
                        foreach (var n in node.DataList) dfs(n);
                    }
                    dfs(tree);
                    if (nodes.Count == 0) return;
                    var drawCmd = new Command.ThPDSSystemDiagramCommand(graph, nodes);
                    drawCmd.Execute();
                    AcHelper.Active.Editor.Regen();
                }
                finally
                {
                    UI.ElecSandboxUI.TryGetCurrentWindow()?.Show();
                }
            });
            Action createBackupCircuit = null;
            var createBackupCircuitCmd = new RelayCommand(() =>
            {
                createBackupCircuit?.Invoke();
            });
            var treeCmenu = new ContextMenu()
            {
                ItemsSource = new MenuItem[] {
                    new MenuItem()
                    {
                        Header="批量生成",
                        Command=batchGenCmd,
                    },
                    new MenuItem()
                    {
                        Header = "全部勾选",
                        Command = selectAllCmd,
                    },
                      new MenuItem()
                    {
                        Header = "新建备用回路",
                        Command = createBackupCircuitCmd,
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
                var h = "平衡相序";
                if (!treeCMenu.Items.SourceCollection.OfType<MenuItem>().Any(x => x.Header as string == h))
                {
                    treeCMenu.Items.Add(new MenuItem()
                    {
                        Header = h,
                        Command = new RelayCommand(() =>
                        {
                            ThPDSProjectGraphService.BalancedPhaseSequence(graph, GetCurrentVertice());
                        }),
                    });
                }
            }
            {
                var h = "全部勾选";
                if (!treeCMenu.Items.SourceCollection.OfType<MenuItem>().Any(x => x.Header as string == h))
                {
                    treeCMenu.Items.Add(new MenuItem()
                    {
                        Header = h,
                        Command = selectAllCmd,
                    });
                }
            }
            {
                var h = "新建备用回路";
                if (!treeCMenu.Items.SourceCollection.OfType<MenuItem>().Any(x => x.Header as string == h))
                {
                    treeCMenu.Items.Add(new MenuItem()
                    {
                        Header = h,
                        Command = createBackupCircuitCmd,
                    });
                }
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
                        cm.Items.Add(new MenuItem()
                        {
                            Header = "全部勾选",
                            Command = selectAllCmd,
                        });
                        cm.Items.Add(new MenuItem()
                        {
                            Header = "新建备用回路",
                            Command = createBackupCircuitCmd,
                        });
                    }
                    else
                    {
                        tv.ContextMenu = treeCMenu;
                    }
                    var boxVM = new ThPDSDistributionBoxModel(vertice);
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
                if (vm is ThPDSBreakerModel breaker)
                {
                    if (breaker.ComponentType == ComponentType.CB)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSBreakerModel>("RCDType", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSBreakerModel>("ResidualCurrent", false);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSBreakerModel>("RCDType", true);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSBreakerModel>("ResidualCurrent", true);
                    }
                    if (breaker.ComponentType == ComponentType.组合式RCD)
                    {
                        ThPDSPropertyDescriptorHelper.SetReadOnlyProperty<ThPDSBreakerModel>("Appendix", true);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetReadOnlyProperty<ThPDSBreakerModel>("Appendix", false);
                    }
                }
                if (vm is ThPDSCircuitModel circuit)
                {
                    if (circuit.IsDualPower)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSCircuitModel>("Power", false);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSCircuitModel>("LowPower", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSCircuitModel>("HighPower", false);
                    }
                    if (circuit.LoadType == PDSNodeType.Unkown)
                    {
                        ThPDSPropertyDescriptorHelper.SetReadOnlyProperty<ThPDSCircuitModel>("CircuitType", false);
                    }
                    else
                    {
                        ThPDSPropertyDescriptorHelper.SetReadOnlyProperty<ThPDSCircuitModel>("CircuitType", true);
                    }
                }
                if (vm is ThPDSConductorModel conductor)
                {
                    if (conductor.ComponentType == ComponentType.Conductor)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSConductorModel>("ConductorCount", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSConductorModel>("ControlConductorCrossSectionalArea", false);
                    }
                    else if (conductor.ComponentType == ComponentType.ControlConductor)
                    {
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSConductorModel>("NumberOfPhaseWire", false);
                        ThPDSPropertyDescriptorHelper.SetBrowsableProperty<ThPDSConductorModel>("ConductorCrossSectionalArea", false);
                    }
                }
                if (vm is ThPDSCircuitModel circuitVM)
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
            createBackupCircuit = () =>
            {
                var vertice = GetCurrentVertice();
                if (vertice is null) return;
                ThPDSProjectGraphService.CreatBackupCircuit(graph, vertice);
                UpdateCanvas();
            };
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
                var boxVM = new ThPDSDistributionBoxModel(vertice);
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
                                            ThPDSProjectGraphService.InsertUndervoltageProtector(graph, vertice);
                                            UpdateCanvas();
                                        }),
                                    };
                                    yield return new MenuItem()
                                    {
                                        Header = "增加电能表",
                                        Command = new RelayCommand(() =>
                                        {
                                            ThPDSProjectGraphService.InsertEnergyMeter(graph, vertice);
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
                                            ThPDSProjectGraphService.RemoveUndervoltageProtector(vertice);
                                            ThPDSProjectGraphService.RemoveEnergyMeter(vertice);
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
                                    void UpdateBreakerViewModel()
                                    {
                                        void reg(Breaker breaker, string templateStr)
                                        {
                                            var vm = new ThPDSBreakerModel(breaker);
                                            vm.PropertyChanged += (s, e) =>
                                            {
                                                if (e.PropertyName == nameof(ThPDSBreakerModel.RatedCurrent))
                                                {
                                                    ThPDSProjectGraphService.UpdateWithEdge(edge);
                                                }
                                            };
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
                                                var m = new MenuItem() { Header = "新建控制回路", };
                                                cm.Items.Add(m);
                                                foreach (var scinfo in ThPDSProjectGraphService.GetSecondaryCircuitInfos(edge))
                                                {
                                                    m.Items.Add(new MenuItem()
                                                    {
                                                        Header = scinfo.Description,
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
                                                            var vm = new ThPDSCPSModel(cps);
                                                            vm.PropertyChanged += (s, e) =>
                                                            {
                                                                if (e.PropertyName == nameof(ThPDSCPSModel.RatedCurrent))
                                                                {
                                                                    ThPDSProjectGraphService.UpdateWithEdge(edge);
                                                                }
                                                            };
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
                                        else
                                        {
                                            m.Visibility = Visibility.Collapsed;
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
                                        else
                                        {
                                            m.Visibility = Visibility.Collapsed;
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
                                        else
                                        {
                                            m.Visibility = Visibility.Collapsed;
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
                                    var sw = ThPDSProjectGraphService.GetCircuitFormOutSwitcher(edge);
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
                                        var tree = new ThPDSCircuitGraphTreeModel() { DataList = new(), };
                                        void Update(bool filt)
                                        {
                                            tree.DataList.Clear();
                                            foreach (var node in ThPDSProjectGraphService.GetUndistributeLoad(graph, filt))
                                            {
                                                tree.DataList.Add(new ThPDSCircuitGraphTreeModel() { Name = node.Load.ID.LoadID, Tag = node });
                                            }
                                            ctrl.treeView.DataContext = tree;
                                        }
                                        ctrl.cbxFilt.Checked += (s, e) =>
                                        {
                                            Update(true);
                                        };
                                        ctrl.cbxFilt.Unchecked += (s, e) =>
                                        {
                                            Update(false);
                                        };
                                        Update(ctrl.cbxFilt.IsChecked.Value);
                                        var ok = false;
                                        ctrl.btnYes.Command = new RelayCommand(() =>
                                        {
                                            ok = true;
                                            w.Close();
                                        });
                                        w.Content = ctrl;
                                        w.ShowDialog();
                                        if (ok)
                                        {
                                            foreach (var node in tree.DataList.Where(x => x.IsChecked == true).Select(x => x.Tag).Cast<ThPDSProjectGraphNode>())
                                            {
                                                ThPDSProjectGraphService.DistributeLoad(graph, node, vertice);
                                            }
                                            UpdateCanvas();
                                        }
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
                IEnumerable<SecondaryCircuit> GetSortedSecondaryCircuits(IEnumerable<SecondaryCircuit> scs)
                {
                    return from sc in scs
                           let scVm = new ThPDSSecondaryCircuitModel(sc)
                           let id = scVm.CircuitID ?? ""
                           orderby id.Length == 0 ? 1 : 0 ascending, circuitIDSortNames.IndexOf(circuitIDSortNames.FirstOrDefault(x => id.ToUpper().StartsWith(x))) + id ascending
                           select sc;
                }
                IEnumerable<ThPDSProjectGraphEdge> GetSortedEdges(IEnumerable<ThPDSProjectGraphEdge> edges)
                {
                    return from edge in edges
                           where edge.Source == vertice
                           let circuitVM = new ThPDSCircuitModel(edge)
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
                                    void UpdateBreakerViewModel()
                                    {
                                        void reg(Breaker breaker, string templateStr)
                                        {
                                            var vm = new ThPDSBreakerModel(breaker);
                                            vm.PropertyChanged += (s, e) =>
                                            {
                                                if (e.PropertyName == nameof(ThPDSBreakerModel.RatedCurrent))
                                                {
                                                    ThPDSProjectGraphService.UpdateWithMiniBusbar(vertice);
                                                }
                                            };
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
                                        if (edge.Details.CircuitForm is PDS.Project.Module.Circuit.RegularCircuit regularCircuit)
                                        {
                                            breaker = regularCircuit.breaker;
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                        var blkVm = new ThPDSBlockViewModel();
                                        void UpdateBreakerViewModel()
                                        {
                                            void reg(Breaker breaker, string templateStr)
                                            {
                                                var vm = new ThPDSBreakerModel(breaker);
                                                vm.PropertyChanged += (s, e) =>
                                                {
                                                    if (e.PropertyName == nameof(ThPDSBreakerModel.RatedCurrent))
                                                    {
                                                        ThPDSProjectGraphService.UpdateWithEdge(edge);
                                                    }
                                                };
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
                                var vm = new ThPDSMiniBusbarModel(vertice, mbb);
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
                                var edges = ThPDSProjectGraphService.GetSuitableSmallBusbarCircuit(graph, vertice);
                                for (int i = 0; i < edges.Count; i++)
                                {
                                    var edge = edges[i];
                                    node.DataList.Add(new ThPDSCircuitGraphTreeModel() { Id = i, Name = edge.Circuit.ID.CircuitID.LastOrDefault(), });
                                }
                                var w = new UserContorls.ThPDSAssignCircuit2SmallBusbar() { Width = 400, Height = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen, };
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
                        {
                            var mi = new MenuItem();
                            menu.Items.Add(mi);
                            mi.Header = "删除";
                            mi.Command = new RelayCommand(() =>
                            {
                                ThPDSProjectGraphService.DeleteSmallBusbar(vertice, mbb);
                                UpdateCanvas();
                            });
                        }
                        cvs.Cursor = Cursors.Hand;
                        canvas.Children.Add(cvs);
                    }
                }
                var visitedSecondaryCircuits = new HashSet<SecondaryCircuit>();
                var visitedEdges = new HashSet<ThPDSProjectGraphEdge>();
                {
                    var fs = new Dictionary<ThPDSProjectGraphEdge, Action>();
                    foreach (var kv in vertice.Details.SecondaryCircuits)
                    {
                        List<ThPDSProjectGraphEdge> edges;
                        HashSet<SecondaryCircuit> scs;
                        {
                            var _sc = kv.Key;
                            if (visitedSecondaryCircuits.Contains(_sc)) continue;
                            visitedSecondaryCircuits.Add(_sc);
                            var _edges = GetSortedEdges(ThPDSProjectGraphService.GetControlCircuit(graph, vertice, _sc)).Except(visitedEdges).ToHashSet();
                            if (_edges.Count == 0) continue;
                            scs = new() { _sc };
                            foreach (var k in vertice.Details.SecondaryCircuits.Keys)
                            {
                                if (k == _sc) continue;
                                var egs = ThPDSProjectGraphService.GetControlCircuit(graph, vertice, k);
                                foreach (var edge in egs)
                                {
                                    if (_edges.Contains(edge))
                                    {
                                        scs.Add(k);
                                        foreach (var eg in egs)
                                        {
                                            _edges.Add(eg);
                                        }
                                        break;
                                    }
                                }
                            }
                            foreach (var sc in scs)
                            {
                                visitedSecondaryCircuits.Add(sc);
                            }
                            foreach (var edge in _edges)
                            {
                                visitedEdges.Add(edge);
                            }
                            edges = GetSortedEdges(_edges).ToList();
                        }
                        fs.Add(edges.First(), () =>
                        {
                            foreach (var edge in edges)
                            {
                                DrawEdge(edge);
                            }
                            if (scs.Count > 0)
                            {
                                var pts = new List<Point>();
                                var cvt1 = new DoubleCollectionConverter();
                                var dashArrBORDERBorder = (DoubleCollection)cvt1.ConvertFrom("12.7, 6.35, 12.7, 6.35, 1, 6.35 ");
                                var dashArrBORDER2Border5x = (DoubleCollection)cvt1.ConvertFrom("6.35, 3.175, 6.35, 3.175, 1, 3.175 ");
                                var dashArrBORDERX2Border2x = (DoubleCollection)cvt1.ConvertFrom("25.4, 12.7, 25.4, 12.7, 1, 12.7 ");
                                var currentDashArr = new DoubleCollection(new double[] { 12.7, 6.35, 12.7, 6.35, 1, 6.35 }.Select(x => x * .5));
                                var hasCPS = edges.Any(x => x.Details.CircuitForm.CircuitFormType.GetDescription().Contains("CPS"));
                                foreach (var sc in GetSortedSecondaryCircuits(scs))
                                {
                                    var scVm = new Project.Module.ThPDSSecondaryCircuitModel(sc);
                                    scVm.PropertyChanged += (s, e) =>
                                    {
                                        if (e.PropertyName == nameof(ThPDSSecondaryCircuitModel.CircuitDescription))
                                        {
                                            if (!scVm.ConductorModel.IsCustom)
                                            {
                                                scVm.ConductorModel.RaisePropertyChanged(nameof(ThPDSConductorModel.Content));
                                            }
                                        }
                                    };
                                    var start = new Point(busStart.X + 205, -dy - 10);
                                    var end = start.OffsetY(40);
                                    busEnd = end;
                                    dy -= end.Y - start.Y;
                                    var pt = new Point(start.X - 205, start.Y);
                                    {
                                        var h = 38.0 * (edges.Count - 1);
                                        switch (edges.Last().Details.CircuitForm.CircuitFormType)
                                        {
                                            case CircuitFormOutType.None:
                                                break;
                                            case CircuitFormOutType.常规:
                                                break;
                                            case CircuitFormOutType.漏电:
                                                break;
                                            case CircuitFormOutType.接触器控制:
                                                break;
                                            case CircuitFormOutType.热继电器保护:
                                                break;
                                            case CircuitFormOutType.配电计量_上海CT:
                                                break;
                                            case CircuitFormOutType.配电计量_上海直接表:
                                                break;
                                            case CircuitFormOutType.配电计量_CT表在前:
                                                break;
                                            case CircuitFormOutType.配电计量_直接表在前:
                                                break;
                                            case CircuitFormOutType.配电计量_CT表在后:
                                                break;
                                            case CircuitFormOutType.配电计量_直接表在后:
                                                break;
                                            case CircuitFormOutType.电动机_分立元件:
                                                break;
                                            case CircuitFormOutType.电动机_CPS:
                                                break;
                                            case CircuitFormOutType.电动机_分立元件星三角启动:
                                                break;
                                            case CircuitFormOutType.电动机_CPS星三角启动:
                                                break;
                                            case CircuitFormOutType.双速电动机_分立元件detailYY:
                                                h += 100;
                                                break;
                                            case CircuitFormOutType.双速电动机_分立元件YY:
                                                h += 60;
                                                break;
                                            case CircuitFormOutType.双速电动机_CPSdetailYY:
                                                h += 100;
                                                break;
                                            case CircuitFormOutType.双速电动机_CPSYY:
                                                h += 60;
                                                break;
                                            case CircuitFormOutType.消防应急照明回路WFEL:
                                                break;
                                            default:
                                                break;
                                        }
                                        var st = new Point(pt.X + (hasCPS ? 46 : 144), pt.Y + 10 - h);
                                        var ed = st;
                                        ed.Y = pt.Y + 40;
                                        pts.Add(st);
                                        pts.Add(ed);
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
                                                        var vm = scVm.ConductorModel;
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
                                {
                                    var st = new Point(pts[0].X, pts.Select(x => x.Y).Min());
                                    var ed = new Point(pts[0].X, pts.Select(x => x.Y).Max());
                                    var ln = CreateLine(null, Brushes.Black, st, ed);
                                    ln.StrokeDashCap = PenLineCap.Square;
                                    ln.StrokeDashArray = currentDashArr;
                                    canvas.Children.Add(ln);
                                }
                            }
                        });
                    }
                    foreach (var edge in GetSortedEdges(fs.Keys))
                    {
                        fs[edge]();
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
                            var sw = ThPDSProjectGraphService.GetCircuitFormInSwitcher(vertice);
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
                if (busEnd.Y > canvas.Height)
                {
                    canvas.Height = busEnd.Y + 300;
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
    }
}