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
    public class ThElementSimplifier : ThBuildElementSimplifier
    {
        public double OFFSET_DISTANCE { get; set; }
        public double DISTANCE_TOLERANCE { get; set; }
        public double TESSELLATE_ARC_LENGTH { get; set; }

        public ThElementSimplifier()
        {
            OFFSET_DISTANCE = 20.0;
            DISTANCE_TOLERANCE = 1.0;
            TESSELLATE_ARC_LENGTH = 10.0;
        }

        public override DBObjectCollection Simplify(DBObjectCollection windows)
        {
            var objs = new DBObjectCollection();
            windows.Cast<AcPolygon>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的墙线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                objs.Add(o.DPSimplify(DISTANCE_TOLERANCE));
            });
            return objs;
        }

        public override DBObjectCollection Normalize(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();
            foreach (AcPolygon wall in walls)
            {
                wall.Buffer(-OFFSET_DISTANCE)
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
        public override DBObjectCollection Tessellate(DBObjectCollection curves)
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
    public class ThWindowSimplifier : ThElementSimplifier
    {
        public ThWindowSimplifier()
        {
            OFFSET_DISTANCE = 20.0;
            DISTANCE_TOLERANCE = 1.0;
            TESSELLATE_ARC_LENGTH = 10.0;
        }
    }
    public class ThSlabSimplifier : ThElementSimplifier
    {
        public ThSlabSimplifier()
        {
            OFFSET_DISTANCE = 20.0;
            DISTANCE_TOLERANCE = 1.0;
            TESSELLATE_ARC_LENGTH = 100.0;
        }
        public override DBObjectCollection Tessellate(DBObjectCollection curves)
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
