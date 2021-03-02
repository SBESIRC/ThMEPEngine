using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Service
{
    public class ThSeparateLightEdgeService
    {
        public List<Line> FirstLines { get; private set; }
        public List<Line> SecondLines { get; private set; }
        private List<Line> CenterLines { get; set; }
        private List<Line> LightEdges { get; set; }
        private double OffsetDistance { get; set; } //RacywaySpace / 2.0
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThSeparateLightEdgeService(List<Line> centerLines,List<Line> lightEdges,double offsetDistance)
        {
            CenterLines = centerLines;
            LightEdges = lightEdges;
            OffsetDistance = offsetDistance;
            FirstLines = new List<Line>();
            SecondLines = new List<Line>();
            SpatialIndex = ThGarageLightUtils.BuildSpatialIndex(LightEdges);
        }
        public void Separate()
        {
            CenterLines.ForEach(o => Find(o));
        }
        private void Find(Line centerLine)
        {            
            var length = ThOffsetLineService.CalOffsetLength(centerLine, OffsetDistance);
            var firstObjs = centerLine.GetOffsetCurves(length);
            var secondObjs = centerLine.GetOffsetCurves(-length);
            var firstLine = firstObjs[0] as Line;
            var secondLine = secondObjs[0] as Line;           
            var firstLines = Filter(firstLine.StartPoint, firstLine.EndPoint);
            var secondLines = Filter(secondLine.StartPoint, secondLine.EndPoint);
            AddToFirstLines(firstLines);
            AddToSecondLines(secondLines);
        }
        private void AddToFirstLines(List<Line> firstLines)
        {
            foreach(Line line in firstLines)
            {
                if(FirstLines.IndexOf(line)<0)
                {
                    FirstLines.Add(line);
                }
            }
        }
        private void AddToSecondLines(List<Line> secondLines)
        {
            foreach (Line line in secondLines)
            {
                if (SecondLines.IndexOf(line) < 0)
                {
                    SecondLines.Add(line);
                }
            }
        }
        private List<Line> Filter(Point3d sp,Point3d ep)
        {
            var rectangle = ThDrawTool.ToRectangle(sp, ep, 2.0);
            var objs = SpatialIndex.SelectCrossingPolygon(rectangle);
            var vec = sp.GetVectorTo(ep);
            return objs
                .Cast<Line>()
                .Where(o => vec.IsParallelToEx(o.StartPoint.GetVectorTo(o.EndPoint)))
                .ToList();
        }
    }
}
