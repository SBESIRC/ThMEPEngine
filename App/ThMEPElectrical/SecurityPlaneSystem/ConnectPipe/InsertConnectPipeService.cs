using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.CAD;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public static class InsertConnectPipeService
    {
        public static void InsertConnectPipe(List<Polyline> polylines, string layerName, string lineType)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThCADCommon.ElectricalSecurityPlaneDwgPath(), layerName);
                acadDatabase.Database.ImportLinetype(ThCADCommon.ElectricalSecurityPlaneDwgPath(), lineType);

                foreach (var poly in polylines)
                {
                    for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                    {
                        var pipe = new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1));
                        pipe.Linetype = lineType;
                        pipe.Layer = layerName;
                        pipe.ColorIndex = 256;
                        acadDatabase.ModelSpace.Add(pipe);
                    }
                }
            }

        }
    }
}
