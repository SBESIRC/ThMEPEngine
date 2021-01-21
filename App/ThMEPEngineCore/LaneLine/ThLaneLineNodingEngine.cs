using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineNodingEngine
    {
        public static DBObjectCollection Noding(DBObjectCollection curves)
        {
            var results = new List<Line>();
            var objs = ExplodeCurves(curves);
            var geometry = objs.ToCollection().ToNTSNodedLineStrings();
            if (geometry is LineString line)
            {
                results.Add(line.ToDbline());
            }
            else if (geometry is MultiLineString lines)
            {
                results.AddRange(lines.Geometries.Cast<LineString>().Select(o => o.ToDbline()));
            }
            else
            {
                throw new NotSupportedException();
            }
            return results.ToCollection();
        }

        private static List<Curve> ExplodeCurves(DBObjectCollection curves)
        {
            var objs = new List<Curve>();
            foreach (Curve curve in curves)
            {
                if (curve is Line line)
                {
                    objs.Add(line.WashClone() as Line);
                }
                else if (curve is Polyline polyline)
                {
                    var entitySet = new DBObjectCollection();
                    polyline.Explode(entitySet);
                    objs.AddRange(ExplodeCurves(entitySet));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }
    }
}
