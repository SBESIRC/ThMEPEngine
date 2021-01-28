using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public abstract class ThLaneLineEngine
    {
        protected static readonly double extend_distance = 20.0;

        public static DBObjectCollection Noding(DBObjectCollection curves)
        {
            return NodingLines(curves).ToCollection();
        }

        public static DBObjectCollection Explode(DBObjectCollection curves)
        {
            return ExplodeCurves(curves).ToCollection();
        }

        protected static List<Curve> ExplodeCurves(DBObjectCollection curves)
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

        protected static List<Line> NodingLines(DBObjectCollection curves)
        {
            var results = new List<Line>();
            var geometry = curves.ToNTSNodedLineStrings();
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
            return results;
        }

        protected static DBObjectCollection Simplify(DBObjectCollection curves)
        {
            return curves.Cast<Polyline>().Select(o => o.TPSimplify(extend_distance)).ToCollection();
        }
    }
}
