using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Electrical;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPElectrical.DCL.Data
{
    /// <summary>
    /// 构件防雷引线需要的数据
    /// </summary>
    internal class ThDclDataInfo
    {
        private const string SpecialBeltLayer = "E-THUN-WIRE";
        private const string DualpurposeBeltLayer = "E-GRND-WIRE";
        #region ---------- 属性 ----------
        public DBObjectCollection Columns { get; private set; }
        public DBObjectCollection ShearWalls { get; private set; }
        public DBObjectCollection ArchitectureWalls { get; private set; }
        public DBObjectCollection CurtainWalls { get; private set; }
        public DBObjectCollection Doors { get; private set; }
        public DBObjectCollection Windows { get; private set; }
        public DBObjectCollection Cornices { get; private set; }
        public DBObjectCollection Slabs { get; private set; }
        public DBObjectCollection Beams { get; private set; }
        public List<ThEStoreyInfo> Storeys { get; private set; }
        public List<Curve> SpecialBelts { get; private set; }
        public List<Curve> DualpurposeBelts { get; private set; }
        public ThMEPOriginTransformer Transformer { get; set; }
        #endregion
        public ThDclDataInfo()
        {
            Slabs = new DBObjectCollection();
            Beams = new DBObjectCollection();
            SpecialBelts = new List<Curve>();
            Doors = new DBObjectCollection();
            Columns = new DBObjectCollection();
            Windows = new DBObjectCollection();
            Cornices = new DBObjectCollection();
            Storeys = new List<ThEStoreyInfo>();
            DualpurposeBelts = new List<Curve>();
            ShearWalls = new DBObjectCollection();
            CurtainWalls = new DBObjectCollection();
            ArchitectureWalls = new DBObjectCollection();  
        }
        public void Build(Database database,Point3dCollection pts)
        {
            // 提取
            var spetialBeals = ExtractSpecialBelt(database);
            var dualpurposeBelts = ExtractDualpurposeBelt(database);
            var storyes = ExtractStoreys(database);
            SpecialBelts = RecognizeSpecialBelts(spetialBeals, pts);// 专用接闪带                                                    
            DualpurposeBelts = RecognizeDualpurposeBelts(dualpurposeBelts, pts);// 兼用接闪带                                                              
            Storeys = RecognizeEStoreys(storyes, pts);// 楼层框定
            if (!IsValid())
            {
                return;
            }
            var vm = Extract(database);
            var columnDatas = new List<ThRawIfcBuildingElementData>();
            columnDatas.AddRange(vm.ColumnVisitor.Results);
            columnDatas.AddRange(vm.DB3ColumnVisitor.Results);
            Columns = RecognizeColumn(columnDatas, pts); // 柱
                                                         
            var shearWallObjs = RecognizeShearWall(vm.ShearWallVisitor.Results, pts);
            var db3ShearWallObjs = RecognizeDB3ShearWall(vm.DB3ShearWallVisitor.Results, pts);
            ShearWalls = ShearWalls.Union(shearWallObjs);
            ShearWalls = ShearWalls.Union(db3ShearWallObjs);// 剪力墙

            var archWallDatas = new List<ThRawIfcBuildingElementData>();
            archWallDatas.AddRange(vm.DB3ArchWallVisitor.Results);
            archWallDatas.AddRange(vm.DB3PcArchWallVisitor.Results);
            ArchitectureWalls = RecognizeDB3ArchWall(archWallDatas, pts);

            Beams = RecognizeDB3Beam(vm.DB3BeamVisitor.Results,pts); // 梁
            Windows = RecognizeDB3Window(vm.DB3WindowVisitor.Results, pts);// 窗户                                                          
            Slabs = RecognizeDB3Slab(vm.DB3SlabVisitor.Results, pts); // 楼板                                                       
            Cornices = RecognizeDB3Cornice(vm.DB3CorniceVisitor.Results, pts);// 线脚                                                              
            CurtainWalls = RecognizeDB3CurtainWall(vm.DB3CurtainWallVisitor.Results, pts);// 幕墙
                                                                                          // 
            var doorDatas = new List<ThRawIfcBuildingElementData>();
            doorDatas.AddRange(vm.DB3DoorMarkVisitor.Results);
            doorDatas.AddRange(vm.DB3DoorStoneVisitor.Results);
            Doors = RecognizeDB3Door(doorDatas, pts); // 门

            Transformer = pts.Count>0?CreateTransformer(pts): CreateTransformer();
        }
        public bool IsValid()
        {
            return Storeys.Count > 0 && (SpecialBelts.Count + DualpurposeBelts.Count) > 0;
        }
        private ThMEPOriginTransformer CreateTransformer()
        {
            var objs = new DBObjectCollection();
            objs = objs.Union(SpecialBelts.ToCollection());
            objs = objs.Union(DualpurposeBelts.ToCollection());
            return CreateTransformer(objs);
        }
        private ThMEPOriginTransformer CreateTransformer(Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            return new ThMEPOriginTransformer(center);
        }
        private ThMEPOriginTransformer CreateTransformer(DBObjectCollection objs)
        {
            return new ThMEPOriginTransformer(objs);
        }
        #region ----------- 提取 ----------
        private ThBuildingElementVisitorManager Extract(Database database)
        {
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);

            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);

            extractor.Accept(visitors.DB3PcArchWallVisitor);
            extractor.Accept(visitors.DB3ArchWallVisitor);

            extractor.Accept(visitors.DB3BeamVisitor);
            extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.DB3SlabVisitor);
            extractor.Accept(visitors.DB3CorniceVisitor);
            extractor.Accept(visitors.DB3CurtainWallVisitor);

            extractor.Accept(visitors.DB3DoorMarkVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Extract(database);
            return visitors;

        }
        private List<Curve> ExtractSpecialBelt(Database database)
        {
            var results = new List<Curve>();
            var service1 = new ThExtractLineService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service1.Extract(database, new Point3dCollection());
            results.AddRange(service1.Lines);

            var service2 = new ThExtractArcService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service2.Extract(database, new Point3dCollection());
            results.AddRange(service2.Arcs);

            var service3 = new ThExtractPolylineService()
            {
                ElementLayer = SpecialBeltLayer,
            };
            service3.Extract(database, new Point3dCollection());
            results.AddRange(service3.Polys);

            return results;
        }
        private List<Curve> ExtractDualpurposeBelt(Database database)
        {
            var results = new List<Curve>();
            var service1 = new ThExtractLineService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service1.Extract(database, new Point3dCollection());
            results.AddRange(service1.Lines);

            var service2 = new ThExtractArcService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service2.Extract(database, new Point3dCollection());
            results.AddRange(service2.Arcs);

            var service3 = new ThExtractPolylineService()
            {
                ElementLayer = DualpurposeBeltLayer,
            };
            service3.Extract(database, new Point3dCollection());
            results.AddRange(service3.Polys);
            return results;
        }
        private List<ThEStoreys> ExtractStoreys(Database database)
        {
            var engine = new ThEStoreysRecognitionEngine();
            engine.Recognize(database, new Point3dCollection());
            return engine.Elements.OfType<ThEStoreys>().ToList();
        }
        #endregion
        #region ----------- 识别 ----------
        private DBObjectCollection RecognizeColumn(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThColumnRecognitionEngine());
        }
        private DBObjectCollection RecognizeShearWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThShearWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3ShearWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3ShearWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3ArchWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3ArchWallRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Window(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3WindowRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Beam(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3BeamRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Slab(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3SlabRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3Cornice(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3CorniceRecognitionEngine());
        }
        private DBObjectCollection RecognizeDB3CurtainWall(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            return Recognize(datas, polygon, new ThDB3CurtainWallRecognitionEngine());
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
        private List<Curve> RecognizeSpecialBelts(List<Curve> belts, Point3dCollection pts)
        {
            return Recognize(belts.ToCollection(), pts).OfType<Curve>().ToList();
        }
        private List<Curve> RecognizeDualpurposeBelts(List<Curve> belts, Point3dCollection pts)
        {
            return Recognize(belts.ToCollection(), pts).OfType<Curve>().ToList();
        }
        private List<ThEStoreyInfo> RecognizeEStoreys(List<ThEStoreys> storeys, Point3dCollection pts)
        {
            if(pts.Count>0)
            {
                var results = new List<ThEStoreys>();
                var center = pts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(center);
                var newPts= transformer.Transform(pts);
                var polygon = newPts.CreatePolyline();
                return storeys.Where(o => polygon.Contains(transformer.Transform(o.Data.Position)))
                    .Select(o => new ThEStoreyInfo(o)).ToList();
            }
            return storeys.Select(o => new ThEStoreyInfo(o)).ToList();
        }
        private DBObjectCollection Recognize(List<ThRawIfcBuildingElementData> datas,Point3dCollection polygon,
            ThBuildingElementRecognitionEngine recognition)
        {
            var results = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            recognition.Recognize(datas, newPts);
            results = recognition.Elements.Select(o => o.Outline).ToCollection();
            transformer.Reset(results);
            return results;
        }
        private DBObjectCollection Recognize(DBObjectCollection objs, Point3dCollection pts)
        {
            if(pts.Count>0)
            {
                var transformer = new ThMEPOriginTransformer(objs);
                transformer.Transform(objs);
                var newPts = transformer.Transform(pts);
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var results = spatialIndex.SelectCrossingPolygon(pts);
                transformer.Reset(results);
                return results;
            }
            return objs;
        }
        private Dictionary<BuiltInCategory, DBObjectCollection> CreateDoorDependences()
        {
            var doorNeiborData = new Dictionary<BuiltInCategory, DBObjectCollection>();
            doorNeiborData.Add(BuiltInCategory.ArchitectureWall, ArchitectureWalls);
            doorNeiborData.Add(BuiltInCategory.ShearWall, ShearWalls);
            doorNeiborData.Add(BuiltInCategory.Column, Columns);
            doorNeiborData.Add(BuiltInCategory.CurtainWall, CurtainWalls);
            doorNeiborData.Add(BuiltInCategory.Window, Windows);
            return doorNeiborData;
        }
        #endregion
    }
}
