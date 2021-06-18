using DotNetARX;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThDoorOpeningExtractor : ThExtractorBase,IPrint
    {
        public List<Polyline> Doors { get; set; }
        
        public ThDoorOpeningExtractor()
        {
            Doors = new List<Polyline>();
            Category = BuiltInCategory.DoorOpening.ToString();
            UseDb3Engine = false;
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Doors.ForEach(o =>
            {
                var switchStatus = SwitchStatus.Open.ToString(); //后期根据现实状况调整
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.SwitchPropertyName, switchStatus);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            if(UseDb3Engine)
            {
                using (var doorEngine = new ThDB3DoorRecognitionEngine())
                {
                    doorEngine.Recognize(database, pts);
                    Doors = doorEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                }
            }
            else
            {
                var instance = new ThMEPEngineCore.Temp.ThExtractDoorOpeningService()
                {
                    ElementLayer = "门",
                };
                instance.Extract(database, pts);
                Doors = instance.Openings;
            }
        }

        public void Print(Database database)
        {
            Doors.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
