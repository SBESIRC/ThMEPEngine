using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
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

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
        {
            var objs = Check(geometries, sprinklers);
            Present(objs);
        }

        private DBObjectCollection Check(List<ThGeometry> geometries, List<ThIfcDistributionFlowElement> sprinklers)
        {
            var outlines = new DBObjectCollection();
            sprinklers.Cast<ThSprinkler>().Where(o => o.Category == Category).ForEach(o => outlines.Add(o.Outline));
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
