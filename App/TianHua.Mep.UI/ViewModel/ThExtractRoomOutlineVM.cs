using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using ThMEPEngineCore;
using ThMEPWSS.Command;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Mep.UI.Command;
using ThMEPEngineCore.Model.Common;
using ThControlLibraryWPF.ControlUtils;
using ThCADExtension;
using AcHelper.Commands;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractRoomOutlineVM : NotifyPropertyChangedBase
    {
        private const string AIWallLayer = "AI-墙线";
        private const string AIShearWallLayer = "AI-剪力墙";
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }
        private bool ynExtractShearWall;
        public bool YnExtractShearWall
        {
            get => ynExtractShearWall;
            set
            {
                if (value != ynExtractShearWall)
                {
                    ynExtractShearWall = value;
                    OnPropertyChanged(nameof(YnExtractShearWall));
                }
            }
        }
        public ThExtractRoomOutlineVM()
        {
            LayerInfos = new ObservableCollection<ThLayerInfo>(LoadLayers());
            ynExtractShearWall = ThExtratRoomOutlineConfig.Instance.YnExtractShearWall;
        }
        public void ExtractRoomDatas()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var cmd = new ThExtractRoomDataCmd(GetLayers()))
            {
                cmd.YnExtractShearWall = YnExtractShearWall;
                SetFocusToDwgView();
                cmd.Execute();
                if (cmd.RangePts.Count>=3)
                {
                    Active.Database.CreateAILayer(AIWallLayer, 7);
                    EraseEntities(cmd.RangePts, AIWallLayer);
                    PrintEntities(cmd.Walls, AIWallLayer);

                    Active.Database.CreateAIColumnLayer();
                    EraseEntities(cmd.RangePts, ThMEPEngineCoreLayerUtils.COLUMN);
                    PrintEntities(cmd.Columns, ThMEPEngineCoreLayerUtils.COLUMN);

                    Active.Database.CreateAIDoorLayer();
                    EraseEntities(cmd.RangePts, ThMEPEngineCoreLayerUtils.DOOR);
                    PrintEntities(cmd.Doors, ThMEPEngineCoreLayerUtils.DOOR);

                    Active.Database.CreateAIShearWallLayer();
                    EraseEntities(cmd.RangePts, ThMEPEngineCoreLayerUtils.SHEARWALL);
                    PrintEntities(cmd.ShearWalls, ThMEPEngineCoreLayerUtils.SHEARWALL);

                    SetCurrentLayer(AIWallLayer);
                }
            }
        }
        public void BuildRoomOutline()
        {
            var roomDatas = GetRoomDataFromMS();
            SuperBoundary(roomDatas);
        }
        public void BuildDoors()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var cmd = new ThBuildDoorsCmd(AIWallLayer, AIShearWallLayer, ThMEPEngineCoreLayerUtils.DOOR, ThMEPEngineCoreLayerUtils.COLUMN))
            {
                SetFocusToDwgView();
                cmd.Execute();
                Active.Database.CreateAIDoorLayer();
                PrintEntities(cmd.doors, ThMEPEngineCoreLayerUtils.DOOR);
                //Active.Editor.Regen();
            }
        }
        private DBObjectCollection GetRoomDataFromMS()
        {
            var roomDatas = new DBObjectCollection();
            var walls = GetEntitiesFromMS(AIWallLayer);
            var doors = GetEntitiesFromMS(ThMEPEngineCoreLayerUtils.DOOR);
            var columns = GetEntitiesFromMS(ThMEPEngineCoreLayerUtils.COLUMN);
            var shearWalls = GetEntitiesFromMS(ThMEPEngineCoreLayerUtils.SHEARWALL);
            roomDatas = roomDatas.Union(walls);
            roomDatas = roomDatas.Union(doors);
            roomDatas = roomDatas.Union(columns);
            roomDatas = roomDatas.Union(shearWalls);
            return roomDatas;
        }

        private void SuperBoundary(DBObjectCollection roomDatas)
        {
            if (roomDatas.Count==0)
            {
                return;
            }
            else
            {
                using (var docLock = Active.Document.LockDocument())
                using (var cmd = new ThSuperBoundaryCmd(roomDatas))
                {
                    SetFocusToDwgView();
                    cmd.Execute();
                }
            }
        }
        public void BlockConfig()
        {
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THWTKSB");
        }
        public void Confirm()
        {
            SaveLayers();
            ThExtratRoomOutlineConfig.Instance.YnExtractShearWall = ynExtractShearWall;
        }
        public void SelectLayer()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                while(true)
                {
                    var pneo = new PromptNestedEntityOptions("\n请选择墙体线:");
                    var pner = Active.Editor.GetNestedEntity(pneo);
                    if (pner.Status == PromptStatus.OK)
                    {
                        if (pner.ObjectId != ObjectId.Null)
                        {
                            var entity = acdb.Element<Entity>(pner.ObjectId);
                            if (entity is Curve || entity is Mline)
                            {
                                var sameSuffixLayers = GetSameSuffixLayers(entity.Layer);
                                sameSuffixLayers.ForEach(layer =>
                                {
                                    if (!IsExisted(layer))
                                    {
                                        AddLayer(layer);
                                    }
                                });
                            }
                        }
                    }
                    else if(pner.Status == PromptStatus.Cancel)
                    {
                        break;
                    }
                }
            }
        }

        private List<string> GetSameSuffixLayers(string layer)
        {
            using (var acdb = AcadDatabase.Active())
            {
                var suffix= ThMEPXRefService.OriginalFromXref(layer).ToUpper();
                return acdb.Layers
                    .Where(o =>
                    {
                        var currentSuffix = ThMEPXRefService.OriginalFromXref(o.Name).ToUpper();
                        return suffix == currentSuffix;
                    })
                    .Select(o => o.Name)
                    .Distinct()
                    .ToList();
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
        private List<string> GetAEWallLayers()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o => IsAEWallLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private bool IsAWallLayer(string layer)
        {
            //以A-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("A-WALL");
        }
        private bool IsAEWallLayer(string layer)
        {
            //以AE-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("AE-WALL");
        }
        private bool IsExisted(string layer)
        {
            return LayerInfos.Where(o => o.Layer == layer).Any();
        }
        private void PrintEntities(DBObjectCollection walls, string layer)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                walls.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.Layer = layer;
                    e.ColorIndex = (int)ColorIndex.BYLAYER;
                    e.LineWeight = LineWeight.ByLayer;
                    e.Linetype = "ByLayer";
                });
            }
        }

        private void EraseEntities(Point3dCollection pts, string layer)
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

        private DBObjectCollection GetEntitiesFromMS(string layer)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                return acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(e => e is Curve || e is MPolygon)
                    .Where(e => e.Layer == layer)
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

            var aeWallLayers = GetAEWallLayers().Select(o => new ThLayerInfo()
            {
                Layer = o,
                IsSelected = true,
            }).ToList();

            // 存在于DB中的
            var storeInfos = FilterLayers(ThExtratRoomOutlineConfig.Instance.LayerInfos);
            storeInfos = storeInfos.Where(o => !aWallLayers.Select(s => s.Layer).Contains(o.Layer)).ToList();
            storeInfos = storeInfos.Where(o => !aeWallLayers.Select(s => s.Layer).Contains(o.Layer)).ToList();

            var results = new List<ThLayerInfo>();
            results.AddRange(aWallLayers);
            results.AddRange(aeWallLayers);
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
