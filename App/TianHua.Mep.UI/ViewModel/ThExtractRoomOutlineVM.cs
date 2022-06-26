using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using ThMEPEngineCore;
using ThMEPWSS.Command;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Mep.UI.Command;
using ThMEPEngineCore.Model.Common;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractRoomOutlineVM
    {
        private readonly string AIWallLayer = "AI-墙线";
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }
        public bool YnExtractShearWall { get; set; }
        public ThExtractRoomOutlineVM()
        {            
            LayerInfos = new ObservableCollection<ThLayerInfo>(LoadLayers());
            YnExtractShearWall = ThExtratRoomOutlineConfig.Instance.YnExtractShearWall;
        }
        public void ExtractWalls()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var cmd = new ThExtractWallLinesCmd(GetLayers()))
            {
                cmd.YnExtractShearWall = YnExtractShearWall;
                SetFocusToDwgView();                
                cmd.Execute();
                CreateAILayer(AIWallLayer, 7);
                if (cmd.RangePts.Count>=3 && cmd.Walls.Count>0)
                {
                    EraseWallLines(cmd.RangePts, AIWallLayer);
                }               
                PrintWallLines(cmd.Walls, AIWallLayer);
                SetCurrentLayer(AIWallLayer);
                Active.Editor.Regen();
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
            ThExtratRoomOutlineConfig.Instance.YnExtractShearWall = YnExtractShearWall;
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
        public void RemoveLayers(List<string> layers)
        {
            if (layers.Count > 0)
            {
                var layerInfos = LayerInfos
                .OfType<ThLayerInfo>()
                .Where(o => !layers.Contains(o.Layer))
                .ToList();
                LayerInfos = new ObservableCollection<ThLayerInfo>(layerInfos);
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
        private List<string> GetAWallLayers()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o => IsAWallLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private bool IsAWallLayer(string layer)
        {
            //以A-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("A-WALL");
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
                var pneo = new PromptNestedEntityOptions("\n请选择墙体线:");
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
        private void PrintWallLines(DBObjectCollection walls,string layer)
        {
            using (var acadDb = AcadDatabase.Active())
            {
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

        private void EraseWallLines(Point3dCollection pts,string layer)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if (!acadDb.Layers.Contains(layer))
                {
                    return;
                }
                acadDb.Database.OpenAILayer(layer);
                var objs = acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(c => c.Layer == layer)
                    .ToCollection();
                var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(objs, true);
                objs = spatialIndex.SelectCrossingPolygon(pts);
                objs.OfType<Entity>().ForEach(c =>
                {
                    var entity = acadDb.Element<Entity>(c.ObjectId, true);
                    entity.Erase();
                });
            }
        }

        private DBObjectCollection GetWallLines()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                return acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(e => e is Curve || e is MPolygon)
                    .Where(e => e.Layer == AIWallLayer)
                    .ToCollection();
            }
        }

        private List<ThLayerInfo> LoadLayers()
        {            
            // 优先获取以A_WALL结尾的梁
            var aWallLayers = GetAWallLayers().Select(o => new ThLayerInfo()
            {
                Layer = o,
                IsSelected = true,
            }).ToList();
            
            // 存在于DB中的
            var storeInfos = FilterLayers(ThExtratRoomOutlineConfig.Instance.LayerInfos);
            storeInfos = storeInfos.Where(o => !aWallLayers.Select(s => s.Layer).Contains(o.Layer)).ToList();

            var results = new List<ThLayerInfo>();
            results.AddRange(aWallLayers);
            results.AddRange(storeInfos);

            //results = Sort(results);
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
        private void CreateAILayer(string layer, short colorIndex)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.CreateAILayer(layer, colorIndex);
                acadDb.Database.OpenAILayer(layer);
            }
        }
        private void SetCurrentLayer(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.SetCurrentLayer(layerName);
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
        public bool YnExtractShearWall { get; set; } = true;
    }
}
