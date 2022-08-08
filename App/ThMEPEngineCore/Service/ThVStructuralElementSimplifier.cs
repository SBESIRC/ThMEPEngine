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
        private const double TESSELLATE_ARC_LENGTH = 100.0;
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
            var objs = new DBObjectCollection();
            foreach(Curve c in curves)
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

        public static void Classify(DBObjectCollection curves, DBObjectCollection columns, DBObjectCollection walls)
        {
            // 还原带洞的区域
            var polygons = curves.BuildArea();

            // 首先判断出矩形柱和墙
            polygons.Cast<Entity>()
                .Where(e => e is AcPolygon)
                .Cast<AcPolygon>()
                .Where(e => IsRectangle(e))
                .ForEach(e =>
                {
                    if (AspectRatio(e) > 3)
                    {
                        walls.Add(e);
                    }
                    else
                    {
                        columns.Add(e);
                    }
                });
            polygons.Cast<Entity>()
                .Where(e => e is MPolygon)
                .Cast<MPolygon>()
                .Where(e => IsRectangle(e))
                .ForEach(e =>
                {
                    if (AspectRatio(e.Outline()) > 3)
                    {
                        walls.Add(e);
                    }
                    else
                    {
                        columns.Add(e);
                    }
                });

            // 接着判断非矩形（柱+剪力墙）
            var others = polygons.Except(walls).Except(columns);
            others.Cast<Entity>()
                .Where(e => e is AcPolygon)
                .Cast<AcPolygon>()
                .ForEach(e => Decompose(e, columns, walls));
            others.Cast<Entity>()
                .Where(e => e is MPolygon)
                .Cast<MPolygon>()
                .ForEach(e => Decompose(e, columns, walls));
        }

        private static void Decompose(AcPolygon polygon, DBObjectCollection columns, DBObjectCollection walls)
        {
            var polygons = polygon.Buffer(-DECOMPOSE_OFFSET_DISTANCE);
            if (polygons.Count == 0)
            {
                walls.Add(polygon);
            }
            else
            {
                var results = polygons.Cast<Entity>()
                    .Where(e => e is AcPolygon)
                    .Cast<AcPolygon>()
                    .Where(e => IsRectangle(e))
                    .Where(e => AspectRatio(e) <= 3)
                    .ToCollection();
                if (results.Count == 0)
                {
                    walls.Add(polygon);
                }
                else
                {
                    var columnObjs = results
                        .Cast<AcPolygon>()
                        .Select(o => o.Buffer(DECOMPOSE_OFFSET_DISTANCE)[0])
                        .ToCollection();
                    columnObjs.Cast<DBObject>().ForEach(o => columns.Add(o));
                    var wallObjs = polygon.Difference(columnObjs);
                    wallObjs = MakeValid(wallObjs);
                    wallObjs = Normalize(wallObjs);
                    wallObjs = Simplify(wallObjs);
                    wallObjs.Cast<DBObject>().ForEach(o => walls.Add(o));
                }
            }
        }

        private static void Decompose(MPolygon mPolygon, DBObjectCollection columns, DBObjectCollection walls)
        {
            if (mPolygon.HasHoles())
            {
                throw new NotSupportedException();
            }
            Decompose(mPolygon.Shell(), columns, walls);
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

        private static bool IsRectangle(MPolygon mPolygon)
        {
            return IsRectangle(mPolygon.Outline());
        }
    }
}
