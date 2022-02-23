using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public abstract class ThBuildElementSimplifier
    {
        public double AREATOLERANCE = 1.0;
        public double OFFSETDISTANCE = 30.0;
        public double DISTANCETOLERANCE = 1.0;
        public double TESSELLATEARCLENGTH = 100.0;
        public double ClOSED_DISTANC_TOLERANCE = 100.0;

        public virtual DBObjectCollection Simplify(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            curves.Cast<AcPolygon>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                objs.Add(o.DPSimplify(DISTANCETOLERANCE));
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
                curve.Buffer(-OFFSETDISTANCE)
                    .Cast<AcPolygon>()
                    .ForEach(o =>
                    {
                        o.Buffer(OFFSETDISTANCE)
                        .Cast<AcPolygon>()
                        .ForEach(e => objs.Add(e));
                    });
            }
            return objs;
        }

        public virtual void MakeClosed(DBObjectCollection curves)
        {
            curves.OfType<AcPolygon>().ForEach(p =>
            {
                if (ThMEPFrameService.IsClosed(p, ClOSED_DISTANC_TOLERANCE))
                {
                    p.Closed = true;
                }
            });
        }

        public virtual DBObjectCollection Filter(DBObjectCollection Polygons)
        {
            return Polygons.FilterSmallArea(AREATOLERANCE);
        }
        public virtual DBObjectCollection Tessellate(DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (Curve c in curves)
            {
                if (c is AcPolygon polygon)
                {
                    objs.Add(polygon.Tessellate(TESSELLATEARCLENGTH));
                }
                else if (c is Circle circle)
                {
                    objs.Add(circle.Tessellate(TESSELLATEARCLENGTH));
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
