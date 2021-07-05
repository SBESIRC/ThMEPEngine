using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Interface;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThBreakDrainageFacilityService
    {
        public List<Entity> CollectingWells { get; set; }
        public List<Entity> DrainageDitches { get; set; }

        public double DitchMaxWidth { get; set; }
        public double DrainWellMaxLength { get; set; }

        public ThBreakDrainageFacilityService()
        {
            CollectingWells = new List<Entity>();
            DrainageDitches = new List<Entity>();
            DitchMaxWidth = 510.0;
            DrainWellMaxLength = 3500;
        }
        public void Break(DBObjectCollection curves)
        {
            // Preprocess
            var service = new ThDrainageFacilityCleanService();
            var objs = service.Clean(curves); // 处理后的线

            // 获取集水井
            var clones = objs.Cast<Curve>().Select(o => o.WashClone() as Curve).ToCollection();
            CollectingWells = ObtainDrainWells(clones);

            // 获取排水沟
            DrainageDitches = ObtainDrainDitches(CollectingWells, objs);
        }   

        private List<Entity> ObtainDrainWells(DBObjectCollection lines)
        {
            // Polygons     
            lines = lines.Cast<Line>().Select(o => o.ExtendLine(1.0)).ToCollection();
            IPolygonize polygonizeServie = new ThNTSPolygonizeService();
            var polygons = polygonizeServie.Polygonize(lines).Cast<Polyline>().Where(o=>o.Area>1.0).ToCollection();

            polygons = FilterLessThan(polygons, DrainWellMaxLength);
            polygons = TransPolylineToRectangle1(polygons);

            // 内缩
            var innerDis = DitchMaxWidth/2.0*1.2; //内缩长度
            var innerBufferObjs = new DBObjectCollection();
            polygons.Cast<Polyline>().ForEach(o =>
            {
                o.Buffer(-innerDis).Cast<Entity>().ForEach(e=>innerBufferObjs.Add(e));
            });
            innerBufferObjs = innerBufferObjs.Cast<Polyline>().Where(o => o.Area > 1.0).ToCollection();

            // 合并
            var unions = innerBufferObjs
                .UnionPolygons()
                .Cast<Polyline>()
                .Where(o => o.Area > 1.0)
                .ToCollection(); //把排水井内的框线吃掉
            unions = FilterLessThan(unions, DrainWellMaxLength);
            unions = TransPolylineToRectangle1(unions);

            // 外扩
            var outerBufferObjs = new DBObjectCollection();
            unions.Cast<Polyline>().ForEach(o =>
            {
                o.Buffer(innerDis).Cast<Entity>().ForEach(e => outerBufferObjs.Add(e));
            });

            var results = outerBufferObjs.Cast<Polyline>().Where(o => o.Area > 1.0).ToCollection();
            results = TransPolylineToRectangle2(results);
            //results = results.Cast<Polyline>().Where(o => o.Area > DitchMaxWidth * DitchMaxWidth).ToCollection();
            results = FilterNeibours(results);
            return results.Cast<Entity>().ToList();
        }
        private List<Entity> ObtainDrainDitches(List<Entity> drainWells, DBObjectCollection lines)
        {
            if(lines.Count==0)
            {
                return new List<Entity>();
            }
            lines = lines.Cast<Line>().Select(o => o.ExtendLine(1.0)).ToCollection();
            lines = lines.NodingLines().ToCollection();
            lines = lines.Cast<Line>().Where(o => o.Length > 2.0).ToCollection();

            // 过滤
            IBuffer bufferService = new ThNTSBufferService();
            drainWells.ForEach(o =>
            {
                var ent = bufferService.Buffer(o, DitchMaxWidth * 0.2);
                ISpatialIndex spatialIndex = new ThNTSSpatialIndexExService(lines);
                var objs = new DBObjectCollection();
                if (ent is Polyline polyline)
                {
                    objs = spatialIndex.SelectWindowPolygon(polyline);
                }
                else if (ent is MPolygon mPolygon)
                {
                    objs = spatialIndex.SelectWindowPolygon(mPolygon);
                }
                objs.Cast<Entity>().ForEach(e => lines.Remove(e));
            });
            return lines.Cast<Entity>().ToList();
        }

        private DBObjectCollection FilterLessThan(DBObjectCollection polygons, double length)
        {
            return polygons = polygons.Cast<Polyline>().Where(o =>
            {
                return !o.GetEdges().Where(e => e.Length > length).Any();
            }).ToCollection();
        }
        private DBObjectCollection MakeValid(DBObjectCollection polygons)
        {
            IMakeValid makeValid = new ThNTSMakeValidService();
            return makeValid.MakeValid(polygons);
        }

        private DBObjectCollection DPSimplify(DBObjectCollection polygons)
        {
            ISimplify simplify = new ThNTSSimplifyService();
            return polygons.Cast<Polyline>().Select(o => simplify.DPSimplify(o,1.0)).ToCollection();
        }

        private DBObjectCollection TransPolylineToRectangle1(DBObjectCollection polygons)
        {
            var results = new DBObjectCollection();
            polygons = MakeValid(polygons);
            polygons = DPSimplify(polygons);
            polygons.Cast<Polyline>().ForEach(o =>
            {
                if(o.IsRectangle())
                {
                    results.Add(o);
                }
                else 
                {
                    var converter = new ThRectangleConverter(o);
                    var rec = converter.ConvertRectangle1();
                    if (rec.Area > 0.0)
                    {
                        results.Add(rec);
                    }
                    else
                    {
                        results.Add(o);
                    }
                }
            });
            return results;
        }

        private DBObjectCollection TransPolylineToRectangle2(DBObjectCollection polygons)
        {
            var results = new DBObjectCollection();
            polygons = MakeValid(polygons);
            polygons = DPSimplify(polygons);
            polygons.Cast<Polyline>().ForEach(o =>
            {
                var converter = new ThRectangleConverter(o);
                var rec = converter.ConvertRectangle2();
                if (rec.Area > 0.0)
                {
                    results.Add(rec);
                }
                else
                {
                    results.Add(converter.ConvertRectangle3());
                }
            });
            return results;
        }

        private DBObjectCollection FilterNeibours(DBObjectCollection drainWells)
        {
            var results = new DBObjectCollection();
            IBuffer bufferService = new ThNTSBufferService();
            while (drainWells.Count>0)
            {
                var first = drainWells[0] as Polyline;
                drainWells.Remove(first);
                ISpatialIndex spatialIndex = new ThNTSSpatialIndexService(drainWells);
                var outline = bufferService.Buffer(first, DitchMaxWidth * 0.1) as Polyline;
                var objs = spatialIndex.SelectCrossingPolygon(outline.Vertices());
                if(objs.Count>0)
                {
                    objs.Add(first);
                    var smallestPoly = objs.Cast<Polyline>().OrderBy(o => o.Area).First();
                    if(!results.Contains(first))
                    {
                        results.Add(smallestPoly);
                    }                   
                    objs.Remove(smallestPoly);
                    objs.Cast<Polyline>().ForEach(o => drainWells.Remove(o));
                }
                else
                {
                    if(!results.Contains(first))
                    {
                        results.Add(first);
                    }
                }
            }
            return results;
        }
    }
}
