using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Command;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;
using Dreambuild.AutoCAD;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;

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
        public EdgeComponentDrawVM()
        {
            DrawModel = new EdgeComponentDrawModel();
            DwgSources = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.DwgSources);
            SortWays = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.SortWays);
            LeaderTypes = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.LeaderTypes);
            MarkPositions = new ObservableCollection<string>(ThEdgeComponentDrawConfig.Instance.MarkPositions);
            EdgeComponents = new ObservableCollection<EdgeComponentExtractInfo>();
            if(ComponentBoundaries==null)
            {
                ComponentBoundaries = new List<DBObjectCollection>();
            }  
            else
            {
                RemoveComponentFrames();
            }
            GroupResults = new List<List<EdgeComponentExtractInfo>>();
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
                if(cmd.IsSuccess)
                {
                    Clear();
                    cmd.ExtractInfos.ForEach(o => EdgeComponents.Add(o));
                    var obbs = cmd.ExtractInfos
                        .Select(o => o.EdgeComponent)
                        .ToCollection()
                        .GetObbFrames();
                    RemoveComponentFrames();
                    ComponentBoundaries.Add(obbs);
                    ShowComponentFrames();
                }
            };
        }
        public void Clear()
        {
            EdgeComponents = new ObservableCollection<EdgeComponentExtractInfo>();
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
                for(int i=0;i< items.Count;i++)
                {
                    items[i].Number = group.Key + index;
                    index++;
                    EdgeComponents.Add(items[i]);
                }
            }
            // 把编号再赋值给同组的其它成员
            foreach(var info in EdgeComponents)
            {
                GroupResults.ForEach(g =>
                {
                    if (g.Contains(info))
                    {
                        g.ForEach(o => o.Number = info.Number);
                    }
                });
            }
        }
        public void Draw()
        {
            using (var cmd = new ThReinforceDrawCmd())
            {
                cmd.ExtractInfos = EdgeComponents.ToList();
                cmd.ExtractInfoGroups = GroupResults;
                DrawModel.SetConfig(); // 把参数传递配置中
                cmd.Execute();
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
