using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThFpShearwallExtractor : ThShearwallExtractor
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
