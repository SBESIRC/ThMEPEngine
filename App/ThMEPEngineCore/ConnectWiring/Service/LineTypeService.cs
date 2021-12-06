using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public static class LineTypeService
    {
        public static void InsertConnectPipe(List<Polyline> polylines, string layerName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), true);
                foreach (var poly in polylines)
                {
                    poly.Layer = layerName;
                    poly.Linetype = "ByLayer";
                    poly.ColorIndex = (int)ColorIndex.BYLAYER;
                    acadDatabase.ModelSpace.Add(poly);
                }
            }
        }
    }
}
