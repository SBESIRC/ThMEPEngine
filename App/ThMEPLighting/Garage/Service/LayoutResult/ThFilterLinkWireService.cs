using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /*
     * --------- Lamp -------- Lamp -------- Lamp ----------
     * 过滤掉一端连接灯线，一端未连任何物体的线
     */
    internal class ThFilterLinkWireService
    {
        /// <summary>
        /// 
        /// </summary>
        private double BreakLength { get; set; }
        /// <summary>
        /// 灯线
        /// </summary>
        private DBObjectCollection Lights { get; set; }
        /// <summary>
        /// 连接灯的直线
        /// 注意：不是跳接线
        /// </summary>
        private DBObjectCollection Wires { get; set; }
        private ThCADCoreNTSSpatialIndex WireSpatialIndex  { get; set; }
        private ThCADCoreNTSSpatialIndex LightSpatialIndex { get; set; }

        public ThFilterLinkWireService(DBObjectCollection wires, DBObjectCollection lights,double breakLength)
        {
            Wires = wires;
            Lights = lights;
            BreakLength = breakLength;
            WireSpatialIndex = new ThCADCoreNTSSpatialIndex(wires);
            LightSpatialIndex = new ThCADCoreNTSSpatialIndex(lights);
        }

        public DBObjectCollection Filter()
        {
            var freeLines = FindFreePorts();
            var collector = new List<Line>();
            freeLines.ForEach(o =>
            {
                var lines = new List<Line> { o.Item1 };
                var findPt = ThGarageLightUtils.GetNextLinkPt(o.Item2, o.Item1.StartPoint, o.Item1.EndPoint);
                Find(findPt, lines);
                collector.AddRange(lines);
            });
            collector.ForEach(o => Wires.Remove(o));
            return Wires;
        }

        private void Find(Point3d pt,List<Line> lines)
        {
            var line = lines.Last();
            var envelop = CreatePolyline(line,pt,ThGarageLightCommon.RepeatedPointDistance);
            var lights = QueryLights(envelop);
            if(lights.Count>0)
            {
                return;
            }
            envelop = CreatePolyline(pt);
            var links = QueryWires(envelop);
            links.Remove(line);
            if (links.Count==1)
            {
                lines.Add(links[0] as Line);
                var nextPt = pt.GetNextLinkPt(lines.Last().StartPoint, lines.Last().EndPoint);
                Find(nextPt, lines);
            }
        }

        private List<Tuple<Line, Point3d>> FindFreePorts()
        {
            var results = new List<Tuple<Line, Point3d>>();
            Wires.OfType<Line>().ForEach(l =>
            {
                var startFrame = CreatePolyline(l, l.StartPoint,ThGarageLightCommon.RepeatedPointDistance);
                var startObjs = Query(startFrame);
                Remove(startObjs, l);

                var endFrame = CreatePolyline(l, l.EndPoint, ThGarageLightCommon.RepeatedPointDistance);
                var endObjs = Query(endFrame);
                Remove(endObjs, l);

                if (startObjs.Count == 0 && endObjs.Count > 0)
                {
                    results.Add(Tuple.Create(l, l.StartPoint));
                }
                else if (startObjs.Count > 0 && endObjs.Count == 0)
                {
                    results.Add(Tuple.Create(l, l.EndPoint));
                }
            });
            return results;
        }

        private Polyline CreatePolyline(Line line,Point3d pt,double tolterance=0.0)
        {
            var vec = line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
            bool isStart = pt.DistanceTo(line.StartPoint) < pt.DistanceTo(line.EndPoint);
            if(isStart)
            {
                var newStartPt = line.StartPoint + vec.MultiplyBy(tolterance);
                return CreatePolyline(newStartPt, vec.Negate(), BreakLength);
            }
            else
            {
                var newEndPt = line.EndPoint - vec.MultiplyBy(tolterance);
                return CreatePolyline(newEndPt, vec, BreakLength);
            }
        }
        
        private Polyline CreateFrame(Point3d linePort,Vector3d vec)
        {
            return CreatePolyline(linePort, vec, BreakLength);
        }

        private void Remove(DBObjectCollection objs, DBObject dbObj)
        {
            while(objs.Contains(dbObj))
            {
                objs.Remove(dbObj);
            }
        }

        private DBObjectCollection Query(Polyline frame)
        {
            var results = new DBObjectCollection();
            results = results.Union(QueryWires(frame));
            results = results.Union(QueryLights(frame));
            return results;
        }

        private DBObjectCollection QueryWires(Polyline frame)
        {
            return WireSpatialIndex.SelectCrossingPolygon(frame);
        }

        private DBObjectCollection QueryLights(Polyline frame)
        {
            return LightSpatialIndex.SelectCrossingPolygon(frame);
        }

        private Polyline CreatePolyline(Point3d pt,Vector3d vec,double length,double width=1.0)
        {
            var extendPt = pt+vec.GetNormal().MultiplyBy(length);
            return ThDrawTool.ToRectangle(pt, extendPt, width);
        }
        private Polyline CreatePolyline(Point3d pt,double width = 2.0)
        {
            return pt.CreateSquare(width);
        }
    }
}
