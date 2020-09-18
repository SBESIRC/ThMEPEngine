using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Segment;

namespace ThMEPEngineCore.Service
{
    public class ThLinealBeamSplitter: ThBeamSplitter, IDisposable
    {
        private ThIfcLineBeam LineBeam { get; set; }       
        private Line CenterLine { get; set; }
       
        public ThLinealBeamSplitter(ThIfcLineBeam thIfcLineBeam, List<ThSegment> segments) :base(segments)
        {
            LineBeam = thIfcLineBeam;
            CenterLine = new Line(LineBeam.StartPoint, LineBeam.EndPoint);
        }
        public override void Split()
        {
            var intersectAreas = CreateIntersectAreas();
            if (intersectAreas.Count==0)
            {
                return;
            }
            var breakPoints = CreateBreakPoints(intersectAreas);
            if (breakPoints.Count == 0)
            {
                return;
            }
            var linePoints = BreakLineBeamByCenter(LineBeam.StartPoint, LineBeam.EndPoint, breakPoints);
            if(linePoints.Count>1)
            {
                linePoints.ForEach(o=>
                {
                    if(o.Item1.DistanceTo(o.Item2)>0.0)
                    {
                        SplitBeams.Add(CreateLineBeam(LineBeam, o.Item1, o.Item2));
                    }
                });
            }
        }
        private List<Tuple<Polyline, Point3d, Point3d>> CreateIntersectAreas()
        {
            List<Tuple<Polyline, Point3d, Point3d>> intersectAreas = new List<Tuple<Polyline, Point3d, Point3d>>();
            foreach (var segment in Segments)
            {
                if(segment is ThArcSegment)
                {
                    continue;
                }
                Point3dCollection intersectPts = IntersectWithEx(LineBeam.Outline, segment.Extend(
                    LineBeam.ActualWidth* ThMEPEngineCoreCommon.BeamIntersectionRatio));
                intersectPts=OrderbyPts(intersectPts);
                if (intersectPts.Count!=4 || !ValidateIntersectPts(intersectPts, LineBeam))
                {
                    continue;
                }
                Polyline intersectArea = intersectPts.CreatePolyline();
                Point3dCollection centerLineIntersPts = IntersectWithEx(CenterLine, intersectArea);
                if (centerLineIntersPts.Count == 2)
                {
                    if (LineBeam.StartPoint.DistanceTo(centerLineIntersPts[0]) <
                        LineBeam.StartPoint.DistanceTo(centerLineIntersPts[1]))
                    {
                        intersectAreas.Add(Tuple.Create(intersectArea, centerLineIntersPts[0], centerLineIntersPts[1]));
                    }
                    else
                    {
                        intersectAreas.Add(Tuple.Create(intersectArea, centerLineIntersPts[1], centerLineIntersPts[0]));
                    }
                }
                else
                {
                    intersectArea.Dispose();
                }

            }
            //对获取的相交区域根据直梁方向由近及远排序
            intersectAreas=intersectAreas.OrderBy(o => LineBeam.StartPoint.DistanceTo(o.Item2.GetMidPt(o.Item3))).ToList();
            return intersectAreas;
        }
        private List<Tuple<Point3d, Point3d>> CreateBreakPoints(List<Tuple<Polyline, Point3d, Point3d>> intersectAreas)
        {
            List<Tuple<Point3d, Point3d>> breakPoints = new List<Tuple<Point3d, Point3d>>();
            for (int i = 0; i < intersectAreas.Count; i++)
            {
                int m = i;
                Point3d startPt = intersectAreas[i].Item2;
                for (int j = i + 1; j < intersectAreas.Count; j++)
                {
                    if (!CheckTwoLineUnIntersect(intersectAreas[m].Item2, intersectAreas[m].Item3,
                        intersectAreas[j].Item2, intersectAreas[j].Item3))
                    {
                        m = j;
                    }
                }
                Point3d endPt = intersectAreas[m].Item3;
                i = m;
                breakPoints.Add(Tuple.Create(startPt, endPt));
            }
            return breakPoints;
        }
        protected Point3dCollection OrderbyPts(Point3dCollection pts)
        {
            List<Point3d> ptList = new List<Point3d>();
            foreach (Point3d pt in pts)
            {
                ptList.Add(pt);
            }
            Point3d basePt = ptList.OrderBy(o => o.Y).ThenBy(o => o.X).FirstOrDefault();
            ptList.Remove(basePt);
            Point3dCollection sortPts = new Point3dCollection();
            ptList.OrderBy(o => Math.Cos(basePt.GetVectorTo(o).GetAngleTo(
                Vector3d.XAxis, Vector3d.ZAxis))).ToList().ForEach(o => sortPts.Add(o));
            sortPts.Insert(0, basePt);
            return sortPts;
        }
        protected bool ValidateIntersectPts(Point3dCollection intersPts, ThIfcLineBeam thIfcLineBeam, double tolerance = 1.0)
        {
            bool valid = true;
            foreach (Point3d pt in intersPts)
            {
                Point3d projectPt = pt.GetProjectPtOnLine(thIfcLineBeam.StartPoint, thIfcLineBeam.EndPoint);
                if (projectPt.DistanceTo(thIfcLineBeam.StartPoint) <= tolerance ||
                    projectPt.DistanceTo(thIfcLineBeam.EndPoint) <= tolerance)
                {
                    valid = false;
                    break;
                }
            }
            return valid;
        }
        /// <summary>
        /// 打断直梁中心线
        /// </summary>
        /// <param name="breakPoints">在线内，相互无交集</param>
        /// <returns></returns>
        protected List<Tuple<Point3d, Point3d>> BreakLineBeamByCenter(
            Point3d startPt, Point3d endPt, List<Tuple<Point3d, Point3d>> breakPoints)
        {
            List<Tuple<Point3d, Point3d>> pts = new List<Tuple<Point3d, Point3d>>();
            Point3d lineSp = startPt;
            for (int i = 0; i < breakPoints.Count; i++)
            {
                Point3d midPt = breakPoints[i].Item1.GetMidPt(breakPoints[i].Item2);
                if (lineSp.DistanceTo(midPt) >1.0)
                {
                    pts.Add(Tuple.Create(lineSp, midPt));
                }                
                lineSp = midPt;
            }
            if(lineSp.DistanceTo(endPt) > 1.0)
            {
                pts.Add(Tuple.Create(lineSp, endPt));
            }            
            return pts;
        }
        protected List<Tuple<Point3d, Point3d>> BreakLineBeamBBoundary(
            Point3d startPt, Point3d endPt, List<Tuple<Point3d, Point3d>> breakPoints)
        {
            List<Tuple<Point3d, Point3d>> pts = new List<Tuple<Point3d, Point3d>>();
            Point3d lineSp = startPt;
            for (int i = 0; i < breakPoints.Count; i++)
            {
                if (lineSp.DistanceTo(breakPoints[i].Item1) > 1.0)
                {
                    pts.Add(Tuple.Create(lineSp, breakPoints[i].Item1));
                }
                lineSp = breakPoints[i].Item2;
            }
            if (lineSp.DistanceTo(endPt) > 1.0)
            {
                pts.Add(Tuple.Create(lineSp, endPt));
            }
            return pts;
        }
        public void Dispose()
        {
            CenterLine.Dispose();
        }
    }
}
