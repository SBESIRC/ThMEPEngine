using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWFloorDrainExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWFloorDrainExtractionVisitor()
            {
                LayerFilter = ThClosestoolLayerManager.XrefLayers(database),
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }
    public class ThWFloorDrainRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            Elements.AddRange(RecognizeToiletFloorDrain(database, polygon));
            Elements.AddRange(RecognizeBalconyFloorDrain(database, polygon));
        }
        private List<ThWFloorDrain> RecognizeToiletFloorDrain(Database database, Point3dCollection polygon)
        {
            List<ThWFloorDrain> floorDrains = new List<ThWFloorDrain>();
            var engine = new ThWFloorDrainExtractionEngine();
            engine.Extract(database);
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWFloorDrain.Create(o)));
            floorDrains.ForEach(o => o.Use = UseKind.Toilet);           
            return floorDrains;
        }
        private List<ThWFloorDrain> RecognizeBalconyFloorDrain(Database database, Point3dCollection polygon)
        {
            List<ThWFloorDrain> floorDrains = new List<ThWFloorDrain>();
            var engine = new ThWFloorDrainExtractionEngine();
            engine.Extract(database);
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWFloorDrain.Create(o)));
            floorDrains.ForEach(o => o.Use = UseKind.Balcony);
            return floorDrains;
        }
    }
}
