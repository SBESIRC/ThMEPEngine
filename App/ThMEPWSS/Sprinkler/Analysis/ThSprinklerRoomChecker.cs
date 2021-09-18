using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using ThMEPEngineCore.Model;
using ThMEPWSS.Hydrant.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerRoomChecker
    {
        readonly static string LayerName = "AI-喷头校核-房间是否布置喷头";

        public DBObjectCollection Check(List<ThGeometry> geometries, List<ThIfcDistributionFlowElement> sprinklers)
        {
            var outlines = new DBObjectCollection();
            sprinklers.Cast<ThSprinkler>().Where(o => o.Category != "侧喷").ForEach(o => outlines.Add(o.Outline));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(outlines);

            var objs = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("Category") && (g.Properties["Category"] as string).Contains("Room"))
                {
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

        public void Present(Database database, DBObjectCollection objs)
        {
            foreach (Entity e in objs)
                {
                    var service = new ThHydrantPrintService(database, LayerName);
                    service.Print(e, 2);
                }
        }
    }
}
