using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 分割线
    /// </summary>
    public static class ThSplitLineService
    {
        public static Dictionary<Line, List<Line>> Split(this List<Line> originLines)
        {
            var results = new Dictionary<Line, List<Line>>();
            originLines.ForEach(o =>
            {
                var splitLines = o.Split(originLines);
                if (splitLines.Count == 0)
                {
                    var lines = new List<Line>();
                    lines.Add(new Line(o.StartPoint, o.EndPoint));
                    results.Add(o, lines);
                }
                else
                {
                    results.Add(o, splitLines);
                }
            });
            return results;
        }

        /// <summary>
        /// 求当前线被传入的线段分割线段
        /// </summary>
        /// <param name="current"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Line> Split(this Line current,List<Line> lines)
        {
            var splitLines = new List<Line>();
            var spatialIndex = ThGarageLightUtils.BuildSpatialIndex(lines);
            Polyline outline = ThDrawTool.ToOutline(current.StartPoint, current.EndPoint,
            ThGarageLightCommon.BranchPortToMainDistance * 2);
            var objs = spatialIndex.SelectCrossingPolygon(outline);
            objs.Remove(current);
            var filterLines =
                lines
                .Where(o => objs.Contains(o))
                .Where(o => !ThGeometryTool.IsCollinearEx(
                    current.StartPoint, current.EndPoint,
                    o.StartPoint, o.EndPoint))
                .Where(o => !current.IsLink(o, ThGarageLightCommon.RepeatedPointDistance))
                .ToList();
            var branchPts = new List<Point3d>();
            filterLines.ForEach(o =>
            {
                var pts = BuildBranchPt(current, o);
                if (pts.Count > 0)
                {
                    branchPts.Add(pts[0]);
                }
            });            
            if (branchPts.Count > 0)
            {
                splitLines = Split(current, branchPts);
            }
            return splitLines;
        }
        /// <summary>
        /// 一根线被多个点分割
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="splitPts"></param>
        /// <param name="closeDis"></param>
        /// <returns></returns>
        public static List<Line> Split(this Line origin, List<Point3d> splitPts, double closeDis = 10.0)
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
            else
            {
                splitLines.Add(new Line(origin.StartPoint, origin.EndPoint));
            }
            return splitLines.Where(o=>o.Length>1.0).ToList();
        }
        private static Point3dCollection BuildBranchPt(Line mainEdge, Line secondaryEdge)
        {
            var mainLine = mainEdge.ExtendLine(1.0);
            var secondaryLine = secondaryEdge.ExtendLine(1.0);
            return mainLine.IntersectWithEx(secondaryLine, Intersect.ExtendBoth);
        }
    }
}
