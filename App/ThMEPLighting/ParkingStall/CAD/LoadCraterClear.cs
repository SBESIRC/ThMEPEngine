using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;

namespace ThMEPLighting.ParkingStall.CAD
{
    class LoadCraterClear
    {
        static List<string> layerNames = new List<string>()
        {
            ParkingStallCommon.PARKINGLIGHTCONNECT_LAYERAME
        };
        public static void LoadBlockLayerToDocument(Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LightingEmgLightDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    foreach (var item in layerNames)
                    {
                        if (string.IsNullOrEmpty(item))
                            continue;
                        var layer = blockDb.Layers.ElementOrDefault(item);
                        if (null == layer)
                            continue;
                        currentDb.Layers.Import(layer, true);
                    }
                }
            }
        }
        public static void ClearHistoryLines(Database database, string layerName, Polyline outPolyline, List<Polyline> innerPolylines, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                var delCurves = new List<Curve>();
                var targetLines = currentDb.ModelSpace
                   .OfType<Curve>().Where(o => o.Layer == layerName);
                if (targetLines == null || targetLines.Count() < 1)
                    return;
                targetLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    if (null != originTransformer)
                        originTransformer.Transform(transCurve);
                    if(outPolyline.Contains(transCurve))
                        delCurves.Add(x);
                });
                if (delCurves.Count < 1)
                    return;
                var objs = new DBObjectCollection();
                delCurves.ForEachDbObject(c => objs.Add(c));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }
    }
}
