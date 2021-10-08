using System;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBeamChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var objs = Check(geometries, pline);
            if (objs.Count > 0) 
            {
                Present(objs);
            }
        }

        private DBObjectCollection Check(List<ThGeometry> geometries, Polyline pline)
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
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectCrossingPolygon(pline);
        }

        public override void Clean(Polyline polyline)
        {
            CleanPline(ThSprinklerCheckerLayer.Beam_Checker_LayerName, polyline);
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAIBeamCheckerLayer();
                foreach (Polyline pline in objs.Buffer(200))
                {
                    acadDatabase.ModelSpace.Add(pline);
                    pline.LayerId = layerId;
                    pline.ConstantWidth = 50;
                }
            }
        }
    }
}
