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
        protected double OFFSETDISTANCE = 30.0;
        protected double DISTANCETOLERANCE = 1.0;
        protected double TESSELLATEARCLENGTH = 10.0;
        protected double AREATOLERANCE = 1.0;

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
            //double OFFSETDISTANCE = OffsetDistance();
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

        public virtual DBObjectCollection Filter(DBObjectCollection walls)
        {
            return walls.Cast<Entity>().Where(o =>
            {
                if (o is AcPolygon polygon)
                {
                    return polygon.Area > AREATOLERANCE;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area > AREATOLERANCE;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).ToCollection();
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
