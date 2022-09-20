using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThPlatform3D.Data
{
    public class ThAxisBasePointExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThAxisBasePointExtractionVisitor()
            {
                LayerFilter = HatchXrefLayers(database),
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

        private HashSet<string> HatchXrefLayers(Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                return acadDb.Layers
                    .Where(o => IsVisibleLayer(o))
                    //.Where(o => IsBasePointLayer(o.Name))
                    .Select(o => o.Name)
                    .ToHashSet();
            }
        }

        private bool IsBasePointLayer(string name)
        {
            string layer = ThMEPXRefService.OriginalFromXref(name).ToUpper();
            return layer.Contains("DEFPOINTS");
        }

        private bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
    }
}
