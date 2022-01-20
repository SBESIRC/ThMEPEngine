using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThLinkWireFilter
    {
        /*
         *  --------***-------***-------***--------***---------
         *   --- 代表在Edge上创建的线 Wires
         *   *** 代表默认灯自己的线 Lights
         *   目的是为了过滤此类的线: 一端连着灯，一端未连灯
         *   一个编号代表一个回路，每个回路都有自己连接的线
         */
        #region ---------- input ------------
        /// <summary>
        /// 灯线
        /// </summary>
        private DBObjectCollection Wires { get; set; }
        /// <summary>
        /// 灯
        /// </summary>
        private DBObjectCollection Lights { get; set; }
        #endregion
        #region ---------- output ----------
        private Dictionary<Curve, int> WireUseNumberDict { get; set; }
        public DBObjectCollection Results { get; private set; }
        #endregion

        public ThLinkWireFilter(
            DBObjectCollection wires,
            DBObjectCollection lights)
        {
            Wires = wires;
            Lights = lights;
            Results = new DBObjectCollection();
        }

        public void Filter()
        {
            var lightRoute = new ThLightRouteService(Wires, Lights)
            {
                IsTraverseLightMidPoint = true,
            };
            lightRoute.Traverse();
            var links = lightRoute.Links;
            links = DuplicateRemove(links);
            var garbages = new List<Tuple<ThLightLink, ThLightLink>>(); // <丢弃的链路，保留的链路>
            var results = Select(links.Select(o=>o).ToList(),out garbages);
            var eraseCurves = Filter(garbages, links);
            results.ForEach(l => AddToResults(l.Wires));
            RemoveFromResults(eraseCurves);
        }

        private ThLinkEntity FindTarget(ThLightLink link)
        {
            if(link.Wires.Count==1)
            {
                return link.Wires[0].EndPoint.DistanceTo(link.Source.LinkPt) <
                    link.Wires[0].EndPoint.DistanceTo(link.Target.LinkPt) ? link.Source : link.Target;
            }
            else
            {
                return null;
            }
        }
        
        private List<Curve> Filter(List<Tuple<ThLightLink, ThLightLink>> garbages,List<ThLightLink> links)
        {
            var results = new List<Curve>();
            garbages.ForEach(o =>
            {
                // 找到跳接线终点连接的灯
                var tartget = FindTarget(o.Item2); 
                if(tartget!=null)
                {
                    // 找到与丢弃的链路具有共边的链路
                    var overlaps = FindOverlapLinks(links, o.Item1);
                    overlaps.Remove(o.Item1);
                    // 找到具有与tartget相同的链路
                    overlaps = overlaps.Where(l => l.Source.IsEqual(tartget) || l.Target.IsEqual(tartget)).ToList();
                    if (overlaps.Count == 1)
                    {
                        var wires = new DBObjectCollection();
                        o.Item1.Wires.ForEach(w => wires.Add(w));
                        overlaps[0].Wires.ForEach(w => wires.Add(w));
                        int threewayNumber = GetThreewayNumber(wires);
                        if(threewayNumber==1)
                        {
                            results.AddRange(FindPublic(o.Item1.Wires, overlaps[0].Wires));
                        }
                    }
                }
            });
            return results;
        }

        private List<Curve> FindPublic(List<Curve> first,List<Curve> second)
        {
            var results = new List<Curve>();
            for(int i=0;i< first.Count;i++)
            {
                if(second.Contains(first[i]))
                {
                    results.Add(first[i]);
                }
            }
            return results;
        }

        private int GetThreewayNumber(DBObjectCollection wires)
        {
            return wires.Distinct().OfType<Line>().ToList().GetThreeWays().Count;
        }

        private List<ThLightLink> FindOverlapLinks(List<ThLightLink> links, ThLightLink garbage)
        {
            return links.Where(o => IsContains(garbage.Wires, o.Wires)).ToList();
        }

        private bool IsContains(List<Curve> first,List<Curve> second)
        {
            return second.Where(o => first.Contains(o)).Any();
        }

        private List<ThLightLink> DuplicateRemove(List<ThLightLink> links)
        {
            // 过滤具有相同路径
            var results = new List<ThLightLink>();  
            while(links.Count>0)
            {
                var first = links.First();
                results.Add(first);
                links.Remove(first);
                links = links.Where(o => !first.IsEqual(o)).ToList();
            }
            return results;
        }
        private List<ThLightLink> Select(List<ThLightLink> links,
            out List<Tuple<ThLightLink, ThLightLink>> garbages)
        {
            var results = new List<ThLightLink>();
            garbages = new List<Tuple<ThLightLink, ThLightLink>>();
            while(links.Count>0)
            {
                var first = links.First();
                var sameIdLinks = links.Where(o => IsSourceTargetIdEqual(first, o)).ToList();
                sameIdLinks.ForEach(o => links.Remove(o));
                if(sameIdLinks.Count == 1)
                {
                    results.Add(sameIdLinks[0]);
                }
                else if(sameIdLinks.Count == 2)
                {
                    var shortest = sameIdLinks.OrderBy(o => o.Wires.Count).First(); // 保留连线数量最少的
                    if (shortest.Wires.Count == 1)
                    {
                        sameIdLinks.Remove(shortest);
                        results.Add(shortest);
                        garbages.Add(Tuple.Create(sameIdLinks[0], shortest));
                    }
                    else
                    {
                        results.AddRange(sameIdLinks);
                    }
                }
                else 
                {
                    //
                    results.AddRange(sameIdLinks);
                }
            }
            return results;
        }
        private bool IsSourceTargetIdEqual(ThLightLink first, ThLightLink second)
        {
            return (first.Source.Id == second.Source.Id && first.Target.Id == second.Target.Id) ||
                (first.Source.Id == second.Target.Id && first.Target.Id == second.Source.Id);
        }
        private void AddToResults(List<Curve> pathLines)
        {
            pathLines
                .Where(o => !Results.Contains(o))
                .ForEach(o => Results.Add(o));
        }
        private void RemoveFromResults(List<Curve> eraseEdges)
        {
            eraseEdges.ForEach(o => Results.Remove(o));
        }
    }
    internal class ThJumpWireFilter
    {
        /*
         *      /----------------------\      /----------------------\
         *     /                        \    /                        \
         *    /                          \  /                          \
         *  ***                          ***                           ***
         *   - / 代表跳接线（用来连接两盏灯）
         *   *** 代表默认灯
         *   目的是为了过滤此类的线: 一端连着灯，一端未连灯,
         *   一个编号代表一个回路，每个回路都有自己连接的线
         */
        #region ---------- input ------------
        /// <summary>
        /// 灯线
        /// </summary>
        private DBObjectCollection Wires { get; set; }
        /// <summary>
        /// 灯坐标位置
        /// </summary>
        Dictionary<Point3d, Tuple<double, string>> LightPos { get; set; }
        /// <summary>
        /// 灯长
        /// </summary>
        private double LampLength { get; set; }
        #endregion
        private ThCADCoreNTSSpatialIndex WireSpatialIndex { get; set; }
        public Dictionary<Point3d, Tuple<double, string>> Results { get; private set; }
        public ThJumpWireFilter(
            DBObjectCollection wires,
            Dictionary<Point3d, Tuple<double,string>> lightPos,
            double lampLength)
        {
            Wires = wires;
            LightPos = lightPos;
            LampLength = lampLength;
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(wires);
            Results = new Dictionary<Point3d, Tuple<double, string>>();
        }

        public void Filter()
        {
            LightPos.ForEach(o =>
            {
                var light = ThBuildLightLineService.CreateLine(o.Key, o.Value.Item1, LampLength);
                var extents = Extend(light.StartPoint, light.EndPoint,ThGarageLightCommon.RepeatedPointDistance);
                var outline = CreatePolyline(extents.Item1, extents.Item2, ThGarageLightCommon.RepeatedPointDistance * 2.0);
                var wires = Query(outline);
                if(wires.Count == 0)
                {
                    Results.Add(o.Key,o.Value);
                }
                light.Dispose(); // 释放资源
                outline.Dispose(); // 
            });
        }

        private DBObjectCollection Query(Polyline outline)
        {
            return WireSpatialIndex.SelectCrossingPolygon(outline);
        }

        private Polyline CreatePolyline(Point3d start,Point3d endPt,double width)
        {
            return ThDrawTool.ToOutline(start, endPt, width);
        }

        private Tuple<Point3d,Point3d> Extend(Point3d start, Point3d endPt,double length)
        {
            var dir = start.GetVectorTo(endPt).GetNormal();
            var newStart = start - dir.MultiplyBy(length);
            var newEnd = endPt + dir.MultiplyBy(length);
            return Tuple.Create(newStart, newEnd);
        }
    }
}
