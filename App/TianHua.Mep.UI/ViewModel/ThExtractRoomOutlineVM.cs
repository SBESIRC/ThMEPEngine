﻿using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using ThMEPEngineCore;
using ThMEPWSS.Command;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.Command;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractRoomOutlineVM
    {
        private readonly string AIWallLayer = "AI-DB墙线";
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }
        public ThExtractRoomOutlineVM()
        {
            LayerInfos = new ObservableCollection<ThLayerInfo>(LoadLayers());
        }
        public void ExtractWalls()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var cmd = new ThExtractWallLinesCmd(GetLayers()))
            {
                SetFocusToDwgView();
                cmd.Execute();
                PrintWallLines(cmd.Walls);
            }
        }
        public void BuildRoomOutline()
        {
            var wallLines = GetWallLines();
            SetFocusToDwgView();
            if (wallLines.Count==0)
            {                
                CommandHandlerBase.ExecuteFromCommandLine(false, "THKJSQ");
            }
            else
            {
                using (var docLock = Active.Document.LockDocument())
                using (var cmd = new ThPickRoomCmd(wallLines))
                {
                    cmd.Execute();
                }
            }
        }
        public void Confirm()
        {
            SaveLayers();            
        }
        public void SelectLayer()
        {
            var layer = PickUp();
            if(string.IsNullOrEmpty(layer))
            {
                return;
            }
            if(!IsExisted(layer))
            {
                AddLayer(layer);
            }
        }
        private void AddLayer(string layer)
        {
            LayerInfos.Add(new ThLayerInfo()
            {
                Layer =layer,
                IsSelected=true,
            });
        }
        private List<string> GetLayers()
        {
            return LayerInfos.Select(o => o.Layer).ToList();
        }
        private bool IsExisted(string layer)
        {
            return LayerInfos.Where(o => o.Layer == layer).Any();
        }
        private string PickUp()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                var pneo = new PromptNestedEntityOptions("\n请选择幕墙线:");
                var pner = Active.Editor.GetNestedEntity(pneo);
                if (pner.Status == PromptStatus.OK)
                {
                    if (pner.ObjectId != ObjectId.Null)
                    {
                        var entity = acdb.Element<Entity>(pner.ObjectId);
                        if (entity is Curve)
                        {
                            return entity.Layer;
                        }
                    }
                }
                return "";
            }
        }
        private void PrintWallLines(DBObjectCollection walls)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.CreateAILayer(AIWallLayer, 7);
                acadDb.Database.OpenAILayer(AIWallLayer);
                walls.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.Layer = AIWallLayer;
                    e.ColorIndex = (int)ColorIndex.BYLAYER;
                    e.LineWeight = LineWeight.ByLayer;
                    e.Linetype = "ByLayer";
                });
            }
        }
        private DBObjectCollection GetWallLines()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                return acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(e => e is Curve)
                    .Where(e => e.Layer == AIWallLayer)
                    .ToCollection();
            }
        }
        private List<ThLayerInfo> LoadLayers()
        {
            // 存在于DB中的
            var results = FilterLayers(ThExtratRoomOutlineConfig.Instance.LayerInfos);
            results = Sort(results);
            return results;
        }
        private List<ThLayerInfo> Sort(List<ThLayerInfo> infos)
        {
            // 把选中的放前面，再按名称排名
            var results = new List<ThLayerInfo>();
            var selected = infos.Where(o => o.IsSelected).ToList();
            var unSelected = infos.Where(o => !o.IsSelected).ToList();
            results.AddRange(selected.OrderBy(o => o.Layer));
            results.AddRange(unSelected.OrderBy(o => o.Layer));
            return results;
        }
        private void SaveLayers()
        {
            // 保存LayerInfos结果
            var results = new List<ThLayerInfo>();
            results.AddRange(LayerInfos);
            ThExtratRoomOutlineConfig.Instance.LayerInfos = FilterLayers(results);
        }
        private List<ThLayerInfo> FilterLayers(List<ThLayerInfo> layerInfos)
        {
            // existed in current database
            using (var acdb = AcadDatabase.Active())
            {
                return layerInfos.Where(o => acdb.Layers.Contains(o.Layer)).ToList();
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

    public class ThExtratRoomOutlineConfig
    {
        private static readonly ThExtratRoomOutlineConfig instance = new ThExtratRoomOutlineConfig() { };
        public static ThExtratRoomOutlineConfig Instance { get { return instance; } }
        internal ThExtratRoomOutlineConfig()
        {
            LayerInfos= new List<ThLayerInfo>();
        }
        static ThExtratRoomOutlineConfig()
        {
        }
        public List<ThLayerInfo> LayerInfos { get; set; }
    }
}
