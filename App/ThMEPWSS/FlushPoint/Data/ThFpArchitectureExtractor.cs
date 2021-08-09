using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThFpArchitectureExtractor : ThArchitectureExtractor
    {
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateShearwalls = ThElementIsolateFilterService.Filter(Walls, Rooms);
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var isolate = isolateShearwalls.Contains(o);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, isolate);
                geometry.Boundary = o;
                if (isolate)
                {
                    geos.Add(geometry);
                }
            });
            return geos;
        }
    }
}
