using System;
using System.IO;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Windows.Media;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
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
                        new ThPDSSecondaryPushDataService().Push();
                        PDS.Project.PDSProject.Instance.DataChanged?.Invoke();
                        UpdateView(panel);
                    }, () =>!hasDataError),
                    AcceptCmd = new RelayCommand(() => { }, () => !hasDataError|| regenCount > 1),
                    CreateCmd = new RelayCommand(() => { }, () =>
                  !hasDataError || regenCount > 1),
                    UpdateCmd = new RelayCommand(() =>
                    {
                        new ThPDSUpdateToDwgService().Update();
                    }, () => !hasDataError || regenCount > 1),
                };
                vm.ReadAndRegenCmd = new RelayCommand(() =>
                {
                    new Command.ThPDSCommand().Execute();
                    ++regenCount;
                    PDS.Project.PDSProject.Instance.DataChanged?.Invoke();
                    UpdateView(panel);
                    vm.CompareCmd.NotifyCanExecuteChanged();
                    vm.AcceptCmd.NotifyCanExecuteChanged();
                    vm.CreateCmd.NotifyCanExecuteChanged();
                    vm.UpdateCmd.NotifyCanExecuteChanged();
                });
                panel.DataContext = vm;
            }
            {
                var node = new ThPDSCircuitGraphTreeModel() { DataList = new(), };
                AcadApp.DocumentManager.OfType<Document>().Where(x => x.IsNamedDrawing).ForEach(x =>
                {
                    node.DataList.Add(new() { Name = Path.GetFileNameWithoutExtension(x.Name), Key = x.Name });
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
                var info = new CircuitDiffInfo() { Items = new(), };
                foreach (var edge in g.Edges)
                {
                    var tag = edge.Tag;
                    if (tag is ThPDSProjectGraphEdgeCompositeTag)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Mild,
                            Img = PDSImageSources.Mild,
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeIdChangeTag projectGraphEdgeIdChangeTag)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = "回路编号变化，原编号" + projectGraphEdgeIdChangeTag.ChangedLastCircuitID,
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeMoveTag projectGraphEdgeMoveTag)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = "此回路被移动",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeAddTag)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此回路为新增",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeDeleteTag)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此回路被删除",
                        });
                    }
                    else if (tag is ThPDSProjectGraphEdgeDataTag projectGraphEdgeDataTag)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Safe,
                            Img = PDSImageSources.Safe,
                            Hint = projectGraphEdgeDataTag.ToLastCircuitID,
                        });
                    }
                    else
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.None,
                            Img = PDSImageSources.None,
                        });
                    }
                }
                panel.dg1.DataContext = info;
            }
            {
                var info = new LoadDiffInfo() { Items = new(), };
                foreach (var node in g.Vertices)
                {
                    var tag = node.Tag;
                    if (tag is ThPDSProjectGraphNodeCompositeTag)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Mild,
                            Img = PDSImageSources.Mild,
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeIdChangeTag projectGraphNodeIdChangeTag)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = $"负载编号变化，原编号{projectGraphNodeIdChangeTag.ChangedID}",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeExchangeTag projectGraphNodeExchangeTag)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = $"此负载与{projectGraphNodeExchangeTag.ExchangeToID}交换",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeMoveTag projectGraphNodeMoveTag)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Moderate,
                            Img = PDSImageSources.Moderate,
                            Hint = $"此负载由{projectGraphNodeMoveTag}移动至此",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeAddTag)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此负载为新增",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeDeleteTag)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.Servere,
                            Img = PDSImageSources.Servere,
                            Hint = "此负载被删除",
                        });
                    }
                    else if (tag is ThPDSProjectGraphNodeDataTag dataTag)
                    {
                        if (dataTag.TagD)
                        {
                            info.Items.Add(new()
                            {
                                LoadId = node.Load.ID.LoadID,
                                LoadType = node.Load.LoadTypeCat_1.ToString(),
                                LoadPower = node.Details.LowPower.ToString(),
                                Dwg = node.Load.Location?.ReferenceDWG,
                                Background = PDSColorBrushes.Servere,
                                Img = PDSImageSources.Servere,
                                Hint = "描述变化",
                            });
                        }
                        if (dataTag.TagF)
                        {
                            info.Items.Add(new()
                            {
                                LoadId = node.Load.ID.LoadID,
                                LoadType = node.Load.LoadTypeCat_1.ToString(),
                                LoadPower = node.Details.LowPower.ToString(),
                                Dwg = node.Load.Location?.ReferenceDWG,
                                Background = PDSColorBrushes.Servere,
                                Img = PDSImageSources.Servere,
                                Hint = "消防变化",
                            });
                        }
                        if (dataTag.TagP)
                        {
                            info.Items.Add(new()
                            {
                                LoadId = node.Load.ID.LoadID,
                                LoadType = node.Load.LoadTypeCat_1.ToString(),
                                LoadPower = node.Details.LowPower.ToString(),
                                Dwg = node.Load.Location?.ReferenceDWG,
                                Background = PDSColorBrushes.Servere,
                                Img = PDSImageSources.Servere,
                                Hint = "功率变化",
                            });
                        }
                    }
                    else
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                            Background = PDSColorBrushes.None,
                            Img = PDSImageSources.None,
                        });
                    }
                }
                panel.dg2.DataContext = info;
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
    public class CircuitDiffInfo
    {
        public List<CircuitDiffItem> Items { get; set; }
    }
    public class LoadDiffInfo
    {
        public List<LoadDiffItem> Items { get; set; }
    }
    public class CircuitDiffItem
    {
        public string CircuitId { get; set; }
        public string CircuitType { get; set; }
        public string ParentBox { get; set; }
        public string Dwg { get; set; }
        public string Hint { get; set; } = "提示文本";
        public Brush Background { get; set; }
        public ImageSource Img { get; set; }
    }
    public class LoadDiffItem
    {
        public string LoadId { get; set; }
        public string LoadType { get; set; }
        public string LoadPower { get; set; }
        public string Dwg { get; set; }
        public string Hint { get; set; } = "提示文本";
        public Brush Background { get; set; }
        public ImageSource Img { get; set; }
    }
}