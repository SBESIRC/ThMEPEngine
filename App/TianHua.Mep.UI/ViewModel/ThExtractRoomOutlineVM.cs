using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThControlLibraryWPF.ControlUtils;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.Data;
using TianHua.Mep.UI.Command;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractRoomOutlineVM : NotifyPropertyChangedBase,IDisposable
    {
        private const string AIWallLayer = "AI-墙线";
        private const string AIShearWallLayer = "AI-剪力墙";        
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }
        public ObservableCollection<ThBlockInfo> DoorBlkInfos { get; set; }

        private bool _isShowDoorOpenState = false;
        public bool IsShowDoorOpenState
        {
            get => _isShowDoorOpenState;
            set
            {
                _isShowDoorOpenState = value;
            }
        }

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

        private string _id = "";
        public string Id => _id;
        private DBObjectCollection _doorBlkObbs;
        private Point3dCollection _rangePts;

        public ThExtractRoomOutlineVM()
        {
            LoadFromActiveDatabase();
            if(LayerInfos.Count==0)
            {
                var defaultWallLayers = LoadDefaultWallLayers();
                defaultWallLayers.ForEach(o => LayerInfos.Add(o));
            }
            _id = Guid.NewGuid().ToString();
            _rangePts = new Point3dCollection();
            _doorBlkObbs = new DBObjectCollection();
        }
        public void Dispose()
        {
            _doorBlkObbs.MDispose();
        }
        public void ExtractRoomDatas()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                using (var lockDoc = Active.Document.LockDocument())
                using (ThExtractRoomDataCmd cmd = new ThExtractRoomDataCmd(GetLayers()))
                {
                    cmd.YnExtractShearWall = YnExtractShearWall;
                    SetFocusToDwgView();
                    cmd.Execute();
                    _rangePts = cmd.RangePts;
                    if (cmd.RangePts.Count >= 3)
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
        }
        public void BuildRoomOutline()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                //借助于SuperBoundary的SBND_PICK命令
                using (var docLock = Active.Document.LockDocument())
                {
                    // 0、创建图层（把显示的区域打印到此图层上）
                    Active.Database.CreateAIRoomOutlineLayer();

                    // 1、把房间数据获取到
                    var roomDatas = GetRoomDataFromMS();
                    if (roomDatas.Count == 0)
                    {
                        return;
                    }

                    // 2、再显示已存在的房间区域
                    var roomAreaIds = ShowExistedRoomAreas(Active.Database);

                    // 3、构建房间区域
                    using (var cmd = new ThSuperBoundaryCmd(roomDatas))
                    {
                        SetFocusToDwgView();
                        cmd.Execute();
                    }

                    // 4、删除显示的房间区域
                    Erase(Active.Database, roomAreaIds);
                }
            }
        }
        public void BuildRoomOutline1()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                //借助于SuperBoundary的SBND_ALL命令
                using (var docLock = Active.Document.LockDocument())
                {
                    // 选取范围
                    var pts = ThAuxiliaryUtils.GetRange();

                    // 0、获取房间名称
                    var roomNameTexts = GetRoomNames(Active.Database, pts);
                    if (roomNameTexts.Count == 0)
                    {
                        MessageBox.Show("未找到任何的房间名称，无法生成房间框线!", "信息提示"
                                , MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 1、创建图层（把显示的区域打印到此图层上）
                    Active.Database.CreateAIRoomOutlineLayer();

                    // 2、把房间数据获取到
                    var roomDatas = GetRoomDataFromMS(pts);
                    if (roomDatas.Count == 0)
                    {
                        MessageBox.Show("未获取到任何的墙线元素，无法生成房间框线！", "信息提示"
                            , MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    //// 3、再显示已存在的房间区域
                    //var roomAreaIds = ShowExistedRoomAreas(Active.Database);

                    // 4、构建房间区域
                    using (var cmd = new ThSuperBoundaryCmd(roomDatas, roomNameTexts))
                    {
                        SetFocusToDwgView();
                        cmd.Execute();
                    }

                    //// 5、删除显示的房间区域
                    //Erase(Active.Database,roomAreaIds);

                    // 6、释放房间名称
                    roomNameTexts.OfType<Entity>()
                        .Where(o => o.ObjectId != ObjectId.Null)
                        .ToCollection().MDispose();
                }
            }
        }
        public void BuildDoors()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                var doorBlkNames = DoorBlkInfos.Select(o => o.Name).ToList();
                using (var lockDoc = Active.Document.LockDocument())
                using (var cmd = new ThBuildDoorsCmd(
                    AIWallLayer, AIShearWallLayer,
                    ThMEPEngineCoreLayerUtils.DOOR,
                    ThMEPEngineCoreLayerUtils.COLUMN, doorBlkNames))
                {
                    SetFocusToDwgView();
                    cmd.Execute();
                    Active.Database.CreateAIDoorLayer();
                    PrintEntities(cmd.doors, ThMEPEngineCoreLayerUtils.DOOR);
                    //Active.Editor.Regen();
                }
            }
        }
        public void SaveToDatabase()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                using (var lockDoc = Active.Document.LockDocument())
                using (var acadDb = AcadDatabase.Active())
                {
                    var extractRoomConfigNamedDictId = acadDb.Database.GetNamedDictionary(ThConfigDataTool.ExtractRoomNamedDictKey);
                    if (extractRoomConfigNamedDictId == ObjectId.Null)
                    {
                        extractRoomConfigNamedDictId = acadDb.Database.AddNamedDictionary(ThConfigDataTool.ExtractRoomNamedDictKey);
                    }
                    // 保存墙图层
                    var wallTvs = new TypedValueList();
                    LayerInfos.ForEach(o => wallTvs.Add(DxfCode.ExtendedDataAsciiString, o.Layer));
                    extractRoomConfigNamedDictId.UpdateXrecord(ThConfigDataTool.WallLayerSearchKey, wallTvs);

                    // 保存门图块配置
                    var doorBlkTvs = new TypedValueList();
                    DoorBlkInfos.ForEach(o => doorBlkTvs.Add(DxfCode.ExtendedDataAsciiString, o.Name));
                    extractRoomConfigNamedDictId.UpdateXrecord(ThConfigDataTool.DoorBlkNameConfigSearchKey, doorBlkTvs);

                    // 保存是否提取剪力墙
                    var ynExtractShearWallTvs = new TypedValueList();
                    ynExtractShearWallTvs.Add(DxfCode.Bool, ynExtractShearWall);
                    extractRoomConfigNamedDictId.UpdateXrecord(ThConfigDataTool.YnExtractShearWallSearchKey, ynExtractShearWallTvs);
                    MessageBox.Show("配置已保存到当前图纸中！", "保存提示",MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public void PickWallLayer()
        {
            if(acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                using (var docLock = Active.Document.LockDocument())
                using (var acdb = AcadDatabase.Active())
                {
                    SetFocusToDwgView();
                    while (true)
                    {
                        var pneo = new PromptNestedEntityOptions("\n请选择墙体线:");
                        var pner = Active.Editor.GetNestedEntity(pneo);
                        if (pner.Status == PromptStatus.OK)
                        {
                            if (pner.ObjectId != ObjectId.Null)
                            {
                                var pickedEntity = acdb.Element<Entity>(pner.ObjectId);
                                if (pickedEntity is Curve || pickedEntity is Mline)
                                {
                                    var entityLayerSuffix = ThMEPXRefService.OriginalFromXref(pickedEntity.Layer);
                                    if (entityLayerSuffix != "0")
                                    {
                                        var sameSuffixLayers = GetSameSuffixLayers(entityLayerSuffix);
                                        sameSuffixLayers.ForEach(layer => AddLayer(layer));
                                    }
                                    else
                                    {
                                        var containers = pner.GetContainers();
                                        if (containers.Length > 0)
                                        {
                                            // 如果 pick 到的实体是0图层，就获取其父亲的图层
                                            var parentEntity = acdb.Element<Entity>(containers.First());
                                            entityLayerSuffix = ThMEPXRefService.OriginalFromXref(parentEntity.Layer);
                                            var sameSuffixLayers = GetSameSuffixLayers(entityLayerSuffix);
                                            sameSuffixLayers.ForEach(layer => AddLayer(layer));
                                        }
                                    }
                                }
                            }
                        }
                        else if (pner.Status == PromptStatus.Cancel)
                        {
                            break;
                        }
                    }
                }
            }            
        }
        public void PickDoorBlock()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                using (var docLock = Active.Document.LockDocument())
                using (var acdbDb = AcadDatabase.Active())
                {
                    SetFocusToDwgView();
                    while (true)
                    {
                        var nestedEntOpt = new PromptNestedEntityOptions("\nPick nested entity in block:");
                        Editor ed = Active.Document.Editor;
                        var nestedEntRes = ed.GetNestedEntity(nestedEntOpt);
                        if (nestedEntRes.ObjectId != ObjectId.Null)
                        {
                            var pickedEntity = acdbDb.Element<Entity>(nestedEntRes.ObjectId);
                            if (pickedEntity is BlockReference br)
                            {
                                var blockName = ThMEPXRefService.OriginalFromXref(br.GetEffectiveName());
                                AddDoorBlockInfo(blockName);
                            }
                            else
                            {
                                if (pickedEntity.IsTCHElement())
                                {
                                    System.Windows.MessageBox.Show("当前选择的是天正物体，请重新选择！", "选择提示",
                                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                                    continue;
                                }
                                else
                                {
                                    if (nestedEntRes.GetContainers().Length > 0)
                                    {
                                        var containerId = nestedEntRes.GetContainers().First();
                                        var dbObj2 = acdbDb.Element<Entity>(containerId);
                                        if (dbObj2 is BlockReference br2)
                                        {
                                            var blockName = ThMEPXRefService.OriginalFromXref(br2.GetEffectiveName());
                                            AddDoorBlockInfo(blockName);
                                        }
                                    }
                                }
                            }
                        }
                        else if (nestedEntRes.Status == PromptStatus.Cancel)
                        {
                            break;
                        }
                    }
                }
            }            
        }
        public void ShowDoorOutline()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                using (var docLock = Active.Document.LockDocument())
                {
                    if (_doorBlkObbs.Count == 0)
                    {
                        var doorBlkNames = DoorBlkInfos.Select(o => o.Name).ToList();
                        _doorBlkObbs = ThConfigDataTool.GetDoorZones(Active.Database, _rangePts, doorBlkNames);
                    }
                    ClearTransientGraphics(_doorBlkObbs);
                    AddToTransient(_doorBlkObbs);
                    SetFocusToDwgView();
                }
            }            
        }
        public void CloseDoorOutline()
        {
            if (acadApp.Application.DocumentManager.MdiActiveDocument != null)
            {
                using (var docLock = Active.Document.LockDocument())
                {
                    ClearTransientGraphics(_doorBlkObbs);
                    SetFocusToDwgView();
                }
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
        public void RemoveDoorBlocks(List<ThBlockInfo> doorBlkInfos)
        {
            if (doorBlkInfos.Count > 0)
            {
                doorBlkInfos.ForEach(o => DoorBlkInfos.Remove(o));
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
        private DBObjectCollection GetRoomDataFromMS(Point3dCollection pts)
        {
            var roomDatas = GetRoomDataFromMS();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(roomDatas);
            return spatialIndex.SelectCrossingPolygon(pts);
        }
        private DBObjectCollection GetAIRooms(Database db, Point3dCollection pts)
        {
            var builder = new ThRoomBuilderEngine();
            var rooms = builder.BuildFromMS(db, pts, true);
            return rooms.Select(r => r.Boundary).ToCollection();
        }
        private List<string> GetSameSuffixLayers(string suffixLayer)
        {
            using (var acdb = AcadDatabase.Active())
            {
                var suffix= suffixLayer.ToUpper();
                return acdb.Layers
                    .Where(o => ThMEPXRefService.OriginalFromXref(o.Name).ToUpper() == suffix)
                    .Select(o => o.Name).Distinct().ToList();
            }            
        }        
        private void AddLayer(string layer)
        {
            if(!IsExisted(layer))
            {
                LayerInfos.Add(new ThLayerInfo()
                {
                    Layer = layer,
                    IsSelected = true,
                });
            }
        }
        private void AddDoorBlockInfo(string name)
        {
            if(!IsDoorBlkExisted(name))
            {
                DoorBlkInfos.Add(new ThBlockInfo()
                {
                    Name = name,
                    IsSelected = true,
                });
            }
        }
        private List<string> GetLayers()
        {
            return LayerInfos.Select(o => o.Layer).ToList();
        }
        private List<string> GetAWallLayers()
        {
            if (acadApp.Application.DocumentManager.Count > 0)
            {
                using (var acdb = AcadDatabase.Active())
                {
                    return acdb.Layers
                        .Where(o => IsAWallLayer(o.Name))
                        .Select(o => o.Name)
                        .ToList();
                }
            }
            else
            {
                return new List<string>();
            }
        }
        private List<string> GetAEWallLayers()
        {
            if(acadApp.Application.DocumentManager.Count > 0)
            {
                using (var acdb = AcadDatabase.Active())
                {
                    return acdb.Layers
                        .Where(o => IsAEWallLayer(o.Name))
                        .Select(o => o.Name)
                        .ToList();
                }
            }
            else
            {
                return new List<string>();
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
        private bool IsDoorBlkExisted(string name)
        {
            return DoorBlkInfos.Where(o => o.Name == name).Any();
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
        private List<ThLayerInfo> LoadDefaultWallLayers()
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

            var results = new List<ThLayerInfo>();
            results.AddRange(aWallLayers);
            results.AddRange(aeWallLayers);
            return results;
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
        private void LoadFromActiveDatabase()
        {
            this.ynExtractShearWall = false;
            this.DoorBlkInfos = new ObservableCollection<ThBlockInfo>();
            this.LayerInfos = new ObservableCollection<ThLayerInfo>();
            // 从当前database获取图层
            if (acadApp.Application.DocumentManager.MdiActiveDocument!=null)
            {
                using (var acadDb = AcadDatabase.Active())
                {
                    var extractRoomConfigNamedDictId = acadDb.Database.GetNamedDictionary(ThConfigDataTool.ExtractRoomNamedDictKey);
                    if (extractRoomConfigNamedDictId != ObjectId.Null)
                    {
                        var wallTvs = extractRoomConfigNamedDictId.GetXrecord(ThConfigDataTool.WallLayerSearchKey);
                        if(wallTvs!=null)
                        {
                            foreach (TypedValue tv in wallTvs)
                            {
                                this.LayerInfos.Add(new ThLayerInfo { Layer = tv.Value.ToString() });
                            }
                        }
                       
                        var doorBlkTvs = extractRoomConfigNamedDictId.GetXrecord(ThConfigDataTool.DoorBlkNameConfigSearchKey);
                        if(doorBlkTvs!=null)
                        {
                            foreach (TypedValue tv in doorBlkTvs)
                            {
                                this.DoorBlkInfos.Add(new ThBlockInfo { Name = tv.Value.ToString()});
                            }
                        }                        

                        var ynExtractShearWallTvs = extractRoomConfigNamedDictId.GetXrecord(ThConfigDataTool.YnExtractShearWallSearchKey);
                        if(ynExtractShearWallTvs!=null && ynExtractShearWallTvs.Count == 1)
                        {
                            this.ynExtractShearWall = (short)ynExtractShearWallTvs[0].Value==1;
                        }                        
                    }                   
                }
            }
        }
        private void AddToTransient(DBObjectCollection curves)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                curves.OfType<Curve>().ForEach(o =>
                {
                    tm.AddTransient(o, cadGraph.TransientDrawingMode.Highlight, 1, intCol);
                });
            }
        }
        private void ClearTransientGraphics(DBObjectCollection curves)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                curves.OfType<Curve>().ForEach(o =>
                {
                    tm.EraseTransient(o, intCol);
                });
            }
        }
        private void Erase(Database database, ObjectIdCollection objIds)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                objIds
                    .OfType<ObjectId>()
                    .Where(o => !o.IsNull && !o.IsErased && o.IsValid)
                    .Select(o => acadDb.Element<Entity>(o, true))
                    .ForEach(o => o.Erase());
            }
        }
        private ObjectIdCollection ShowExistedRoomAreas(Database database)
        {
            // 把已有的房间框线显示出来
            using (var acadDb = AcadDatabase.Use(database))
            {
                var results = new ObjectIdCollection();
                var rooms = GetAIRooms(Active.Database, _rangePts);
                rooms.OfType<Entity>().ForEach(e =>
                {
                    if (e is Polyline poly)
                    {
                        var ids = Show(acadDb, poly, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                        ids.OfType<ObjectId>().ForEach(o => results.Add(o));
                    }
                    else if (e is MPolygon polygon)
                    {
                        var ids = Show(acadDb, polygon, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                        ids.OfType<ObjectId>().ForEach(o => results.Add(o));
                    }
                });
                return results;
            }
        }
        private ObjectIdCollection Show(AcadDatabase acadDb, MPolygon polygon, string layer)
        {
            var results = new ObjectIdCollection();
            var shell = polygon.Shell();
            var holes = polygon.Holes();
            var shellId = AddToDatabase(acadDb, shell, layer);
            var holeIds = new ObjectIdCollection();
            holes.ForEach(h => holeIds.Add(AddToDatabase(acadDb, h, layer)));

            Hatch oHatch = new Hatch();
            oHatch.HatchObjectType = HatchObjectType.HatchObject;
            oHatch.Normal = Vector3d.ZAxis;
            oHatch.Elevation = 0.0;
            //oHatch.PatternAngle = config.PatternAngle;
            oHatch.PatternScale = 1.0;
            //oHatch.PatternSpace = config.PatternSpace;
            oHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            oHatch.ColorIndex = (int)ColorIndex.BYLAYER;
            oHatch.Layer = layer;
            var hatchId = acadDb.ModelSpace.Add(oHatch);
            oHatch.Associative = true;
            if (holes.Count == 0)
            {
                oHatch.AppendLoop((int)HatchLoopTypes.Default,
                    new ObjectIdCollection { shellId });
            }
            else
            {
                oHatch.AppendLoop(HatchLoopTypes.Outermost,
                        new ObjectIdCollection { shellId });
                holeIds.OfType<ObjectId>().ForEach(o =>
                {
                    oHatch.AppendLoop(HatchLoopTypes.Default,
                        new ObjectIdCollection { o });
                });
            }
            oHatch.EvaluateHatch(true);
            results.Add(hatchId);

            results.Add(shellId);
            holeIds.OfType<ObjectId>().ForEach(o => results.Add(o));
            return results;
        }
        private ObjectIdCollection Show(AcadDatabase acadDb, Polyline polyline, string layer)
        {
            var objIds = new ObjectIdCollection();
            objIds.Add(AddToDatabase(acadDb, polyline, layer));

            Hatch oHatch = new Hatch();
            oHatch.HatchObjectType = HatchObjectType.HatchObject;
            oHatch.Normal = Vector3d.ZAxis;
            oHatch.Elevation = 0.0;
            //oHatch.PatternAngle = config.PatternAngle;
            oHatch.PatternScale = 1.0;
            //oHatch.PatternSpace = config.PatternSpace;
            oHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            oHatch.ColorIndex = 21;
            oHatch.Layer = layer;
            //oHatch.Transparency = new Autodesk.AutoCAD.Colors.Transparency(77); //30%
            var hatchId = acadDb.ModelSpace.Add(oHatch);
            oHatch.Associative = true;
            oHatch.AppendLoop((int)HatchLoopTypes.Default, objIds);
            oHatch.EvaluateHatch(true);

            objIds.Add(hatchId);
            return objIds;
        }
        private ObjectId AddToDatabase(AcadDatabase acadDb, Entity entity, string layer)
        {
            var objId = acadDb.ModelSpace.Add(entity);
            entity.Layer = layer;
            entity.Linetype = "Bylayer";
            entity.LineWeight = LineWeight.ByLayer;
            entity.ColorIndex = 21;
            return objId;
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

        #region ---------- 获取房间名称 ------------
        private DBObjectCollection GetRoomNames(Database database, Point3dCollection pts)
        {
            var results = new DBObjectCollection();
            // 获取门标注->设计师按图层定义的规则
            var tianHuaMarks = RecognizeTianHuaDoorMarks(database, pts);
            results = results.Union(tianHuaMarks);

            // 获取DB门标注
            var dbDoorMarks = RecognizeDBDoorMarks(database, pts);
            results = results.Union(dbDoorMarks);

            // 获取AI门标注
            var aiDoorMarks = RecognizeAIDoorMarks(database, pts);
            results = results.Union(aiDoorMarks);

            return results;
        }

        private DBObjectCollection RecognizeAIDoorMarks(Database database, Point3dCollection pts)
        {
            var aiRoomEngine = new ThAIRoomMarkRecognitionEngine();
            aiRoomEngine.Recognize(database, pts);
            aiRoomEngine.RecognizeMS(database, pts);
            return aiRoomEngine.Elements
                .OfType<ThIfcTextNote>()
                .Select(o => o.Geometry)
                .ToCollection();
        }

        private DBObjectCollection RecognizeTianHuaDoorMarks(Database database, Point3dCollection pts)
        {
            var engine = new ThDB3RoomMarkRecognitionEngine();
            engine.Recognize(database, pts);
            //engine.RecognizeMS(database, pts);
            return engine.Elements
                .OfType<ThIfcTextNote>()
                .Select(o => o.Geometry)
                .ToCollection();
        }

        private DBObjectCollection RecognizeDBDoorMarks(Database database, Point3dCollection pts)
        {
            var doorMarkVisitor = new ThDB3DoorMarkExtractionVisitor()
            {
                LayerFilter = ThDoorMarkLayerManager.XrefLayers(database),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(doorMarkVisitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            var allDbDoorMarks = doorMarkVisitor.Results.Select(o => o.Geometry).ToCollection();
            if (pts.Count > 2)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(allDbDoorMarks);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pts);
                allDbDoorMarks.Difference(filterObjs);
                allDbDoorMarks.MDispose();
                return filterObjs;
            }
            else
            {
                return allDbDoorMarks;
            }
        }
        #endregion
    }
}
