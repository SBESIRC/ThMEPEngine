using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using ThMEPEngineCore;
using ThMEPStructure.Reinforcement.TSSD;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Command;
using ThMEPStructure.Reinforcement.Service;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class EdgeComponentDrawVM
    {
        public ObservableCollection<EdgeComponentExtractInfo> EdgeComponents { get; set; }
        public EdgeComponentDrawModel DrawModel { get; set; }
        public ObservableCollection<string> DwgSources { get; set; }
        public ObservableCollection<string> SortWays { get; set; }
        public ObservableCollection<string> LeaderTypes { get; set; }
        public ObservableCollection<string> MarkPositions { get; set; }
        private string LayerLinkCharater = "、";
        private static List<DBObjectCollection> ComponentBoundaries { get; set; }
        private List<List<EdgeComponentExtractInfo>> GroupResults { get; set; }
        public bool IsNeedMerge { get; private set; } = false;
        // 收集非标的构件，用于把非标的构件打印到图纸上，目前和UI的展现是没关系的
        private List<EdgeComponentExtractInfo> noneStandardEdgeComponents;

        public EdgeComponentDrawVM()
        {
            DrawModel = new EdgeComponentDrawModel();
            DwgSources = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.DwgSources);
            SortWays = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.SortWays);
            LeaderTypes = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.LeaderTypes);
            MarkPositions = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.MarkPositions);
            EdgeComponents = new ObservableCollection<EdgeComponentExtractInfo>();
            if (ComponentBoundaries == null)
            {
                ComponentBoundaries = new List<DBObjectCollection>();
            }
            else
            {
                RemoveComponentFrames();
            }
            GroupResults = new List<List<EdgeComponentExtractInfo>>();
            noneStandardEdgeComponents = new List<EdgeComponentExtractInfo>();            
        }
        public void Select()
        {
            using (var cmd = new ThYjkReinforceExtractCmd())
            {
                // 传入参数
                cmd.WallLayers = Split(DrawModel.WallLayer, LayerLinkCharater);
                cmd.TextLayers = Split(DrawModel.TextLayer, LayerLinkCharater);
                cmd.WallColumnLayers = Split(DrawModel.WallColumnLayer, LayerLinkCharater);

                cmd.Execute();
                if (cmd.IsSuccess)
                {
                    Clear();
                    noneStandardEdgeComponents = new List<EdgeComponentExtractInfo>();
                    if (cmd.ExtractInfos.Count > 0)
                    {
                        IsNeedMerge = true;
                    }
                    cmd.ExtractInfos.ForEach(o => EdgeComponents.Add(o));
                    var obbs = cmd.ExtractInfos
                        .Select(o => o.EdgeComponent)
                        .ToCollection()
                        .GetObbFrames();
                    RemoveComponentFrames();
                    ComponentBoundaries.Add(obbs);
                    ShowComponentFrames();

                    // 收集非标的构件
                    // EdgeComponents中包括非标的构件，是用于在UI上呈现的，归并后就只剩标准件了
                    EdgeComponents.Where(o => !o.IsStandard).ForEach(o => noneStandardEdgeComponents.Add(o));
                }
            };
        }
        private void Clear()
        {
            EdgeComponents = new ObservableCollection<EdgeComponentExtractInfo>();
        }
        public void ClearTable()
        {
            Clear();
            IsNeedMerge = false;
        }
        public void Merge()
        {
            // 分组
            var infos = new List<EdgeComponentExtractInfo>();
            for (int i = 0; i < EdgeComponents.Count; i++)
            {
                infos.Add(EdgeComponents[i]);
            }
            var grouper = new ThDataGroupService(DrawModel.IsConsiderWall,
                DrawModel.StirrupRatio, DrawModel.ReinforceRatio);
            GroupResults = grouper.Group(infos); // 保存分组的结果

            var groups = GroupResults
                .Where(o => o.Count > 0).Select(o => o.First())
                .GroupBy(o => o.ComponentType.ToString())
                .ToList();
            // 重新编号
            Clear();
            foreach (var group in groups)
            {
                int index = 1;
                var items = group.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Number = group.Key + index;
                    index++;
                    EdgeComponents.Add(items[i]);
                }
            }
            // 把编号再赋值给同组的其它成员
            foreach (var info in EdgeComponents)
            {
                GroupResults.ForEach(g =>
                {
                    if (g.Contains(info))
                    {
                        g.ForEach(o => o.Number = info.Number);
                    }
                });
            }

            // 做完合并后，关闭此选项
            IsNeedMerge = false;
        }
        public void Draw()
        {
            // 绘制标准构件
            int gbzLastCodeIndex = 0;
            int ybzLastCodeIndex = 0;
            using (var cmd = new ThReinforceDrawCmd())
            {
                RemoveComponentFrames();
                cmd.ExtractInfos = EdgeComponents.ToList();
                cmd.ExtractInfoGroups = GroupResults;
                DrawModel.SetConfig(); // 把参数传递配置中
                cmd.Execute();
                // 做完绘制后，关闭此选项
                IsNeedMerge = false;

                gbzLastCodeIndex = cmd.GBZLastCodeIndex;
                ybzLastCodeIndex = cmd.YBZLastCodeIndex;
            }

            // 打印非标构件
            PrintNonStandardEdgeComponentToCad(noneStandardEdgeComponents);

            // 写入到TSSD
            string nonStandardEdgeComponentLayer = ThImportTemplateStyleService.NonStandardEdgeComponentLayer;
            var tssdConfig = Create(ThEdgeComponentDrawConfig.Instance, gbzLastCodeIndex + 1, 
                ybzLastCodeIndex + 1, nonStandardEdgeComponentLayer);
            WriteToTSSD(tssdConfig);
        }

        private TSSDEdgeComponentConfig Create(ThEdgeComponentDrawConfig config,int gbzStartNumber,int ybzStartNumber,string wallColumnLayer)
        {
            return new TSSDEdgeComponentConfig()
            {
                CalculationSoftware = config.DwgSource, //YJK
                WallColumnLayer = wallColumnLayer,
                SortWay = config.SortWay,
                LeaderType = config.LeaderType,
                MergeSize = config.Size.ToString(),
                MarkPosition = config.MarkPosition,
                MergeStirrupRatio = config.StirrupRatio.ToString(),
                MergeReinforceRatio = config.ReinforceRatio.ToString(),
                MergeConsiderWall = config.IsConsiderWall,
                ConstructPrefixStartNumber = gbzStartNumber.ToString(),
                ConstraintPrefixStartNumber = ybzStartNumber.ToString(),
            };
        }

        private void WriteToTSSD(TSSDEdgeComponentConfig config)
        {
            using (var writer = new TSSDEdgeComponentConfigWriter())
            {
                writer.WriteToIni(config);
            }
        }

        private void PrintNonStandardEdgeComponentToCad(List<EdgeComponentExtractInfo> infos)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var nonStandardLayer = ThImportTemplateStyleService.NonStandardEdgeComponentLayer;
                acadDb.Database.CreateAILayer(nonStandardLayer, 255);
                infos.Where(o => o.EdgeComponent != null)
                    .ForEach(o =>
                    {
                        var clone = o.EdgeComponent.Clone() as Polyline;
                        clone.Layer = nonStandardLayer;
                        clone.ColorIndex = (int)ColorIndex.BYLAYER;
                        clone.LineWeight = LineWeight.ByLayer;
                        clone.Linetype = "Bylayer";
                    });
            }
        }

        public void SetWallColumnLayer()
        {
            var layer = SelectEntityLayer();
            DrawModel.WallColumnLayer = AddLayer(DrawModel.WallColumnLayer,layer);
        }
        public void SetTextLayer()
        {
            var layer = SelectEntityLayer();
            DrawModel.TextLayer = AddLayer(DrawModel.TextLayer, layer);
        }
        public void SetWallLayer()
        {
            var layer = SelectEntityLayer();
            DrawModel.WallLayer = AddLayer(DrawModel.WallLayer, layer);
        }
        public void ShowComponentFrames()
        {
            if(ComponentBoundaries!=null && ComponentBoundaries.Count>0)
            {
                AddToTransient(ComponentBoundaries.Last());
            }
        }
        public void RemoveComponentFrames()
        {
            if(ComponentBoundaries!=null && ComponentBoundaries.Count > 0)
            {
                ComponentBoundaries.ForEach(o => ClearTransientGraphics(o));
            }
        }
        private string AddLayer(string originLayer,string newAdd)
        {
            if (string.IsNullOrEmpty(newAdd))
            {
                return originLayer;
            }
            var splitStrs = Split(originLayer, "、");
            if(splitStrs.Contains(newAdd))
            {
                return string.Join("、", splitStrs);
            }
            else
            {
                splitStrs.Add(newAdd);
                return string.Join("、", splitStrs);
            }
        }
        private List<string> Split(string content,string splitChar)
        {
            var chars = new string[] { splitChar };
            var splitStrs = content.Split(chars,StringSplitOptions.RemoveEmptyEntries);
            var results = new List<string>();
            foreach (string str in splitStrs)
            {
                results.Add(str.Trim());
            }
            return results;
        }
        private string SelectEntityLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                while (true)
                {
                    var per = Active.Editor.GetEntity("\n选择一个对象");
                    if(per.Status==Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                    {
                        var entity = acadDb.Element<Entity>(per.ObjectId);
                        return entity.Layer;
                    }
                    else if(per.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.Cancel)
                    {
                        break;
                    }
                }
                return "";
            }
        }          
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        private void AddToTransient(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                var intCol = new IntegerCollection();
                objs.OfType<Entity>()
                    .ForEach(e =>
                {
                    e.ColorIndex = 200;
                    e.LineWeight = LineWeight.LineWeight030;
                    tm.AddTransient(e, cadGraph.TransientDrawingMode.
                        Highlight, 1, intCol);
                });
            }
        }
        private void ClearTransientGraphics(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                objs.OfType<Entity>()
                    .ForEach(e =>
                    {
                        tm.EraseTransient(e, intCol);
                    });
            }
        }
    }
}
