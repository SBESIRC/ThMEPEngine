using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPTCH.CAD;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace TianHua.Mep.UI.Data
{
    internal class ThArchitectureOutlineDataSetFactory : ThMEPDataSetFactory,IDisposable
    {
        private ThArchitectureOutlineDataSetConfig _config;
        private HashSet<DBObject> _garbageCollector = new HashSet<DBObject>();
        private DBObjectCollection _architectureWalls = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _shearWalls = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _columns = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _dbDoors = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _windows = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _slabs = new DBObjectCollection();  //仅支持Polyline
        private DBObjectCollection _cornices = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _curtainWalls = new DBObjectCollection();
        private DBObjectCollection _configWalls = new DBObjectCollection();
        private DBObjectCollection _tchArcWalls = new DBObjectCollection();
        private DBObjectCollection _tchLinearWalls = new DBObjectCollection();
        private DBObjectCollection _tchDoors = new DBObjectCollection(); // 不支持弧门

        public DBObjectCollection AllLines
        {
            get
            {
                var allLines = new HashSet<DBObject>();
                AllObjCollection.ForEach(o => allLines.UnionWith(o.OfType<DBObject>().ToHashSet()));               
                return allLines.ToCollection();
            }
        }

        private List<DBObjectCollection> AllObjCollection
        {
            get
            {
                return new List<DBObjectCollection>()
                {
                    _dbDoors,_tchDoors,_shearWalls,_configWalls,_slabs,_windows,_cornices,
                    _curtainWalls,_architectureWalls,_tchArcWalls,_tchLinearWalls,_columns
                };
            }
        }

        public ThArchitectureOutlineDataSetFactory(ThArchitectureOutlineDataSetConfig config)
        {
            _config = config;
        }

        public void Dispose()
        {
            _garbageCollector.ExceptWith(AllLines.OfType<DBObject>().ToHashSet());
            _garbageCollector.ToCollection().MDispose();
        }       

        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = new List<ThGeometry>(),
            };
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            #region ----------提取----------
            // 创建提取围合房间框线元素的Visitor
            var roomDataVisitors = CreateVisitors(database);

            // 从块中提取
            Extract(database, roomDataVisitors);

            // 从本地提取
            ExtractFromMS(database, roomDataVisitors.Where(o => o is ThWallExtractionVisitor).ToList());
            ExtractFromMS(database, roomDataVisitors.Where(o => o is ThTCHArchWallExtractionVisitor).ToList());
            ExtractFromMS(database, roomDataVisitors.Where(o => o is ThTCHDoorExtractionVisitor).ToList());

            AddGarbageCollector(roomDataVisitors.SelectMany(o=>o.Results).Select(o=>o.Geometry).ToCollection());
            #endregion

            #region ----------移动----------
            // 创建Transformer，把提取的对象移动到近似原点
            var transformer = new ThMEPOriginTransformer(Point3d.Origin);
            if (collection.Count>2)
            {
                var center = collection.Envelope().Flatten().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            roomDataVisitors
                .SelectMany(o => o.Results)
                .Select(o => o.Geometry)
                .OfType<Entity>()
                .ForEach(o=>transformer.Transform(o));
            roomDataVisitors
                .Where(o => o is ThDB3DoorMarkExtractionVisitor)
                .SelectMany(o => o.Results)
                .Select(o => o.Data)
                .OfType<Entity>()
                .ForEach(o => transformer.Transform(o));
            var newFrame = transformer.Transform(collection);
            #endregion

            #region --------- 识别 ----------
            // 建筑墙
            _architectureWalls = Recognize(GetDB3ArchWallElements(roomDataVisitors), newFrame, new ThDB3ArchWallRecognitionEngine());

            // 剪力墙
            var shearWallObjs = Recognize(GetShearWallElements(roomDataVisitors), newFrame, new ThShearWallRecognitionEngine());
            var db3ShearWallObjs = Recognize(GetDB3ShearWallElements(roomDataVisitors), newFrame, new ThDB3ShearWallRecognitionEngine());
            _shearWalls = _shearWalls.Union(shearWallObjs);
            _shearWalls = _shearWalls.Union(db3ShearWallObjs);

            // 柱
            var oldColumnBufferSwitch = ThMEPEngineCoreService.Instance.ExpandColumn;
            try
            {
                ThMEPEngineCoreService.Instance.ExpandColumn = false;
                var columnObjs = Recognize(GetColumnElements(roomDataVisitors), newFrame, new ThColumnRecognitionEngine());
                var db3ColumnObjs = Recognize(GetDB3ColumnElements(roomDataVisitors), newFrame, new ThDB3ColumnRecognitionEngine());
                _columns = _columns.Union(columnObjs);
                _columns = _columns.Union(db3ColumnObjs);
            }
            finally
            {
                ThMEPEngineCoreService.Instance.ExpandColumn = oldColumnBufferSwitch;
            }            

            // 窗户
            _windows = Recognize(GetDB3WindowElements(roomDataVisitors), newFrame, new ThDB3WindowRecognitionEngine());

            // 楼板
            _slabs = Recognize(GetDB3SlabElements(roomDataVisitors), newFrame, new ThDB3SlabRecognitionEngine());

            // 线脚
            _cornices = Recognize(GetDB3CorniceElements(roomDataVisitors), newFrame, new ThDB3CorniceRecognitionEngine());

            // 幕墙
            _curtainWalls = Recognize(GetDB3CurtainWallElements(roomDataVisitors), newFrame, new ThDB3CurtainWallRecognitionEngine());

            // 门
            var doorDatas = new List<ThRawIfcBuildingElementData>();
            doorDatas.AddRange(GetDB3DoorMarkElements(roomDataVisitors));
            doorDatas.AddRange(GetDB3DoorStoneElements(roomDataVisitors));
            _dbDoors = RecognizeDB3Door(doorDatas, newFrame);

            // 根据图层配置的墙            
            _configWalls = SelectCrossPolygon(GetConfigWallElements(roomDataVisitors).Select(o => o.Geometry).ToCollection(), newFrame);

            // 天正墙
            var tchDatas = GetTCHWallElements(roomDataVisitors);
            var tchArcWallElements = tchDatas.Where(o => o.Data is TArchWall archWall && archWall.IsArc).ToList();
            var tchLinearWallElements = tchDatas.Where(o => o.Data is TArchWall archWall && !archWall.IsArc).ToList();
            _tchArcWalls = SelectCrossPolygon(tchArcWallElements.Select(o => o.Geometry).ToCollection(), newFrame);
            _tchLinearWalls = SelectCrossPolygon(tchLinearWallElements.Select(o => o.Geometry).ToCollection(), newFrame);

            // 天正门
            _tchDoors = SelectCrossPolygon(GetTCHDoorElements(roomDataVisitors).Select(o => o.Geometry).ToCollection(),newFrame);

            // 用天正门中心线造门洞
            var doorOpenings = CreateLinearDoorOpening(_tchLinearWalls, _tchDoors);
            _tchDoors = doorOpenings.Values.ToCollection(); // 只收集天正的的门洞
            #endregion

            #region--------- 过滤 ----------
            // 过滤Polygon内部的元素(柱子，剪力墙...)
            FilterInnerPolygons();

            // 过滤孤立的元素
            // 过滤孤立柱            
            var discardColumns =  FilterIsolatedElements(_columns, ColumnNeibours, _config.NeibourRangeDistance);
            _columns = _columns.Difference(discardColumns);
            AddGarbageCollector(discardColumns);
            #endregion

            AllObjCollection.ForEach(o => transformer.Reset(o));    
            ConvertToCurves(AllObjCollection); // 把Mpolygon转成Curves
        }
        private DBObjectCollection ColumnNeibours
        {
            get
            {
                var neibours = new HashSet<DBObject>();
                var index = AllObjCollection.IndexOf(_columns);
                for(int i=0;i<AllObjCollection.Count;i++)
                {
                    if(i==index)
                    {
                        continue;
                    }
                    var current = AllObjCollection[i];
                    neibours.UnionWith(current.OfType<DBObject>().ToHashSet());
                }
                return neibours.ToCollection();
            }            
        }        
        private void AddGarbageCollector(DBObjectCollection objs)
        {
            objs.OfType<DBObject>().ForEach(o => AddGarbageCollector(o));
        }
        private void AddGarbageCollector(DBObject obj)
        {
            _garbageCollector.Add(obj);
        }

        private void ConvertToCurves(List<DBObjectCollection> objsList)
        {
            for(int i=0;i<objsList.Count;i++)
            {
                objsList[i] = ConvertToCurves(objsList[i]);
            }
        }

        private DBObjectCollection ConvertToCurves(DBObjectCollection objs)
        {
            // 把MPolygon 转成 Curves
            var results = new DBObjectCollection();
            foreach(Entity entity in objs)
            {
                if(entity is MPolygon polygon)
                {
                    var curves = ToCurves(polygon);
                    curves.OfType<Curve>().ForEach(c => results.Add(c));
                    AddGarbageCollector(polygon);
                }
                else
                {
                    results.Add(entity);
                }
            }
            return results;
        }
        private DBObjectCollection ToCurves(MPolygon mPolygon)
        {
            var results = new DBObjectCollection();
            results.Add(mPolygon.Shell());
            mPolygon.Holes().ForEach(o => results.Add(o));
            return results;
        }        
        #region ---------- 获取Visitor中的元素
        private List<ThRawIfcBuildingElementData> GetDB3DoorMarkElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3DoorMarkExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3DoorStoneElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3DoorStoneExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3CurtainWallElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3CurtainWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3CorniceElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3CorniceExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3SlabElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3SlabExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3WindowElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3WindowExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3ColumnElements(
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3ColumnExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetColumnElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThColumnExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3ShearWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3ShearWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetShearWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThShearWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3ArchWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3ArchWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetConfigWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetTCHWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            // 对天正的门造洞(暂时不考虑弧门,暂时默认tchDoors没有弧门;暂时不考虑弧墙)
            return visitors
                .Where(o => o is ThTCHArchWallExtractionVisitor)
                .SelectMany(o => o.Results)           
                .ToList();
        }

        private List<ThRawIfcBuildingElementData> GetOtherShearwallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThOtherShearWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetTCHDoorElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThTCHDoorExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }
        #endregion
        #region ---------- 提取元素 ----------
        private void Extract(Database database,List<ThBuildingElementExtractionVisitor> visitors)
        {
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.ToArray());
            extractor.Extract(database);
        }

        private void ExtractFromMS(Database database, List<ThBuildingElementExtractionVisitor> visitors)
        {
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors.ToArray());
            extractor.ExtractFromMS(database);
        }

        private List<Polyline> ExtractPolylineFromMS(Database database,string layer)
        {
            var extractPolyService = new ThExtractPolylineService()
            {
                ElementLayer = layer,
            };
            extractPolyService.Extract(database, new Point3dCollection());
            return extractPolyService.Polys;
        }

        private List<ThBuildingElementExtractionVisitor> CreateVisitors(Database database)
        {
            var visitors  = new List<ThBuildingElementExtractionVisitor>();

            // 房间标准数据
            var vm = new ThBuildingElementVisitorManager(database);
            visitors.Add(vm.DB3PcArchWallVisitor);
            visitors.Add(vm.DB3ArchWallVisitor);
            visitors.Add(vm.DB3ColumnVisitor);
            visitors.Add(vm.DB3DoorMarkVisitor);
            visitors.Add(vm.DB3DoorStoneVisitor);
            visitors.Add(vm.DB3WindowVisitor);
            visitors.Add(vm.DB3SlabVisitor);
            visitors.Add(vm.DB3CorniceVisitor);
            visitors.Add(vm.DB3CurtainWallVisitor);
            visitors.Add(vm.DB3ShearWallVisitor);
            visitors.Add(vm.ColumnVisitor);

            if(_config.UseConfigShearWallLayer)
            {
                vm.ShearWallVisitor.LayerFilter = ThExtractShearWallConfig.Instance.LayerInfos.Select(o=>o.Layer).ToHashSet();
            }

            if (_config.YnExtractShearWall)
            {               
                visitors.Add(vm.ShearWallVisitor);
            }

            // 配置墙线Visitor
            var configWallVisitor = CreateConfigWallVisitor(database, _config.WallLayers);
            visitors.Add(configWallVisitor);

            // 天正墙Visitor
            var tchWallVisitor = new ThTCHArchWallExtractionVisitor();
            visitors.Add(tchWallVisitor);

            // 天正门Visitor
            var tchDoorVisitor = new ThTCHDoorExtractionVisitor();
            visitors.Add(tchDoorVisitor);

            return visitors;
        }

        private ThBuildingElementExtractionVisitor CreateConfigWallVisitor(Database database, List<string> wallLayers)
        {
            //把图层配置提取的墙线，合并到Walls中
            var layers = new List<string>();
            var defaultPCLayers = ThPCArchitectureWallLayerManager.CurveXrefLayers(database);
            layers.AddRange(defaultPCLayers);
            layers.AddRange(wallLayers.Where(o => !defaultPCLayers.Contains(o)));

            return new ThWallExtractionVisitor()
            {
                LayerFilter = layers.ToHashSet(),
            };
        }
        #endregion
        #region ---------- 识别元素 ----------
        private DBObjectCollection RecognizeDB3Door(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            // 创建门依赖索引(请在此之前把门依赖的元素提取出来)
            var neibourObjDict = CreateDoorDependences();
            ThSpatialIndexCacheService.Instance.Build(neibourObjDict);
            var recognition = new ThDB3DoorRecognitionEngine();
            recognition.Recognize(datas, polygon);
            return recognition.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection Recognize(
            List<ThRawIfcBuildingElementData> datas,
            Point3dCollection polygon,
            ThBuildingElementRecognitionEngine recognition)
        {
            recognition.Recognize(datas, polygon);
            return recognition.Elements.Select(o => o.Outline).ToCollection();
        }

        private Dictionary<BuiltInCategory, DBObjectCollection> CreateDoorDependences()
        {
            var doorNeiborData = new Dictionary<BuiltInCategory, DBObjectCollection>();
            doorNeiborData.Add(BuiltInCategory.ArchitectureWall, _architectureWalls);
            doorNeiborData.Add(BuiltInCategory.ShearWall, _shearWalls);
            doorNeiborData.Add(BuiltInCategory.Column, _columns);
            doorNeiborData.Add(BuiltInCategory.CurtainWall, _curtainWalls);
            doorNeiborData.Add(BuiltInCategory.Window, _windows);
            return doorNeiborData;
        }
        private DBObjectCollection SelectCrossPolygon(DBObjectCollection objs, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                return spatialIndex.SelectCrossingPolygon(polygon);
            }
            else
            {
                return objs;
            }
        }
        #endregion
        #region ---------- 过滤元素 ----------
        private void FilterInnerPolygons(double bufferTolerance = 2.5)
        {
            var polygonCollections = new List<DBObjectCollection> {
                _columns,_architectureWalls,_shearWalls,_dbDoors,_tchDoors,_tchArcWalls,_tchLinearWalls ,_windows};
            for (int i = 0; i < polygonCollections.Count; i++)
            {
                var polygons = polygonCollections[i];
                var discardInnerPolygons = FilterInnerPolygons(polygons, bufferTolerance);
                polygons = polygons.Difference(discardInnerPolygons);
                AddGarbageCollector(discardInnerPolygons);
            }
        }
        private DBObjectCollection FilterInnerPolygons(DBObjectCollection polygons,double bufferTolerance)
        {
            // 过滤在polygon内部的元素
            var spatialIndex = new ThCADCoreNTSSpatialIndex(polygons);
            var bufferService = new ThNTSBufferService();
            var discardElements = new HashSet<DBObject>();
            foreach(Entity entity in polygons)
            {
                if(discardElements.Contains(entity))
                {
                    continue;
                }
                var bufferEntity = bufferService.Buffer(entity, bufferTolerance);
                if(bufferEntity==null)
                {
                    continue;
                }
                var objs = spatialIndex.SelectWindowPolygon(bufferEntity);
                objs.Remove(entity);
                foreach(DBObject innerObj in objs)
                {
                    discardElements.Add(innerObj);
                }
                bufferEntity.Dispose();
            }
            return discardElements.ToCollection();
        }
        private DBObjectCollection FilterIsolatedElements(DBObjectCollection polygons, DBObjectCollection neibours, double rangeTolerance)
        {
            var isolatedElements = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(neibours);
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var enlargePolygon = bufferService.Buffer(e, rangeTolerance);
                if(enlargePolygon!=null)
                {
                    var neibourObjs = spatialIndex.SelectCrossingPolygon(enlargePolygon);
                    if (neibourObjs.Count == 0)
                    {
                        isolatedElements.Add(e);
                    }
                    enlargePolygon.Dispose();
                }
            });
            return isolatedElements;
        }

        private Polyline CreateDoorOpening(Polyline linearWall, Polyline linearDoor)
        {
            var shortPairs = GetRectangleShortPair(linearDoor);
            var longPairs = GetRectangleLongPair(linearWall);
            if (shortPairs.Count == 2 && longPairs.Count == 2)
            {
                var firstPt = shortPairs[0].Item1.GetMidPt(shortPairs[0].Item2);
                var secondPt = shortPairs[1].Item1.GetMidPt(shortPairs[1].Item2);
                var pt1 = ThGeometryTool.GetProjectPtOnLine(firstPt, longPairs[0].Item1, longPairs[0].Item2);
                var pt2 = ThGeometryTool.GetProjectPtOnLine(secondPt, longPairs[0].Item1, longPairs[0].Item2);
                var pt3 = ThGeometryTool.GetProjectPtOnLine(firstPt, longPairs[1].Item1, longPairs[1].Item2);
                var pt4 = ThGeometryTool.GetProjectPtOnLine(secondPt, longPairs[1].Item1, longPairs[1].Item2);
                var pts = new Point3dCollection() { pt1, pt3, pt4, pt2 };
                return pts.CreatePolyline();
            }
            else
            {
                return null;
            }
        }
        private List<Tuple<Point3d, Point3d>> GetRectangleShortPair(Polyline rectangle)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            var edges = GetRectangleEdges(rectangle);
            edges = edges.Where(o => o.Item1.DistanceTo(o.Item2) > 1e-6).ToList();
            if (edges.Count == 4)
            {
                var first = edges[0];
                var third = edges[2];
                var second = edges[1];
                var fourth = edges[3];
                if (first.Item1.DistanceTo(first.Item2) < second.Item1.DistanceTo(second.Item2))
                {
                    results.Add(first);
                    results.Add(third);
                }
                else
                {
                    results.Add(second);
                    results.Add(fourth);
                }
            }
            return results;
        }
        private List<Tuple<Point3d, Point3d>> GetRectangleLongPair(Polyline rectangle)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            var edges = GetRectangleEdges(rectangle);
            edges = edges.Where(o => o.Item1.DistanceTo(o.Item2) > 1e-6).ToList();
            if (edges.Count == 4)
            {
                var first = edges[0];
                var third = edges[2];
                var second = edges[1];
                var fourth = edges[3];
                if (first.Item1.DistanceTo(first.Item2) > second.Item1.DistanceTo(second.Item2))
                {
                    results.Add(first);
                    results.Add(third);
                }
                else
                {
                    results.Add(second);
                    results.Add(fourth);
                }
            }
            return results;
        }
        private List<Tuple<Point3d, Point3d>> GetRectangleEdges(Polyline rectangle)
        {
            var edges = new List<Tuple<Point3d, Point3d>>();
            for (int i = 0; i < rectangle.NumberOfVertices; i++)
            {
                var segType = rectangle.GetSegmentType(i);
                if (segType != SegmentType.Line)
                {
                    continue;
                }
                var lineSeg = rectangle.GetLineSegmentAt(i);
                edges.Add(Tuple.Create(lineSeg.StartPoint, lineSeg.EndPoint));
            }
            return edges;
        }
        private Dictionary<Polyline, Polyline> CreateLinearDoorOpening(
           DBObjectCollection linearWalls,
           DBObjectCollection linearDoors)
        {
            var doorSpatialIndex = new ThCADCoreNTSSpatialIndex(linearDoors);
            var bufferService = new ThNTSBufferService();
            var results = new Dictionary<Polyline, Polyline>();
            // 修正墙里的门
            linearWalls.OfType<Polyline>().ForEach(wall =>
            {
                var bufferObj = bufferService.Buffer(wall, 5.0);
                var innerDoors = doorSpatialIndex.SelectWindowPolygon(bufferObj);
                innerDoors.OfType<Polyline>().ForEach(door =>
                {
                    var doorOpening = CreateDoorOpening(wall, door);
                    if (doorOpening != null && !results.ContainsKey(door))
                    {
                        results.Add(door, doorOpening);
                    }
                });
            });
            return results;
        }
        private DBObjectCollection FilterInnerObjs(DBObjectCollection firstPolygons, DBObjectCollection secondPolygons)
        {
            // 过滤在First内部的元素
            var sptialIndex = new ThCADCoreNTSSpatialIndex(secondPolygons);
            var results = new DBObjectCollection();
            firstPolygons.OfType<Entity>().ForEach(e =>
            {
                var innerObjs = sptialIndex.SelectWindowPolygon(e);
                innerObjs.OfType<DBObject>().ForEach(o => results.Add(o));
            });
            return results;
        }
        #endregion
    }    
    internal class ThArchitectureOutlineDataSetConfig
    {
        /// <summary>
        /// 判断元素多少范围以内是否有物体
        /// 没有则视为孤立元素
        /// </summary>
        public double NeibourRangeDistance { get; set; }
        /// <summary>
        /// 是否提取剪力墙
        /// </summary>
        public bool YnExtractShearWall { get; set; }
        /// <summary>
        /// 使用剪力墙图层配置的图层
        /// </summary>
        public bool UseConfigShearWallLayer { get; set; }
        /// <summary>
        /// 配置的墙体图层
        /// </summary>
        public List<string> WallLayers { get; set; }
        public ThArchitectureOutlineDataSetConfig()
        {
            YnExtractShearWall = true;
            NeibourRangeDistance = 200.0;
            UseConfigShearWallLayer = false;
            WallLayers = new List<string>();
        }
    }
}
