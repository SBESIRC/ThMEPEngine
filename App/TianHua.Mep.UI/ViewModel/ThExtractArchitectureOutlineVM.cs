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
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.Data;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Service;

namespace TianHua.Mep.UI.ViewModel
{
    public class ThExtractArchitectureOutlineVM : NotifyPropertyChangedBase,IDisposable
    {
        private const string AIArchAuxiliaryWallLayer = "AI-建筑轮廓辅助线";
        private const double ArchitectureOutlineAreaTolerance = 5000000; //过滤面积A＜5000000的轮廓线
        private const double ArcTesslateLength = 100.0;
        private const double BuildingExpandJointMaximumGapDistance = 300.0; // 相邻建筑轮廓线之间的最大Gap距离
        private Func<DBObjectCollection, DBObjectCollection> BuildArchOutlineMethod;
        private HashSet<DBObject> _garbageCollector;
        public ObservableCollection<ThLayerInfo> LayerInfos { get; set; }

        private string _id = "";
        public string Id => _id;
        private Point3dCollection _rangePts;
        public ThExtractArchitectureOutlineVM()
        {
            LoadFromActiveDatabase();
            if(LayerInfos.Count==0)
            {
                var defaultWallLayers = LoadDefaultWallLayers();
                defaultWallLayers.ForEach(o => LayerInfos.Add(o));
            }
            _id = Guid.NewGuid().ToString();
            _rangePts = new Point3dCollection();
            _garbageCollector = new HashSet<DBObject>();
            BuildArchOutlineMethod = BuildArchOutlineByLineService;
        }
        public void Dispose()
        {
            _garbageCollector.ToCollection().MDispose();
        }
        public void ExtractDatas()
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
                    var config = new ThArchitectureOutlineDataSetConfig()
                    {
                        WallLayers = GetLayers(),                        
                    };
                    using (var datasetFactory = new ThArchitectureOutlineDataSetFactory(config))
                    {
                        ThStopWatchService.Start();
                        datasetFactory.Create(Active.Database, rangePts);
                        ThStopWatchService.Stop();
                        ThStopWatchService.Print("提取墙线耗时：");
                        Active.Database.CreateAILayer(AIArchAuxiliaryWallLayer, 7);
                        EraseEntities(rangePts, AIArchAuxiliaryWallLayer);
                        PrintEntities(datasetFactory.AllLines, AIArchAuxiliaryWallLayer);
                    }
                }                
            }
        }
        public void BuildArchitectureOutline()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                SetFocusToDwgView();

                // 0、获取范围
                var rangePts = ThAuxiliaryUtils.GetRange(); 
                if (rangePts.Count < 3)
                {
                    return;
                }

                // 1、获取建筑轮廓线数据
                var architectureOutlineDatas = GetArchitectureOutlineDataFromMS(rangePts);
                if (architectureOutlineDatas.Count == 0)
                {
                    return;
                }

                // 2、构建建筑轮廓线            
                var archOutlines = BuildArchOutlineMethod(architectureOutlineDatas);
                
                // 3、过滤
                var validArchOutlines = archOutlines.FilterSmallArea(ArchitectureOutlineAreaTolerance);
                var discardArchOutlines = archOutlines.Difference(validArchOutlines);
                AddToGarbageCollector(discardArchOutlines);

                // 4、处理变形缝
                var finalArchOutlines = HandleBuildingExpansionJoint(validArchOutlines, BuildingExpandJointMaximumGapDistance);
                finalArchOutlines = finalArchOutlines.FilterSmallArea(ArchitectureOutlineAreaTolerance);

                // 5、打印
                Active.Database.CreateAIArchOutlineLayer();
                PrintEntities(finalArchOutlines, ThMEPEngineCoreLayerUtils.ARCHOUTLINE);
            }
        }

        public void SaveToDatabase()
        {
            using (var lockDoc = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                var extractArchitectureOutlineConfigNamedDictId = acadDb.Database.GetNamedDictionary(ThConfigDataTool.ExtractArchitectureOutlineNamedDictKey);
                if (extractArchitectureOutlineConfigNamedDictId == ObjectId.Null)
                {
                    extractArchitectureOutlineConfigNamedDictId = acadDb.Database.AddNamedDictionary(ThConfigDataTool.ExtractArchitectureOutlineNamedDictKey);
                }
                // 保存墙图层
                var wallTvs = new TypedValueList();
                LayerInfos.ForEach(o => wallTvs.Add(DxfCode.ExtendedDataAsciiString, o.Layer));
                extractArchitectureOutlineConfigNamedDictId.UpdateXrecord(ThConfigDataTool.WallLayerSearchKey, wallTvs);

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

        private DBObjectCollection HandleBuildingExpansionJoint(DBObjectCollection outlineAreas, double bufferLength)
        {
            //处理建筑伸缩缝
            var transformer = new ThMEPOriginTransformer(outlineAreas);
            var garbageCollector = new HashSet<DBObject>();
            transformer.Transform(outlineAreas);

            var objs1 = Buffer(outlineAreas, bufferLength); // 中间数据
            garbageCollector.UnionWith(objs1.OfType<DBObject>().ToHashSet());
            var objs2 = objs1.UnionPolygons(true); // 中间数据
            garbageCollector.UnionWith(objs2.OfType<DBObject>().ToHashSet());
            var objs3 = Buffer(objs2, -1.0 * bufferLength);           
            var results = new DBObjectCollection();
            foreach (Entity polygon in objs3)
            {
                if(polygon is MPolygon mPolygon)
                {
                    results.Add(mPolygon.Shell());
                    garbageCollector.Add(mPolygon);
                }
                else
                {
                    results.Add(polygon);
                }
            }

            garbageCollector.ExceptWith(results.OfType<DBObject>().ToHashSet());
            garbageCollector.ExceptWith(outlineAreas.OfType<DBObject>().ToHashSet());
            garbageCollector.ToCollection().MDispose();
            transformer.Reset(outlineAreas);
            transformer.Reset(results);          
            return results;
        }

        private DBObjectCollection Buffer(DBObjectCollection polygons,double distance)
        {
            var bufferService = new ThNTSBufferService();
            var results = new DBObjectCollection();
            foreach (Entity polygon in polygons)
            {
                var newPolygon = bufferService.Buffer(polygon, distance);
                if (newPolygon != null)
                {
                    results.Add(newPolygon);
                }
            }
            return results;
        }

        private DBObjectCollection BuildArchOutlineByTotalBoundary(DBObjectCollection architectureOutlineDatas)
        {
            // 因未购买TotalBoundary，此功能暂时不开放
            using (var cmd = new ThTotalBoundaryCmd(architectureOutlineDatas))
            {
                cmd.Execute();
                return cmd.ArchOutlineBoundaries;
            }
        }
        private DBObjectCollection BuildArchOutlineByLineService(DBObjectCollection architectureOutlineDatas)
        {
            var garbage = new HashSet<DBObject>();
            var lines = architectureOutlineDatas.ToLines(ArcTesslateLength);
            garbage.UnionWith(lines.OfType<DBObject>().ToHashSet());

            var transformer = new ThMEPOriginTransformer(lines);
            transformer.Transform(lines);
            var lineSegments = lines.OfType<Line>().Select(o => o.ToNTSLineSegment()).ToList();
            var lineService = new ThTotalBoundaryService(lineSegments, 1.0, 1.0.AngToRad());
            var ntsPolygons = lineService.GetPolygons(true, false, true);
            var cadPolygons = ntsPolygons.SelectMany(o => o.ToDbCollection(false).OfType<DBObject>()).ToCollection();
            garbage.UnionWith(cadPolygons.OfType<DBObject>().ToHashSet());

            var cleanPolygons = Clean(cadPolygons);
            garbage.UnionWith(cleanPolygons.OfType<DBObject>().ToHashSet());
            var unionPolygons = cleanPolygons.UnionPolygons(false);
            garbage.ExceptWith(unionPolygons.OfType<DBObject>().ToHashSet());
            garbage.ToCollection().MDispose();

            transformer.Reset(unionPolygons);
            return unionPolygons;
        }

        private DBObjectCollection Clean(DBObjectCollection polygons)
        {
            var cleanService = new ThPolygonCleanService();
            return cleanService.Clean(polygons);
        }

        private DBObjectCollection GetArchitectureOutlineDataFromMS()
        {
            var roomDatas = new DBObjectCollection();
            var walls = GetEntitiesFromMS(AIArchAuxiliaryWallLayer);
            //var doors = GetEntitiesFromMS(ThMEPEngineCoreLayerUtils.DOOR);
            //var columns = GetEntitiesFromMS(ThMEPEngineCoreLayerUtils.COLUMN);
            //var shearWalls = GetEntitiesFromMS(ThMEPEngineCoreLayerUtils.SHEARWALL);
            roomDatas = roomDatas.Union(walls);
            //roomDatas = roomDatas.Union(doors);
            //roomDatas = roomDatas.Union(columns);
            //roomDatas = roomDatas.Union(shearWalls);
            return roomDatas;
        }
        private DBObjectCollection GetArchitectureOutlineDataFromMS(Point3dCollection pts)
        {
            var roomDatas = GetArchitectureOutlineDataFromMS();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(roomDatas);
            return spatialIndex.SelectCrossingPolygon(pts);
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
        private List<string> GetLayers()
        {
            return LayerInfos.Select(o => o.Layer).ToList();
        }
        private List<string> GetSBeamLayers()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o=>!(o.IsOff || o.IsFrozen))
                    .Where(o => IsSBeamLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }
        private List<string> GetSWallLayers()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Layers
                    .Where(o => !(o.IsOff || o.IsFrozen))
                    .Where(o => IsSWallLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
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
        private bool IsSWallLayer(string layer)
        {
            //以A-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("S-WALL") || 
                layer.ToUpper().EndsWith("S_WALL");
        }
        private bool IsSBeamLayer(string layer)
        {
            //以A-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("S-BEAM") ||
                layer.ToUpper().EndsWith("S_BEAM");
        }
        private bool IsAWallLayer(string layer)
        {
            //以A-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("A-WALL") ||
                layer.ToUpper().EndsWith("A_WALL");
        }
        private bool IsAEWallLayer(string layer)
        {
            //以AE-WALL结尾的所有图层
            return layer.ToUpper().EndsWith("AE-WALL") ||
                layer.ToUpper().EndsWith("AE_WALL");
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

            // 获取剪力墙图层
            var swallLayers = GetSWallLayers().Select(o => new ThLayerInfo()
            {
                Layer = o,
                IsSelected = true,
            }).ToList();

            // 获取梁图层
            var sbeamLayers = GetSBeamLayers().Select(o => new ThLayerInfo()
            {
                Layer = o,
                IsSelected = true,
            }).ToList();

            var results = new List<ThLayerInfo>();
            results.AddRange(aWallLayers);
            results.AddRange(aeWallLayers);
            results.AddRange(swallLayers);
            results.AddRange(sbeamLayers);
            return results;
        }       
        private void LoadFromActiveDatabase()
        {
            this.LayerInfos = new ObservableCollection<ThLayerInfo>();
            // 从当前database获取图层
            using (var acadDb = AcadDatabase.Active())
            {
                var extractArchitectureOutlineConfigNamedDictId = acadDb.Database.GetNamedDictionary(ThConfigDataTool.ExtractArchitectureOutlineNamedDictKey);
                if (extractArchitectureOutlineConfigNamedDictId != ObjectId.Null)
                {
                    var wallTvs = extractArchitectureOutlineConfigNamedDictId.GetXrecord(ThConfigDataTool.WallLayerSearchKey);
                    if (wallTvs != null)
                    {
                        foreach (TypedValue tv in wallTvs)
                        {
                            this.LayerInfos.Add(new ThLayerInfo { Layer = tv.Value.ToString() });
                        }
                    }
                }
            }
        }

        private void AddToGarbageCollector(DBObjectCollection objs)
        {
            _garbageCollector.UnionWith(objs.OfType<DBObject>().ToHashSet());
        }

        private void AddToGarbageCollector(DBObject obj)
        {
            _garbageCollector.Add(obj);
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
    }
}
