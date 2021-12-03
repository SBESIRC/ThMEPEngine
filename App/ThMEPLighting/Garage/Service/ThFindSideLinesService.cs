using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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
        private const double SideTolerance = 3.0;
        private const double EnvelopLength = 4.0;
        private const double LineCommonLowerLimitedValue = 5.0; //两条平行线公共部分的长度下限值

        public ThFindSideLinesService(ThFindSideLinesParameter findParameter)
        {
            FindParameter = findParameter;
            SideLinesDic = new Dictionary<Line, List<Line>>();
            PortLinesDic = new Dictionary<Line, List<Line>>();
            SideSpatialIndex = ThGarageLightUtils.BuildSpatialIndex(FindParameter.SideLines);
        }
        public static ThFindSideLinesService Find(ThFindSideLinesParameter findParameter)
        {
            var instance = new ThFindSideLinesService(findParameter);
            instance.FindSide();
            instance.FindPort();
            return instance;
        }
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> FindSides()
        {
            var results = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
            FindParameter.CenterLines.ForEach(o =>
            {
                var dir = o.LineDirection();
                var upDir = dir.GetPerpendicularVector();
                var downDir = upDir.Negate();
                var upLines = FilterSides(o, upDir).Where(u=>!IsIn(u, results)).ToList();
                var downLines = FilterSides(o, downDir).Where(d => !IsIn(d, results)).ToList();
                results.Add(o, Tuple.Create(upLines, downLines));
            });
            return results;
        }
        private bool IsIn(Line line, Dictionary<Line, Tuple<List<Line>, List<Line>>> dict)
        {
            return dict.SelectMany(o =>
            {
                var results = new List<Line>();
                results.AddRange(o.Value.Item1);
                results.AddRange(o.Value.Item2);
                return results;
            }).ToList().Contains(line);
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
                .Where(o => center.LineDirection().IsParallelToEx(o.LineDirection()))
                .Where(o => DistanceIsValid(center, o))
                .Where(o=>!IsUsed(o))
                .ToList();
        }
        private List<Line> FilterSides(Line center,Vector3d vec)
        {
            var sp = center.StartPoint + vec.GetNormal().MultiplyBy(FindParameter.HalfWidth);
            var ep = center.EndPoint + vec.GetNormal().MultiplyBy(FindParameter.HalfWidth);
            var outline = ThDrawTool.ToRectangle(sp, ep, SideTolerance);
            var objs = SideSpatialIndex.SelectCrossingPolygon(outline);
            return objs.Cast<Line>()
                .Where(o => o.Length > 0.0)
                .Where(o => center.HasCommon(o,LineCommonLowerLimitedValue))  //平行且有公共区域的线
                .Where(o => DistanceIsValid(center, o)) // 间距满足一定距离
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
            var portObjs = portPt.Query(SideSpatialIndex, EnvelopLength);
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
            return first.LineDirection().IsVertical(second.LineDirection());
        }

        protected bool DistanceIsValid(Line first,Line second)
        {
            double dis = first.Distance(second);
            return dis >= (FindParameter.HalfWidth - SideTolerance / 2.0) &&
                dis <= (FindParameter.HalfWidth + SideTolerance / 2.0);
        }
        private List<Line> FilterUncertainSides(Line center, List<Line> sideLines)
        {
            // 长度<= FindParameter.HalfWidth
            // 首尾有连接
            // 投影都在center里的
            return sideLines
                .Where(l => l.Length <= FindParameter.HalfWidth)
                .Where(l => IsLink(l))
                .Where(l => ThGeometryTool.IsProjectionPtInLine(center.StartPoint, center.EndPoint, l.StartPoint) &&
                ThGeometryTool.IsProjectionPtInLine(center.StartPoint, center.EndPoint, l.EndPoint)).ToList();
        }
        private bool IsLink(Line sideLine)
        {
            var startObjs = sideLine.StartPoint.Query(SideSpatialIndex, EnvelopLength);
            var endObjs = sideLine.EndPoint.Query(SideSpatialIndex, EnvelopLength);
            startObjs.Remove(sideLine);
            endObjs.Remove(sideLine);
            return startObjs.Count > 0 && endObjs.Count > 0;
        }
    }
}
