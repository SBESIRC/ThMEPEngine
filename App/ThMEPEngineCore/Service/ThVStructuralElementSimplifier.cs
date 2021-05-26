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
    public class ThVStructuralElementSimplifier
    {
        private const double OFFSET_DISTANCE = 30.0;
        private const double DISTANCE_TOLERANCE = 1.0;
        private const double TESSELLATE_ARC_LENGTH = 10.0;
        private const double DECOMPOSE_OFFSET_DISTANCE = 151.0;
        private const double SIMILARITY_MEASURE_TOLERANCE = 0.99;

        public static DBObjectCollection Simplify(DBObjectCollection curves)
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

        public static DBObjectCollection MakeValid(DBObjectCollection curves)
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

        public static DBObjectCollection Normalize(DBObjectCollection curves)
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

        public static DBObjectCollection Tessellate(DBObjectCollection curves)
        {
            return curves.Cast<Circle>().Select(o => o.Tessellate(TESSELLATE_ARC_LENGTH)).ToCollection();
        }

        public static void Classify(DBObjectCollection curves, DBObjectCollection columns, DBObjectCollection walls)
        {
            // 首先判断出矩形柱和墙
            curves.Cast<AcPolygon>().Where(o => IsRectangle(o)).ForEach(o =>
            {
                if (AspectRatio(o) > 3)
                {
                    walls.Add(o);
                }
                else
                {
                    columns.Add(o);
                }
            });

            // 接着判断非矩形（柱+剪力墙）

        }

        private static void Decompose(AcPolygon polygon, DBObjectCollection columns, DBObjectCollection walls)
        {
            var results = polygon.Buffer(-DECOMPOSE_OFFSET_DISTANCE);
            if (results.Count == 0)
            {
                walls.Add(polygon);
            }
            else
            {
                foreach(AcPolygon item in results)
                {
                    var column = item.Buffer(DECOMPOSE_OFFSET_DISTANCE)[0] as AcPolygon;
                    columns.Add(column);
                    polygon.Difference(column).Cast<AcPolygon>().ForEach(o => walls.Add(o));
                }
            }
        }

        private static double AspectRatio(AcPolygon polygon)
        {
            var obb = OBB(polygon);
            var length1 = obb.GetPoint2dAt(0).GetDistanceTo(obb.GetPoint2dAt(1));
            var length2 = obb.GetPoint2dAt(1).GetDistanceTo(obb.GetPoint2dAt(2));
            return length1 > length2 ? (length1 / length2) : (length2 / length1);
        }

        private static bool IsRectangle(AcPolygon polygon)
        {
            return polygon.IsSimilar(OBB(polygon), SIMILARITY_MEASURE_TOLERANCE);
        }

        private static AcPolygon OBB(AcPolygon polygon)
        {
            return polygon.GetMinimumRectangle();
        }
    }
}
