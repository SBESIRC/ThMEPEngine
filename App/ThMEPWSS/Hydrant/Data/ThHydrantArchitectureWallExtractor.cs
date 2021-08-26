using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantArchitectureWallExtractor : ThArchitectureExtractor
    {
        public override void Extract(Database database, Point3dCollection pts)
        {
            using (var engine = new ThDB3ArchWallRecognitionEngine())
            {
                var walls = new List<Entity>();
                engine.Recognize(database, pts);
                engine.Elements.ForEach(o => walls.Add(o.Outline));
                if (FilterMode == FilterMode.Window)
                {
                    walls = FilterWindowPolygon(pts, walls);
                }
                Walls.AddRange(walls);
            }
        }
    }
}
