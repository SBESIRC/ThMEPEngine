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
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.Data;
using TianHua.Mep.UI.Command;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Config;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractRoomOutlineVM : NotifyPropertyChangedBase,IDisposable
    {
        private const string AIWallLayer = "AI-墙线";
        private const string AIShearWallLayer = "AI-剪力墙";        
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }
        public ObservableCollection<ThBlockInfo> DoorBlkInfos { get; set; }

        private bool _isShowDoorOpenState = false;
        private bool _isExtractDoorObbs = false;
        public bool IsShowDoorOpenState
        {
            get => _isShowDoorOpenState;
            set
            {
                _isShowDoorOpenState = value;
            }
        }

        private bool ynExtractShearWall =true;
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
            this._isExtractDoorObbs = DoorBlkInfos.Count > 0;
        }
        public void Dispose()
        {
            _doorBlkObbs.MDispose();
        }
        public void ExtractRoomDatas()
        {
            using (var lockDoc = Active.Document.LockDocument())
            {
                SetFocusToDwgView();
                var rangePts = ThAuxiliaryUtils.GetRange(); //获取布置范围
                if (rangePts.Count < 3)
                {
                    return;
                }
                else
                {
                    this._rangePts = rangePts;
                    var config = new ThRoomDataSetConfig()
                    {
                        WallLayers = GetLayers(),
                        YnExtractShearWall = this.YnExtractShearWall,
                        NeibourRangeDistance = 200.0,
                        UseConfigShearWallLayer = ThExtractShearWallConfig.Instance.ShearWallLayerOption== ShearwallLayerConfigOps.LayerConfig,
                    };
                    using (var datasetFactory = new ThRoomDataSetFactory(config))
                    {
                        ThStopWatchService.Start();
                        datasetFactory.Create(Active.Database, rangePts);
                        ThStopWatchService.Stop();
                        ThStopWatchService.Print("提取墙线耗时：");
                        Active.Database.CreateAILayer(AIWallLayer, 7);
                        EraseEntities(rangePts, AIWallLayer);
                        PrintEntities(datasetFactory.Walls, AIWallLayer);

                        Active.Database.CreateAIColumnLayer();
                        EraseEntities(rangePts, ThMEPEngineCoreLayerUtils.COLUMN);
                        PrintEntities(datasetFactory.Columns, ThMEPEngineCoreLayerUtils.COLUMN);

                        Active.Database.CreateAIDoorLayer();
                        EraseEntities(rangePts, ThMEPEngineCoreLayerUtils.DOOR);
                        PrintEntities(datasetFactory.Doors, ThMEPEngineCoreLayerUtils.DOOR);

                        Active.Database.CreateAIShearWallLayer();
                        EraseEntities(rangePts, ThMEPEngineCoreLayerUtils.SHEARWALL);
                        PrintEntities(datasetFactory.ShearWalls, ThMEPEngineCoreLayerUtils.SHEARWALL);
                        PrintEntities(datasetFactory.OtherShearWalls, ThMEPEngineCoreLayerUtils.SHEARWALL);
                        SetCurrentLayer(AIWallLayer);
                    }
                }                
            }
        }
        public void BuildRoomOutline()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                SetFocusToDwgView();

                // 0、创建图层（把显示的区域打印到此图层上）
                Active.Database.CreateAIRoomOutlineLayer();

                // 1、把房间数据获取到
                var roomDatas = GetRoomDataFromMS();
                if (roomDatas.Count == 0)
                {
                    return;
                }

                // 2、再显示已存在的房间区域                
                var existRooms = GetAIRooms(Active.Database, new Point3dCollection());// 这些房间是不能删的
                var roomAreaIds = ShowExistedRoomAreas(Active.Database, existRooms); //用Hatch填充，提示用户已存在的区域

                // 3、构建房间区域
                var newRooms = new DBObjectCollection();
                using (var cmd = new ThSuperBoundaryCmd(roomDatas))
                {
                    cmd.Execute();
                    newRooms = cmd.RoomBoundaries;
                }

                // 4、对新成对房间框线和已生成的房间框线去重
                var repeatedObjs = FilerSimilarObjs(existRooms, newRooms);
                Erase(Active.Database, repeatedObjs.OfType<DBObject>().Select(o=>o.ObjectId).ToCollection());

                // 5、删除显示的房间区域
                Erase(Active.Database, roomAreaIds);
            }
        }
        public void BuildRoomOutline1()
        {
            //借助于SuperBoundary的SBND_ALL命令
            using (var docLock = Active.Document.LockDocument())
            {
                SetFocusToDwgView();

                // 0、选取范围
                var pts = ThAuxiliaryUtils.GetRange();
                if(pts.Count<3)
                {                    
                    return;
                }

                // 1、获取房间名称
                var roomNameTexts = GetRoomNames(Active.Database, pts);
                if (roomNameTexts.Count == 0)
                {
                    MessageBox.Show("未找到任何的房间名称，无法生成房间框线!", "信息提示"
                            , MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var existRooms = GetAIRooms(Active.Database, pts);// 这些房间是不能删的

                // 2、创建图层（把显示的区域打印到此图层上）
                Active.Database.CreateAIRoomOutlineLayer();

                // 3、把房间数据获取到
                var roomDatas = GetRoomDataFromMS(pts);
                if (roomDatas.Count == 0)
                {
                    MessageBox.Show("未获取到任何的墙线元素，无法生成房间框线！", "信息提示"
                        , MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }                

                // 4、构建房间区域
                var newRooms = new DBObjectCollection();
                using (var cmd = new ThSuperBoundaryCmd(roomDatas, roomNameTexts))
                {
                    cmd.Execute();
                    newRooms = cmd.RoomBoundaries;
                }

                // 只保留框内的范围
                var innerRooms = SelectWindowPolygon(newRooms, pts);
                var outerRooms = newRooms.Difference(innerRooms);
                Erase(Active.Database, outerRooms.OfType<DBObject>().Select(o => o.ObjectId).ToCollection());

                // 5、对新成对房间框线和已生成的房间框线去重                
                var repeatedObjs = FilerSimilarObjs(existRooms, innerRooms);
                Erase(Active.Database, repeatedObjs.OfType<DBObject>().Select(o => o.ObjectId).ToCollection());

                // 6、释放房间名称
                roomNameTexts.OfType<Entity>()
                    .Where(o => o.ObjectId != ObjectId.Null)
                    .ToCollection().MDispose();
            }
        }
        public void BuildDoors()
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
        public void SaveToDatabase()
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
                MessageBox.Show("配置已保存到当前图纸中！", "保存提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public void PickWallLayer()
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
        public void PickDoorBlock()
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
        public void ShowDoorOutline()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                ClearTransientGraphics(_doorBlkObbs);
                if (_isExtractDoorObbs)
                {
                    var doorBlkNames = DoorBlkInfos.Select(o => o.Name).ToList();
                    _doorBlkObbs = ThConfigDataTool.GetDoorZones(Active.Database, _rangePts, doorBlkNames);
                }
                AddToTransient(_doorBlkObbs);
                SetFocusToDwgView();
                _isExtractDoorObbs = false;
            }
        }
        public void CloseDoorOutline()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                ClearTransientGraphics(_doorBlkObbs);
                SetFocusToDwgView();
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
                _isExtractDoorObbs = true;
                doorBlkInfos.ForEach(o => DoorBlkInfos.Remove(o));
            }
        }
        private DBObjectCollection FilerSimilarObjs(DBObjectCollection existedRooms,DBObjectCollection newRooms)
        {
            var simpilfer = new ThRoomOutlineSimplifier();
            return simpilfer.OverKill(existedRooms, newRooms);
        }
        private DBObjectCollection SelectWindowPolygon(DBObjectCollection rooms,Point3dCollection pts)
        {
            var frame = pts.CreatePolyline();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(rooms);
            var results = spatialIndex.SelectWindowPolygon(frame);
            frame.Dispose();
            return results;
        }
        private void ShowSBNDRunCmdTip()
        {
            MessageBox.Show("正在运行房间轮廓线生成命令，无法执行当前操作！", "信息提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
            // 获取本地的房间框线
            using (var acadDb =  AcadDatabase.Use(db))
            {
                var objs  = acadDb.ModelSpace.OfType<Entity>()
                    .Where(o => (o is Polyline poly && poly.Closed && poly.Area>1.0)
                    || (o is MPolygon polygon && polygon.Area>1.0))
                    .Where(o => o.Layer == ThMEPEngineCoreLayerUtils.ROOMOUTLINE)
                    .ToCollection();
                if (pts.Count > 2)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    return spatialIndex.SelectCrossingPolygon(pts);
                }
                else
                {
                    return objs;
                }                
            }
        }
        //private DBObjectCollection GetAIRooms(Database db, Point3dCollection pts)
        //{
        //    var builder = new ThRoomBuilderEngine();
        //    var rooms = builder.BuildFromMS(db, pts, true);
        //    return rooms.Select(r => r.Boundary).ToCollection();
        //}
        private List<string> GetSameSuffixLayers(string suffixLayer)
        {
            using (var acdb = AcadDatabase.Active())
            {
                var suffix= suffixLayer.ToUpper();
                return acdb.Layers
                    .Where(o => !(o.IsOff || o.IsFrozen))
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
                _isExtractDoorObbs = true;
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
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o => !(o.IsOff || o.IsFrozen))
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
                    .Where(o => !(o.IsOff || o.IsFrozen))
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
                    if (e.ObjectId==ObjectId.Null && !e.IsDisposed && !e.IsErased)
                    {
                        acadDb.ModelSpace.Add(e);
                        e.Layer = layer;
                        e.ColorIndex = (int)ColorIndex.BYLAYER;
                        e.LineWeight = LineWeight.ByLayer;
                        e.Linetype = "ByLayer";
                    }
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
            this.DoorBlkInfos = new ObservableCollection<ThBlockInfo>();
            this.LayerInfos = new ObservableCollection<ThLayerInfo>();
            // 从当前database获取图层
            using (var acadDb = AcadDatabase.Active())
            {
                var extractRoomConfigNamedDictId = acadDb.Database.GetNamedDictionary(ThConfigDataTool.ExtractRoomNamedDictKey);
                if (extractRoomConfigNamedDictId != ObjectId.Null)
                {
                    var wallTvs = extractRoomConfigNamedDictId.GetXrecord(ThConfigDataTool.WallLayerSearchKey);
                    if (wallTvs != null)
                    {
                        foreach (TypedValue tv in wallTvs)
                        {
                            this.LayerInfos.Add(new ThLayerInfo { Layer = tv.Value.ToString() });
                        }
                    }

                    var doorBlkTvs = extractRoomConfigNamedDictId.GetXrecord(ThConfigDataTool.DoorBlkNameConfigSearchKey);
                    if (doorBlkTvs != null)
                    {
                        foreach (TypedValue tv in doorBlkTvs)
                        {
                            this.DoorBlkInfos.Add(new ThBlockInfo { Name = tv.Value.ToString() });
                        }
                    }

                    var ynExtractShearWallTvs = extractRoomConfigNamedDictId.GetXrecord(ThConfigDataTool.YnExtractShearWallSearchKey);
                    if (ynExtractShearWallTvs != null && ynExtractShearWallTvs.Count == 1)
                    {
                        this.ynExtractShearWall = (short)ynExtractShearWallTvs[0].Value == 1;
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
        private ObjectIdCollection ShowExistedRoomAreas(Database database,DBObjectCollection rooms)
        {
            // 把已有的房间框线显示出来
            using (var acadDb = AcadDatabase.Use(database))
            {
                var results = new ObjectIdCollection();
                rooms.OfType<Entity>().ForEach(e =>
                {
                    if (e is Polyline poly)
                    {
                        var ids = Show(acadDb, poly.Clone() as Polyline, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                        ids.OfType<ObjectId>().ForEach(o => results.Add(o));
                    }
                    else if (e is MPolygon polygon)
                    {
                        var mClone = polygon.Clone() as MPolygon;
                        var ids = Show(acadDb, mClone, ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                        ids.OfType<ObjectId>().ForEach(o => results.Add(o));
                        mClone.Dispose();
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
        // 获取文字
        private DBObjectCollection GetRoomNames(Database database, Point3dCollection pts)
        {
            var results = new DBObjectCollection();
            // 获取DB门标注
            var dbDoorMarks = RecognizeDBRoomMarks(database, pts);
            results = results.Union(dbDoorMarks);

            // 获取AI门标注
            var aiDoorMarks = RecognizeAIRoomMarks(database, pts);
            results = results.Union(aiDoorMarks);

            return results;
        }

        private DBObjectCollection RecognizeAIRoomMarks(Database database, Point3dCollection pts)
        {
            var extractionEngine = new ThAIRoomMarkExtractionEngine();
            extractionEngine.Extract(database);
            extractionEngine.ExtractFromMS(database);
            var newResults = extractionEngine.Results;

            var objs = newResults
                .Select(o => o.Geometry)
                .OfType<Polyline>()
                .Where(o => o.Area >= 1e-6)
                .ToCollection();
            if (pts.Count > 2)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pts);
                newResults = newResults.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }

            var results = new DBObjectCollection();
            newResults.ForEach(o =>
            {
                if (o.Data is DBText dbText)
                {
                    results.Add(dbText);
                }
                else if (o.Data is MText mText)
                {
                    results.Add(mText);
                }
            });
            return results;
        }

        private DBObjectCollection RecognizeDBRoomMarks(Database database, Point3dCollection pts)
        {
            // 只获取文字
            var extractionEngine = new ThDB3RoomMarkExtractionEngine();
            extractionEngine.Extract(database);
            var newResults = extractionEngine.Results;
            var objs = newResults
                .Select(o => o.Geometry)
                .OfType<Polyline>()
                .Where(o=>o.Area>=1e-6)
                .ToCollection();            
            if (pts.Count > 2)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pts);
                newResults = newResults.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }

            var results = new DBObjectCollection();
            newResults.ForEach(o =>
            {
                if (o.Data is DBText dbText)
                {
                    results.Add(dbText);
                }
                else if (o.Data is MText mText)
                {
                    results.Add(mText);
                }
            });
            return results;
        }
        #endregion
    }
}
