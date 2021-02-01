using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindSideLinesService
    {
        public Dictionary<Line, List<Line>> SideLinesDic { get; set; }
        public Dictionary<Line, List<Line>> PortLinesDic { get; set; }

        protected ThFindSideLinesParameter FindParameter { get; set; }

        protected ThCADCoreNTSSpatialIndex SideSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex CenterSpatialIndex { get; set; }
        protected double SideTolerance = 1.0;

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
                    if(lines.Count>1)
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
            Polyline outline = ThDrawTool.ToOutline(center.StartPoint, center.EndPoint, FindParameter.HalfWidth + SideTolerance+5);
            var objs=SideSpatialIndex.SelectCrossingPolygon(outline);
            return objs
                .Cast<Line>()
                .Where(o=>o.Length>0.0)
                .Where(o => FindParameter.SideLines.Contains(o))
                .Where(o => ThGeometryTool.IsParallelToEx(center.LineDirection(), o.LineDirection()))
                .Where(o => DistanceIsValid(center, o))
                .ToList();
        }
        private List<Line> FilterPort(Line center)
        {
            var portLines = new List<Line>();
            if(IsPort(center, center.StartPoint))
            {
                portLines.Add(CreateLine(center, center.StartPoint));
            }
            if(IsPort(center, center.EndPoint))
            {
                portLines.Add(CreateLine(center, center.EndPoint));
            }
            return portLines;
        }

        private Line CreateLine(Line line,Point3d portPt)
        {
            var vec = line.LineDirection().GetPerpendicularVector().GetNormal();
            var sp = portPt + vec.MultiplyBy(FindParameter.HalfWidth);
            var ep = portPt - vec.MultiplyBy(FindParameter.HalfWidth);
            return new Line(sp, ep);
        }

        private bool IsPort(Line center, Point3d portPt)
        {
            var square = ThDrawTool.CreateSquare(portPt, 1.0);
            var portObjs = CenterSpatialIndex.SelectCrossingPolygon(square);
            portObjs.Remove(center);
            if (portObjs.Count == 0)
            {
                var vec = center.LineDirection().GetPerpendicularVector().GetNormal();
                var sp = portPt + vec.MultiplyBy(FindParameter.HalfWidth + SideTolerance);
                var ep = portPt - vec.MultiplyBy(FindParameter.HalfWidth + SideTolerance);
                var rectangle = ThDrawTool.ToOutline(sp, ep, 1.0);
                var sideObjs = SideSpatialIndex.SelectCrossingPolygon(rectangle);
                var filterObjs = sideObjs
                .Cast<Line>()
                .Where(o => o.Length > 0.0)
                .Where(o => FindParameter.SideLines.Contains(o))
                .Where(o => ThGeometryTool.IsParallelToEx(center.LineDirection(), o.LineDirection()))
                .Where(o => DistanceIsValid(center, o))
                .ToList();
                if (filterObjs.Count == 2)
                {
                    return true;
                }
            }
            return false;
        }

        protected bool DistanceIsValid(Line first,Line second)
        {
            double dis = first.Distance(second);
            return dis >= (FindParameter.HalfWidth - SideTolerance / 2.0) &&
                dis <= (FindParameter.HalfWidth + SideTolerance / 2.0);
        }
    }
}
