using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.FlushPoint.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Data
{    
    public class ThArchitectureExtractor : ThExtractorBase,IPrint
    {
        public List<Entity> Walls { get; private set; }
        private List<ThIfcRoom> Rooms { get; set; }
        public ThArchitectureExtractor()
        {
            Category = BuiltInCategory.ArchitectureWall.ToString();
            Walls = new List<Entity>();
            Rooms = new List<ThIfcRoom>();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateShearwalls = ThElementIsolateFilterService.Filter(Walls, Rooms);
            Walls.ForEach(o =>
            {
                var isolate = isolateShearwalls.Contains(o);
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, isolate);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            using (var engine = new ThArchitectureWallRecognitionEngine())
            {
                engine.Recognize(database, pts);
                engine.Elements.ForEach(o => Walls.Add(o.Outline));
            }
        }
        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }

        public void Print(Database database)
        {
            Walls.CreateGroup(database,ColorIndex);
        }
    }
}
