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
using Autodesk.AutoCAD.Geometry;
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
                    return lines;
                }
            }
        }

        /// <summary>
        /// 骨架线
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static List<Line> StraightSkeleton(Entity polygon)
        {
            //
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThPolygonCenterLineMgd();
                var serializer = GeoJsonSerializer.Create();
                var geos = new List<ThGeometry>();
                geos.Add(new ThGeometry()
                {
                    Boundary = polygon,
                });
                var results = engine.StraightSkeleton(ThGeoOutput.Output(geos));
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
                    return lines;
                }
            }
        }

        /// <summary>
        /// 多边形分割（按半径）
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Dictionary<Entity, bool> Partition(Entity polygon, double radius)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThPolygonPartitionMgd();
                var serializer = GeoJsonSerializer.Create();
                var geos = new List<ThGeometry>();
                geos.Add(new ThGeometry()
                {
                    Boundary = polygon,
                });
                var results = engine.Partition(ThGeoOutput.Output(geos), radius);
                using (var stringReader = new StringReader(results))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var dictionary = new Dictionary<Entity, bool>();
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    features.Where(f => f.Geometry is Polygon).ForEach(f =>
                    {
                        var pline = f.Geometry.ToDbCollection()[0] as AcPolygon;
                        var entity = MakeValid(pline);
                        if (entity.Area > 0)
                        {
                            entity = Normalize(entity);
                            entity = Simplify(entity);
                            entity = Filter(entity);
                        }
                        if (entity.Area > 0)
                        {
                            dictionary.Add(entity, (bool)f.Attributes["is_centerline_covered"]);
                        }
                    });
                    return dictionary;
                }
            }
        }

        /// <summary>
        /// 多边形分割（按UCS）
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="radian"></param>
        /// <returns></returns>
        public static Dictionary<Entity, Vector3d> PartitionUCS(Entity polygon, double radian)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThPolygonPartitionMgd();
                var serializer = GeoJsonSerializer.Create();
                var geos = new List<ThGeometry>();
                geos.Add(new ThGeometry()
                {
                    Boundary = polygon,
                });
                var results = engine.PartitionUCS(ThGeoOutput.Output(geos), radian);
                using (var stringReader = new StringReader(results))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var dictionary = new Dictionary<Entity, Vector3d>();
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    features.Where(f => f.Geometry is Polygon).ForEach(f =>
                    {
                        var pline = f.Geometry.ToDbCollection()[0] as AcPolygon;
                        var entity = MakeValid(pline);
                        if (entity.Area > 0)
                        {
                            entity = Normalize(entity);
                            entity = Simplify(entity);
                            entity = Filter(entity);
                        }
                        if (entity.Area > 0)
                        {
                            var direction = f.Attributes["ucs_direction"] as List<object>;
                            var vector = new Vector3d(Convert.ToDouble(direction[0]), Convert.ToDouble(direction[1]), 0.0);
                            dictionary.Add(entity, vector);
                        }
                    });
                    return dictionary;
                }
            }
        }

        private static AcPolygon MakeValid(AcPolygon line)
        {
            var result = line.MakeValid().Cast<AcPolygon>();
            if (result.Any())
            {
                return result.OrderByDescending(p => p.Area).First();
            }
            return new AcPolygon();
        }

        private static AcPolygon Normalize(AcPolygon pline)
        {
            var objs = new List<AcPolygon>();
            pline.Buffer(-OFFSET_DISTANCE)
                    .Cast<AcPolygon>()
                    .ForEach(o =>
                    {
                        o.Buffer(OFFSET_DISTANCE)
                        .Cast<AcPolygon>()
                        .ForEach(e => objs.Add(e));
                    });
            return objs.OrderBy(o => o.Area).First();
        }

        public static AcPolygon Simplify(AcPolygon pline)
        {
            return pline.DPSimplify(DISTANCE_TOLERANCE);
        }

        public static AcPolygon Filter(AcPolygon pline)
        {
            if (pline.Area > AREA_TOLERANCE)
            {
                return pline;
            }
            return new AcPolygon();
        }
    }
}
#endif