using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThLinkElbowService
    {
        private List<ThWireOffsetData> WireOffsetDatas { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThLinkElbowService(List<ThWireOffsetData> wireOffsetDatas)
        {
            WireOffsetDatas = wireOffsetDatas;
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex
                (WireOffsetDatas.Select(o=>o.Center).ToList());
        }
        public static void Link(List<ThWireOffsetData> wireOffsetDatas)
        {
            var instance = new ThLinkElbowService(wireOffsetDatas);
            instance.Link();
        }
        private void Link()
        {
            WireOffsetDatas.ForEach(o => Link(o));
        }
        private void Link(ThWireOffsetData currentWire)
        {
            Link(currentWire, currentWire.Center.StartPoint);
            Link(currentWire, currentWire.Center.EndPoint);
        }
        private void Link(ThWireOffsetData currentWire,Point3d pt)
        {
            var links = Find(currentWire.Center, pt);
            if (links.Count > 0.0)
            {
                var wires = WireOffsetDatas.Where(o => links.Contains(o.Center)).ToList();
                if (wires.Count == 1)
                {
                    Link(currentWire, wires.First());
                }
                else if (wires.Count == 2)
                {
                    if (ThGeometryTool.IsCollinearEx(
                        wires[0].Center.StartPoint,
                        wires[0].Center.EndPoint,
                        wires[1].Center.StartPoint,
                        wires[1].Center.EndPoint))
                    {
                        Link(currentWire, wires[0],false);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else if (wires.Count() > 2)
                {
                    throw new NotSupportedException();
                }
            }
        }
        private List<Line> Find(Line center, Point3d portPt)
        {
            var portLines = SearchLines(portPt, 1.0);
            portLines.Remove(center);
            if (portLines.Count == 0)
            {
                return new List<Line>();
            }
            return FilterUnCollinearLines(center, portLines);
        }
        private List<Line> FilterUnCollinearLines(Line line, List<Line> linkLines)
        {
            linkLines.Remove(line);
            var collinearLines = linkLines.Where(o => ThGeometryTool.IsCollinearEx(
                  line.StartPoint, line.EndPoint, o.StartPoint, o.EndPoint)).ToList();
            if (collinearLines.Count > 0)
            {
                return new List<Line>();
            }
            var unCollinearLines = linkLines.Where(o => !ThGeometryTool.IsCollinearEx(
                  line.StartPoint, line.EndPoint, o.StartPoint, o.EndPoint)).ToList();
            return unCollinearLines;
        }
        private List<Line> SearchLines(Point3d portPt, double length)
        {
            Polyline envelope = ThDrawTool.CreateSquare(portPt, length);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs
                .Cast<Line>()
                .Where(o => o.IsLink(portPt,ThGarageLightCommon.RepeatedPointDistance))
                .ToList();
        }

        private void Link(ThWireOffsetData current, ThWireOffsetData other,bool isBothLink=true)
        {
            if(ThGeometryTool.IsCollinearEx(
                current.Center.StartPoint,current.Center.EndPoint,
                other.Center.StartPoint,other.Center.EndPoint))
            {
                return;
            }
            var centerInters = new Point3dCollection();
            current.Center.IntersectWith(other.Center, Intersect.ExtendBoth, centerInters, IntPtr.Zero, IntPtr.Zero);
            if (centerInters.Count == 0)
            {
                return;
            }
            Extend(current.First, other.First, other.Second);
            Extend(current.Second, other.First, other.Second);
            if(isBothLink)
            {
                Extend(other.First, current.First, current.Second);
                Extend(other.Second, current.First, current.Second);
            }
        }
        public static void Extend(Line current,Line first,Line second)
        {
            var firstInters = new Point3dCollection();
            var secondInters=new Point3dCollection();
            current.IntersectWith(first, Intersect.ExtendBoth, firstInters, IntPtr.Zero, IntPtr.Zero);
            current.IntersectWith(second, Intersect.ExtendBoth, secondInters, IntPtr.Zero, IntPtr.Zero);
            if(firstInters.Count==0 || secondInters.Count == 0)
            {
                return;
            }
            var ptPairs = new List<Tuple<Point3d, Point3d>>();
            ptPairs.Add(Tuple.Create(current.StartPoint, firstInters[0]));
            ptPairs.Add(Tuple.Create(current.StartPoint, secondInters[0]));
            ptPairs.Add(Tuple.Create(current.EndPoint, firstInters[0]));
            ptPairs.Add(Tuple.Create(current.EndPoint, secondInters[0]));
            var ptItem=ptPairs.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First();
            current.StartPoint = ptItem.Item1;
            current.EndPoint = ptItem.Item2;
        }
    }
}
