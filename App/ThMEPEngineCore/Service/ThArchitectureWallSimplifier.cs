using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public class ThArchitectureWallSimplifier
    {
        private const double OFFSET_DISTANCE = 20.0;
        private const double DISTANCE_TOLERANCE = 1.0;

        public static DBObjectCollection Simplify(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();
            walls.Cast<AcPolygon>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的墙线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                objs.Add(o.DPSimplify(DISTANCE_TOLERANCE));
            });
            return objs;
        }

        public static DBObjectCollection MakeValid(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();
            walls.Cast<AcPolygon>().ForEach(o =>
            {
                var results = o.MakeValid().Cast<AcPolygon>();
                if (results.Any())
                {
                    objs.Add(results.OrderByDescending(p => p.Area).First());
                }
            });
            return objs;
        }

        public static DBObjectCollection Normalize(DBObjectCollection walls)
        {
            var objs = new DBObjectCollection();
            foreach(AcPolygon wall in walls)
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

        public static DBObjectCollection BuildArea(DBObjectCollection walls)
        {
            // 外缩
            var results = Buffer(walls, 5.0);
            results = FilterSmallAreaEntity(results);            
            results = results.UnionPolygons();
            results = results.BuildArea();
            // 内缩
            results = Buffer(results, -5.0);
            results = FilterSmallAreaEntity(results);
            return results;
        }
        private static DBObjectCollection Buffer(DBObjectCollection objs ,double length)
        {
            var results = new DBObjectCollection();
            foreach (Entity  obj in objs)
            {
                if (obj is Polyline polyline)
                {
                    var bufferRes = polyline.ToNTSPolygon().Buffer(length).ToDbCollection();
                    bufferRes.Cast<Entity>().ForEach(e => results.Add(e));
                }
                else if(obj is MPolygon mPolygon)
                {
                    var bufferRes = mPolygon.ToNTSPolygon().Buffer(length).ToDbCollection();
                    bufferRes.Cast<Entity>().ForEach(e => results.Add(e));
                }                
            }
            return results;
        }
        private static DBObjectCollection FilterSmallAreaEntity(DBObjectCollection polygons,double areaTolerance=1.0)
        {
            return polygons.Cast<Entity>().Where(o =>
            {
                if (o is Polyline polyline)
                {
                    return polyline.Area > areaTolerance;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area > areaTolerance;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).ToCollection();
        }
    }
}
