using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThFpColumnExtractor:ThColumnExtractor
    {
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateColumns = ThElementIsolateFilterService.Filter(Columns.Cast<Entity>().ToList(), Rooms);
            Columns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var isolate = isolateColumns.Contains(o);
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
