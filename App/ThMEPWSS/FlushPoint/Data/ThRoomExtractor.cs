using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Interface;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThRoomExtractor : ThExtractorBase,IPrint
    {        
        public List<ThIfcRoom> Rooms { get; private set; }
        private List<string> ParkingStallNames { get; set; }
        private double OffsetDis { get; set; }
        public ThRoomExtractor()
        {
            Category = BuiltInCategory.Room.ToString();
            Rooms = new List<ThIfcRoom>();
            ParkingStallNames = new List<string>() { "停车", "汽车", "车库", "地库", "地下车库" };
            OffsetDis = 500.0;
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
            var parkingStallExtractor = new ThParkingStallExtractor();
            parkingStallExtractor.Extract(database, pts);
            ResetName(parkingStallExtractor.ParkingStalls.ToCollection());
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
        private void ResetName(DBObjectCollection parkingStalls)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(parkingStalls);
            Rooms.ForEach(o =>
            {
                var tags = o.Tags.Contains(o.Name) ? o.Tags : o.Tags.Append(o.Name);
                o.Name = string.Join(";", tags.ToArray());
                if (o.Tags.Append(o.Name).ToList().Where(n => IsParkingStallArea(n)).Any())
                {
                    o.Name = "停车区域";
                }
                else
                {
                    IBuffer bufferService = new ThNTSBufferService();
                    var ent = bufferService.Buffer(o.Boundary, OffsetDis);
                    var objs = spatialIndex.SelectWindowPolygon(ent);
                    if (objs.Count >= 6)
                    {
                        o.Name = "停车区域";
                    }
                }
            });
        }
        private bool IsParkingStallArea(string name)
        {
            return ParkingStallNames.Where(o => name.Contains(o)).Any();
        }

        public void Print(Database database)
        {
            Rooms.Select(o => o.Boundary).Cast<Entity>().ToList().CreateGroup(database,ColorIndex);
        }
    }
}
