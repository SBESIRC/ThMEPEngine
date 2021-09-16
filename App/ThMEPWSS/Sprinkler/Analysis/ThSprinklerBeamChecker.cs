using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.Sprinkler.Service;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBeamChecker
    {
        public DBObjectCollection Check(List<ThGeometry> geometries)
        {
            var objs = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains("Beam"))
                {
                    if(g.Properties.ContainsKey("BottomDistanceToFloor") && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) >= 700)
                    {
                        objs.Add(g.Boundary);
                    }
                }
            });
            return objs;
        }

        public void Present(Database database, DBObjectCollection objs)
        {
            var layerId = database.CreateAIBeamsCheckerLayer();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                foreach(Polyline pline in objs.Buffer(200))
                {
                    acadDatabase.ModelSpace.Add(pline);
                    pline.LayerId = layerId;
                    pline.ConstantWidth = 100;
                }
            }
        }
    }
}
