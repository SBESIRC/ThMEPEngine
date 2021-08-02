using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.GroundingGrid.Data
{
    public class ThGroundColumnExtractor :ThColumnExtractor,IGroup
    {
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Columns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, o));
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            Columns.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }
    }
}
