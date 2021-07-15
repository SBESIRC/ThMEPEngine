using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Engine;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageToilateRoomExtractor : ThExtractorBase, IAreaId
    {
        public string AreaId { get; private set; }
        public static string AreaIdPropertyName = "AreaId";
        public List<ThIfcRoom> Rooms { get; private set; }
        public ThDrainageToilateRoomExtractor()
        {
            Rooms = new List<ThIfcRoom>();
            Category = BuiltInCategory.Space.ToString();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                Rooms = roomEngine.BuildFromMS(database, pts);
                Clean();
            }
        }
        private void Clean()
        {
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Boundary is Polyline polyline)
                {
                    Rooms[i].Boundary = ThMEPFrameService.Normalize(polyline);
                }
            }
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();

                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if (o.Name != null && !o.Tags.Contains(o.Name))
                {
                    o.Tags.Add(o.Name);
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, string.Join(";", o.Tags.ToArray()));
                geometry.Properties.Add(AreaIdPropertyName, AreaId);

                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        public void setAreaId(string groupId)
        {
            if (GroupSwitch)
            {
                AreaId = groupId;
            }
        }
    }
}
