using System;
using System.IO;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using System.ComponentModel;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.UserContorls;
using Microsoft.Toolkit.Mvvm.Input;
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
        public void Init(ThPDSInfoCompare panel)
        {
            {
                var hasDataError = false;
                var regenCount = 0L;
                var vm = new
                ThPDSInfoCompareViewModel()
                {
                    CompareCmd = new RelayCommand(() =>
                    {
                        if (panel.lbx.DataContext is not ThPDSCircuitGraphTreeModel tree) return;
                        var databases = tree.DataList
                        .Where(x => x.IsChecked == true)
                        .Select(x => ((Document)x.Tag).Database);
                        if (!databases.Any()) return;
                        new ThPDSSecondaryPushDataService().Push(databases.ToList());
                        PDS.Project.PDSProject.Instance.DataChanged?.Invoke();
                        UpdateView(panel);
                    }, () => !hasDataError),
                    AcceptCmd = new RelayCommand(() => { }, () => !hasDataError || regenCount > 1),
                    CreateCmd = new RelayCommand(() =>
                    {
                        var w = new ThPDSCreateLoad() { Width = 250, Height = 250, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen, };
                        w.ShowDialog();
                    }, () => !hasDataError || regenCount > 1),
                    UpdateCmd = new RelayCommand(() =>
                    {
                        new ThPDSUpdateToDwgService().Update();
                    }, () => !hasDataError || regenCount > 1),
                };
                vm.ReadAndRegenCmd = new RelayCommand(() =>
                {
                    if (panel.lbx.DataContext is not ThPDSCircuitGraphTreeModel tree) return;
                    var databases = tree.DataList
                    .Where(x => x.IsChecked == true)
                    .Select(x => ((Document)x.Tag).Database);
                    if (!databases.Any()) return;
                    new ThPDSPushDataService().Push(databases.ToList());
                    PDS.Project.PDSProject.Instance.DataChanged?.Invoke();
                    UpdateView(panel);
                    vm.CompareCmd.NotifyCanExecuteChanged();
                    vm.AcceptCmd.NotifyCanExecuteChanged();
                    vm.CreateCmd.NotifyCanExecuteChanged();
                    vm.UpdateCmd.NotifyCanExecuteChanged();
                    ++regenCount;
                });
                panel.DataContext = vm;
            }
            {
                var node = new ThPDSCircuitGraphTreeModel() { DataList = new(), };
                AcadApp.DocumentManager.OfType<Document>().Where(x => x.IsNamedDrawing).ForEach(x =>
                {
                    node.DataList.Add(new() { Name = Path.GetFileNameWithoutExtension(x.Name), Key = x.Name, Tag = x, });
                });
                panel.lbx.DataContext = node;
                AcadApp.DocumentManager.DocumentCreated += (s, e) =>
                {
                    if (e.Document.IsNamedDrawing)
                    {
                        node.DataList.Add(new() { Name = Path.GetFileNameWithoutExtension(e.Document.Name), Key = e.Document.Name });
                    }
                };
                AcadApp.DocumentManager.DocumentDestroyed += (s, e) =>
                {
                    var file = e.FileName;
                    if (!string.IsNullOrEmpty(file))
                    {
                        node.DataList.Remove(node.DataList.FirstOrDefault(x => x.Key?.ToUpper() == file?.ToUpper()));
                    }
                };
            }
        }

        public void UpdateView(ThPDSInfoCompare panel)
        {
            var g = Project.PDSProjectVM.Instance.InformationMatchViewModel.Graph;
            {
                var items = new List<CircuitDiffItem>();
                foreach (var edge in g.Edges)
                {
                    var tag = edge.Tag;
                    if (tag is ThPDSProjectGraphEdgeCompositeTag)
                    {
                        //throw new NotSupportedException();
                    }
                    else if (tag is ThPDSProjectGraphEdgeIdChangeTag projectGraphEdgeIdChangeTag)
                    {
                        items.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = "回路编号变化，原编号" + projectGraphEdgeIdChangeTag.ChangedLastCircuitID,
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeMoveTag projectGraphEdgeMoveTag)
                    {
                        items.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = "此回路被移动",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeAddTag)
                    {
                        items.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此回路为新增",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeDeleteTag)
                    {
                        items.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此回路被删除",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeDataTag projectGraphEdgeDataTag)
                    {
                        items.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.Safe,
                            Img = PDSImageSources.Safe,
                            Hint = projectGraphEdgeDataTag.ToLastCircuitID,
                        });
                    }
                    else
                    {
                        items.Add(new()
                        {
                            Edge = edge,
                            Background = PDSColorBrushes.None,
                            Img = PDSImageSources.None,
                            Hint = "无变化",
                        });
                    }
                }

                // CollectionViewSource with group, sort, and filter
                var cv = CollectionViewSource.GetDefaultView(items);
                cv.SortDescriptions.Add(new SortDescription("CircuitNumber", ListSortDirection.Ascending));
                cv.GroupDescriptions.Add(new PropertyGroupDescription("SourcePanelID"));
                panel.CircuitDataGrid.ItemsSource = cv;
                panel.CircuitSearchBar.SearchStarted += (s, e) =>
                {
                    var searchKey = e.Info;
                    cv.Filter = (e) =>
                    {
                        if (string.IsNullOrEmpty(searchKey))
                        {
                            return true;
                        }
                        else
                        {
                            var item = e as CircuitDiffItem;
                            return item.CircuitNumber.Contains(searchKey);
                        }
                    };
                };

                //// Column header clicked
                //// http://www.scottlogic.co.uk/blog/colin/2008/12/wpf-datagrid-detecting-clicked-cell-and-row/
                //panel.CircuitDataGrid.MouseRightButtonUp += (s, e) =>
                //{
                //    DependencyObject dep = (DependencyObject)e.OriginalSource;

                //    // iteratively traverse the visual tree
                //    while ((dep != null) &&
                //    dep is not DataGridCell &&
                //    dep is not DataGridColumnHeader)
                //    {
                //        dep = VisualTreeHelper.GetParent(dep);
                //    }

                //    if (dep == null)
                //        return;

                //    if (dep is DataGridColumnHeader columnHeader)
                //    {
                //        DataGridColumn column = columnHeader.Column;
                //        if ((string)column.Header == "回路编号")
                //        {
                //            ListSortDirection direction = (column.SortDirection != ListSortDirection.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                //            ICollectionView cv = CollectionViewSource.GetDefaultView(panel.CircuitDataGrid.ItemsSource);
                //            if (cv != null && cv.CanSort == true)
                //            {
                //                using (cv.DeferRefresh())
                //                {
                //                    cv.SortDescriptions.Clear();
                //                    cv.SortDescriptions.Add(new SortDescription("回路编号", ListSortDirection.Descending));
                //                }
                //            }
                //        }
                //    }

                //    if (dep is DataGridCell)
                //    {
                //        DataGridCell cell = dep as DataGridCell;
                //        // do something
                //    }
                //};
            }
            {
                var items = new List<LoadDiffItem>();
                foreach (var node in g.Vertices)
                {
                    var tag = node.Tag;
                    if (tag is ThPDSProjectGraphNodeCompositeTag)
                    {
                        //throw new NotSupportedException();
                    }
                    else if (tag is ThPDSProjectGraphNodeIdChangeTag projectGraphNodeIdChangeTag)
                    {
                        items.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = $"负载编号变化，原编号{projectGraphNodeIdChangeTag.ChangedID}",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeExchangeTag projectGraphNodeExchangeTag)
                    {
                        items.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = $"此负载与{projectGraphNodeExchangeTag.ExchangeToID}交换",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeMoveTag projectGraphNodeMoveTag)
                    {
                        items.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = $"此负载由{projectGraphNodeMoveTag}移动至此",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeAddTag)
                    {
                        items.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此负载为新增",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeDeleteTag)
                    {
                        items.Add(new()
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
                            items.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Servere,
                                Img = PDSImageSources.Servere,
                                Hint = "描述变化",
                            });
                        }
                        if (dataTag.TagF)
                        {
                            items.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Servere,
                                Img = PDSImageSources.Servere,
                                Hint = "消防变化",
                            });
                        }
                        if (dataTag.TagP)
                        {
                            items.Add(new()
                            {
                                Node = node,
                                Background = PDSColorBrushes.Servere,
                                Img = PDSImageSources.Servere,
                                Hint = "功率变化",
                            });
                        }
                    }
                    else
                    {
                        items.Add(new()
                        {
                            Node = node,
                            Background = PDSColorBrushes.None,
                            Img = PDSImageSources.None,
                            Hint = "无变化",
                        });
                    }
                }

                // CollectionViewSource with group, sort, and filter
                var cv = CollectionViewSource.GetDefaultView(items);
                cv.SortDescriptions.Add(new SortDescription("LoadId", ListSortDirection.Ascending));
                cv.GroupDescriptions.Add(new PropertyGroupDescription("LoadType"));
                panel.LoadDataGrid.ItemsSource = cv;
                panel.LoadSearchBar.SearchStarted += (s, e) =>
                {
                    var searchKey = e.Info;
                    ICollectionView cv = CollectionViewSource.GetDefaultView(panel.LoadDataGrid.ItemsSource);
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
                };

                // MouseDoubleClick event
                panel.LoadDataGrid.MouseDoubleClick += (s, e) =>
                {
                    if (panel.LoadDataGrid.SelectedItem == null) return;
                    var item = panel.LoadDataGrid.SelectedItem as LoadDiffItem;
                    var engine = new ThPDSZoomEngine();
                    engine.Zoom(item.Node);
                };
            }
        }

        private class ThPDSInfoCompareViewModel
        {
            public RelayCommand CompareCmd { get; set; }
            public RelayCommand AcceptCmd { get; set; }
            public RelayCommand CreateCmd { get; set; }
            public RelayCommand UpdateCmd { get; set; }
            public RelayCommand ReadAndRegenCmd { get; set; }
        }
    }
    public class CircuitDiffItem
    {
        public string CircuitNumber
        {
            get
            {
                return Edge.Circuit.ID.CircuitNumber.Last();
            }
        }
        public string CircuitId
        {
            get
            {
                var id = Edge.Circuit.ID.CircuitID.Last();
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
        public string SourcePanelID
        {
            get
            {
                var id = Edge.Circuit.ID.SourcePanelID.Last();
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
        public string LoadId
        {
            get
            {
                var id = Node.Load.ID.LoadID;
                if (string.IsNullOrEmpty(id))
                {
                    return "未知负载";
                }
                return id;
            }
        }
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
                if (Node.Details.IsDualPower)
                {
                    return string.Format("{0}/{1}", Node.Details.HighPower, Node.Details.LowPower);
                }
                else
                {
                    return string.Format("{0}", Node.Details.HighPower);
                }
            }
        }
        public string Dwg
        {
            get
            {
                return Node.Load.Location.ReferenceDWG;
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
