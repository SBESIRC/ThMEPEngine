using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public static class LineTypeService
    {
        public static void InsertConnectPipe(List<Polyline> polylines, string layerName)
        {
            using (Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(layerName);

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
