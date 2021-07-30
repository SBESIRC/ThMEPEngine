using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPElectrical.GroundingGrid.Data
{
    public class ThGroundStoreyExtractor:ThEStoreyExtractor
    {
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorTypePropertyName, o.StoreyType);
                if (o.StoreyNumber == "")
                    geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, o.StoreyNumber);
                else
                    geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, StoreyNumberMap[o.StoreyNumber]);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Id);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BasePointPropertyName, o.BasePoint);
                geometry.Boundary = null;
                geos.Add(geometry);
            });
            return geos;
        }
    }
}
