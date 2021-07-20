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
    public class ThFaFireproofshutterExtractor : ThFireproofShutterExtractor, IBuildGeometry
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
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
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o));
                }
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
    }
}
