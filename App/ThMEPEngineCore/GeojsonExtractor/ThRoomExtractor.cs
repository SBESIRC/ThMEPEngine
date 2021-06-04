using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThRoomExtractor : ThExtractorBase,IPrint
    {        
        public List<ThIfcRoom> Rooms { get; private set; } 
        public ThRoomExtractor()
        {
            Rooms = new List<ThIfcRoom>();
            Category = BuiltInCategory.Room.ToString();      
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if(!o.Tags.Contains(o.Name))
                {
                    o.Tags.Add(o.Name);
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, string.Join(";", o.Tags.ToArray()));
                var privacy = CheckPrivate(o);
                if (privacy != Privacy.Unknown)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.PrivacyPropertyName, privacy.ToString());
                }
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        private Privacy CheckPrivate(ThIfcRoom room)
        {
            if(room.Tags.Where(o => o.Contains("私立")).Count() > 0)
            {
                return Privacy.Private;
            }
            if (room.Tags.Where(o => o.Contains("公共")).Count() > 0)
            {
                return Privacy.Public;
            }
            return Privacy.Unknown;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                Rooms =roomEngine.BuildFromMS(database, pts);                
                Clean();
            }            
        }
        private void Clean()
        {
            for(int i =0;i<Rooms.Count;i++)
            {
                if(Rooms[i].Boundary is Polyline polyline)
                {
                    Rooms[i].Boundary = ThMEPFrameService.Normalize(polyline);
                }
            }
        }      
 
        public void Print(Database database)
        {
            Rooms.Select(o => o.Boundary).Cast<Entity>().ToList().CreateGroup(database,ColorIndex);
        }
    }
}
