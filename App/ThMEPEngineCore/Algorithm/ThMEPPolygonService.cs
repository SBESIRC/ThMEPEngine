#if (ACAD2016 || ACAD2018)
using CLI;
using System;
using NFox.Cad;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.IO;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using NetTopologySuite.Features;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Algorithm
{
    public static class ThMEPPolygonService
    {
        private const double AREA_TOLERANCE = 1.0;
        private const double OFFSET_DISTANCE = 10.0;
        private const double DISTANCE_TOLERANCE = 1.0;

        /// <summary>
        /// 中心线
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static List<Line> CenterLine(Entity polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThPolygonCenterLineMgd();
                var serializer = GeoJsonSerializer.Create();
                var geos = new List<ThGeometry>();
                geos.Add(new ThGeometry()
                {
                    Boundary = polygon,
                });
                var results = engine.Generate(ThGeoOutput.Output(geos));
                using (var stringReader = new StringReader(results))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var lines = new List<Line>();
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    foreach (var f in features)
                    {
                        if (f.Geometry is LineString line)
                        {
                            lines.Add(line.ToDbline());
                        }
                    }
                    var merge = new ThListLineMerge(ThLineUnionService.UnionLineList(lines));
                    while (merge.Needtomerge(out Line refline, out Line moveline))
                    {
                        merge.Domoveparallellines(refline, moveline);
                    }
                    merge.Simplifierlines();
                    return merge.Lines;
                }
            }
        }

        /// <summary>
        /// 多边形分割（按半径）
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static DBObjectCollection Partition(Entity polygon, double radius)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThPolygonPartitionMgd();
                var geos = new List<ThGeometry>();
                geos.Add(new ThGeometry()
                {
                    Boundary = polygon,
                });
                var results = engine.Partition(ThGeoOutput.Output(geos), radius);
                return Partition(results);
            }
        }

        /// <summary>
        /// 多边形分割（按UCS）
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="radian"></param>
        /// <returns></returns>
        public static DBObjectCollection PartitionUCS(Entity polygon, double radian)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThPolygonPartitionMgd();
                var geos = new List<ThGeometry>();
                geos.Add(new ThGeometry()
                {
                    Boundary = polygon,
                });
                var results = engine.PartitionUCS(ThGeoOutput.Output(geos), radian);
                return Partition(results);
            }
        }

        private static DBObjectCollection Partition(string results)
        {
            using (var stringReader = new StringReader(results))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                var serializer = GeoJsonSerializer.Create();
                var features = serializer.Deserialize<FeatureCollection>(jsonReader);


                var objs = new DBObjectCollection();
                foreach (var f in features)
                {
                    if (f.Geometry is Polygon e)
                    {
                        e.ToDbPolylines().ForEach(o => objs.Add(o));
                    }
                }
                var entities = MakeValid(objs);
                if (entities.Count > 0)
                {
                    entities = Normalize(entities);
                    entities = Simplify(entities);
                    entities = Filter(entities);
                }
                return entities;
            }
        }

        private static DBObjectCollection MakeValid(DBObjectCollection entities)
        {
            var objs = new DBObjectCollection();
            entities.Cast<AcPolygon>().ForEach(o =>
            {
                var results = o.MakeValid().Cast<AcPolygon>();
                if (results.Any())
                {
                    objs.Add(results.OrderByDescending(p => p.Area).First());
                }
            });
            return objs;
        }

        private static DBObjectCollection Normalize(DBObjectCollection entities)
        {
            var objs = new DBObjectCollection();
            foreach (AcPolygon wall in entities)
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

        public static DBObjectCollection Simplify(DBObjectCollection entities)
        {
            var objs = new DBObjectCollection();
            entities.Cast<AcPolygon>().ForEach(o =>
            {
                // 由于投影误差，DB3切出来的墙线中有非常短的线段（长度<1mm)
                // 这里使用简化算法，剔除掉这些非常短的线段
                objs.Add(o.DPSimplify(DISTANCE_TOLERANCE));
            });
            return objs;
        }

        public static DBObjectCollection Filter(DBObjectCollection entities)
        {
            return entities.Cast<Entity>().Where(o =>
            {
                if (o is AcPolygon polygon)
                {
                    return polygon.Area > AREA_TOLERANCE;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area > AREA_TOLERANCE;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).ToCollection();
        }
    }
}
#endif