using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThRoomExtractor : ThExtractorBase, IPrint
    {
        public List<ThIfcRoom> Rooms { get; private set; }
        public IRoomPrivacy iRoomPrivacy { get; set; }
        public double TESSELLATE_ARC_LENGTH { get; set; }
        public ThRoomExtractor()
        {
            Rooms = new List<ThIfcRoom>();
            TESSELLATE_ARC_LENGTH = 50.0;            
            Category = BuiltInCategory.Room.ToString();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if (!o.Tags.Contains(o.Name))
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
        public override void Extract(Database database, Point3dCollection pts)
        {
            if(UseDb3Engine)
            {
                using (var roomEngine = new ThRoomBuilderEngine())
                {
                    Rooms = roomEngine.BuildFromMS(database, pts);
                    Clean();                    
                }
            }
            else
            {
                //TODO
            }
#if DEBUG
            var entities = ThRoomBuildAreaService.BuildArea(Rooms.Select(o => o.Boundary as Polyline).ToList());
            Rooms = entities.Select(o => ThIfcRoom.Create(o)).ToList();
#endif
        }
        private void Clean()
        {            
            using (var instance = new ThCADCoreNTSArcTessellationLength(TESSELLATE_ARC_LENGTH))
            {
                var simplifier = new ThElementSimplifier()
                {
                    TESSELLATE_ARC_LENGTH = TESSELLATE_ARC_LENGTH,
                };
                for (int i = 0; i < Rooms.Count; i++)
                {
                    if (Rooms[i].Boundary is Polyline polyline)
                    {
                        var objs = new DBObjectCollection();
                        objs.Add(polyline);
                        simplifier.Tessellate(objs);
                        objs = simplifier.MakeValid(objs);
                        objs = simplifier.Normalize(objs);
                        if(objs.Count>0)
                        {
                            Rooms[i].Boundary = objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
                        }
                        else
                        {

                        }
                    }
                    else if (Rooms[i].Boundary is MPolygon mPolygon)
                    {
                        var polygon = mPolygon.ToNTSPolygon(); 
                        Rooms[i].Boundary = polygon.ToDbMPolygon();
                    }
                }
            }  
        }

        public void Print(Database database)
        {
            Rooms.Select(o => o.Boundary).Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
        private Privacy CheckPrivate(ThIfcRoom room)
        {
            if(iRoomPrivacy !=null)
            {
                return iRoomPrivacy.Judge(room);
            }
            return Privacy.Unknown;
        }
    }
}
