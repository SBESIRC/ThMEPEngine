using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWBasinExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWBasinExtractionVisitor()
            {
                LayerFilter = ThBasintoolLayerManager.XrefLayers(database),
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }
    }

    public class ThWBasinRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThWBasinExtractionEngine();
            engine.Extract(database);
            var dbObjs = engine.Results.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Elements.AddRange(dbObjs.Cast<Entity>().Select(o => ThWBasin.Create(o)));
        }
    }
}
