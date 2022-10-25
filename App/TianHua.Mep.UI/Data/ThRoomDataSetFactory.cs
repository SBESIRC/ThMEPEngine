using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
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
    internal class ThRoomDataSetFactory : ThMEPDataSetFactory,IDisposable
    {
        private ThRoomDataSetConfig _config;
        private HashSet<DBObject> _garbageCollector = new HashSet<DBObject>();
        private DBObjectCollection _architectureWalls = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _shearWalls = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _columns = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _dbDoors = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _windows = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _slabs = new DBObjectCollection();  //仅支持Polyline
        private DBObjectCollection _cornices = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _roomSplitlines = new DBObjectCollection();
        private DBObjectCollection _curtainWalls = new DBObjectCollection();
        private DBObjectCollection _configWalls = new DBObjectCollection();
        private DBObjectCollection _tchArcWalls = new DBObjectCollection();
        private DBObjectCollection _tchLinearWalls = new DBObjectCollection();
        private DBObjectCollection _tchDoors = new DBObjectCollection(); // 不支持弧门
        private DBObjectCollection _otherShearWalls = new DBObjectCollection();

        public DBObjectCollection ShearWalls => _shearWalls;
        public DBObjectCollection Columns => _columns;
        public DBObjectCollection Doors => _dbDoors.Union(_tchDoors);
        public DBObjectCollection OtherShearWalls => _otherShearWalls;
        public DBObjectCollection Walls
        {
            get
            {
                return GetWallList().SelectMany(o => o.OfType<DBObject>()).ToCollection();
            }
        }
        public ThRoomDataSetFactory(ThRoomDataSetConfig config)
        {
            _config = config;
        }

        public void Dispose()
        {
            var validObjs = new HashSet<DBObject>();
            Doors.OfType<DBObject>().ForEach(o => validObjs.Add(o));
            Walls.OfType<DBObject>().ForEach(o => validObjs.Add(o));
            Columns.OfType<DBObject>().ForEach(o => validObjs.Add(o));
            ShearWalls.OfType<DBObject>().ForEach(o => validObjs.Add(o));
            OtherShearWalls.OfType<DBObject>().ForEach(o => validObjs.Add(o));
            var inValidObjs = _garbageCollector.Except(validObjs).ToCollection();
            inValidObjs.MDispose();
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
            var roomSplitLines = ExtractPolylineFromMS(database, ThMEPEngineCoreLayerUtils.ROOMSPLITLINE);
            ExtractFromMS(database, roomDataVisitors.Where(o => o is ThWallExtractionVisitor).ToList());
            ExtractFromMS(database, roomDataVisitors.Where(o => o is ThTCHArchWallExtractionVisitor).ToList());
            ExtractFromMS(database, roomDataVisitors.Where(o => o is ThTCHDoorExtractionVisitor).ToList());

            AddGarbageCollector(roomDataVisitors.SelectMany(o=>o.Results).Select(o=>o.Geometry).ToCollection());
            roomSplitLines.ForEach(o => AddGarbageCollector(o));
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
            roomSplitLines.ForEach(o => transformer.Transform(o));
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

            // 分割线
            _roomSplitlines = SelectCrossPolygon(roomSplitLines.ToCollection(), newFrame);

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

            // 其它剪力墙
            _otherShearWalls = Recognize(GetOtherShearwallElements(roomDataVisitors), newFrame, new ThOtherShearWallRecognitionEngine());

            // 用天正门中心线造门洞
            var doorOpenings = CreateLinearDoorOpening(_tchLinearWalls, _tchDoors);
            _tchDoors = doorOpenings.Values.ToCollection(); // 只收集天正的的门洞
            #endregion

            #region----------过滤-----------
            FilterInnerBetweenDoors(); // 把DB包含天正的门过滤掉，反之亦然
            FilterColumnInnerObjs(); // 把柱子内部的元素过滤掉
            FilterIsolatedColumns(); // 过滤孤立的柱子
            FilterIsolatedOtherShearwalls(); // 过滤孤立的其它剪力墙
            #endregion

            #region ----------还原----------
            // 还原位置
            transformer.Reset(Walls);
            transformer.Reset(Doors);
            transformer.Reset(Columns);            
            transformer.Reset(ShearWalls);
            transformer.Reset(OtherShearWalls);            
            #endregion
            ConvertToCurves(); // 把Mpolygon转成Curves
        }
        private void AddGarbageCollector(DBObjectCollection objs)
        {
            objs.OfType<DBObject>().ForEach(o => AddGarbageCollector(o));
        }
        private void AddGarbageCollector(DBObject obj)
        {
            _garbageCollector.Add(obj);
        }
        private void ConvertToCurves()
        {
            // 把MPolygon 转成 Curves
            var objsList = GetTotalObjsList();
            for (int i = 0; i < objsList.Count; i++)
            {
                var polygons = objsList[i].OfType<MPolygon>().ToCollection();
                if (polygons.Count>0)
                {
                    polygons.OfType<MPolygon>().ForEach(m => objsList[i].Remove(m));
                    polygons.OfType<MPolygon>().ForEach(m =>
                    {
                        var curves = ToCurves(m);
                        curves.OfType<Curve>().ForEach(c => objsList[i].Add(m));
                        AddGarbageCollector(m);
                    });
                }
            }
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

            // 其它墙Visitor
            var otherShearWallVisitor = new ThOtherShearWallExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database).ToHashSet(),
            };
            otherShearWallVisitor.BlackVisitors.Add(vm.ShearWallVisitor);
            otherShearWallVisitor.BlackVisitors.Add(vm.DB3ShearWallVisitor);
            otherShearWallVisitor.BlackVisitors.Add(vm.ColumnVisitor);
            otherShearWallVisitor.BlackVisitors.Add(vm.DB3ColumnVisitor);
            visitors.Add(otherShearWallVisitor);
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
        private void FilterInnerBetweenDoors()
        {
            // 用DB的门过滤天正的门
            var dbBufferDoors = _dbDoors.BufferPolygons(5.0);
            var innerTchDoors = FilterInnerObjs(dbBufferDoors, _tchDoors);
            _tchDoors = _tchDoors.Difference(innerTchDoors);
            AddGarbageCollector(innerTchDoors);
            AddGarbageCollector(dbBufferDoors);

            // 用天正的门过滤DB的门
            var tchBufferDoors = _tchDoors.BufferPolygons(5.0);
            var innerDbDoors = FilterInnerObjs(tchBufferDoors, _dbDoors);
            _dbDoors = _dbDoors.Difference(innerDbDoors);
            AddGarbageCollector(innerDbDoors);
            AddGarbageCollector(tchBufferDoors);
        }
        private void FilterColumnInnerObjs()
        {
            var objsList = new List<DBObjectCollection>()
            {
                _configWalls,_slabs,_windows,_cornices,_curtainWalls,_roomSplitlines,_architectureWalls,
                _dbDoors,_shearWalls,_otherShearWalls,_tchArcWalls,_tchLinearWalls,_tchDoors
            };

            for(int i=0;i< objsList.Count;i++)
            {
                var objs = objsList[i];
                if(objs.Count==0)
                {
                    continue;
                }
                var columnInnerObjs = FilterInnerObjs(_columns, objs);
                columnInnerObjs.OfType<DBObject>().ForEach(e => objs.Remove(e));
                AddGarbageCollector(columnInnerObjs);
            }
        }

        private List<DBObjectCollection> GetWallList()
        {
            // 以下均视为墙线
            var results = new List<DBObjectCollection>()
            {
                _configWalls,_slabs,_windows,_cornices,_curtainWalls,
                _roomSplitlines,_architectureWalls,_tchArcWalls,_tchLinearWalls
            };
            return results;
        }

        private List<DBObjectCollection> GetTotalObjsList()
        {
            // 返回
            return new List<DBObjectCollection>()
            {
                _architectureWalls,_shearWalls,_columns,_dbDoors,_windows,_slabs,_cornices,_roomSplitlines,
                _curtainWalls,_configWalls,_tchArcWalls,_tchLinearWalls,_tchDoors,_otherShearWalls
            };
        }

        private void FilterIsolatedColumns()
        {
            // 过滤孤立柱
            var objsList = new List<DBObjectCollection>()
            { 
                _dbDoors,_tchDoors,_shearWalls,_otherShearWalls,
            };
            objsList.AddRange(GetWallList());
            var objs = new DBObjectCollection();
            objsList.ForEach(o =>
            {
                o.OfType<DBObject>().ForEach(e=> objs.Add(e));
            });
            var objSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var isolatedObjs = FilterIsolatedElements(_columns, objSpatialIndex, _config.NeibourRangeDistance);
            isolatedObjs.OfType<DBObject>().ForEach(o => _columns.Remove(o));
            AddGarbageCollector(isolatedObjs);
        }

        private void FilterIsolatedOtherShearwalls()
        {
            // 过滤其它剪力墙
            // 过滤孤立柱
            var objsList = new List<DBObjectCollection>()
            {
                _dbDoors,_tchDoors,_shearWalls,_columns,
            };
            objsList.AddRange(GetWallList());
            var objs = new DBObjectCollection();
            objsList.ForEach(o =>
            {
                o.OfType<DBObject>().ForEach(e => objs.Add(e));
            });
            var objSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var isolatedObjs = FilterIsolatedElements(_otherShearWalls, objSpatialIndex, _config.NeibourRangeDistance);
            isolatedObjs.OfType<DBObject>().ForEach(o => _otherShearWalls.Remove(o));
            AddGarbageCollector(isolatedObjs);
        }

        private DBObjectCollection FilterIsolatedElements(DBObjectCollection polygons, ThCADCoreNTSSpatialIndex spatialIndex, double rangeTolerance)
        {
            var isolatedElements = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var enlargePolygon = bufferService.Buffer(e, rangeTolerance);
                var neibourObjs = spatialIndex.SelectCrossingPolygon(enlargePolygon);
                if (neibourObjs.Count == 0)
                {
                    isolatedElements.Add(e);
                }
                enlargePolygon.Dispose();
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
    internal class ThOldRoomDataSetFactory : ThMEPDataSetFactory, IDisposable
    {
        private ThRoomDataSetConfig _config;
        private DBObjectCollection _doors = new DBObjectCollection();
        private DBObjectCollection _walls = new DBObjectCollection();
        private DBObjectCollection _columns = new DBObjectCollection();
        private DBObjectCollection _shearwalls = new DBObjectCollection();
        private DBObjectCollection _otherShearwalls = new DBObjectCollection();

        public DBObjectCollection Doors => _doors;
        public DBObjectCollection Walls => _walls;
        public DBObjectCollection Columns => _columns;
        public DBObjectCollection ShearWalls => _shearwalls;
        public DBObjectCollection OtherShearWalls => _otherShearwalls;

        public ThOldRoomDataSetFactory(ThRoomDataSetConfig config)
        {
            _config = config;
        }
        public void Dispose()
        {
            //
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = new List<ThGeometry>(),
            };
        }

        protected override void GetElements(Database database, Point3dCollection rangePts)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                // 提取数据 + 收集数据
                var roomData = GetRoomData(acadDb.Database, rangePts);
                var wallObjs = GetConfigWalls(acadDb.Database, rangePts);
                var tchwallElements = GetTCHWalls(acadDb.Database, rangePts);
                var tchDoorElements = GetTCHDoors(acadDb.Database, rangePts);
                _otherShearwalls = GetOtherShearwalls(acadDb.Database, rangePts);
                // 收集建筑墙线  
                _walls = _walls.Union(wallObjs);
                _walls = _walls.Union(roomData.Slabs);
                _walls = _walls.Union(roomData.Windows);
                _walls = _walls.Union(roomData.Cornices);
                _walls = _walls.Union(roomData.CurtainWalls);
                _walls = _walls.Union(roomData.RoomSplitlines);
                _walls = _walls.Union(roomData.ArchitectureWalls);
                // 收集门
                _doors = _doors.Union(roomData.Doors);
                // 收集柱
                _columns = _columns.Union(roomData.Columns);
                // 收集剪力墙
                _shearwalls = _shearwalls.Union(roomData.ShearWalls);
                // 其它剪力墙在此处不收集，需要处理

                // 把数据移动到近似原点
                var centerPt = rangePts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(centerPt);
                transformer.Transform(_walls);
                transformer.Transform(_doors);
                transformer.Transform(_columns);
                transformer.Transform(_shearwalls);
                transformer.Transform(_otherShearwalls);

                var tchDoors = tchDoorElements.Select(o => o.Geometry).ToCollection();
                var tchwalls = tchwallElements.Select(o => o.Geometry).ToCollection();
                transformer.Transform(tchwalls);
                transformer.Transform(tchDoors);

                // 对天正的门造洞(暂时不考虑弧门,暂时默认tchDoors没有弧门;暂时不考虑弧墙)
                var linearWalls = tchwallElements
                    .Where(o => o.Data is TArchWall archWall && archWall.IsArc == false)
                    .Select(o => o.Geometry).ToCollection();

                var doorOpenings = CreateLinearDoorOpening(linearWalls, tchDoors);
                tchDoors.MDispose();
                tchDoors = doorOpenings.Values.ToCollection(); // 只收集天正的的门洞

                // 用DB的门过滤天正的门洞
                var dbBufferDoors = _doors.BufferPolygons(5.0);
                var innerTchDoors = FilterInnerObjs(dbBufferDoors, tchDoors);
                tchDoors = tchDoors.Difference(innerTchDoors);
                innerTchDoors.MDispose();
                dbBufferDoors.MDispose();

                // 用天正的门过滤DB的门
                var tchBufferDoors = tchDoors.BufferPolygons(5.0);
                var innerDbDoors = FilterInnerObjs(tchBufferDoors, _doors);
                _doors = _doors.Difference(innerDbDoors);
                innerDbDoors.MDispose();
                tchBufferDoors.MDispose();

                // 把天正的门洞放入DB门中
                _doors = _doors.Union(tchDoors);
                // 把天正的墙放入建筑墙中
                _walls = _walls.Union(tchwalls);

                // 把柱子内部的元素过滤掉
                FilterColumnInnerObjs();

                // 过滤孤立元素(附近多少距离以内没有东西算孤立元素。)     
                FilterIsolatedColumns();
                FilterIsolatedOtherShearwalls();

                // 把数据还原到原位置
                transformer.Reset(_walls);
                transformer.Reset(_doors);
                transformer.Reset(_columns);
                transformer.Reset(_shearwalls);
                transformer.Reset(_otherShearwalls);

                // 把Mpolygon转成Curve
                _walls = ToCurves(_walls, true);
                _columns = ToCurves(_columns, true);
                _shearwalls = ToCurves(_shearwalls, true);
                _otherShearwalls = ToCurves(_otherShearwalls, true);
            }
        }

        private void FilterIsolatedColumns()
        {
            // 过滤孤立柱
            var objs = new DBObjectCollection();
            objs = objs.Union(_doors);
            objs = objs.Union(_walls);
            objs = objs.Union(_shearwalls);
            objs = objs.Union(_otherShearwalls);
            var objSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var isolatedObjs = FilterIsolatedElements(_columns, objSpatialIndex, _config.NeibourRangeDistance);
            isolatedObjs.OfType<DBObject>().ForEach(o => _columns.Remove(o));
            isolatedObjs.MDispose();
        }

        private void FilterIsolatedOtherShearwalls()
        {
            // 过滤其它剪力墙
            var objs = new DBObjectCollection();
            objs = objs.Union(_doors);
            objs = objs.Union(_walls);
            objs = objs.Union(_columns);
            objs = objs.Union(_shearwalls);
            var objSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var isolatedObjs = FilterIsolatedElements(_otherShearwalls, objSpatialIndex, _config.NeibourRangeDistance);
            isolatedObjs.OfType<DBObject>().ForEach(o => _otherShearwalls.Remove(o));
            isolatedObjs.MDispose();
        }

        private void FilterColumnInnerObjs()
        {
            var columnInnerWalls = FilterInnerObjs(_columns, _walls);
            columnInnerWalls.OfType<DBObject>().ForEach(e => _walls.Remove(e));
            columnInnerWalls.MDispose();

            var columnInnerDoors = FilterInnerObjs(_columns, _doors);
            columnInnerDoors.OfType<DBObject>().ForEach(e => _doors.Remove(e));
            columnInnerDoors.MDispose();

            var columnInnerShearwalls = FilterInnerObjs(_columns, _shearwalls);
            columnInnerShearwalls.OfType<DBObject>().ForEach(e => _shearwalls.Remove(e));
            columnInnerShearwalls.MDispose();

            var columnInnerOtherShearwalls = FilterInnerObjs(_columns, _otherShearwalls);
            columnInnerOtherShearwalls.OfType<DBObject>().ForEach(e => _otherShearwalls.Remove(e));
            columnInnerOtherShearwalls.MDispose();
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

        private DBObjectCollection FilterIsolatedElements(DBObjectCollection polygons, ThCADCoreNTSSpatialIndex spatialIndex, double rangeTolerance)
        {
            var isolatedElements = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var enlargePolygon = bufferService.Buffer(e, rangeTolerance);
                var neibourObjs = spatialIndex.SelectCrossingPolygon(enlargePolygon);
                if (neibourObjs.Count == 0)
                {
                    isolatedElements.Add(e);
                }
                enlargePolygon.Dispose();
            });
            return isolatedElements;
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

        private DBObjectCollection ToCurves(DBObjectCollection objs, bool disposeMpolygon = false)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                if (e is Curve curve)
                {
                    results.Add(curve);
                }
                else if (e is MPolygon mPolygon)
                {
                    results = results.Union(ToCurves(mPolygon));
                    if (disposeMpolygon)
                    {
                        mPolygon.Dispose();
                    }
                }
            });
            return results;
        }

        private DBObjectCollection ToCurves(MPolygon mPolygon)
        {
            var results = new DBObjectCollection();
            results.Add(mPolygon.Shell());
            mPolygon.Holes().ForEach(o => results.Add(o));
            return results;
        }
        private DBObjectCollection GetConfigWalls(Database database, Point3dCollection frame)
        {
            //把图层配置提取的墙线，合并到Walls中
            var layers = new List<string>();
            var defaultPCLayers = ThPCArchitectureWallLayerManager.CurveXrefLayers(database);
            layers.AddRange(defaultPCLayers);
            layers.AddRange(_config.WallLayers.Where(o => !defaultPCLayers.Contains(o)));

            var wallVisitor = new ThWallExtractionVisitor()
            {
                LayerFilter = layers.ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(wallVisitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            var totalObjs = wallVisitor.Results
                .Select(o => o.Geometry).ToCollection();

            var transformer = new ThMEPOriginTransformer(totalObjs);
            var newFrame = transformer.Transform(frame);
            transformer.Transform(totalObjs);
            var results = SelectCrossPolygon(totalObjs, newFrame);
            transformer.Reset(totalObjs);
            var restObjs = totalObjs.Difference(results);
            restObjs.MDispose();
            return results;
        }
        private List<ThRawIfcBuildingElementData> GetTCHWalls(Database database, Point3dCollection polygon)
        {
            var visitor = new ThTCHArchWallExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            var geometries = visitor.Results.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer();
            if (polygon.Count >= 3)
            {
                var center = polygon.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                transformer = new ThMEPOriginTransformer(geometries);
            }
            var newFrame = transformer.Transform(polygon);
            transformer.Transform(geometries);
            var filterObjs = SelectCrossPolygon(geometries, newFrame);
            transformer.Reset(geometries);
            var results = visitor.Results.Where(o => filterObjs.Contains(o.Geometry)).ToList();

            // 释放
            var restObjs = geometries.Difference(filterObjs);
            restObjs.MDispose();
            return results;
        }

        private List<ThRawIfcBuildingElementData> GetTCHDoors(Database database, Point3dCollection polygon)
        {
            var visitor = new ThTCHDoorExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            var geometries = visitor.Results.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer();
            if (polygon.Count >= 3)
            {
                var center = polygon.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                transformer = new ThMEPOriginTransformer(geometries);
            }
            var newFrame = transformer.Transform(polygon);
            transformer.Transform(geometries);
            var filterObjs = SelectCrossPolygon(geometries, newFrame);
            transformer.Reset(geometries);
            var results = visitor.Results.Where(o => filterObjs.Contains(o.Geometry)).ToList();

            // 释放
            var restObjs = geometries.Difference(filterObjs);
            restObjs.MDispose();

            return results;
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
        private ThRoomdata GetRoomData(Database database, Point3dCollection frame)
        {
            var data = new ThRoomdata(false)
            {
                YnExtractShearWall = this._config.YnExtractShearWall,
                UseConfigShearWallLayer = this._config.UseConfigShearWallLayer,
            };
            data.Build(database, frame);
            return data;
        }

        private DBObjectCollection GetOtherShearwalls(Database database, Point3dCollection frame)
        {
            var otherShearWallEngine = new ThOtherShearWallRecognitionEngine();
            otherShearWallEngine.Recognize(database, frame);
            return otherShearWallEngine.Geometries;
        }
    }
    internal class ThRoomDataSetConfig
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
        /// 配置的墙体图层
        /// </summary>
        public List<string> WallLayers { get; set; }
        /// <summary>
        /// 使用剪力墙图层配置的图层
        /// </summary>
        public bool UseConfigShearWallLayer { get; set; }
        public ThRoomDataSetConfig()
        {
            NeibourRangeDistance = 200.0;
            YnExtractShearWall = true;
            WallLayers = new List<string>();
            UseConfigShearWallLayer = false;
        }
    }
}
