using System;
using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThDefaultLinkWireFilter
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
        public DBObjectCollection Results { get; private set; }
        #endregion

        public ThDefaultLinkWireFilter(
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
            lightRoute.Links.ForEach(l => AddToResults(l.Wires));
        }

        private List<ThLightLink> Select(List<ThLightLink> links)
        {
            var results = new List<ThLightLink>();
            while(links.Count>0)
            {
                var first = links.First();
                var sameIdLinks = links.Where(o => IsSourceTargetIdEqual(first, o)).ToList();
                var shortest = sameIdLinks.OrderBy(o => o.Length).First();
                results.Add(shortest);
                sameIdLinks.ForEach(o => links.Remove(o));
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
