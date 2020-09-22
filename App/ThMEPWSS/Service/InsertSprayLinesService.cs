using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThWSS;

namespace ThMEPWSS.Service
{
    public static class InsertSprayLinesService
    {
        public static void InsertSprayLines(List<Polyline> sprayLines)
        {
            using (var db = AcadDatabase.Active())
            {
                LayerTools.AddLayer(db.Database, ThWSSCommon.Layout_Line_LayerName);
                foreach (var line in sprayLines)
                {
                    line.Layer = ThWSSCommon.Layout_Line_LayerName;
                    line.ColorIndex = 130;
                    db.ModelSpace.Add(line);
                }
            }
        }
    }
}
