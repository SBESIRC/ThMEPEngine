using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using Linq2Acad;
using ThMEPEngineCore.Service;
using ThCADExtension;

namespace ThMEPStructure.GirderConnect.Data
{
    class ThBeamConnectorDataFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; } = new List<ThGeometry>();
        public DBObjectCollection Columns { get; private set; } = new DBObjectCollection();
        public DBObjectCollection Shearwalls { get; private set; } = new DBObjectCollection();
        public DBObjectCollection MainBuildings { get; private set; } = new DBObjectCollection();

        public ThBeamConnectorDataFactory()
        {
        }

        protected override ThMEPDataSet BuildDataSet()
        {
            var geos = new List<ThGeometry>();
            geos.AddRange(Columns.OfType<Entity>().Select(e => new ThGeometry { Boundary= e}).ToList());
            geos.AddRange(Shearwalls.OfType<Entity>().Select(e => new ThGeometry { Boundary = e }).ToList());
            geos.AddRange(MainBuildings.OfType<Entity>().Select(e => new ThGeometry { Boundary = e }).ToList());
            return new ThMEPDataSet() { Container = Geos };
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            UpdateTransformer(collection);
            var columns = ExtractColumns(database, collection);
            var columns1 = ExtractColumns1(database, collection);

            var shearwalls = ExtractShearwalls(database, collection);
            var shearwalls1 = ExtractShearwalls1(database, collection);

            var mainBuildings = ExtractMainBuildings(database);

            Transformer = new ThMEPOriginTransformer(Point3d.Origin); // for test(正式发布的时候删除)`

            // 移动到原点
            Move(mainBuildings, Transformer);

            // 数据处理
           var newPts = collection.OfType<Point3d>().Select(o => Transformer.Transform(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(mainBuildings);
            mainBuildings = spatialIndex.SelectCrossingPolygon(newPts);

            // 还原位置
            Reset(mainBuildings, Transformer);

            // 收集数据
            Columns = Columns.Union(columns);
            Columns = Columns.Union(columns1);
            Shearwalls = Shearwalls.Union(shearwalls);
            Shearwalls = Shearwalls.Union(shearwalls1);
            MainBuildings = mainBuildings;
        }
        private DBObjectCollection ExtractColumns(Database database,Point3dCollection pts)
        {
            var columnBuilder = new ThColumnBuilderEngine();
            columnBuilder.Build(database, pts);
            return columnBuilder.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractColumns1(Database database, Point3dCollection pts)
        {
            var allLayers = GetAllLayers(database);
            var coluLayers = allLayers.Where(o=>o.ToUpper().EndsWith("S_COLU")).ToList();
            var columnVisitor = new ThCurveExtractionVisitor()
            {
                LayerFilter = coluLayers,
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(columnVisitor);
            extractor.Extract(database);

            var transformer =new ThMEPOriginTransformer(columnVisitor.Results.Select(o=>o.Geometry).ToCollection());
            columnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts.OfType<Point3d>().Select(p => transformer.Transform(p)).ToCollection();

            var columnBuilderEngine = new ThColumnBuilderEngine();
            var results = columnBuilderEngine.Recognize(columnVisitor.Results, newPts);
            var objs = results.Select(o => o.Outline).ToCollection();
            transformer.Reset(objs);
            return objs;
        }

        private List<string> GetAllLayers(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }

        private DBObjectCollection ExtractShearwalls(Database database, Point3dCollection pts)
        {
            var shearwallBuilder = new ThShearwallBuilderEngine();
            shearwallBuilder.Build(database, pts);
            return shearwallBuilder.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractShearwalls1(Database database, Point3dCollection pts)
        {
            var allLayers = GetAllLayers(database);
            var wallLayers = allLayers.Where(o => o.ToUpper().EndsWith("S_WALL")).ToList();
            var visitor = new ThCurveExtractionVisitor()
            {
                LayerFilter = wallLayers,
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);

            var transformer = new ThMEPOriginTransformer(visitor.Results.Select(o => o.Geometry).ToCollection());
            visitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            var newPts = pts.OfType<Point3d>().Select(p => transformer.Transform(p)).ToCollection();

            var shearwallBuilder = new ThShearwallBuilderEngine();
            var results = shearwallBuilder.Recognize(visitor.Results, newPts);
            var objs = results.Select(o => o.Outline).ToCollection();
            transformer.Reset(objs);
            return objs;
        }


        private DBObjectCollection ExtractMainBuildings(Database database)
        {
            // 主楼填充数据
            var mainBuildingVisitor = new ThMainBuildingHatchExtractionVisitor()
            {
                LayerFilter = ThMainBuildingLayerManager.HatchXrefLayers(database),
            };
            var spatialExtractor = new ThSpatialElementExtractor();
            spatialExtractor.Accept(mainBuildingVisitor);
            spatialExtractor.Extract(database);
            var mainBuildings = mainBuildingVisitor.Results.Select(o => o.Geometry).ToCollection();
            return Clean(mainBuildings);
        }

        private DBObjectCollection Clean(DBObjectCollection objs)
        {
            var simplifier = new ThPolygonalElementSimplifier();
            var results = simplifier.Simplify(objs);
            results = simplifier.Normalize(results);
            return results;
        }

        private  void Move(DBObjectCollection objs, ThMEPOriginTransformer transformer)
        {
            transformer.Transform(objs);
        }

        private void Reset(DBObjectCollection objs, ThMEPOriginTransformer transformer)
        {
            transformer.Reset(objs);
        }
    }
}
