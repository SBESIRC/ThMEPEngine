using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Electrical;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPElectrical.DCL.Service;

namespace ThMEPElectrical.DCL.Data
{
    public class ThDclDataSetFactory : ThMEPDataSetFactory,IDisposable
    {
        private double _beltTesslateLength = 200.0;
        private string _specialBeltLayer = "E-THUN-WIRE";
        private string _dualpurposeBeltLayer = "E-GRND-WIRE";
        private List<ThEStoreyInfo> _storeys = new List<ThEStoreyInfo>();
        private HashSet<DBObject> _garbageCollector = new HashSet<DBObject>();
        private List<ThGeometry>  _geos = new List<ThGeometry>();

        /// <summary>
        /// estoreys请从高到低排序
        /// </summary>
        /// <param name="estoreys"></param>
        public ThDclDataSetFactory(List<ThEStoreys> estoreys)
        {
            if(estoreys!=null)
            {
                _storeys = estoreys.Select(o => new ThEStoreyInfo(o)).ToList();
            }            
        }
        public void Dispose()
        {
            _garbageCollector.ToCollection().MDispose();
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = _geos,
            }; 
        }

        protected override void GetElements(Database database, Point3dCollection pts)
        {
            if(this._storeys.Count==0)
            {
                return;
            }
            // 提取全部数据
            var visitors = CreateVisitors(database);
            var allRangePts = new Point3dCollection();
            Extract(database, visitors);
            var architectureOutlines = ExtractPolylines(database, allRangePts, "AI-建筑轮廓线"); // 建筑轮廓线
            var specialBelts = ExtractSpecialBelt(database, allRangePts);
            var dualpurposeBelts = ExtractDualpurposeBelt(database, allRangePts);
            specialBelts.ForEach(o => AddToGarbageCollector(o));
            dualpurposeBelts.ForEach(o => AddToGarbageCollector(o));
            specialBelts = Tesslate(specialBelts, _beltTesslateLength);
            dualpurposeBelts = Tesslate(dualpurposeBelts, _beltTesslateLength);
            var extractObjs = new DBObjectCollection();
            extractObjs = extractObjs.Union(visitors.SelectMany(o => o.Results).Select(o => o.Geometry).ToCollection());
            extractObjs = extractObjs.Union(specialBelts.ToCollection());
            extractObjs = extractObjs.Union(dualpurposeBelts.ToCollection());
            extractObjs = extractObjs.Union(dualpurposeBelts.ToCollection());
            extractObjs = extractObjs.Union(architectureOutlines.ToCollection());
            AddToGarbageCollector(extractObjs);
            
            // 创建Transformer，把提取的对象移动到近似原点
            var storeyBoundaries = this._storeys.Select(o => o.Boundary).ToCollection();
            var transformer = new ThMEPOriginTransformer(storeyBoundaries);
            transformer.Transform(extractObjs);
            transformer.Transform(storeyBoundaries);

            // prepare data
            var columnElements = new List<ThRawIfcBuildingElementData>();
            columnElements.AddRange(GetColumnElements(visitors));
            columnElements.AddRange(GetDB3ColumnElements(visitors));

            var shearWallElements = new List<ThRawIfcBuildingElementData>();
            shearWallElements.AddRange(GetShearWallElements(visitors));
            shearWallElements.AddRange(GetDB3ShearWallElements(visitors));

            var beamElements = new List<ThRawIfcBuildingElementData>();
            beamElements.AddRange(GetRawBeamElements(visitors));
            beamElements.AddRange(GetDB3BeamElements(visitors));
            var lplwStoreyDatas = new List<LightProtectLeadWireStoreyData>();
            int storeyCount = this._storeys.Count;
            for (int i = storeyCount-1; i >=0 ; i--)
            {
                //this._storeys[i]->楼层顺序是从高到低
                //给到浙大算法的顺序必须是从低到高
                var naturalFlrNo = (storeyCount-i) + "F";
                var currentStorey = this._storeys[i];
                var storeyBoundaryPts = currentStorey.Boundary.Vertices();                

                var columnEngine = CreateColumnRecognizeEngine(columnElements, storeyBoundaryPts);
                var shearWallEngine = CreateShearWallRecognizeEngine(shearWallElements, storeyBoundaryPts);
                var beamEngine = CreateBeamRecognizeEngine(beamElements, storeyBoundaryPts);

                // 识别主梁、悬挑梁
                var beamTypeEngine = new ThBeamConnectRecogitionEngine();
                beamTypeEngine.Recognize(columnEngine, shearWallEngine, beamEngine);                

                // 识别建筑内外轮廓线
                var storeyArchOutlines = SelectWindowPolygon(architectureOutlines.ToCollection(), storeyBoundaryPts);
                var architectureOutlineAreas = RecognizeArchitectureOutlineAreas(storeyArchOutlines,500.0,1000.0,1.0);

                // 本层接闪带
                var storeySpecialBelts = SelectWindowPolygon(specialBelts.ToCollection(), storeyBoundaryPts);
                var storeyDualpurposeBelts = SelectWindowPolygon(dualpurposeBelts.ToCollection(), storeyBoundaryPts);

                // 识别外圈柱、其它柱、外圈剪力墙、其它剪力墙
                var struData = new ThStruOuterVertialComponentData()
                {
                    Columns = columnEngine.Geometries,
                    Shearwalls = shearWallEngine.Geometries,
                    ArchOutlineAreas = architectureOutlineAreas,
                    PrimaryBeams = beamTypeEngine.PrimaryBeamLinks,
                    OverhangingPrimaryBeams = beamTypeEngine.OverhangingPrimaryBeamLinks,
                };
                var vcEngine = new ThStruOuterVerticalComponentRecognizer(struData);
                vcEngine.Recognize();
                var lplwStoreyData = new LightProtectLeadWireStoreyData()
                {
                    StoreyId = currentStorey.Id,                    
                    FloorNumber = naturalFlrNo,                    
                    BasePoint = currentStorey.BasePoint,
                    StoreyType = currentStorey.StoreyType,
                    StoreyFrameBoundary = currentStorey.Boundary,
                    Beams = beamTypeEngine.BeamEngine.Geometries,
                    ArchOutlineAreas = architectureOutlineAreas,
                    OuterColumns = vcEngine.OuterColumnsMap,
                    OtherColumns = vcEngine.OtherColumns.ToCollection(),
                    OuterShearWalls = vcEngine.OuterShearWallsMap,
                    OtherShearWalls = vcEngine.OtherShearWalls.ToCollection(),
                    SpecialBelts = storeySpecialBelts.OfType<Curve>().ToList(),
                    DualpurposeBelts= storeyDualpurposeBelts.OfType<Curve>().ToList(),
                };
                lplwStoreyDatas.Add(lplwStoreyData);
            }

            // 接下来会在“BuildDataSet”函数中，制造Geo数据，还原位置
            var geoFactory = new ThLightningProtectLeadWireGeoFactory();
            lplwStoreyDatas.ForEach(o => _geos.AddRange(geoFactory.Work(o)));

            // 还原位置
            var geometries = _geos.Select(o => o.Boundary).ToHashSet<DBObject>().ToCollection();
            transformer.Reset(geometries);
            _garbageCollector.ExceptWith(_geos.Select(o => o.Boundary).ToHashSet());
        }        
        private List<Curve> Tesslate(List<Curve> curves,double length)
        {
            return curves.Select(o => ThTesslateService.Tesslate(o, length) as Curve).ToList();
        }        
        private void AddToGarbageCollector(DBObjectCollection objs)
        {
            _garbageCollector.UnionWith(objs.OfType<DBObject>().ToHashSet());
        }
        private void AddToGarbageCollector(DBObject obj)
        {
            _garbageCollector.Add(obj);
        }
        #region ---------- Extract datas --------------- 
        private void Extract(Database database, List<ThBuildingElementExtractionVisitor> visitors)
        {
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.ToArray());
            extractor.Extract(database);
        }
        private List<ThBuildingElementExtractionVisitor> CreateVisitors(Database database)
        {
            var visitors = new List<ThBuildingElementExtractionVisitor>();
            // 房间标准数据
            var vm = new ThBuildingElementVisitorManager(database);
            visitors.Add(vm.ColumnVisitor);
            visitors.Add(vm.DB3ColumnVisitor);
            visitors.Add(vm.ShearWallVisitor);
            visitors.Add(vm.DB3ShearWallVisitor);
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                visitors.Add(vm.DB3BeamVisitor);
            }
            else
            {
                visitors.Add(vm.RawBeamVisitor);
            }
            return visitors;
        }
        private List<ThRawIfcBuildingElementData> GetColumnElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThColumnExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }
        private List<ThRawIfcBuildingElementData> GetDB3ColumnElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3ColumnExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetShearWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThShearWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }
        private List<ThRawIfcBuildingElementData> GetDB3ShearWallElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3ShearWallExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetDB3BeamElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThDB3BeamExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }

        private List<ThRawIfcBuildingElementData> GetRawBeamElements(List<ThBuildingElementExtractionVisitor> visitors)
        {
            return visitors.Where(o => o is ThRawBeamExtractionVisitor)
                .SelectMany(o => o.Results).ToList();
        }
        private List<Polyline> ExtractPolylines(Database database, Point3dCollection pts, string layer)
        {
            var extractor = new ThExtractPolylineService()
            {
                ElementLayer = layer,
            };
            extractor.Extract(database, pts);
            return extractor.Polys;
        }
        private List<Curve> ExtractSpecialBelt(Database database, Point3dCollection pts)
        {
            var results = new List<Curve>();
            var service1 = new ThExtractLineService()
            {
                ElementLayer = _specialBeltLayer,
            };
            service1.Extract(database, pts);
            results.AddRange(service1.Lines);

            var service2 = new ThExtractArcService()
            {
                ElementLayer = _specialBeltLayer,
            };
            service2.Extract(database, pts);
            results.AddRange(service2.Arcs);

            var service3 = new ThExtractPolylineService()
            {
                ElementLayer = _specialBeltLayer,
            };
            service3.Extract(database, pts);
            results.AddRange(service3.Polys);

            return results;
        }
        private List<Curve> ExtractDualpurposeBelt(Database database, Point3dCollection pts)
        {
            var results = new List<Curve>();
            var service1 = new ThExtractLineService()
            {
                ElementLayer = _dualpurposeBeltLayer,
            };
            service1.Extract(database, pts);
            results.AddRange(service1.Lines);

            var service2 = new ThExtractArcService()
            {
                ElementLayer = _dualpurposeBeltLayer,
            };
            service2.Extract(database, pts);
            results.AddRange(service2.Arcs);

            var service3 = new ThExtractPolylineService()
            {
                ElementLayer = _dualpurposeBeltLayer,
            };
            service3.Extract(database, pts);
            results.AddRange(service3.Polys);
            return results;
        }
        #endregion
        #region---------- Recognize datas -------------
        private List<MPolygon> RecognizeArchitectureOutlineAreas(
            DBObjectCollection outlines, double offsetDistance=20.0,
            double tesslateLength=100.0, double areaTolerance=1.0)
        {
            var cleanDatas = CleanArchitectureOutlines(outlines, offsetDistance, tesslateLength, areaTolerance);
            var areas = CreateOuterInnerOuline(cleanDatas);
            var results = new List<MPolygon>(); 
            areas.OfType<Entity>().ForEach(e =>
            {
                if(e is Polyline polyline)
                {
                    results.Add(ThMPolygonTool.CreateMPolygon(polyline));
                }
                else if(e is MPolygon polygon)
                {
                    results.Add(polygon);
                }
            });
            return results;
        }
        private DBObjectCollection CleanArchitectureOutlines(
            DBObjectCollection architectureOutlines, double offsetDistance,
            double tesslateLength, double areaTolerance)
        {            
            var roomSimplifier = new ThRoomOutlineSimplifier()
            {
                AREATOLERANCE = areaTolerance,
                OFFSETDISTANCE = offsetDistance,
                TESSELLATEARCLENGTH = tesslateLength,
            };
            var results = roomSimplifier.Filter(architectureOutlines);
            results = roomSimplifier.Normalize(results);
            results = results.FilterSmallArea(areaTolerance);
            results = roomSimplifier.MakeValid(results);
            results = results.FilterSmallArea(areaTolerance);
            results = roomSimplifier.Simplify(results);
            results = results.FilterSmallArea(areaTolerance);
            results = roomSimplifier.OverKill(results);
            return results;
        }
        private DBObjectCollection CreateOuterInnerOuline(DBObjectCollection architectureOutlines)
        {            
            var bufferObjs = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            architectureOutlines
                .OfType<Polyline>()
                .ForEach(p =>
                {
                    var entity = bufferService.Buffer(p, -1.0);
                    if (entity != null)
                    {
                        bufferObjs.Add(entity);
                    }
                });
            AddToGarbageCollector(bufferObjs);
            var areas = bufferObjs.BuildArea();
            AddToGarbageCollector(areas);
            areas = areas.FilterSmallArea(1.0);
            var results = new DBObjectCollection();
            areas.OfType<Entity>().ForEach(e =>
            {
                var entity = bufferService.Buffer(e, 1);
                if (entity != null)
                {
                    results.Add(entity);
                }
            });
            return results;
        }
        private DBObjectCollection SelectWindowPolygon(DBObjectCollection objs, Point3dCollection frame)
        {
            var window = frame.CreatePolyline();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var results = spatialIndex.SelectWindowPolygon(window);
            window.Dispose();
            return results;
        }
        private ThColumnRecognitionEngine CreateColumnRecognizeEngine(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var engine = new ThColumnRecognitionEngine();
            engine.Recognize(datas, polygon);
            return engine;
        }
        private ThShearWallRecognitionEngine CreateShearWallRecognizeEngine(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var engine = new ThShearWallRecognitionEngine();
            engine.Recognize(datas, polygon);
            return engine;
        }
        private ThBeamBuilderEngine CreateBeamRecognizeEngine(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var engine = new ThBeamBuilderEngine();
            engine.Recognize(datas, polygon);
            return engine;
        }
        #endregion
        #region ---------- Print Geometry --------------
        public void PrintOuterColumns(short colorIndex,string layer="0")
        {
            string category = BuiltInCategory.Column.ToString();
            var nameKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.NamePropertyName;
            var categoryKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.CategoryPropertyName;
            var outerColumns = _geos.Where(o => o.Properties[categoryKey].ToString() == category)
                .Where(o => o.Properties[nameKey].ToString() == "外圈柱")
                .Select(o => o.Boundary.Clone() as Entity).ToCollection();
            PrintEntities(outerColumns, colorIndex, layer);
        }
        public void PrintOuterShearWalls(short colorIndex, string layer = "0")
        {
            string category = BuiltInCategory.ShearWall.ToString();
            var nameKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.NamePropertyName;
            var categoryKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.CategoryPropertyName;
            var outerColumns = _geos.Where(o => o.Properties[categoryKey].ToString() == category)
                .Where(o => o.Properties[nameKey].ToString() == "外圈剪力墙")
                .Select(o => o.Boundary.Clone() as Entity).ToCollection();
            PrintEntities(outerColumns, colorIndex, layer);
        }
        public void PrintOtherColumns(short colorIndex, string layer = "0")
        {
            string category = BuiltInCategory.Column.ToString();
            var nameKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.NamePropertyName;
            var categoryKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.CategoryPropertyName;
            var outerColumns = _geos.Where(o => o.Properties[categoryKey].ToString() == category)
                .Where(o => o.Properties[nameKey].ToString() == "其他柱")
                .Select(o => o.Boundary.Clone() as Entity).ToCollection();
            PrintEntities(outerColumns, colorIndex, layer);
        }
        public void PrintOtherShearWalls(short colorIndex, string layer = "0")
        {
            string category = BuiltInCategory.ShearWall.ToString();
            var nameKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.NamePropertyName;
            var categoryKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.CategoryPropertyName;
            var outerColumns = _geos.Where(o => o.Properties[categoryKey].ToString() == category)
                .Where(o => o.Properties[nameKey].ToString() == "其他剪力墙")
                .Select(o => o.Boundary.Clone() as Entity).ToCollection();
            PrintEntities(outerColumns, colorIndex, layer);
        }

        public void PrintArchOutlines(short colorIndex, string layer = "0")
        {
            string category = BuiltInCategory.ArchitectureOutline.ToString();
            var categoryKey = ThMEPEngineCore.IO.ThExtractorPropertyNameManager.CategoryPropertyName;
            var archOutlines = _geos.Where(o => o.Properties[categoryKey].ToString() == category)
                .Select(o => o.Boundary.Clone() as Entity).ToCollection();
            PrintEntities(archOutlines, colorIndex, layer);
        }

        private void PrintEntities(DBObjectCollection objs, short colorIndex, string layer)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                objs.OfType<Entity>().ForEach(e =>
                {
                    var clone = e.Clone() as Entity;
                    acadDb.ModelSpace.Add(clone);
                    clone.ColorIndex = colorIndex;
                    clone.Layer = layer;
                });
            }                
        }
        #endregion
    }
}
