using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThCableTrayCutService
    {
        private Dictionary<Line, List<Line>> CenterSideLines { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private double Width { get; set; }
        private ThCableTrayCutService(Dictionary<Line, List<Line>> centerSideLines, double width)
        {
            Width = width;
            CenterSideLines = centerSideLines;
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(centerSideLines.Select(o => o.Key).ToList());
        }
        public static void Cut(Dictionary<Line, List<Line>> centerSideLines, double width = 5.0)
        {
            var instance = new ThCableTrayCutService(centerSideLines, width);
            instance.Cut();
        }
        private void Cut()
        {
            var centerLines = CenterSideLines.Select(o => o.Key).ToList();
            for (int i=0;i< centerLines.Count;i++)
            {
                Cut(centerLines[i]);
            }
        }
        private void Cut(Line center)
        {
            var outline = ThDrawTool.ToOutline(center.StartPoint, center.EndPoint, 1.0);
            var dbObjs = SpatialIndex.SelectCrossingPolygon(outline);
            dbObjs.Remove(center);
            var lineVec = center.StartPoint.GetVectorTo(center.EndPoint);
            var unParallels = dbObjs
                .Cast<Line>()
                .Where(o => !ThGeometryTool.IsParallelToEx(
                    lineVec, o.StartPoint.GetVectorTo(o.EndPoint)))
                .ToList();
            var cutPtLines = FindCutLines(center, unParallels);
            var sideLines = CenterSideLines[center];
            var newSideLines = new List<Line>();
            sideLines.ForEach(o =>
            {
                var cutLines = Cut(o, cutPtLines);
                newSideLines.AddRange(cutLines);
            });
            CenterSideLines[center] = newSideLines;
        }
        private List<Tuple<Point3d, Line>> FindCutLines(Line center, List<Line> unParallels)
        {
            var cutLines = new List<Tuple<Point3d, Line>>();
            unParallels.ForEach(o =>
            {
                var pts = new Point3dCollection();
                center.IntersectWith(o, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                bool isIntersect = pts
                .Cast<Point3d>()
                .Where(p => ThGeometryTool.IsPointInLine(center.StartPoint, center.EndPoint, p, Width / 2.0))
                .Any();
                if (isIntersect)
                {
                    cutLines.Add(Tuple.Create(pts[0], o));
                }
            });
            return cutLines;
        }
        private List<Line> Cut(Line sideLine, List<Tuple<Point3d, Line>> cutPtLines)
        {
            var reuslts = new List<Line>();
            var pts = new List<Point3d>();
            cutPtLines.ForEach(o =>
            {
                var projectPt = o.Item1.GetProjectPtOnLine(sideLine.StartPoint, sideLine.EndPoint);
                if (ThGeometryTool.IsPointInLine(sideLine.StartPoint, sideLine.EndPoint, projectPt, Width))
                {
                    pts.Add(projectPt);
                }
            });
            pts = pts
                .Where(o => sideLine.StartPoint.DistanceTo(o) > 1.0)
                .OrderBy(o => sideLine.StartPoint.DistanceTo(o))
                .ToList();
            pts.Insert(0, sideLine.StartPoint);
            pts.Add(sideLine.EndPoint);
            for (int i = 0; i < pts.Count-1; i++)
            {
                reuslts.Add(new Line(pts[i],pts[i+1]));
            }
            return reuslts;
        }
    }
}
