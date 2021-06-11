using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public abstract class ThBuildElementSimplifier
    {
        private const double OFFSET_DISTANCE = 30.0;
        private const double DISTANCE_TOLERANCE = 1.0;
        private const double TESSELLATE_ARC_LENGTH = 10.0;

        public virtual DBObjectCollection Simplify(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            curves.Cast<AcPolygon>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                objs.Add(o.DPSimplify(DISTANCE_TOLERANCE));
            });
            return objs;
        }

        public virtual DBObjectCollection MakeValid(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            curves.Cast<AcPolygon>().ForEach(o =>
            {
                var results = o.MakeValid().Cast<AcPolygon>();
                if (results.Any())
                {
                    objs.Add(results.OrderByDescending(p => p.Area).First());
                }
            });
            return objs;
        }

        public virtual DBObjectCollection Normalize(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (AcPolygon curve in curves)
            {
                curve.Buffer(-OFFSET_DISTANCE)
                    .Cast<AcPolygon>()
                    .ForEach(o =>
                    {
                        o.Buffer(OFFSET_DISTANCE)
                        .Cast<AcPolygon>()
                        .ForEach(e => objs.Add(e));
                    });
            }
            return objs;
        }

        public virtual DBObjectCollection Tessellate(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (Curve c in curves)
            {
                if (c is AcPolygon polygon)
                {
                    objs.Add(polygon.Tessellate(TESSELLATE_ARC_LENGTH));
                }
                else if (c is Circle circle)
                {
                    objs.Add(circle.Tessellate(TESSELLATE_ARC_LENGTH));
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
