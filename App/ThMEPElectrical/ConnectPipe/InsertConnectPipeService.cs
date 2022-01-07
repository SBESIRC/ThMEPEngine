using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.ConnectPipe
{
    public static class InsertConnectPipeService
    {
        public static void InsertConnectPipe(List<Polyline> polylines)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(
                    blockDb.Layers.ElementOrDefault(ThMEPCommon.ConnectPipeLayerName), false);
                //acadDatabase.Linetypes.Import(
                //    blockDb.Linetypes.ElementOrDefault(ThMEPCommon.ConnectPipeLineType), false);
                foreach (var poly in polylines)
                {
                    for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                    {
                        var pipe = new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1));
                        pipe.Linetype = ThMEPCommon.ConnectPipeLineType;
                        pipe.Layer = ThMEPCommon.ConnectPipeLayerName;
                        pipe.ColorIndex = 256;
                        acadDatabase.ModelSpace.Add(pipe);
                    }
                }
            }
        }
    }
}
