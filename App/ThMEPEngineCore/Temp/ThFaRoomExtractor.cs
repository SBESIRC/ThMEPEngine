using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPEngineCore.Temp
{
    public class ThFaRoomExtractor : ThRoomExtractor, ISetStorey, IBuildGeometry
    {
        private List<StoreyInfo> StoreyInfos { get; set; }
        public new List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o.Boundary);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Boundary);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ParentIdPropertyName, parentId);
                if (!string.IsNullOrEmpty(o.Name) && !o.Tags.Contains(o.Name))
                {
                    o.Tags.Add(o.Name);
                }
                geometry.Properties.Add(NamePropertyName, string.Join(",", o.Tags.ToArray()));
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        public StoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new StoreyInfo();
        }

        public void Set(List<StoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }
    }
}
