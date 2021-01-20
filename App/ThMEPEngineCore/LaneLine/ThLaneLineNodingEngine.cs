using System;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineNodingEngine
    {
        public static DBObjectCollection Noding(DBObjectCollection curves)
        {
            var objs = ExplodeCurves(curves);
            return objs.ToCollection().ToNTSNodedLineStrings().ToDbCollection();
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
