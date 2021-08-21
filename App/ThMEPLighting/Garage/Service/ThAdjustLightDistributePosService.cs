using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPLighting.Garage.Service
{
    public abstract class ThAdjustLightDistributePosService
    {
        protected Point3d StartPt { get; set; }
        protected Point3d EndPt { get; set; }        
        protected List<ThLightEdge> GraphEdges { get; set; }
        /// <summary>
        /// 已经布灯的边
        /// </summary>
        protected List<ThLightEdge> DistributedEdges { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        public ThAdjustLightDistributePosService(
            Tuple<Point3d, Point3d> linePorts,
            ThLightArrangeParameter arrangeParameter,
            List<ThLightEdge> graphEdges,
            List<ThLightEdge> distributedEdges)
        {
            StartPt = linePorts.Item1;
            EndPt = linePorts.Item2;
            GraphEdges = graphEdges;
            ArrangeParameter = arrangeParameter;
            DistributedEdges = distributedEdges;
        }
        public abstract List<Tuple<Point3d, Point3d>> Distribute();
        protected List<Line> FindBranchLines()
        {
            var queryInstance = ThQueryLineService.Create(GraphEdges.Select(o => o.Edge).ToList());
            return queryInstance.QueryUnparallellines(
                StartPt, EndPt, ThGarageLightCommon.RepeatedPointDistance);
        }
        protected virtual double ProjectionDis(Point3d pt)
        {
            var vec = StartPt.GetVectorTo(EndPt);
            var plane = new Plane(StartPt, vec.GetNormal());
            var mt = Matrix3d.WorldToPlane(plane);
            var newPt = pt.TransformBy(mt);
            return newPt.Z;
        }
        protected virtual List<Tuple<Point3d, Point3d>> Merge(List<Tuple<Point3d, Point3d>> originSplitPts)
        {
            var mergeSplitPts = new List<Tuple<Point3d, Point3d>>();
            for (int i = 0; i < originSplitPts.Count; i++)
            {
                var firstSegment = originSplitPts[i];
                int j = i + 1;
                for (; j < originSplitPts.Count; j++)
                {
                    var secondSegment = originSplitPts[j];
                    if (ThGeometryTool.IsOverlap(firstSegment.Item1,
                        firstSegment.Item2, secondSegment.Item1, secondSegment.Item2))
                    {
                        var pts = new List<Point3d> {
                            firstSegment.Item1,
                            firstSegment.Item2,
                            secondSegment.Item1,
                            secondSegment.Item2};
                        firstSegment = pts.GetCollinearMaxPts();
                    }
                    else
                    {
                        break;
                    }
                }
                mergeSplitPts.Add(firstSegment);
                i = j - 1;
            }
            return mergeSplitPts;
        }
        protected List<Tuple<Point3d, Point3d>> ObtainArrangedSegments(List<Tuple<Point3d, Point3d>> occupiedSections)
        {
            //occupiedSections 已经在”GetOccupiedSection“排序
            var results = new List<Tuple<Point3d, Point3d>>();
            if (occupiedSections.Count == 0)
            {
                results.Add(Tuple.Create(StartPt, EndPt));
            }
            else
            {
                occupiedSections = Merge(occupiedSections);
                Point3d basePt = StartPt;
                foreach (var segment in occupiedSections)
                {
                    if (ThGeometryTool.IsPointInLine(segment.Item1, segment.Item2, basePt, -1.0))
                    {
                        basePt = GetSplitPt(segment, true);
                        continue;
                    }
                    else
                    {
                        var prePt = GetSplitPt(segment, false);
                        results.Add(Tuple.Create(basePt, prePt));
                        basePt = GetSplitPt(segment, true);
                    }
                }
                if (ThGeometryTool.IsPointInLine(StartPt, EndPt, basePt, -1.0) &&
                    basePt.DistanceTo(EndPt) > ThGarageLightCommon.RepeatedPointDistance)
                {
                    results.Add(Tuple.Create(basePt, EndPt));
                }
            }
            return results.Where(o => o.Item1.DistanceTo(o.Item2) > 0.0).ToList();
        }
        private Point3d GetSplitPt(Tuple<Point3d, Point3d> segment, bool isNext)
        {
            var segSp = segment.Item1;
            var segEp = segment.Item2;
            var segVec = segment.Item1.GetVectorTo(segment.Item2);
            var lineVec = StartPt.GetVectorTo(EndPt);
            if (!segVec.IsCodirectionalTo(lineVec))
            {
                segSp = segment.Item2;
                segEp = segment.Item1;
            }
            return isNext ? segEp : segSp;
        }
    }
}
