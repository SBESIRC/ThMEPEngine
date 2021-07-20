using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Temp
{
    public class ThFaFireproofshutterExtractor : ThFireproofShutterExtractor, IBuildGeometry, ISetStorey
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
        private List<StoreyInfo> StoreyInfos { get; set; }
        private const string NeibourFireApartIdsPropertyName = "NeibourFireApartIds";

        public ThFaFireproofshutterExtractor()
        {
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }
        public new List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireproofShutter.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ParentIdPropertyName, parentId);
                if (FireDoorNeibourIds.ContainsKey(o))
                {
                    geometry.Properties.Add(NeibourFireApartIdsPropertyName, string.Join(",", FireDoorNeibourIds[o]));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void SetTags(Dictionary<Entity, string> fireApartIds)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(fireApartIds.Select(o => o.Key).ToCollection());
            var bufferService = new ThNTSBufferService();
            FireproofShutter.ForEach(o =>
            {
                var enlarge = bufferService.Buffer(o, 5.0);
                var neibours = spatialIndex.SelectCrossingPolygon(enlarge);
                if (neibours.Count == 2)
                {
                    FireDoorNeibourIds.Add(o, neibours.Cast<Entity>().Select(e => fireApartIds[e]).ToList());
                }
                else if (neibours.Count > 2)
                {
                    throw new NotSupportedException();
                }
            });
        }

        public void Set(List<StoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public StoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new StoreyInfo();
        }
    }
}
