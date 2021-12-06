using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Linq2Acad;
using ThMEPLighting.EmgLight.Common;
using ThCADExtension;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public static class InsertConnectLineService
    {
        public static void InsertConnectLine(List<Polyline> polylines)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(
                    blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.EmgLightConnectLayerName), true);
                acadDatabase.Linetypes.Import(
                    blockDb.Linetypes.ElementOrDefault(ThMEPLightingCommon.EmgLightConnectLineType), true);
                if (polylines != null && polylines.Count > 0)
                {
                    foreach (var poly in polylines)
                    {
                        for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                        {
                            var linkLine = new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1));
                            linkLine.Linetype = "ByLayer";
                            linkLine.Layer = ThMEPLightingCommon.EmgLightConnectLayerName;
                            linkLine.Color = Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightConnectLayerColor);
                            acadDatabase.ModelSpace.Add(linkLine);
                        }
                    }
                }
            }
        }
    }
}
