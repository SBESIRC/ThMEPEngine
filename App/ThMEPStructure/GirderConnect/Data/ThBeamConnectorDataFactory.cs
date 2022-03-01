using System;
using System.Linq;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm.Match;
using Dreambuild.AutoCAD;

namespace ThMEPStructure.GirderConnect.Data
{
    class ThBeamConnectorDataFactory : ThMEPDataSetFactory
    {
        public bool ExpandColumn { get; set; } = true;

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
            geos.AddRange(Columns.OfType<Entity>().Select(e => new ThGeometry { Boundary = e }).ToList());
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
            Columns = Columns.Union(columns);
            Columns = Columns.Union(columns1);
            Shearwalls = Shearwalls.Union(shearwalls);
            Shearwalls = Shearwalls.Union(shearwalls1);
            MainBuildings = ExtractMainBuildings(database);

            //Transformer = new ThMEPOriginTransformer(Point3d.Origin); // for test(正式发布的时候删除)`

            // 移动到原点
            Move(Columns, Transformer);
            Move(Shearwalls, Transformer);
            Move(MainBuildings, Transformer);
            var newPts = collection.OfType<Point3d>().Select(o => Transformer.Transform(o)).ToCollection();

            // 数据处理
            var spatialIndex = new ThCADCoreNTSSpatialIndex(MainBuildings);
            MainBuildings = spatialIndex.SelectCrossingPolygon(newPts);

            Columns = RemoveDuplicated(Columns);
            Shearwalls = RemoveDuplicated(Shearwalls);
            MainBuildings = RemoveDuplicated(MainBuildings);

            Columns = SimilarityMeasure(Columns);
            Shearwalls = SimilarityMeasure(Shearwalls);
            MainBuildings = SimilarityMeasure(MainBuildings);

            Columns = Union(Columns);
            MainBuildings = Union(MainBuildings);

            // 还原位置
            Reset(Columns, Transformer);
            Reset(Shearwalls, Transformer);
            Reset(MainBuildings, Transformer);
        }
        private DBObjectCollection ExtractColumns(Database database, Point3dCollection pts)
        {
            using (var ov = new ThBeamConnectorExpandColumnOverride(false))
            {
                var columnBuilder = new ThColumnBuilderEngine();
                columnBuilder.Build(database, pts);
                return columnBuilder.Elements.Select(o => o.Outline).ToCollection();
            }
        }

        private DBObjectCollection ExtractColumns1(Database database, Point3dCollection pts)
        {
            using (var ov = new ThBeamConnectorExpandColumnOverride(false))
            {
                var allLayers = GetAllLayers(database);
                var coluLayers = allLayers.Where(o => o.ToUpper().EndsWith("S_COLU")).ToList();
                var columnVisitor = new ThCurveExtractionVisitor()
                {
                    LayerFilter = coluLayers,
                };
                var extractor = new ThBuildingElementExtractor();
                extractor.Accept(columnVisitor);
                extractor.Extract(database);

                var transformer = new ThMEPOriginTransformer(columnVisitor.Results.Select(o => o.Geometry).ToCollection());
                columnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
                var newPts = pts.OfType<Point3d>().Select(p => transformer.Transform(p)).ToCollection();

                var columnBuilderEngine = new ThColumnBuilderEngine();
                columnBuilderEngine.Recognize(columnVisitor.Results, newPts);
                var objs = columnBuilderEngine.Elements.Select(o => o.Outline).ToCollection();
                transformer.Reset(objs);
                return objs;
            }
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
            var shearwallBuilder = new ThShearWallBuilderEngine();
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

            var shearwallBuilder = new ThShearWallBuilderEngine();
            shearwallBuilder.Recognize(visitor.Results, newPts);
            var objs = shearwallBuilder.Elements.Select(o => o.Outline).ToCollection();
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
            mainBuildingVisitor.Results
                .ForEach(o =>
                    {
                        if (o.Geometry is Polyline pl)
                            pl.Closed = true;
                    }
               );
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
        private DBObjectCollection Union(DBObjectCollection polygons)
        {
            var results = polygons.UnionPolygons();
            results = results.FilterSmallArea(1.0);
            var simplifer = new ThPolygonalElementSimplifier();
            results = simplifer.Normalize(results);
            results = results.FilterSmallArea(1.0);
            results = simplifer.MakeValid(results);
            results = results.FilterSmallArea(1.0);
            results = simplifer.Simplify(results);
            results = results.FilterSmallArea(1.0);
            return results;
        }

        private DBObjectCollection RemoveDuplicated(DBObjectCollection objs)
        {
            return ThCADCoreNTSGeometryFilter.GeometryEquality(objs);
        }

        private void Move(DBObjectCollection objs, ThMEPOriginTransformer transformer)
        {
            transformer.Transform(objs);
        }

        private void Reset(DBObjectCollection objs, ThMEPOriginTransformer transformer)
        {
            transformer.Reset(objs);
        }

        private DBObjectCollection SimilarityMeasure(DBObjectCollection polygons)
        {
            var similarity = new PolygonSimilarityMeasure(polygons);
            similarity.SimilarityMeasure();
            return similarity.Results;
        }
    }
    internal class PolygonSimilarityMeasure
    {
        private const double degree = 0.95;
        private DBObjectCollection Objs { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public DBObjectCollection Results { get; private set; }
        private DBObjectCollection Garbage { get; set; }
        private Dictionary<Entity, Polygon> PolygonDict { get; set; } = new Dictionary<Entity, Polygon>();

        public PolygonSimilarityMeasure(DBObjectCollection objs)
        {
            Garbage = new DBObjectCollection();
            Results = new DBObjectCollection();
            Objs = objs.OfType<Entity>().Where(o => o.EntityArea() > 0.0).ToCollection();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(Objs);
            Objs.OfType<Entity>().ForEach(o => PolygonDict.Add(o, ToPolygon(o)));
        }
        public void SimilarityMeasure()
        {
            var bufferService = new ThNTSBufferService();
            Objs.OfType<Entity>().ForEach(o =>
            {
                if (!Garbage.Contains(o))
                {
                    var enlarge = bufferService.Buffer(o, 1.0);
                    var innerObjs = SpatialIndex.SelectWindowPolygon(enlarge);
                    innerObjs.Remove(o);
                    innerObjs.OfType<Entity>()
                    .Where(e => IsClose(o.EntityArea(), e.EntityArea()))
                    .Where(e => IsSimilar(PolygonDict[o], PolygonDict[e])).ForEach(e => Garbage.Add(e));
                    Results.Add(o);
                }
            });
        }
        private bool IsClose(double a, double b)
        {
            return Math.Abs(a / b - 1.0) <= (1 - degree);

        }
        private Polygon ToPolygon(Entity polygon)
        {
            if (polygon is Polyline polyline)
            {
                return polyline.ToNTSPolygon();
            }
            else if (polygon is MPolygon mPolygon)
            {
                return mPolygon.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private bool IsSimilar(Polygon first, Polygon second)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(first, second) >= degree;
        }

    }
}
