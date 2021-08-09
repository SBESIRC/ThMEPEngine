using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThFaEStoreyExtractor : ThEStoreyExtractor,IPrint,IGroup
    {
        public ThFaEStoreyExtractor()
        {
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Storey.ObjectId.Handle.ToString());
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorTypePropertyName, o.StoreyType);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, o.StoreyNumber);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BasePointPropertyName, o.BasePoint);
                geometry.Boundary = null;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            //
        }

        public new Dictionary<Entity, string> StoreyIds
        {
            get
            {
                var result = new Dictionary<Entity, string>();
                Storeys.ForEach(o => result.Add(o.Boundary, o.Storey.ObjectId.Handle.ToString()));
                return result;
            }
        }
    }
}
