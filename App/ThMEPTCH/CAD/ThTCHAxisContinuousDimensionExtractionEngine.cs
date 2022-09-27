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
    /// <summary>
    /// 连续标注
    /// </summary>
    public class ThTCHAxisContinuousDimensionExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThTCHAxisContinuousDimensionExtractionVisitor()
            { 
                LayerFilter = ThTCHAxisContinuousDimensionLayerManager.HatchXrefLayers(database).ToHashSet(),
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
    public class ThTCHAxisContinuousDimensionLayerManager : ThDbLayerManager
    {
        public static List<string> HatchXrefLayers(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Where(o => IsTCHAxisContinuousDimensionLayer(o.Name))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private static bool IsTCHAxisContinuousDimensionLayer(string name)
        {
            string layer = ThMEPXRefService.OriginalFromXref(name).ToUpper();
            return layer.EndsWith("AD-AXIS-DIMS");
        }
    }
}
