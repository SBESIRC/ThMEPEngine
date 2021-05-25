using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Linq2Acad;
using ThMEPLighting.EmgLight.Common;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public static class InsertConnectLineService
    {
        public static void InsertConnectLine(List<Polyline> polylines)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.EmgLightConnectLayerName);
                acadDatabase.Database.ImportLinetype(ThMEPLightingCommon.EmgLightConnectLineType);

                foreach (var poly in polylines)
                {
                    for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                    {
                        var linkLine = new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1));
                        linkLine.Linetype = ThMEPLightingCommon.EmgLightConnectLineType;
                        linkLine.Layer = ThMEPLightingCommon.EmgLightConnectLayerName;
                        linkLine.Color = Color.FromColorIndex(ColorMethod.ByLayer, ThMEPLightingCommon.EmgLightConnectLayerColor);
                        acadDatabase.ModelSpace.Add(linkLine);
                    }
                }
            }
        }
    }
}
