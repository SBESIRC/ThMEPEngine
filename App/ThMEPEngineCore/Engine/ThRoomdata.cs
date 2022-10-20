using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using NetTopologySuite.Operation.Buffer;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.Config;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomdata
    {
        private const double ColumnEnlargeDistance = 50.0;
        private const double SlabBufferDistance = 20.0;
        private DBObjectCollection _architectureWall = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _shearWall = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _column = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _door = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _window = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _slab = new DBObjectCollection();  //仅支持Polyline
        private DBObjectCollection _cornice = new DBObjectCollection(); //仅支持Polyline
        private DBObjectCollection _roomSplitline = new DBObjectCollection();
        private DBObjectCollection _curtainWall = new DBObjectCollection();

        public DBObjectCollection ArchitectureWalls => _architectureWall;
        public DBObjectCollection ShearWalls => _shearWall;
        public DBObjectCollection Columns => _column;
        public DBObjectCollection Doors => _door;
        public DBObjectCollection Windows => _window;
        public DBObjectCollection Slabs => _slab;
        public DBObjectCollection Cornices => _cornice;
        public DBObjectCollection RoomSplitlines => _roomSplitline;
        public DBObjectCollection CurtainWalls => _curtainWall;

        public ThMEPOriginTransformer Transformer { get; private set; }
        private Action<Database, Point3dCollection> GetData;

        public bool YnExtractShearWall { get; set; } = true;
        public bool UseConfigShearWallLayer { get; set; } = false;

        public ThRoomdata(bool isUseOldMode)
        {
            if (isUseOldMode)
            {
                GetData = GetOldModeData;
            }
            else
            {
                GetData = GetNewModeData;
            }
        }
        public void Build(Database database, Point3dCollection polygon)
        {
            GetData(database, polygon);
            Transformer = new ThMEPOriginTransformer(polygon.Envelope().CenterPoint());
        }
        private void GetOldModeData(Database database, Point3dCollection polygon)
        {
            // 提取
            var db3ArchWall = ExtractDB3ArchWall(database);
            var shearWall = ExtractShearWall(database);
            var db3ShearWall = ExtractDB3ShearWall(database);
            var column = ExtractColumn(database);
            var db3Column = ExtractDB3Column(database);
            var db3Window = ExtractDB3Window(database);
            var db3Slab = ExtractDB3Slab(database);
            var db3Cornice = ExtractDB3Cornice(database);
            var db3CurtainWall = ExtractDB3CurtainWall(database);
            var roomSplitLine = ExtractRoomSplitLine(database);
            var db3Door = ExtractDB3Door(database);

            // 识别
            _architectureWall = RecognizeDB3ArchWall(db3ArchWall, polygon);

            var shearWallObjs = RecognizeShearWall(shearWall, polygon);
            var db3ShearWallObjs = RecognizeDB3ShearWall(db3ShearWall, polygon);
            _shearWall = _shearWall.Union(shearWallObjs);
            _shearWall = _shearWall.Union(db3ShearWallObjs);

            var columnDatas = new List<ThRawIfcBuildingElementData>();
            columnDatas.AddRange(column);
            columnDatas.AddRange(db3Column);
            _column = RecognizeColumn(columnDatas, polygon);

            _window = RecognizeDB3Window(db3Window, polygon);
            _slab = RecognizeDB3Slab(db3Slab, polygon);
            _cornice = RecognizeDB3Cornice(db3Cornice, polygon);
            _curtainWall = RecognizeDB3CurtainWall(db3CurtainWall, polygon);
            _roomSplitline = RecognizeRoomSplitLine(roomSplitLine, polygon);
            _door = RecognizeDB3Door(db3Door, polygon);
        }
        private void GetNewModeData(Database database, Point3dCollection polygon)
        {
            var vm = Extract(database);
            var roomSplitLine = ExtractRoomSplitLine(database);

            // 识别
            var archWallDatas = new List<ThRawIfcBuildingElementData>();
            archWallDatas.AddRange(vm.DB3ArchWallVisitor.Results);
            archWallDatas.AddRange(vm.DB3PcArchWallVisitor.Results);
            _architectureWall = RecognizeDB3ArchWall(archWallDatas, polygon);

            var shearWallObjs = RecognizeShearWall(vm.ShearWallVisitor.Results, polygon);
            var db3ShearWallObjs = RecognizeDB3ShearWall(vm.DB3ShearWallVisitor.Results, polygon);
            _shearWall = _shearWall.Union(shearWallObjs);
            _shearWall = _shearWall.Union(db3ShearWallObjs);

            var columnDatas = new List<ThRawIfcBuildingElementData>();
            columnDatas.AddRange(vm.ColumnVisitor.Results);
            columnDatas.AddRange(vm.DB3ColumnVisitor.Results);
            _column = RecognizeColumn(columnDatas, polygon);

            _window = RecognizeDB3Window(vm.DB3WindowVisitor.Results, polygon);
            _slab = RecognizeDB3Slab(vm.DB3SlabVisitor.Results, polygon);
            _cornice = RecognizeDB3Cornice(vm.DB3CorniceVisitor.Results, polygon);
            _curtainWall = RecognizeDB3CurtainWall(vm.DB3CurtainWallVisitor.Results, polygon);
            _roomSplitline = RecognizeRoomSplitLine(roomSplitLine, polygon);

            var doorDatas = new List<ThRawIfcBuildingElementData>();
            doorDatas.AddRange(vm.DB3DoorMarkVisitor.Results);
            doorDatas.AddRange(vm.DB3DoorStoneVisitor.Results);
            _door = RecognizeDB3Door(doorDatas, polygon);
        }

        public void Preprocess()
        {
            Deburring();
            FilterIsolatedColumns(ColumnEnlargeDistance);
        }
        private ThBuildingElementVisitorManager Extract(Database database)
        {
            // 提取
            var vm = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(vm.DB3PcArchWallVisitor);
            extractor.Accept(vm.DB3ArchWallVisitor);            
            extractor.Accept(vm.DB3ColumnVisitor);           
            extractor.Accept(vm.DB3DoorMarkVisitor);
            extractor.Accept(vm.DB3DoorStoneVisitor);
            extractor.Accept(vm.DB3WindowVisitor);
            extractor.Accept(vm.DB3SlabVisitor);
            extractor.Accept(vm.DB3CorniceVisitor);
            extractor.Accept(vm.DB3CurtainWallVisitor);
            extractor.Accept(vm.DB3ShearWallVisitor);

            if(UseConfigShearWallLayer)
            {
                vm.ShearWallVisitor.LayerFilter = ThExtractShearWallConfig.Instance.LayerInfos.Select(o => o.Layer).ToHashSet();
            }

            if(YnExtractShearWall)
            {
                extractor.Accept(vm.ColumnVisitor);
                extractor.Accept(vm.ShearWallVisitor);
            }

            extractor.Extract(database);
            return vm;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3ArchWall(Database database)
        {
            var extraction = new ThDB3ArchWallExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractShearWall(Database database)
        {
            var extraction = new ThShearWallExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3ShearWall(Database database)
        {
            var extraction = new ThDB3ShearWallExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractColumn(Database database)
        {
            var extraction = new ThColumnExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3Column(Database database)
        {
            var extraction = new ThDB3ColumnExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3Door(Database database)
        {
            // 提取
            var extraction = new ThDB3DoorExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3Window(Database database)
        {
            var extraction = new ThDB3WindowExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3Slab(Database database)
        {
            var extraction = new ThDB3SlabExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3Cornice(Database database)
        {
            var extraction = new ThDB3CorniceExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<ThRawIfcBuildingElementData> ExtractDB3CurtainWall(Database database)
        {
            var extraction = new ThDB3CurtainWallExtractionEngine();
            extraction.Extract(database);
            return extraction.Results;
        }
        private List<Polyline> ExtractRoomSplitLine(Database database)
        {
            var extractPolyService = new ThExtractPolylineService()
            {
                ElementLayer = ThMEPEngineCoreLayerUtils.ROOMSPLITLINE,
            };
            extractPolyService.Extract(database, new Point3dCollection());
            return extractPolyService.Polys;
        }
        private DBObjectCollection RecognizeDB3ArchWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3ArchWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeShearWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThShearWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3ShearWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3ShearWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeColumn(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThColumnRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Column(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3ColumnRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Window(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3WindowRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Slab(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3SlabRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Cornice(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3CorniceRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Door(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            // 创建门依赖索引(请在此之前把门依赖的元素提取出来)
            var neibourObjDict = CreateDoorDependences();

            // 对门依赖的数据和提取出来的门垛、文字进行偏移
            var transformer = new ThMEPOriginTransformer(datas.Where(o => o is ThRawDoorStone)
                .Select(o => o.Geometry).ToCollection());
            ThSpatialIndexCacheService.Instance.Transformer = transformer;
            ThSpatialIndexCacheService.Instance.Build(neibourObjDict);
            datas.ForEach(e =>
            {
                if (e is ThRawDoorStone doorStone)
                {
                    transformer.Transform(doorStone.Geometry);
                }
                else if (e is ThRawDoorMark doorMark)
                {
                    if (doorMark.Geometry != null)
                    {
                        transformer.Transform(doorMark.Geometry);
                    }
                    if (doorMark.Data != null && doorMark.Data is Entity entity)
                    {
                        transformer.Transform(entity);
                    }

                }
            });
            var newPts = transformer.Transform(polygon);
            var recognition = new ThDB3DoorRecognitionEngine();
            recognition.Recognize(datas, newPts);
            var results = recognition.Elements.Select(o => o.Outline).ToCollection();
            transformer.Reset(results);
            neibourObjDict.ForEach(o => transformer.Reset(o.Value));
            return results;
        }
        private DBObjectCollection RecognizeDB3CurtainWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3CurtainWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeRoomSplitLine(List<Polyline> polyline, Point3dCollection polygon)
        {
            var objs = polyline.ToCollection();
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            var sptialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var results = sptialIndex.SelectCrossingPolygon(newPts);
            transformer.Reset(results);
            return results;
        }
        private DBObjectCollection Recognize(
            List<ThRawIfcBuildingElementData> datas,
            Point3dCollection polygon,
            ThBuildingElementRecognitionEngine recognition)
        {
            var results = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (objs.Count==0)
            {
                return results;
            }
            var center = objs.GeometricExtents().Flatten().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            recognition.Recognize(datas, newPts);
            results = recognition.Elements.Select(o => o.Outline).ToCollection();
            transformer.Reset(results);
            return results;
        }
        private void FilterIsolatedColumns(double enlargeTolerance)
        {
            var data = MergeData();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(data);
            var collector = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            _column.OfType<Entity>().ForEach(e =>
            {
                var newEnt = bufferService.Buffer(e, enlargeTolerance) as Entity;
                var objs = spatialIndex.SelectCrossingPolygon(newEnt);
                objs.Remove(e);
                if (objs.Count == 0)
                {
                    collector.Add(e);
                }
            });
            collector.OfType<Entity>().ForEach(e => _column.Remove(e));
        }
        /// <summary>
        /// 拿到数据后根据需求去毛皮
        /// </summary>
        private void Deburring()
        {
            _architectureWall = _architectureWall.FilterSmallArea(1.0);
            _shearWall = _shearWall.FilterSmallArea(1.0);
            _door = _door.FilterSmallArea(1.0);
            _window = _window.FilterSmallArea(1.0);
            _column = _column.FilterSmallArea(1.0);
            _curtainWall = _curtainWall.FilterSmallArea(1.0);

            //楼板去毛皮
            _slab = BufferCollectionContainsLines(_slab, -SlabBufferDistance);
            _slab = BufferCollectionContainsLines(_slab, SlabBufferDistance);
        }
        /// <summary>
        /// 为包含碎线的DBObjectCollection进行buffer，并保留碎线
        /// </summary>
        /// <param name="polys"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private DBObjectCollection BufferCollectionContainsLines(DBObjectCollection polys, double length, bool lineflag = false)
        {
            DBObjectCollection res = new DBObjectCollection();
            polys.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline poly && ThAuxiliaryUtils.DoubleEquals(poly.Area, 0.0))
                {
                    if (length < 0)
                        res.Add(poly);//TODO: 碎线可能需要延伸一些长度
                    else if (lineflag)
                    {
                        var bufferRes = poly.ToNTSLineString().Buffer(
                            length, new BufferParameters() { JoinStyle = JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square }).ToDbCollection();
                        bufferRes.Cast<Entity>().ForEach(e => res.Add(e));
                    }
                    else
                        res.Add(poly);
                }
                else if (o is Polyline polygon && polygon.Area > 1.0)
                    polygon.ToNTSPolygon().Buffer(length, new BufferParameters() { JoinStyle = JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square })
                    .ToDbCollection().Cast<Entity>()
                    .ForEach(e => res.Add(e));
            });
            return res;
        }
        /// <summary>
        /// 将所有数据汇总打包
        /// </summary>
        /// <returns></returns>
        public DBObjectCollection MergeData()
        {
            var result = new DBObjectCollection();
            result = result.Union(_architectureWall);
            result = result.Union(_shearWall);
            result = result.Union(_column);
            result = result.Union(_door);
            result = result.Union(_window);
            result = result.Union(_slab);
            result = result.Union(_cornice);
            result = result.Union(_roomSplitline);
            result = result.Union(_curtainWall);
            return result;
        }
        /// <summary>
        /// 查询点在构件里
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool IsContatinPoint3d(Point3d p)
        {
            bool isInArchWall = IsInComponents(_architectureWall, p);
            bool isInShearWall = IsInComponents(_shearWall, p);
            bool isInColumn = IsInComponents(_column, p);
            bool isInDoor = IsInComponents(_door, p);
            bool isInWindow = IsInComponents(_window, p);
            bool isInCurtainWall = IsInComponents(_curtainWall, p);
            return isInArchWall || isInShearWall || isInColumn || isInDoor || isInWindow || isInCurtainWall;
        }

        public bool IsContains(Entity polygon)
        {
            if (polygon == null)
            {
                return false;
            }
            bool isArchWallContains = IsContains(_architectureWall, polygon);
            bool isShearWallContains = IsContains(_shearWall, polygon);
            bool isColumnContains = IsContains(_column, polygon);
            bool isDoorContains = IsContains(_door, polygon);
            bool isWindowContains = IsContains(_window, polygon);
            bool isCurtainWallContains = IsContains(_curtainWall, polygon);
            return isArchWallContains || isShearWallContains || isColumnContains ||
                isDoorContains || isWindowContains || isCurtainWallContains;
        }

        public bool IsCloseToComponents(Point3d p, double tolerance)
        {
            bool isCloseToArchWall = IsCloseToComponents(_architectureWall, p, tolerance);
            bool isCloseToShearWall = IsCloseToComponents(_shearWall, p, tolerance);
            bool isCloseToColumn = IsCloseToComponents(_column, p, tolerance);
            bool isCloseToDoor = IsCloseToComponents(_door, p, tolerance);
            bool isCloseToWindow = IsCloseToComponents(_window, p, tolerance);
            bool isCloseToCurtainWall = IsCloseToComponents(_curtainWall, p, tolerance);
            return isCloseToArchWall || isCloseToShearWall || isCloseToColumn ||
                isCloseToDoor || isCloseToWindow || isCloseToCurtainWall;
        }
        private bool IsContains(DBObjectCollection polygons, Entity polygon)
        {
            return polygons.OfType<Entity>().Where(o => o.EntityContains(polygon)).Any();
        }

        private bool IsCloseToComponents(DBObjectCollection polygons, Point3d pt, double tolerance)
        {
            foreach (DBObject obj in polygons)
            {
                if (obj is Polyline polyline)
                {
                    if (IsCloseTo(pt, polyline, tolerance))
                    {
                        return true;
                    }
                }
                else if (obj is MPolygon mPolygon)
                {
                    var shell = mPolygon.Shell();
                    if (IsCloseTo(pt, shell, tolerance))
                    {
                        return true;
                    }
                    if (mPolygon.Holes().Where(o => IsCloseTo(pt, o, tolerance)).Any())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsCloseTo(Point3d pt, Polyline polyline, double tolerance)
        {
            var closePt = polyline.GetClosestPointTo(pt, false);
            return pt.DistanceTo(closePt) <= tolerance;
        }

        private bool IsInComponents(DBObjectCollection polygons, Point3d pt)
        {
            bool isIn = false;
            foreach (DBObject obj in polygons)
            {
                if (obj is Polyline polyline)
                {
                    if (polyline.Area > 1e-6)
                    {
                        isIn = polyline.EntityContains(pt);
                    }
                }
                else if (obj is MPolygon mPolygon)
                {
                    isIn = mPolygon.EntityContains(pt);
                }
                if (isIn)
                {
                    break;
                }
            }
            return isIn;
        }
        public void Transform()
        {
            var objs = MergeData();
            Transformer.Transform(objs);
        }
        public void Reset()
        {
            var objs = MergeData();
            Transformer.Reset(objs);
        }
        private Dictionary<BuiltInCategory, DBObjectCollection> CreateDoorDependences()
        {
            var doorNeiborData = new Dictionary<BuiltInCategory, DBObjectCollection>();
            doorNeiborData.Add(BuiltInCategory.ArchitectureWall, _architectureWall);
            doorNeiborData.Add(BuiltInCategory.ShearWall, _shearWall);
            doorNeiborData.Add(BuiltInCategory.Column, _column);
            doorNeiborData.Add(BuiltInCategory.CurtainWall, _curtainWall);
            doorNeiborData.Add(BuiltInCategory.Window, _window);
            return doorNeiborData;
        }
    }
}