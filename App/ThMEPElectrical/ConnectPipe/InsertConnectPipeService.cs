using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.CAD;

namespace ThMEPElectrical.ConnectPipe
{
    public static class InsertConnectPipeService
    {
        public static void InsertConnectPipe(List<Polyline> polylines)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThMEPCommon.ConnectPipeLayerName);
                acadDatabase.Database.ImportLinetype(ThMEPCommon.ConnectPipeLineType);

                foreach (var poly in polylines)
                {
                    for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                    {
                        var pipe = new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1));
                        pipe.Linetype = ThMEPCommon.ConnectPipeLineType;
                        pipe.Layer = ThMEPCommon.ConnectPipeLayerName;
                        acadDatabase.ModelSpace.Add(pipe);
                    }
                }
            }
            
        }
    }
}
