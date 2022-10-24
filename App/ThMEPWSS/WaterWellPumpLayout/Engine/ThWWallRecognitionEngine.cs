using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.WaterWellPumpLayout.Service;

namespace ThMEPWSS.WaterWellPumpLayout.Engine
{
    public class ThWWallExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThWWallExtractionVisitor()
            {
                LayerFilter = ThWWallLayerManager.XrefLayers(database).ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }
    public class ThWWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public List<Curve> Results { get; set; }
        public ThWWallRecognitionEngine()
        {
            Results = new List<Curve>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            // 来源于 BlockReference
            var extraction = new ThWWallExtractionEngine();
            extraction.Extract(database);
            Recognize(extraction.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> objs, Point3dCollection polygon)
        {
            var geometries = objs.Select(o => o.Geometry).ToCollection();
            if(polygon.Count>0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(geometries);
                geometries = spatialIndex.SelectCrossingPolygon(polygon);
            }
            Results = geometries.Cast<Curve>().ToList(); 
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}
