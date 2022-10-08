using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.CAD
{
    public class ThTCHAxisLineExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThTCHAxisLineExtractionVisitor()
            { 
                LayerFilter = ThTCHAxisLineLayerManager.HatchXrefLayers(database).ToHashSet(),
            };
            var extractor = new ThBlockElementExtractor(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }
    }
    public class ThTCHAxisLineLayerManager : ThDbLayerManager
    {
        public static List<string> HatchXrefLayers(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsTCHAxisLineLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsTCHAxisLineLayer(string name)
        {
            string layer = ThMEPXRefService.OriginalFromXref(name).ToUpper();      
            return layer.EndsWith("AD-AXIS-AXIS");
        }
    }
}
