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
    /// <summary>
    /// 双排布置，需要
    /// </summary>
    public class ThExtendFdxLinesService
    {
        private List<Line> Results { get; set; }
        private List<Line> FdxLines { get; set; }
        private List<ThWireOffsetData> WireOffsetDatas { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThExtendFdxLinesService(
            List<Line> fdxLines, 
            List<ThWireOffsetData> wireOffsetDatas)
        {
            FdxLines = fdxLines;
            WireOffsetDatas = wireOffsetDatas;
            Results = new List<Line>();
            //过滤掉中心线中含非灯线的集合
            var centerDxLines = WireOffsetDatas.Where(o => !FdxLines.IsContains(o.Center)).ToList(); 
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(
                WireOffsetDatas.Select(o => o.Center).ToList());
        }
        public static List<Line> Extend(List<Line> fdxLines,List<ThWireOffsetData> wireOffsetDatas)
        {
            var instance = new ThExtendFdxLinesService(fdxLines, wireOffsetDatas);
            instance.Extend();
            return instance.Results;
        }
        private void Extend()
        {
            FdxLines.ForEach(o =>
            {
                var fdxLine = new Line(o.StartPoint,o.EndPoint);
                Extend(fdxLine);
                Results.Add(new Line(fdxLine.StartPoint, fdxLine.EndPoint));
            });
        }
        private void Extend(Line fdxLine)
        {
            Point3d sp = fdxLine.StartPoint;
            Point3d ep = fdxLine.EndPoint;
            Extend(fdxLine, sp);
            Extend(fdxLine, ep);
        }
        private void Extend(Line fdxLine, Point3d pt)
        {
            var linkLines = SearchLines(pt, 1.0);
            linkLines = linkLines
                .Where(o => !ThGeometryTool.IsCollinearEx(
                  fdxLine.StartPoint, fdxLine.EndPoint, o.StartPoint, o.EndPoint))
                .Where(o => IsClosed(fdxLine, o))
                .ToList();
            if (linkLines.Count == 0)
            {
                return;
            }
            var wires = WireOffsetDatas.Where(o => linkLines.Contains(o.Center)).ToList();
            if (wires.Count == 1)
            {
                ThLinkElbowService.Extend(fdxLine, wires[0].First, wires[0].Second);
            }
            else if (wires.Count == 2)
            {
                if (ThGeometryTool.IsCollinearEx(
                    wires[0].Center.StartPoint,
                    wires[0].Center.EndPoint,
                    wires[1].Center.StartPoint,
                    wires[1].Center.EndPoint))
                {
                    ThLinkElbowService.Extend(fdxLine, wires[0].First, wires[0].Second);
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
        private List<Line> SearchLines(Point3d portPt, double length)
        {
            Polyline envelope = ThDrawTool.CreateSquare(portPt, length);
            var searchObjs = SpatialIndex.SelectCrossingPolygon(envelope);
            return searchObjs.Cast<Line>().ToList();
        }
        private bool IsClosed(Line fdx, Line center)
        {
            var pts = new Point3dCollection();
            fdx.IntersectWith(center, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            if(pts.Count>0)
            {
                return true;
            }
            //获取第一根线的起点距离第二根线的最近点
            Point3d pt = center.GetClosestPointTo(fdx.StartPoint, false);
            if (pt.DistanceTo(fdx.StartPoint)<=ThGarageLightCommon.RepeatedPointDistance)
            {
                return true;
            }
            //获取第一根线的终点距离第二根线的最近点
            pt = center.GetClosestPointTo(fdx.EndPoint, false);
            if (pt.DistanceTo(fdx.EndPoint) <= ThGarageLightCommon.RepeatedPointDistance)
            {
                return true;
            }
            return false;
        }
    }
}
