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

        private List<BranchLinkFilterPath> BranchFilterPaths { get; set; }
        #endregion
        #region ---------- output ----------
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

        public ThLinkWireFilter(
            DBObjectCollection wires,
            List<BranchLinkFilterPath> filterPaths)
        {
            Wires = wires;
            BranchFilterPaths = filterPaths;
            Results = new DBObjectCollection();
        }

        public void Filter1()
        {
            var lightRoute = new ThLightRouteService(Wires, Lights)
            {
                IsTraverseLightMidPoint = true,
            };
            lightRoute.Traverse();
            var links = lightRoute.Links;
            links = DuplicateRemove(links); // 去掉路径相同的链路
            links.ForEach(l => AddToResults(l.Wires)); // 把链路的边添加到Results中
            ////var spatialIndex = new ThCADCoreNTSSpatialIndex(Results); // 供后面查询使用
             
            ////// 查找具有相同起、终点的链路
            ////var sameLinks = Select(links.Select(o=>o).ToList());

            ////// 查找要被过滤的边
            ////var eraseCurves = Filter(sameLinks, links,spatialIndex);

            ////// 从Results里移除eraseCurves中的边
            ////RemoveFromResults(eraseCurves);
        }

        public void Filter2()
        {
            // 查找要过滤路径上的灯线
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Wires);
            var paths = BranchFilterPaths.Where(o => o.Edges.Count > 0).Select(o => o.GetPath()).ToList();
            var collector = new DBObjectCollection();
            paths.ForEach(p =>
            {
                var outline = Buffer(p, 1.0);
                if(outline.Area>1.0)
                {
                    var wires = spatialIndex.SelectCrossingPolygon(outline).OfType<Line>().ToCollection();
                    var pathLines = p.ToLines();
                    wires = wires.OfType<Line>().Where(l => pathLines.Where(o => l.IsCollinear(o, 1.0)).Any()).ToCollection();
                    wires = wires.OfType<Line>().Where(l => pathLines.Where(o => l.HasCommon(o, 1.0)).Any()).ToCollection();
                    collector = collector.Union(wires);
                }
            });

            // 返回值
            Results = Wires.Difference(collector);
        }

        private Polyline Buffer(Polyline path,double length)
        {
            var objs = new DBObjectCollection() { path };
            var results = objs.Buffer(length);
            var polys = results.OfType<Polyline>().OrderByDescending(p => p.Area);
            if(polys.Count()>0)
            {
                return polys.First();
            }
            else
            {
                return new Polyline() { Closed=true};
            }
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
        
        private List<Curve> Filter(
            List<Tuple<ThLightLink, ThLightLink>> sameLinks,
            List<ThLightLink> links, 
            ThCADCoreNTSSpatialIndex spatialIndex)
        {
            /*                 (2)    
             * ----------|------------
             *           |   /
             *       (1) |  /(shortest)
             *           | /
             *           |/
             */
            // 1和2形成的路由和shortest产生冲突，优先保留Shortest
            var results = new List<Curve>();
            sameLinks.ForEach(o =>
            {
                // 找到跳接线终点连接的灯
                var tartget = FindTarget(o.Item1); // Item1->Shortest
                var cornerPts = FindCornerPts(o.Item2.Wires.OfType<Line>().ToList());
                if (cornerPts.Count == 1)
                {
                    var degree = GetDegree(cornerPts[0], spatialIndex);
                    if(degree==3)
                    {
                        var overlaps = FindOverlapLinks(links, o.Item2); //Item2(1,2)
                        overlaps.Remove(o.Item2);
                        // 找到具有与tartget相同的链路
                        overlaps = overlaps.Where(l => l.Source.IsEqual(tartget) || l.Target.IsEqual(tartget)).ToList();
                        overlaps.ForEach(l =>
                        {
                            var pubs = FindPublic(o.Item2.Wires, l.Wires);
                            pubs.ForEach(p =>
                            {
                                if (!results.Contains(p))
                                {
                                    results.Add(p);
                                }
                            });
                        });
                    }
                    else if(degree == 2)
                    {
                        o.Item2.Wires.ForEach(w =>
                        {
                            if (!results.Contains(w))
                            {
                                results.Add(w);
                            }
                        });
                    }
                }
                else if(cornerPts.Count == 0 && o.Item1.Wires.Count==1 && o.Item2.Wires.Count==1)
                {
                    // 用于处理跳线和连接线冲突
                    if (!results.Contains(o.Item2.Wires[0]))
                    {
                        results.Add(o.Item2.Wires[0]);
                    }
                }
            });
            return results;
        }

        private int GetDegree(Point3d pt, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var outline = pt.CreateSquare(ThGarageLightCommon.RepeatedPointDistance);
            var objs = spatialIndex.SelectCrossingPolygon(outline).OfType<Line>().ToCollection();
            outline.Dispose();
            return objs.Count;
        }

        private List<Point3d> FindCornerPts(List<Line> lines)
        {
            var results = new List<Point3d>();
            for (int i =0;i< lines.Count-1;i++)
            {
                var linkPtRes = lines[i].FindLinkPt(lines[i + 1]);
                if (!linkPtRes.HasValue)
                {
                    break;
                }
                if(!lines[i].IsLessThan45Degree(lines[i+1]))
                {
                    results.Add(linkPtRes.Value);
                }
            }
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
        private List<Tuple<ThLightLink, ThLightLink>> Select(List<ThLightLink> links)
        {
            /*-----------|------------
             *           |   /
             *           |  /(shortest)
             *           | /
             *           |/
             */
            var results = new List<Tuple<ThLightLink, ThLightLink>>();
            for (int i = 0; i < links.Count; i++)
            {
                var sameIdLinks = links.Where(o => IsSourceTargetIdEqual(links[i], o)).ToList();
                if (sameIdLinks.Count == 2)
                {
                    if(sameIdLinks[0].Wires.Count== sameIdLinks[1].Wires.Count)
                    {
                        if(sameIdLinks[0].Wires.Count==1)
                        {
                            if(sameIdLinks[0].Wires[0] is Arc)
                            {
                                var shortest = sameIdLinks[0];
                                results.Add(Tuple.Create(shortest, sameIdLinks[1]));
                            }
                            else if(sameIdLinks[1].Wires[0] is Arc)
                            {
                                var shortest = sameIdLinks[1];
                                results.Add(Tuple.Create(shortest, sameIdLinks[0]));
                            }
                        }
                    }
                    else
                    {
                        var shortest = sameIdLinks.OrderBy(o => o.Wires.Count).First(); // 保留连线数量最少的
                        if (shortest.Wires.Count == 1)
                        {
                            sameIdLinks.Remove(shortest);
                            results.Add(Tuple.Create(shortest, sameIdLinks[0]));
                        }
                    }
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
        public Dictionary<Point3d, Tuple<double, string>> RemovedLightPos { get; private set; }
        public ThJumpWireFilter(DBObjectCollection wires, double lampLength,
            Dictionary<Point3d, Tuple<double, string>> lightPos)
        {
            Wires = wires;
            LightPos = lightPos;
            LampLength = lampLength;
        }
        public void Filter()
        {
            var lightFilter = new ThLightFilter(Wires, LampLength, LightPos);
            lightFilter.Filter();
            RemovedLightPos = lightFilter.Results;
        }
    }
    internal class ThLightFilter
    {
        /*
         * 过滤灯没有连线的情况
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
        public ThLightFilter(DBObjectCollection wires,double lampLength,
           Dictionary<Point3d, Tuple<double, string>> lightPos)
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
                var extents = Extend(light.StartPoint, light.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
                var outline = CreatePolyline(extents.Item1, extents.Item2, ThGarageLightCommon.RepeatedPointDistance * 2.0);
                var wires = Query(outline);
                if (wires.Count == 0)
                {
                    Results.Add(o.Key, o.Value);
                }
                light.Dispose(); // 释放资源
                outline.Dispose(); // 
            });
        }
        private DBObjectCollection Query(Polyline outline)
        {
            return WireSpatialIndex.SelectCrossingPolygon(outline);
        }

        private Polyline CreatePolyline(Point3d start, Point3d endPt, double width)
        {
            return ThDrawTool.ToOutline(start, endPt, width);
        }

        private Tuple<Point3d, Point3d> Extend(Point3d start, Point3d endPt, double length)
        {
            var dir = start.GetVectorTo(endPt).GetNormal();
            var newStart = start - dir.MultiplyBy(length);
            var newEnd = endPt + dir.MultiplyBy(length);
            return Tuple.Create(newStart, newEnd);
        }
    }
}
