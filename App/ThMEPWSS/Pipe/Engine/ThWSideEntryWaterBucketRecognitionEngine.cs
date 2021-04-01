using System.Linq;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWSideEntryWaterBucketExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWSideEntryWaterBucketExtractionVisitor()
            {
                LayerFilter = ThSideEntryWaterBucketLayerManager.XrefLayers(database),
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }
    public class ThWSideEntryWaterBucketRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThWSideEntryWaterBucketExtractionEngine();
            engine.Extract(database);
            
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWSideEntryWaterBucket.Create(o)));
        }
    }
}
