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

namespace ThMEPStructure.GirderConnect.Data
{
    class ThBeamConnectorDataFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; }
        public DBObjectCollection Columns { get; private set; }
        public DBObjectCollection Shearwalls { get; private set; }
        public DBObjectCollection MainBuildings { get; private set; }

        public ThBeamConnectorDataFactory()
        {
            Geos = new List<ThGeometry>();
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
            var shearwalls = ExtractShearwalls(database, collection);
            var mainBuildings = ExtractMainBuildings(database);
            
            // for test
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);

            // 移动到原点
            Move(mainBuildings, Transformer);

            // 数据处理
           var newPts = collection.OfType<Point3d>().Select(o => Transformer.Transform(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(mainBuildings);
            mainBuildings = spatialIndex.SelectCrossingPolygon(newPts);

            // 还原位置
            Reset(mainBuildings, Transformer);

            // 收集数据
            Columns = columns;
            Shearwalls = shearwalls;
            MainBuildings = mainBuildings;
        }
        private DBObjectCollection ExtractColumns(Database database,Point3dCollection pts)
        {
            var columnBuilder = new ThColumnBuilderEngine();
            columnBuilder.Build(database, pts);
            return columnBuilder.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractShearwalls(Database database, Point3dCollection pts)
        {
            var shearwallBuilder = new ThShearwallBuilderEngine();
            shearwallBuilder.Build(database, pts);
            return shearwallBuilder.Elements.Select(o => o.Outline).ToCollection();
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
            return mainBuildingVisitor.Results.Select(o => o.Geometry).ToCollection();
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
