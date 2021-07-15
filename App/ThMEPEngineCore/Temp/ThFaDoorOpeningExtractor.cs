using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Temp
{
    public class ThFaDoorOpeningExtractor : ThDoorOpeningExtractor, IBuildGeometry
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
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
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o.Outline));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o.Outline));
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
    }
}
