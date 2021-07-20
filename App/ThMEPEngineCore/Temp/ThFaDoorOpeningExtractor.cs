using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using NFox.Cad;

namespace ThMEPEngineCore.Temp
{
    public class ThFaDoorOpeningExtractor : ThDoorOpeningExtractor, IBuildGeometry, ISetStorey
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
        private List<StoreyInfo> StoreyInfos { get; set; }
        private const string NeibourFireApartIdsPropertyName = "NeibourFireApartIds";
        private const string UsePropertyName = "Use";
        public ThFaDoorOpeningExtractor()
        {
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }
        public new List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Doors.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    var parentId = BuildString(GroupOwner, o.Outline);
                    if (string.IsNullOrEmpty(parentId))
                    {
                        var storeyInfo = Query(o.Outline);
                        parentId = storeyInfo.Id;
                    }
                    geometry.Properties.Add(ParentIdPropertyName, parentId);
                }
                if(FireDoorNeibourIds.ContainsKey(o.Outline))
                {
                    geometry.Properties.Add(NeibourFireApartIdsPropertyName,string.Join(",", FireDoorNeibourIds[o.Outline]));
                }
                if(IsFireDoor(o))
                {
                    geometry.Properties.Add(UsePropertyName, "防火门");
                }
                geometry.Boundary = o.Outline;
                geos.Add(geometry);
            });
            return geos;
        }
        public void SetTags(Dictionary<Entity, string> fireApartIds)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(fireApartIds.Select(o=>o.Key).ToCollection());
            var bufferService = new ThNTSBufferService();
            var fireDoors = Doors.Where(o => IsFireDoor(o)).ToList();
            fireDoors.ForEach(o =>
            {
                var enlarge = bufferService.Buffer(o.Outline, 5.0);
                var neibours = spatialIndex.SelectCrossingPolygon(enlarge);
                if (neibours.Count==2)
                {
                    FireDoorNeibourIds.Add(o.Outline,neibours.Cast<Entity>().Select(e => fireApartIds[e]).ToList());
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
        }
        private bool IsFireDoor(ThIfcDoor door)
        {
            //ToDO
            return door.Code.ToUpper().Contains("FM");
        }

        public void Set(List<StoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public StoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity)); ;
            return results.Count() > 0 ? results.First() : new StoreyInfo();
        }
    }
}
