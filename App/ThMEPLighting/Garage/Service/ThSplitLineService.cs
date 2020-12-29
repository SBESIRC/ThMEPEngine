using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThSplitLineService
    {
        private List<Line> SplitLines { get; set; }
        private Line Current { get; set; }
        private List<Line> Lines { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThSplitLineService(List<Line> lines,Line current)
        {
            Current = current;
            Lines = lines;
            SplitLines = new List<Line>();
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(lines);
        }
        public static List<Line> Split(List<Line> lines, Line current)
        {
            var instance = new ThSplitLineService(lines, current);
            instance.Split();
            return instance.SplitLines;
        }
        private void Split()
        {
            Polyline outline = ThDrawTool.ToOutline(Current.StartPoint,Current.EndPoint,
             ThGarageLightCommon.BranchPortToMainDistance * 2);
            var objs = SpatialIndex.SelectCrossingPolygon(outline);
            objs.Remove(Current);
            var filterLines =
                Lines
                .Where(o => objs.Contains(o))
                .Where(o => !ThGeometryTool.IsCollinearEx(
                    Current.StartPoint, Current.EndPoint,
                    o.StartPoint, o.EndPoint))
                .Where(o => !Current.IsLink(o, ThGarageLightCommon.RepeatedPointDistance))
                .ToList();
            var branchPts = new List<Point3d>();
            filterLines.ForEach(o =>
            {
                var pts = BuildBranchPt(Current, o);
                if (pts.Count > 0)
                {
                    branchPts.Add(pts[0]);
                }
            });            
            if (branchPts.Count > 0)
            {
                SplitLines = SplitLine(Current, branchPts);
            }
        }

        private List<Line> SplitLine(Line origin, List<Point3d> splitPts, double closeDis = 10.0)
        {
            var splitLines = new List<Line>();
            splitPts = splitPts
                .Where(o =>
                o.DistanceTo(origin.StartPoint) > closeDis &&
                o.DistanceTo(origin.EndPoint) > closeDis)
                .Where(o => 
                (o.DistanceTo(origin.StartPoint) + o.DistanceTo(origin.EndPoint))
                <= (origin.Length+1.0)) //有误差，eg. 378360.46402006829<=378360.46402006823
                .ToList();
            splitPts = splitPts.OrderBy(o => o.DistanceTo(origin.StartPoint)).ToList();
            if(splitPts.Count>0)
            {
                Point3d start = origin.StartPoint;
                for (int i = 0; i < splitPts.Count; i++)
                {
                    splitLines.Add(new Line(start, splitPts[i]));
                    start = splitPts[i];
                }
                splitLines.Add(new Line(start, origin.EndPoint));
            }
            return splitLines;
        }
        private Point3dCollection BuildBranchPt(Line mainEdge, Line secondaryEdge)
        {
            var pts = new Point3dCollection();
            mainEdge.IntersectWith(secondaryEdge, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            return pts;
        }
    }
}
