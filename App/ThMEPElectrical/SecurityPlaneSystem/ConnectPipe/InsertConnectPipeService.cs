using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public static class InsertConnectPipeService
    {
        public static List<Line> InsertConnectPipe(List<Polyline> polylines, string layerName, string lineType)
        {
            List<Line> reLines = new List<Line>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalSecurityPlaneDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), true);
                acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(lineType), true);

                foreach (var poly in polylines)
                {
                    for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                    {
                        var pipe = new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1));
                        reLines.Add(pipe);
                        pipe.Linetype = lineType;
                        pipe.Layer = layerName;
                        pipe.ColorIndex = 256;
                        //acadDatabase.ModelSpace.Add(pipe);
                    }
                }
            }
            return reLines;
        }
    }
}
