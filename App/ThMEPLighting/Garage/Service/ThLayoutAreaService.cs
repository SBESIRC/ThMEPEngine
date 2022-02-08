using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public static class ThLayoutAreaService
    {
        /// <summary>
        /// 计算可以布置的线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="polygons"></param>
        /// <param name="filterInner"></param>
        /// <returns></returns>
        public static List<Line> CalculateLayoutParts(this List<Line> lines, DBObjectCollection polygons)
        {
            // 查询
            var spatialIndex = new ThCADCoreNTSSpatialIndex(polygons);
            Func<Line, DBObjectCollection> Query = (l) =>
            {
                var rec = l.Buffer(1.0);
                return spatialIndex.SelectCrossingPolygon(rec);
            };

            var results = new DBObjectCollection();
            var clones = lines.Select(o => o.Clone() as Line).ToCollection();
            clones.OfType<Line>().ForEach(l =>
            {
                var objs = Query(l);
                if (objs.Count == 0)
                {
                    results.Add(l);
                }
                else
                {
                    var splitRes = Split(l, polygons, 1.0);
                    var outerRes = splitRes.GetOutter(polygons);
                    outerRes.OfType<Line>().ForEach(s => results.Add(s));
                }
            });
            return results.OfType<Line>().ToList();
        }

        /// <summary>
        /// 计算可以布置的线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="polygons"></param>
        /// <param name="filterInner"></param>
        /// <returns></returns>
        public static List<Line> CalculateUnLayoutParts(this List<Line> lines, DBObjectCollection polygons)
        {
            // 查询
            var spatialIndex = new ThCADCoreNTSSpatialIndex(polygons);
            Func<Line, DBObjectCollection> Query = (l) =>
            {
                var rec = l.Buffer(1.0);
                return spatialIndex.SelectCrossingPolygon(rec);
            };

            var results = new DBObjectCollection();
            var clones = lines.Select(o => o.Clone() as Line).ToCollection();
            clones.OfType<Line>().ForEach(l =>
            {
                var objs = Query(l);
                if (objs.Count > 0)
                {
                    var splitRes = Split(l, polygons, 1.0);  
                    var innerRes = splitRes.GetInner(polygons); // 获取在polygons内的线
                    innerRes.OfType<Line>().ForEach(s => results.Add(s));
                }
            });
            return results.OfType<Line>().ToList();
        }

        private static DBObjectCollection Split(Line line,DBObjectCollection objs,double closeDis)
        {
            var results = new DBObjectCollection();
            var curves = ToCurves(objs);
            var interPts = line.IntersectPts(curves);
            var splitRes = line.Split(interPts.OfType<Point3d>().ToList(), 1.0);
            splitRes.ForEach(s => results.Add(s));
            return results;
        }

        private static List<Curve> ToCurves(DBObjectCollection polygons)
        {
            var result = new List<Curve>();
            polygons.OfType<Entity>().ForEach(e =>
            {
                if(e is MPolygon mpolygon)
                {
                    result.Add(mpolygon.Shell());
                    result.AddRange(mpolygon.Holes());
                }
                else if(e is Curve curve)
                {
                    result.Add(curve);
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return result;
        }

        private static DBObjectCollection GetOutter(this DBObjectCollection lines, DBObjectCollection polygons,  double bufferDis = 1.0)
        {
            // 过滤在Polygon内部的线
            var filterRes = lines.GetInner(polygons, bufferDis);
            return lines.Difference(filterRes);
        }

        private static DBObjectCollection GetInner(this DBObjectCollection lines, DBObjectCollection polygons, double bufferDis = 1.0)
        {
            // 过滤在Polygon内部的线
            var filterRes = new DBObjectCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var bufferObj = bufferService.Buffer(e, bufferDis);
                var objs = spatialIndex.SelectWindowPolygon(bufferObj);
                filterRes = filterRes.Union(objs);
            });
            return filterRes;
        }

        private static Point3dCollection IntersectPts(this Line line,List<Curve> curves)
        {
            var result = new Point3dCollection();
            curves.ForEach(c =>
            {
                var pts = line.IntersectWithEx(c);
                pts.OfType<Point3d>().ForEach(p =>
                {
                    if(!result.Contains(p))
                    {
                        result.Add(p);
                    }
                });
            });
            return result;
        }
    }
}
