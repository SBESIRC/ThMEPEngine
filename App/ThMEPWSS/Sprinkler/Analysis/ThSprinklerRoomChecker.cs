using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Service;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerRoomChecker : ThSprinklerChecker
    {
        readonly static string LayerName = "AI-喷头校核-房间是否布置喷头";

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var objs = Check(geometries, sprinklers, pline);
            if (objs.Count > 0) 
            {
                Present(objs);
            }
        }

        private DBObjectCollection Check(List<ThGeometry> geometries, List<ThIfcDistributionFlowElement> sprinklers, Polyline pline)
        {
            var outlines = sprinklers.OfType<ThSprinkler>()
                                     .Where(o => o.Category == Category)
                                     .Where(o => pline.Contains(o.Position))
                                     .Select(o => o.Outline)
                                     .ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(outlines);
            var objs = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains("Room"))
                {
                    if(!pline.ToNTSPolygon().Intersects(g.Boundary.ToNTSGeometry()))
                    {
                        return;
                    }
                    var result = spatialIndex.SelectCrossingPolygon(g.Boundary);
                    if (g.Properties.ContainsKey("Placement") && (g.Properties["Placement"] as string).Contains("不可布区域"))
                    {
                        if (result.Count > 0)
                        {
                            objs.Add(g.Boundary);
                        }
                    }
                    else
                    {
                        if (result.Count == 0)
                        {
                            objs.Add(g.Boundary);
                        }
                    }
                }
            });
            return objs;
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                foreach (Entity e in objs)
                {
                    var service = new ThSprinklerRoomPrintService(acadDatabase.Database, LayerName);
                    var colorIndex = 2;
                    service.Print(e, colorIndex);
                }
            }
        }
    }
}
