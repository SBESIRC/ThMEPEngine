using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.ChargerDistribution.Common;
using ThMEPElectrical.ChargerDistribution.Service;

namespace ThMEPElectrical.ChargerDistribution.Command
{
    public class ThChargerGroupModifyCmd : ThMEPBaseCommand
    {
        private int MaxCount = 9;

        public ThChargerGroupModifyCmd()
        {
            ActionName = "调整";
            CommandName = "THCDZPD";
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var currentDb = AcadDatabase.Active())
            {
                // 选择充电桩设置
                var chargers = ThChargerSelector.GetChargerBlock(currentDb);
                if (chargers.Count == 0 || chargers.Count > MaxCount)
                {
                    return;
                }

                // 选择分组线
                var pline = ThChargerSelector.PickPolyline(currentDb);

                // 清理分组线
                var dictionary = new Dictionary<Polyline, List<Point3d>>();
                var groupingPolyline = ThChargerUtils.GroupingPolylineRecognize(currentDb, false);
                var spatialIndex = new ThCADCoreNTSSpatialIndex(groupingPolyline.ToCollection());
                chargers.ForEach(o =>
                {
                    var frame = o.CreateSquare(100.0);
                    spatialIndex.SelectCrossingPolygon(frame).OfType<Polyline>().ForEach(p =>
                    {
                        if (dictionary.ContainsKey(p))
                        {
                            if (!p.Closed)
                            {
                                dictionary[p].RemoveAll(v => v.DistanceTo(o) < 100.0);
                            }
                        }
                        else
                        {
                            if (p.Closed)
                            {
                                dictionary.Add(p, new List<Point3d>());
                            }
                            else
                            {
                                var vertices = p.Vertices().OfType<Point3d>().ToList();
                                vertices.RemoveAll(v => v.DistanceTo(o) < 100.0);
                                dictionary.Add(p, vertices);
                            }
                        }
                    });
                });

                var k = 0;
                var layerId = currentDb.Database.CreateAILayer(ThChargerDistributionCommon.Grouping_Layer, 0);
                dictionary.ForEach(pair =>
                {
                    if (pair.Value.Count == 0 || pair.Value.Count > MaxCount)
                    {
                        return;
                    }
                    ThChargerUtils.Clean(pair.Key);
                    var newLine = ThMinimumPolylineService.CreatePolyline(pair.Value, layerId, k);
                    k++;
                    currentDb.ModelSpace.Add(newLine);
                });

                if (!pline.IsNull() && !dictionary.ContainsKey(pline))
                {
                    if (pline.Closed)
                    {
                        chargers.Add(pline.Centroid());
                    }
                    else
                    {
                        chargers.AddRange(pline.Vertices().OfType<Point3d>());
                    }
                    ThChargerUtils.Clean(pline);
                }
                var groupLine = ThMinimumPolylineService.CreatePolyline(chargers, layerId, k);
                k++;
                currentDb.ModelSpace.Add(groupLine);
            }
        }
    }
}
