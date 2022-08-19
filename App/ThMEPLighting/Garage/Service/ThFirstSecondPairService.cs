using System;
using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThFirstSecondPairService
    {
        //1号线来源于图纸中已编号的线,需要Noding后传入
        //2号线来源于图纸中已编号的线,需要Merge后传入,偏于后期精确布点

        public Dictionary<Line, List<Line>> Pairs { get; private set; }
        private ThCADCoreNTSSpatialIndex SecondSpatialIndex { get; set; }

        /// <summary>
        /// 1号线所有分割线的索引
        /// </summary>
        private ThQueryLineService FirstQueryInstance { get; set; }

        public ThFirstSecondPairService(List<Line> firstLines, List<Line> secondLines, double doubleRowOffsetDis)
        {
            Pairs = new Dictionary<Line, List<Line>>();
            SecondSpatialIndex = ThGarageLightUtils.BuildSpatialIndex(secondLines);
            firstLines.ForEach(o =>
            {
                var newLine = ThGarageLightUtils.NormalizeLaneLine(o);
                var secondlines = Query(newLine.StartPoint, newLine.EndPoint, (doubleRowOffsetDis + 10.0) * 2.0);
                Pairs.Add(o, secondlines);
            });

            FirstQueryInstance = ThQueryLineService.Create(firstLines);
        }

        private List<Line> Query(Point3d sp, Point3d ep, double length)
        {
            //找到与first平行
            //且有共同部分的线
            var rectangle = ThDrawTool.ToRectangle(sp, ep, length);
            var objs = SecondSpatialIndex.SelectCrossingPolygon(rectangle);
            var vec = sp.GetVectorTo(ep);
            var line = new Line(sp, ep);
            return objs
                .Cast<Line>()
                .Where(o => vec.IsParallelToEx(o.StartPoint.GetVectorTo(o.EndPoint)))
                .Where(o => line.HasCommon(o))
                .ToList();
        }

        /// <summary>
        /// 通过1号线找到对应的2号线
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public List<Line> Query(Line first)
        {
            if (Pairs.ContainsKey(first))
            {
                return Pairs[first];
            }
            else
            {
                return new List<Line>();
            }
        }

        public Line FindFirstByPt(Point3d pt, double range = 20.0, bool isLink = false)
        {
            var firstLines = FirstQueryInstance.Query(pt, range, isLink);
            return firstLines.Count > 0 ? firstLines[0] : new Line();
        }

        public List<Line> Intersection()
        {
            var results = new List<Line>();
            Pairs.ForEach(p =>
            {
                var firstSeg = p.Key.ToNTSLineSegment();
                p.Value.ForEach(v =>
                {
                    var secondSeg = v.ToNTSLineSegment();
                    results.Add(firstSeg.Project(secondSeg).ToDbLine());
                });
            });
            return results;
        }

        public List<Line> Difference()
        {
            var results = new List<Line>();
            Pairs.ForEach(p =>
            {
                var firstSeg = p.Key.ToNTSLineSegment();
                var projectionLines = new List<Line>();
                p.Value.ForEach(v =>
                {
                    var secondSeg = v.ToNTSLineSegment();
                    projectionLines.Add(firstSeg.Project(secondSeg).ToDbLine());
                });

                //减去公共部分
                results.AddRange(p.Key.Difference(projectionLines));
            });
            return results;
        }

        public Point3d? FindSecondStart(Point3d start, double doubleRowOffsetDis, double tolerance = 5.0)
        {
            var firstLine = FindFirstByPt(start, tolerance, true);
            foreach (var second in Query(firstLine))
            {
                if (second.LineDirection().IsParallelToEx(firstLine.LineDirection()))
                {
                    if (Math.Abs(start.DistanceTo(second.StartPoint) - doubleRowOffsetDis) <= 5.0)
                    {
                        return second.StartPoint;
                    }
                    if (Math.Abs(start.DistanceTo(second.EndPoint) - doubleRowOffsetDis) <= 5.0)
                    {
                        return second.EndPoint;
                    }
                }
            }
            return null;
        }

        public Dictionary<Line, List<Line>> FindSecondLines(List<Line> firstLines)
        {
            var results = new Dictionary<Line, List<Line>>();
            firstLines.ForEach(f => results.Add(f, Query(f)));
            return results;
        }
    }
}
