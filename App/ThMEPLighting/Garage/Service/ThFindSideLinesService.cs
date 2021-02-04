using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using System;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 查找中心线两边的线槽线和端口线
    /// 不要在此类中创建新的对象返回
    /// </summary>
    public class ThFindSideLinesService
    {
        public Dictionary<Line, List<Line>> SideLinesDic { get; set; }
        public Dictionary<Line, List<Line>> PortLinesDic { get; set; }

        protected ThFindSideLinesParameter FindParameter { get; set; }

        protected ThCADCoreNTSSpatialIndex SideSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex CenterSpatialIndex { get; set; }
        protected double SideTolerance = 3.0;

        protected ThFindSideLinesService(ThFindSideLinesParameter findParameter)
        {
            FindParameter = findParameter;
            SideLinesDic = new Dictionary<Line, List<Line>>();
            PortLinesDic = new Dictionary<Line, List<Line>>();
            SideSpatialIndex = ThGarageLightUtils.BuildSpatialIndex(FindParameter.SideLines);
            CenterSpatialIndex = ThGarageLightUtils.BuildSpatialIndex(FindParameter.CenterLines);
        }
        public static ThFindSideLinesService Find(ThFindSideLinesParameter findParameter)
        {
            var instance = new ThFindSideLinesService(findParameter);
            instance.FindSide();
            instance.FindPort();
            return instance;
        }
        private void FindSide()
        {
            FindParameter.CenterLines.ForEach(o =>
                {
                    var lines = FilterSide(o);
                    if(lines.Count>=2)
                    {
                        SideLinesDic.Add(o, lines);
                    }
                });
        }
        private void FindPort()
        {
            FindParameter.CenterLines.ForEach(o =>
            {
                var lines = FilterPort(o);
                if (lines.Count > 0)
                {
                    PortLinesDic.Add(o, lines);
                }
            });
        }
        private List<Line> FilterSide(Line center)
        {
            var midPt = center.StartPoint.GetMidPt(center.EndPoint);
            var vec = center.StartPoint.GetVectorTo(center.EndPoint);
            var perpendVec = vec.GetPerpendicularVector().GetNormal();
            var sp = midPt - perpendVec.MultiplyBy(FindParameter.HalfWidth + SideTolerance);
            var ep = midPt + perpendVec.MultiplyBy(FindParameter.HalfWidth + SideTolerance);
            Polyline outline = ThDrawTool.ToRectangle(sp, ep,1.0);
            var objs= SideSpatialIndex.SelectCrossingPolygon(outline);
            return objs
                .Cast<Line>()
                .Where(o=>o.Length>0.0)
                .Where(o => ThGeometryTool.IsParallelToEx(center.LineDirection(), o.LineDirection()))
                .Where(o => DistanceIsValid(center, o))
                .Where(o=>!IsUsed(o))
                .ToList();
        }
        private bool IsUsed(Line line)
        {
            foreach(var item in SideLinesDic)
            {
                if(item.Value.IsContains(line))
                {
                    return true;
                }
            }
            return false;
        }
        private List<Line> FilterPort(Line center)
        {
            var portLines = new List<Line>();
            portLines.AddRange(GetPorts(center, center.StartPoint));
            portLines.AddRange(GetPorts(center, center.EndPoint));
            return portLines;
        }

        private Line CreateLine(Line line,Point3d portPt)
        {
            var vec = line.LineDirection().GetPerpendicularVector().GetNormal();
            var sp = portPt + vec.MultiplyBy(FindParameter.HalfWidth);
            var ep = portPt - vec.MultiplyBy(FindParameter.HalfWidth);
            return new Line(sp, ep);
        }

        private List<Line> GetPorts(Line center, Point3d portPt)
        {
            var results = new List<Line>();
            var square = ThDrawTool.CreateSquare(portPt, 4.0);
            var portObjs = SideSpatialIndex.SelectCrossingPolygon(square);
            portObjs.Remove(center);
            if (portObjs.Count == 1)
            {
                var line = portObjs[0] as Line;
               if (IsUprightAngle(center, line) &&
                    Math.Abs(line.Length-2* FindParameter.HalfWidth)<=1.0)
                {
                    results.Add(line);
                }
            }
            return results;
        }
        private bool IsUprightAngle(Line first,Line second)
        {
            var firstVec = first.StartPoint.GetVectorTo(first.EndPoint);
            var secondVec = second.StartPoint.GetVectorTo(second.EndPoint);

            var ang = firstVec.GetAngleTo(secondVec);
            ang = ang / Math.PI * 180.0;

            return Math.Abs(ang - 90.0) <= 1.0;
        }

        protected bool DistanceIsValid(Line first,Line second)
        {
            double dis = first.Distance(second);
            return dis >= (FindParameter.HalfWidth - SideTolerance / 2.0) &&
                dis <= (FindParameter.HalfWidth + SideTolerance / 2.0);
        }
    }
}
