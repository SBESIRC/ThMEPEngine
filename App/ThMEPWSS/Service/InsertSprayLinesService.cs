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
        public static void InsertSprayLines(List<Line> sprayLines)
        {
            using (var db = AcadDatabase.Active())
            {
                LayerTools.AddLayer(db.Database, ThWSSCommon.Layout_Line_LayerName);
                var lineData = sprayLines.Select(x => new Line(x.StartPoint, x.EndPoint)).ToList();
                foreach (var line in lineData)
                {
                    line.Layer = ThWSSCommon.Layout_Line_LayerName;
                    line.ColorIndex = 130;
                    db.ModelSpace.Add(line);
                }
            }
        }
    }
}
