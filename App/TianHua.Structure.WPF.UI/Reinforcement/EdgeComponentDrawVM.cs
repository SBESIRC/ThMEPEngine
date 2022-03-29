using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Command;
using ThMEPStructure.Reinforcement.Model;
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
        public EdgeComponentDrawVM()
        {
            DrawModel = new EdgeComponentDrawModel();
            DwgSources = new ObservableCollection<string>() { "YJK" };
            SortWays = new ObservableCollection<string>() { "从左到右，从下到上" };
            LeaderTypes = new ObservableCollection<string>() { "折现引出" };
            MarkPositions = new ObservableCollection<string>() { "右上", "右下", "左上", "左下" };
            EdgeComponents = new ObservableCollection<EdgeComponentExtractInfo>();
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
                }
            };
        }
        public void Clear()
        {
            EdgeComponents = new ObservableCollection<EdgeComponentExtractInfo>();
        }
        public void Merge()
        {
            var infos = new List<EdgeComponentExtractInfo>();
            for(int i =0;i< EdgeComponents.Count;i++)
            {
                infos.Add(EdgeComponents[i]);
            }
            var grouper = new ThDataGroupService(DrawModel.IsConsiderWall,
                DrawModel.StirrupRatio, DrawModel.ReinforceRatio);
            var results = grouper.Group(infos);
            var groups = results
                .Where(o => o.Count > 0).Select(o => o.First())
                .GroupBy(o => o.NumberPrefix)
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
        }
        public void Draw()
        {
            //TODO

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
    }
}
