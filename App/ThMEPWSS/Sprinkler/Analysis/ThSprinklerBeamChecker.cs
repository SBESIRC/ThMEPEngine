using System;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBeamChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
        {
            var objs = Check(geometries);
            Present(objs);
        }

        private DBObjectCollection Check(List<ThGeometry> geometries)
        {
            var objs = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains("Beam"))
                {
                    if(g.Properties.ContainsKey("BottomDistanceToFloor") && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) >= BeamHeight)
                    {
                        objs.Add(g.Boundary);
                    }
                }
            });
            return objs;
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAIBeamsCheckerLayer();
                foreach (Polyline pline in objs.Buffer(200))
                {
                    acadDatabase.ModelSpace.Add(pline);
                    pline.LayerId = layerId;
                    pline.ConstantWidth = 100;
                }
            }
        }
    }
}
