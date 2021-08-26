using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantShearwallExtractor : ThShearwallExtractor
    {
        public override void Extract(Database database, Point3dCollection pts)
        {
            var walls = new List<Entity>();
            using (var engine = new ThShearWallRecognitionEngine())
            using (var db3Engine = new ThDB3ShearWallRecognitionEngine())
            {
                engine.Recognize(database, pts);
                db3Engine.Recognize(database, pts);
                engine.Elements.ForEach(o => walls.Add(o.Outline));
                db3Engine.Elements.ForEach(o => walls.Add(o.Outline));
                if (FilterMode == FilterMode.Window)
                {
                    walls = FilterWindowPolygon(pts, walls);
                }
                Walls.AddRange(walls);
            }
        }
    }
}
