using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AxisLineGroup
    {
        List<Line> _axisLines;
        List<Arc> _axisArcs;
        public AxisLineGroup(List<Curve> curves) 
        {
            _axisLines = new List<Line>();
            _axisArcs = new List<Arc>();
            if (null == curves || curves.Count < 1)
                return;
            foreach (var curve in curves)
            {
                if (curve is Line line)
                {
                    _axisLines.Add((Line)line.Clone());
                }
                else if (curve is Arc arc)
                {
                    _axisArcs.Add((Arc)arc.Clone());
                }
            }
        }
        public AxisLineGroup(List<Line> axisLines, List<Arc> axisArcs) 
        {
            _axisLines = new List<Line>();
            _axisArcs = new List<Arc>();
            foreach (var curve in axisLines)
            {
                _axisLines.Add((Line)curve.Clone());
            }
            foreach (var curve in axisArcs)
            {
                _axisArcs.Add((Arc)curve.Clone());
            }
        }
        public List<AxisGroupResult> GetLineGroups() 
        {
            var groupLines = new List<AxisGroupResult>();
            var tempArcs = new List<Arc>();
            _axisArcs.ForEach(c => tempArcs.Add(c));
            var tempLines = new List<Line>();
            _axisLines.ForEach(c => tempLines.Add(c));
            var arcGroups = ArcGroupCalc(tempArcs, tempLines);
            var lineGroups = AxisLineGroupByDirection(tempLines);
            groupLines.AddRange(arcGroups);
            groupLines.AddRange(lineGroups);
            return groupLines;
        }
        private List<AxisGroupResult> ArcGroupCalc(List<Arc> axisArcs,List<Line> axisLines) 
        {
            var axisGroupResults = new List<AxisGroupResult>();
            while (axisArcs.Count > 0)
            {
                var arcs = new List<Arc>();
                var first = axisArcs.First();
                axisArcs.Remove(first);
                //获取同心圆
                var center = first.Center;
                var tempArc = axisArcs.Where(c => c.Center.DistanceTo(center) < 1).ToList();
                if (tempArc.Count < 1)
                    continue;
                foreach (var item in tempArc)
                    axisArcs.Remove(item);
                arcs.Add(first);
                arcs.AddRange(tempArc);
                //获取过该圆心的线段、和该弧线相交的线
                var lines = GetArcGroupLines(arcs, axisLines);
                foreach (var line in lines)
                    axisLines.Remove(line);
                var arcGroup = new AxisGroupResult(true);
                arcGroup.MainCurves.AddRange(arcs);
                arcGroup.OtherLines.AddRange(lines);
                axisGroupResults.Add(arcGroup);
            }
            return axisGroupResults;
        }
        private List<AxisGroupResult> AxisLineGroupByDirection(List<Line> lineAxis)
        {
            var groupLines = new List<AxisGroupResult>();
            while (lineAxis.Count > 0)
            {
                var first = lineAxis.First();
                lineAxis.Remove(first);
                var lineDir = (first.EndPoint - first.StartPoint).GetNormal();
                var parallelLines = new List<Line>();
                var verticalLines = new List<Line>();
                foreach (var line in lineAxis)
                {
                    if (IndoorFanCommon.LineIsVerticalDir(line, lineDir))
                        verticalLines.Add(line);
                    else if (IndoorFanCommon.LineIsParallelDir(line, lineDir))
                        parallelLines.Add(line);
                }
                foreach (var rmLine in parallelLines)
                    lineAxis.Remove(rmLine);
                foreach (var rmLine in verticalLines)
                    lineAxis.Remove(rmLine);
                if (verticalLines.Count < 1 || verticalLines.Count < 1)
                    continue;
                parallelLines.Add(first);
                var axisGroup = new AxisGroupResult(false);
                axisGroup.MainCurves.AddRange(parallelLines);
                axisGroup.OtherLines.AddRange(verticalLines);
                groupLines.Add(axisGroup);
            }
            return groupLines;
        }
       
        private List<Line> GetArcGroupLines(List<Arc> arcs, List<Line> targetLines)
         {
            var arcGroupLines = new List<Line>();
            foreach (var line in targetLines)
            {
                if (LineIsCrossArcCenter(line, arcs))
                    arcGroupLines.Add(line);
            }
            return arcGroupLines;
        }
        private bool LineIsCrossArcCenter(Line line, List<Arc> targetLines)
        {
            var lineSp = line.StartPoint;
            var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
            bool isInter = false;
            foreach (var arc in targetLines)
            {
                if (isInter)
                    break;
                var center = arc.Center;
                if (center.PointInLine(lineSp, lineDir))
                {
                    var inter = CircleArcUtil.ArcIntersectLineSegment(arc, line, out List<Point3d> interPoints);
                    isInter = interPoints.Count>0;
                } 
            }
            return isInter;
        }
    }
    class AxisGroupResult 
    {
        public AxisGroupResult(bool isArc) 
        {
            this.MainCurves = new List<Curve>();
            this.OtherLines = new List<Curve>();
            this.IsArc = isArc;
        }
        public bool IsArc { get; }
        public List<Curve> MainCurves { get;  }
        public List<Curve> OtherLines { get; }
    }
}
