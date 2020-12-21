using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPWSS.Service
{
    public static class InsertSprayLinesService
    {
        public static void InsertSprayLines(List<Line> sprayLines)
        {
            using (var db = AcadDatabase.Active())
            {
                LayerTools.AddLayer(db.Database, ThWSSCommon.Layout_Line_LayerName);
                db.Database.UnFrozenLayer(ThWSSCommon.Layout_Line_LayerName);
                db.Database.UnPrintLayer(ThWSSCommon.Layout_Line_LayerName);
                var lineData = sprayLines.Select(x => new Line(x.StartPoint, x.EndPoint)).ToList();
                foreach (var line in lineData)
                {
                    line.Layer = ThWSSCommon.Layout_Line_LayerName;
                    line.ColorIndex = 4;
                    db.ModelSpace.Add(line);
                }
            }
        }
    }
}
