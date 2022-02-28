using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.Reinforcement.Data.YJK;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class EdgeComponentDrawVM
    {
        public ObservableCollection<EdgeComponentInfo> EdgeComponents { get; set; }
        public EdgeComponentDrawModel DrawModel { get; set; }
        public ObservableCollection<string> DwgSources { get; set; }
        public ObservableCollection<string> SortWays { get; set; }
        public ObservableCollection<string> LeaderTypes { get; set; }
        public ObservableCollection<string> MarkPositions { get; set; }
        public EdgeComponentDrawVM()
        {
            DrawModel = new EdgeComponentDrawModel();
            DwgSources = new ObservableCollection<string>() { "YJK" };
            SortWays = new ObservableCollection<string>() { "从左到右，从下到上" };
            LeaderTypes = new ObservableCollection<string>() { "折现引出" };
            MarkPositions = new ObservableCollection<string>() { "右上", "右下", "左上", "左下" };
            EdgeComponents = new ObservableCollection<EdgeComponentInfo>();
        }
        private EdgeComponentInfo Create(string number, string spec, string shape, string type, double reinforceRatio, double stirrupRatio)
        {
            return new EdgeComponentInfo()
            {
                Number = number,
                Spec = spec,
                Shape = shape,
                Type = type,
                StirrupRatio = stirrupRatio,
                ReinforceRatio = reinforceRatio,
            };
        }
        public void Select()
        {
            //ToDO
        }
        public void Clear()
        {
            EdgeComponents = new ObservableCollection<EdgeComponentInfo>();
        }
        public void Merge()
        {
            //TODO
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
