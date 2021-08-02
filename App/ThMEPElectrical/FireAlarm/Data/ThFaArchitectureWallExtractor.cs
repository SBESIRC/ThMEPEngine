using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPElectrical.FireAlarm.Model;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;

namespace FireAlarm.Data
{
    public class ThFaArchitectureWallExtractor : ThArchitectureExtractor, IPrint, IGroup
    {
        public ThFaArchitectureWallExtractor()
        {
        }
        private List<StoreyInfo> StoreyInfos { get; set; }
        public new List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public StoreyInfo Query(Entity entity)
        {
            //ToDo
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new StoreyInfo();
        }

        public void Set(List<StoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            Walls.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }
    }
}
