using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    internal class ThUserDatasetFactory : ThMEPDataSetFactory
    {
        private DBObjectCollection Boundaries { get; set; }
        private DBObjectCollection Obstacles { get; set; }
        private ThMEPDataSet DataSet { get; set; }
        public ThUserDatasetFactory()
        {
            DataSet = new ThMEPDataSet() 
            { 
                Container = new List<ThGeometry>(),
            };
            Obstacles = new DBObjectCollection();
            Boundaries = new DBObjectCollection();
            Transformer= new ThMEPOriginTransformer(Point3d.Origin);
        }

        protected override ThMEPDataSet BuildDataSet()
        {
            return DataSet;
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            Extract(database);
            Transformer = CreateTransformer();

            MoveToOrigin();
            var newPts = Transformer.Transform(collection);

            Obstacles = Recognize(Obstacles, newPts);
            Boundaries = Recognize(Boundaries, newPts);

            var boundaryGeoFacotry = new ThBoundaryGeoJsonFactory(Boundaries);
            var boundaryGeos = boundaryGeoFacotry.BuildGeometries();

            var enlargeBoundaries = Buffer(Boundaries, 50.0);
            var groupDict = CreateBoundaryIds(enlargeBoundaries);
            var obstacleGeoFacotry = new ThObstacleGeoJsonFactory(Obstacles);
            obstacleGeoFacotry.Group(groupDict);
            var obstacleGeos = obstacleGeoFacotry.BuildGeometries();

            DataSet.Container.AddRange(boundaryGeos);
            DataSet.Container.AddRange(obstacleGeos);

            Reset();
        }

        private DBObjectCollection Buffer(DBObjectCollection objs,double length)
        {
            var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
            return objs.OfType<Entity>()
                .Select(e => bufferService.Buffer(e, length))
                .Where(o => o != null)
                .ToCollection();
        }

        private Dictionary<Entity,string> CreateBoundaryIds(DBObjectCollection objs)
        {
            var results = new Dictionary<Entity, string>();
            objs.OfType<Entity>().ForEach(e => results.Add(e, Guid.NewGuid().ToString()));
            return results;
        }
        private void Extract(Database database)
        {
            var extractor = new ThBuildingElementExtractorEx();
            var boundaryVistor = new ThBoundaryExtractionVisitor()
            {
                LayerFilter = new HashSet<string> { "地库边界" },
            };
            var obstacleVisitor = new ThObstacleExtractionVisitor()
            { 
                LayerFilter = new HashSet<string> { "障碍物边缘" },
            };
            extractor.Accept(boundaryVistor);
            extractor.Accept(obstacleVisitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            Obstacles = obstacleVisitor.Results.Select(o => o.Geometry).ToCollection();
            Boundaries = boundaryVistor.Results.Select(o => o.Geometry).ToCollection();
        }
        private ThMEPOriginTransformer CreateTransformer()
        {
            return Boundaries.Count>0 ?
                new ThMEPOriginTransformer(Boundaries): 
                new ThMEPOriginTransformer(Obstacles);
        }
        private void MoveToOrigin()
        {
            Transformer.Transform(Obstacles);
            Transformer.Transform(Boundaries);
        }
        private void Reset()
        {
            Transformer.Reset(Obstacles);
            Transformer.Reset(Boundaries);
        }
        private DBObjectCollection Recognize(DBObjectCollection objs,Point3dCollection pts)
        {
            var results = new DBObjectCollection();
            if (pts.Count>0)
            {
                var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(objs);
                results = spatialIndex.SelectCrossingPolygon(pts);
            }
            else
            {
                results = objs;
            }
            var simplifier = new ThPolygonalElementSimplifier();
            results = simplifier.Tessellate(results);
            results = simplifier.Normalize(results);
            results = simplifier.MakeValid(results);
            results = simplifier.Simplify(results);
            results = results.FilterSmallArea(1.0);
            return results;
        }
    }
}
