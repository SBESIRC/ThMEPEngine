using System;
using System.IO;
using System.Linq;
using ThCADExtension;
using System.Windows;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using System.Windows.Data;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Controls;
using Autodesk.AutoCAD.Geometry;
using System.Collections.ObjectModel;
using Autodesk.AutoCAD.ApplicationServices;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.UI.UserContorls;
using Microsoft.Toolkit.Mvvm.Input;
using PDSGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.PDS.UI.Services
{
    public static class PDSColorBrushes
    {
        public static readonly SolidColorBrush None = null;
        public static readonly SolidColorBrush Servere = new((Color)ColorConverter.ConvertFromString("#E57373"));
        public static readonly SolidColorBrush Moderate = new((Color)ColorConverter.ConvertFromString("#FFBF07"));
        public static readonly SolidColorBrush Mild = new((Color)ColorConverter.ConvertFromString("#4DD0E2"));
        public static readonly SolidColorBrush Safe = new((Color)ColorConverter.ConvertFromString("#8AC348"));
    }
    public static class PDSImageSources
    {
        static readonly ImageSourceConverter cvt = new();
        public static readonly ImageSource None = null;
        public static readonly ImageSource Servere = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Servere.ico"));
        public static readonly ImageSource Moderate = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Moderate.ico"));
        public static readonly ImageSource Mild = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Mild.ico"));
        public static readonly ImageSource Safe = (ImageSource)cvt.ConvertFrom(new Uri("pack://application:,,,/ThControlLibraryWPF;component/Images/Safe.ico"));
    }
    public class ThPDSInfoCompareService
    {
        private readonly ThPDSTransientService TransientService = new();

        private readonly ObservableCollection<LoadDiffItem> Loads = new();

        private readonly ObservableCollection<CircuitDiffItem> Circuits = new();

        private PDSGraph Graph => Project.PDSProjectVM.Instance.InformationMatchViewModel.Graph;

        public void Init(ThPDSInfoCompare panel)
        {
            panel.DataContext = CreateVM(panel);
            {
                var node = new ThPDSCircuitGraphTreeModel() { DataList = new() };
                AcadApp.DocumentManager
                    .OfType<Document>()
                    .Where(x => x.IsNamedDrawing)
                    .ForEach(x =>
                    {
                        node.DataList.Add(new()
                        {
                            Name = Path.GetFileNameWithoutExtension(x.Name),
                            Key = x.Name,
                            Tag = x,
                        });
                    });
                panel.lbx.DataContext = node;
                AcadApp.DocumentManager.DocumentCreated += (s, e) =>
                {
                    if (e.Document.IsNamedDrawing)
                    {
                        node.DataList.Add(new()
                        {
                            Name = Path.GetFileNameWithoutExtension(e.Document.Name),
                            Key = e.Document.Name,
                            Tag = e.Document,
                        });
                    }
                };
                AcadApp.DocumentManager.DocumentToBeDestroyed += (s, e) =>
                {
                    if (e.Document.IsNamedDrawing)
                    {
                        var model = node.DataList.Where(o => o.Tag as Document == e.Document).FirstOrDefault();
                        if (model != null)
                        {
                            node.DataList.Remove(model);
                        }
                    }
                };
                AcadApp.DocumentManager.DocumentToBeDeactivated += (s, e) =>
                {
                    if (e.Document.IsNamedDrawing)
                    {
                        TransientService.ClearTransientGraphics();
                    }
                };
            }
            {
                panel.LoadDataGrid.ContextMenu = GetContextMenu(panel.LoadDataGrid);
                panel.CircuitDataGrid.ContextMenu = GetContextMenu(panel.CircuitDataGrid);
            }
            {
                panel.CircuitSearchBar.SearchStarted += (s, e) =>
                {
                    var searchKey = e.Info;
                    var cv = CollectionViewSource.GetDefaultView(Circuits);
                    if (cv != null && cv.CanFilter)
                    {
                        cv.Filter = (o) =>
                        {
                            if (string.IsNullOrEmpty(searchKey))
                            {
                                return true;
                            }
                            else
                            {
                                var item = o as CircuitDiffItem;
                                return item.CircuitNumber.Contains(searchKey);
                            }
                        };
                    }
                };
                panel.LoadSearchBar.SearchStarted += (s, e) =>
                {
                    var searchKey = e.Info;
                    var cv = CollectionViewSource.GetDefaultView(Loads);
                    if (cv != null && cv.CanFilter)
                    {
                        cv.Filter = (e) =>
                        {
                            if (string.IsNullOrEmpty(searchKey))
                            {
                                return true;
                            }
                            else
                            {
                                var item = e as LoadDiffItem;
                                return item.LoadId.Contains(searchKey);
                            }
                        };
                    }
                };
            }
            {
                panel.LoadDataGrid.MouseDoubleClick += (s, e) =>
                {
                    if (panel.LoadDataGrid.SelectedItem == null) return;
                    var item = panel.LoadDataGrid.SelectedItem as LoadDiffItem;
                    TransientService.ClearTransientGraphics();
                    TransientService.AddToTransient(item.Node);
                };
            }
        }

        private ThPDSInfoCompareViewModel CreateVM(ThPDSInfoCompare panel)
        {
            return new ThPDSInfoCompareViewModel()
            {
                CompareCmd = new RelayCommand(() =>
                {
                    if (panel.lbx.DataContext is not ThPDSCircuitGraphTreeModel tree) return;
                    var databases = tree.DataList
                    .Where(x => x.IsChecked == true)
                    .Select(x => ((Document)x.Tag).Database);
                    if (!databases.Any()) return;
                    new ThPDSSecondaryPushDataService().Push(databases.ToList());
                }),
                CreateCmd = new RelayCommand(() =>
                {
                    var w = new ThPDSCreateLoad();
                    if (w.ShowDialog() == true)
                    {
                        var vm = w.DataContext as ThPDSCreateLoadVM;
                        ThPDSProjectGraphService.CreatNewLoad(CreateData(vm));
                        UpdateView(panel);
                    }
                }),
                UpdateCmd = new RelayCommand(() =>
                {
                    // 切回CAD画布
                    ThPDSCADService.FocusToCAD();

                    // 发送命令更新图纸
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THPDSUPDATEDWG");
                }),
                ValidateCmd = new RelayCommand(() =>
                {
                    new ThPDSGraphVerifyService().Verify(Graph);
                    UpdateView(panel);
                }),
                ReadAndRegenCmd = new RelayCommand(() =>
                {
                    if (panel.lbx.DataContext is not ThPDSCircuitGraphTreeModel tree) return;
                    var databases = tree.DataList
                    .Where(x => x.IsChecked == true)
                    .Select(x => ((Document)x.Tag).Database);
                    if (!databases.Any()) return;
                    new ThPDSPushDataService().Push(databases.ToList());
                }),
                RefreshUICmd = new RelayCommand(() =>
                {
                    UpdateView(panel);
                }),
            };
        }

        private ThPDSProjectGraphNodeData CreateData(ThPDSCreateLoadVM vm)
        {
            var data = ThPDSProjectGraphNodeData.Create();
            data.Power = vm.Power;
            data.Type = vm.Type.Type;
            data.FireLoad = vm.FireLoad;
            data.Storey ??= vm.Storey;
            data.Number ??= vm.Number;
            data.Description ??= vm.Description;
            data.Sync();
            return data;
        }

        private ContextMenu GetContextMenu(DataGrid dataGrid)
        {
            Visual GetDescendantByType(Visual element, Type type)
            {
                if (element == null)
                {
                    return null;
                }

                if (element.GetType() == type)
                {
                    return element;
                }

                Visual foundElement = null;

                if (element is FrameworkElement)
                {
                    (element as FrameworkElement).ApplyTemplate();
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                    foundElement = GetDescendantByType(visual, type);
                    if (foundElement != null)
                        break;
                }
                return foundElement;
            }
            void ExpandAllGroups(DataGrid dataGrid, bool expand = true)
            {
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                foreach (CollectionViewGroup group in view.Groups)
                {
                    ItemContainerGenerator generator = dataGrid.ItemContainerGenerator;
                    GroupItem grpItem = (GroupItem)generator.ContainerFromItem(group);
                    //Retrieve the Expander inside the GroupItem;
                    Expander exp = (Expander)GetDescendantByType(grpItem, typeof(Expander));
                    try
                    {
                        exp.IsExpanded = expand;
                    }
                    catch
                    {
                        //
                    }
                }
            }

            var cmenu = new ContextMenu();
            cmenu.Items.Add(new MenuItem()
            {
                Header = "全部展开",
                Command = new RelayCommand(() =>
                {
                    ExpandAllGroups(dataGrid, true);
                }),
            });
            cmenu.Items.Add(new MenuItem()
            {
                Header = "全部折叠",
                Command = new RelayCommand(() =>
                {
                    ExpandAllGroups(dataGrid, false);
                }),
            });
            return cmenu;
        }

        public void UpdateView(ThPDSInfoCompare panel)
        {
            {
                Circuits.Clear();
                foreach (var edge in Graph.Edges)
                {
                    var tag = edge.Tag;
                    if (edge.Source.Type == Model.PDSNodeType.VirtualLoad)
                    {
                        continue;
                    }
                    else if (tag is null)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.None,
                            Img = PDSImageSources.None,
                            Hint = "无变化",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeCompositeTag)
                    {
                        //throw new NotSupportedException();
                    }
                    else if (tag is ThPDSProjectGraphEdgeIdChangeTag projectGraphEdgeIdChangeTag)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = string.Format("回路编号变化，原编号为{0}", projectGraphEdgeIdChangeTag.ChangedLastCircuitID),
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeMoveTag projectGraphEdgeMoveTag)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = "此回路连接的负载变化",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeAddTag)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此回路为新增",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeDeleteTag)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此回路被删除",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeDuplicateTag)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Mild,
                            Img = PDSImageSources.Mild,
                            Hint = "回路编号重复",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeCascadingErrorTag)
                    {
                        Circuits.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "断路器选型不具备选择性",
                        });
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                // CollectionViewSource with group, sort, and filter
                var cv = CollectionViewSource.GetDefaultView(Circuits);
                if (cv != null && cv.CanGroup)
                {
                    cv.GroupDescriptions.Clear();
                    cv.GroupDescriptions.Add(new PropertyGroupDescription("SourcePanelID"));
                }
                if (cv != null && cv.CanSort)
                {
                    cv.SortDescriptions.Clear();
                    cv.SortDescriptions.Add(new SortDescription("SourcePanelID", ListSortDirection.Ascending));
                    cv.SortDescriptions.Add(new SortDescription("CircuitNumber", ListSortDirection.Ascending));
                }
                panel.CircuitDataGrid.ItemsSource = cv;
            }
            {
                Loads.Clear();
                foreach (var node in Graph.Vertices)
                {
                    var tag = node.Tag;
                    if (node.Type == Model.PDSNodeType.VirtualLoad)
                    {
                        continue;
                    }
                    else if (tag is null)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.None,
                            Img = PDSImageSources.None,
                            Hint = "无变化",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeCompositeTag)
                    {
                        //throw new NotSupportedException();
                    }
                    else if (tag is ThPDSProjectGraphNodeIdChangeTag projectGraphNodeIdChangeTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = string.Format("负载编号变化，原编号{0}", projectGraphNodeIdChangeTag.ChangedID),
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeExchangeTag projectGraphNodeExchangeTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = string.Format("此负载与{0}交换", projectGraphNodeExchangeTag.ExchangeToID),
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeMoveTag projectGraphNodeMoveTag)
                    {
                        if (projectGraphNodeMoveTag.MoveFrom)
                        {
                            Loads.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Moderate,
                                Img = PDSImageSources.Moderate,
                                Hint = string.Format("此负载已移动至{0}", projectGraphNodeMoveTag.AnotherNode.Load.ID.LoadID),
                            });
                        }
                        else
                        {
                            Loads.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Moderate,
                                Img = PDSImageSources.Moderate,
                                Hint = string.Format("此负载由{0}移动至此", projectGraphNodeMoveTag.AnotherNode.Load.ID.LoadID),
                            });
                        }
                    }
                    else if (tag is ThPDSProjectGraphNodeAddTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此负载为新增",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeDeleteTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此负载被删除",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeDataTag dataTag)
                    {
                        if (dataTag.TagD)
                        {
                            Loads.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Safe,
                                Img = PDSImageSources.Safe,
                                Hint = "描述文本变化",
                            });
                        }
                        if (dataTag.TagF)
                        {
                            if (node.Load.FireLoad)
                            {
                                Loads.Add(new()
                                {
                                    Node = node,
                                    Background = PDSColorBrushes.Servere,
                                    Img = PDSImageSources.Servere,
                                    Hint = "负载变为消防负荷",
                                });
                            }
                            else
                            {
                                Loads.Add(new()
                                {
                                    Node = node,
                                    Background = PDSColorBrushes.Mild,
                                    Img = PDSImageSources.Mild,
                                    Hint = "负载变为非消防负荷",
                                });
                            }
                        }
                        if (dataTag.TagP)
                        {
                            string hint = string.Empty;
                            if (!node.Details.LoadCalculationInfo.IsDualPower && !dataTag.TarP.IsDualPower)
                            {
                                hint = string.Format("功率由{0}kW调整到{1}kW", dataTag.TarP.HighPower, node.Details.LoadCalculationInfo.HighPower);
                            }
                            else
                            {
                                hint = string.Format("功率由{0}kW调整到{1}kW；功率由{2}kW调整到{3}kW",
                                    dataTag.TarP.HighPower, node.Details.LoadCalculationInfo.HighPower,
                                    dataTag.TarP.LowPower, node.Details.LoadCalculationInfo.LowPower);
                            }
                            Loads.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Moderate,
                                Img = PDSImageSources.Moderate,
                                Hint = hint,
                            });
                        }
                        if (dataTag.TagType)
                        {
                            Loads.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Mild,
                                Img = PDSImageSources.Mild,
                                Hint = string.Format("负载类型由{0}变化为{1}", dataTag.TarType.GetDescription(), node.Type.GetDescription()),
                            });
                        }
                        if (dataTag.TagPhase)
                        {
                            Loads.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Mild,
                                Img = PDSImageSources.Mild,
                                Hint = "负载相数发生变化",
                            });
                        }
                    }
                    else if (tag is ThPDSProjectGraphNodeDuplicateTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "负载编号重复",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeSingleTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "这是一个孤立的负载",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeFireTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "不满足消防供电要求",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeCascadingErrorTag)
                    {
                        Loads.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "进线断路器选型不具备选择性",
                        });
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }

                // CollectionViewSource with group, sort, and filter
                var cv = CollectionViewSource.GetDefaultView(Loads);
                if (cv != null && cv.CanGroup)
                {
                    cv.GroupDescriptions.Clear();
                    cv.GroupDescriptions.Add(new PropertyGroupDescription("LoadType"));
                }
                if (cv != null && cv.CanSort)
                {
                    cv.SortDescriptions.Clear();
                    cv.SortDescriptions.Add(new SortDescription("LoadType", ListSortDirection.Ascending));
                    cv.SortDescriptions.Add(new SortDescription("LoadId", ListSortDirection.Ascending));
                }
                panel.LoadDataGrid.ItemsSource = cv;
            }
        }

        private class ThPDSInfoCompareViewModel
        {
            public RelayCommand ValidateCmd { get; set; }
            public RelayCommand CompareCmd { get; set; }
            public RelayCommand AcceptCmd { get; set; }
            public RelayCommand CreateCmd { get; set; }
            public RelayCommand UpdateCmd { get; set; }
            public RelayCommand RefreshUICmd { get; set; }
            public RelayCommand ReadAndRegenCmd { get; set; }
        }
    }
    public class CircuitDiffItem
    {
        public string CircuitNumber
        {
            get
            {
                return Edge.Circuit.ID.CircuitNumber;
            }
        }
        public string CircuitId
        {
            get
            {
                var id = Edge.Circuit.ID.CircuitID;
                if (string.IsNullOrEmpty(id))
                {
                    return "未知编号回路";
                }
                return id;
            }
        }
        public string CircuitType
        {
            get
            {
                return Edge.Target.Load.CircuitType.GetDescription();
            }
        }
        public string CircuitLoad => Edge.Target.LoadIdString();
        public string SourcePanelID
        {
            get
            {
                var id = Edge.Circuit.ID.SourcePanelID;
                if (string.IsNullOrEmpty(id))
                {
                    return "未知负载";
                }
                return id;
            }
        }
        public string Hint { get; set; }
        public Brush Background { get; set; }
        public ImageSource Img { get; set; }
        public ThPDSProjectGraphEdge Edge { get; set; }
    }
    public class LoadDiffItem
    {
        public string LoadId => Node.LoadIdString();
        public string LoadType
        {
            get
            {
                return Node.Load.LoadTypeCat_1.GetDescription();
            }
        }
        public string LoadPower
        {
            get
            {
                if (Node.Details.LoadCalculationInfo.IsDualPower)
                {
                    return string.Format("{0}/{1}", Node.Details.LoadCalculationInfo.HighPower, Node.Details.LoadCalculationInfo.LowPower);
                }
                else
                {
                    return string.Format("{0}", Node.Details.LoadCalculationInfo.HighPower);
                }
            }
        }
        public string Dwg
        {
            get
            {
                if (Node.Load.Location != null)
                {
                    return Node.Load.Location.ReferenceDWG;
                }
                return string.Empty;
            }
        }
        public Point3d BasePoint
        {
            get
            {
                return ThPDSPoint3dService.PDSPoint3dToPoint3d(Node.Load.Location.BasePoint);
            }
        }
        public string Hint { get; set; }
        public Brush Background { get; set; }
        public ImageSource Img { get; set; }
        public ThPDSProjectGraphNode Node { get; set; }
    }
}
