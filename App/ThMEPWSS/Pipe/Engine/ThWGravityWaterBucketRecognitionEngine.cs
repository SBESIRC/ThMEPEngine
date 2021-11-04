using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Pipe.Model;
using System;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWGravityWaterBucketExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWGravityWaterBucketExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThWGravityWaterBucketExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);// 提取ModelSpace下的块
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }
    public class ThWGravityWaterBucketRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {    
            var engine = new ThWGravityWaterBucketExtractionEngine();
            engine.Extract(database);
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWGravityWaterBucket.Create(o)));
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThWGravityWaterBucketExtractionEngine();
            engine.ExtractFromMS(database);
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWGravityWaterBucket.Create(o)));
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
    }
}
