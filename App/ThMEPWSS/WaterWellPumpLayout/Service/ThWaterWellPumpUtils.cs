using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.WaterWellPumpLayout.Service
{
    public class ThWaterWellPumpUtils
    {
        public static double TesslateLength = 50.0;
        public static List<Line> ToLines(List<Entity> entities)
        {
            //要设置分割长度TesslateLength
            var results = new List<Line>();
            entities.ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    results.AddRange(polyline.ToLines());
                }
                else if (o is MPolygon mPolygon)
                {
                    results.AddRange(mPolygon.Loops().SelectMany(l => l.ToLines()));
                }
                else if (o is Circle circle)
                {
                    results.AddRange(circle.Tessellate(TesslateLength).ToLines());
                }
                else if (o is Line line)
                {
                    if (line.Length > 0)
                    {
                        results.Add(line);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
    }
}
