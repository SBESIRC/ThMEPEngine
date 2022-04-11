using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.EarthingGrid.Engine;

namespace ThMEPElectrical.EarthingGrid.Data
{
    public class ThEarthingGridDatasetFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; } = new List<ThGeometry>();
        public DBObjectCollection Beams { get; private set; } = new DBObjectCollection();        
        public DBObjectCollection Columns { get; private set; } = new DBObjectCollection();
        public DBObjectCollection Conductors { get; private set; } = new DBObjectCollection();
        public DBObjectCollection Shearwalls { get; private set; } = new DBObjectCollection();
        public DBObjectCollection MainBuildings { get; private set; } = new DBObjectCollection();
        public DBObjectCollection ConductorWires { get; private set; } = new DBObjectCollection();
        public DBObjectCollection ArchitectOutlines { get; private set; } = new DBObjectCollection();
        public List<Tuple<Point3d, Point3d>> BeamCenterLinePts { get; private set; }
        protected override ThMEPDataSet BuildDataSet()
        {
            // 目前是空的
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            var beams = ExtractBeams(database, collection);
            var columns = ExtractColumns(database, collection);
            var shearWalls = ExtractShearwalls(database, collection);
            Conductors = ExtractDownConductors(database, collection);
            ConductorWires = ExtractDownConductorWires(database, collection);
            ArchitectOutlines = ExtractArchitectureOutlines(database, collection);
            MainBuildings = ExtractMainBuildingsA(database, collection);
            BeamCenterLinePts = GetLinearBeamPts(beams);
            Beams = beams.Select(o => o.Outline).ToCollection();
            Columns = columns.Select(o => o.Outline).ToCollection();
            Shearwalls = shearWalls.Select(o => o.Outline).ToCollection();
        }
        private List<ThIfcBuildingElement> ExtractColumns(Database database, Point3dCollection pts)
        {
            var columnBuilder = new ThColumnBuilderEngine();
            columnBuilder.Build(database, pts);
            return columnBuilder.Elements;
        }
        private List<ThIfcBuildingElement> ExtractShearwalls(Database database, Point3dCollection pts)
        {
            var shearwallBuilder = new ThShearWallBuilderEngine();
            shearwallBuilder.Build(database, pts);
            return shearwallBuilder.Elements;
        }
        private List<ThIfcBuildingElement> ExtractBeams(Database database, Point3dCollection pts)
        {
            var beamBuilder = new ThBeamBuilderEngine();
            beamBuilder.Build(database, pts);
            return beamBuilder.Elements;
        }
        private DBObjectCollection ExtractDownConductors(Database database, Point3dCollection pts)
        {
            var elements = new List<ThRawIfcDistributionElementData>();
            var extractionEngine1 = new ThDownConductorExtractionEngine();
            extractionEngine1.ExtractFromMS(database);
            var extractionEngine2 = new ThDownConductorExtractionEngine();
            extractionEngine2.Extract(database);
            elements.AddRange(extractionEngine1.Results);
            elements.AddRange(extractionEngine2.Results);

            var transformer = new ThMEPOriginTransformer(pts.Envelope().CenterPoint());
            transformer.Transform(elements.Select(o =>o.Geometry).ToCollection());
            var newPts = transformer.Transform(pts);
            var recognitionEngine = new ThDownConductorRecognitionEngine();
            recognitionEngine.Recognize(elements, newPts);
            var results = recognitionEngine.Elements.Select(o => o.Outline).ToCollection();
            transformer.Reset(results);
            return results;
        }
        private DBObjectCollection ExtractDownConductorWires(Database database, Point3dCollection pts)
        {
            var extractionEngine = new ThDownConductorWireExtractionEngine();
            extractionEngine.ExtractFromEditor(pts);            
            return extractionEngine.Results.Select(o=>o.Geometry).ToCollection();
        }
        private DBObjectCollection ExtractArchitectureOutlines(Database database, Point3dCollection pts)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(database))
            {
                var outlines = acadDb.ModelSpace
                    .OfType<Polyline>()
                    .Where(p => p.Layer.ToUpper() == "AI-AREA-EXT") //AI-建筑轮廓线
                    .Select(o => o.Clone() as Polyline)
                    .ToCollection();
                var transformer = new ThMEPOriginTransformer(pts.Envelope().CenterPoint());
                transformer.Transform(outlines);
                var newPts = transformer.Transform(pts);
                var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(outlines);
                var results = spatialIndex.SelectCrossingPolygon(newPts);
                transformer.Reset(results);
                return results;
            }
        }
        private DBObjectCollection ExtractMainBuildingsA(Database database, Point3dCollection pts)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(database))
            {
                var outlines = acadDb.ModelSpace
                    .OfType<Polyline>()
                    .Where(p => p.Layer.ToUpper() == "AI-AREA-INT") 
                    .Select(o => o.Clone() as Polyline)
                    .ToCollection();
                var transformer = new ThMEPOriginTransformer(pts.Envelope().CenterPoint());
                transformer.Transform(outlines);
                var newPts = transformer.Transform(pts);
                var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(outlines);
                var results = spatialIndex.SelectCrossingPolygon(newPts);
                transformer.Reset(results);
                return results;
            }
        }
        private DBObjectCollection ExtractMainBuildings(Database database,Point3dCollection pts)
        {
            // 主楼填充数据
            var mainBuildingVisitor = new ThMainBuildingHatchExtractionVisitor()
            {
                LayerFilter = ThMainBuildingLayerManager.HatchXrefLayers(database),
            };
            var spatialExtractor = new ThSpatialElementExtractor();
            spatialExtractor.Accept(mainBuildingVisitor);
            spatialExtractor.Extract(database);
            mainBuildingVisitor.Results
                .ForEach(o =>
                {
                    if (o.Geometry is Polyline pl)
                        pl.Closed = true;
                }
               );
            var mainBuildings = mainBuildingVisitor.Results.Select(o => o.Geometry).ToCollection();
            var newMainBuildings = Clean(mainBuildings);
            mainBuildings = mainBuildings.Difference(newMainBuildings);
            mainBuildings.OfType<Curve>().ForEach(c => c.Dispose());

            var transformer = new ThMEPOriginTransformer(pts.Envelope().CenterPoint());
            transformer.Transform(newMainBuildings);
            var newPts = transformer.Transform(pts);
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(newMainBuildings);
            var results = spatialIndex.SelectCrossingPolygon(newPts);
            transformer.Reset(results);
            newMainBuildings = newMainBuildings.Difference(results);
            newMainBuildings.OfType<Curve>().ForEach(c => c.Dispose());
            return results;
        }
        private DBObjectCollection Clean(DBObjectCollection objs)
        {
            var simplifier = new ThPolygonalElementSimplifier();
            var results = simplifier.Simplify(objs);
            results = simplifier.Normalize(results);
            return results;
        }
        private List<Tuple<Point3d,Point3d>> GetLinearBeamPts(List<ThIfcBuildingElement> beams)
        {
            return beams
                .OfType<ThIfcLineBeam>()
                .Select(o => Tuple.Create(o.StartPoint,o.EndPoint))
                .ToList();
        }
    }
}
