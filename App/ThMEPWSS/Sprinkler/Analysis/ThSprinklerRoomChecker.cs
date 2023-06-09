﻿using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Service;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerRoomChecker : ThSprinklerChecker
    {
        public override void Clean(Polyline pline)
        {
            CleanHatch(ThWSSCommon.Room_Checker_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            if (entity is Polyline pline)
            {
                var objs = Check(geometries, sprinklers, pline);
                if (objs.Count > 0)
                {
                    Present(objs);
                }
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

        private void CleanHatch(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = acadDatabase.ModelSpace
                    .OfType<Hatch>()
                    .Where(o => o.Layer == layerName).ToCollection();
                if (objs.Count > 0) 
                {
                    var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    spatialIndex.SelectCrossingPolygon(bufferPoly)
                                .OfType<Hatch>()
                                .ToList()
                                .ForEach(o =>
                                {
                                    o.UpgradeOpen();
                                    o.Erase();
                                });
                }
            }
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                foreach (Entity e in objs)
                {
                    var service = new ThSprinklerRoomPrintService(acadDatabase.Database, ThWSSCommon.Room_Checker_LayerName);
                    var colorIndex = 2;
                    service.Print(e, colorIndex);
                }
            }
        }

        public override void Extract(Database database, Polyline pline)
        {
            //
        }
    }
}
