using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Segment;

namespace ThMEPEngineCore.Service
{
    public class ThLinealBeamSplitterExtension
    {
        private ThIfcLineBeam LineBeam { get; set; }
        private ThSegment SplitSegment { get; set; }
        public List<ThIfcLineBeam> SplitBeams { get; private set; }
        public ThLinealBeamSplitterExtension(ThIfcLineBeam lineBeam,ThSegment segment)
        {
            LineBeam = lineBeam;
            SplitSegment = segment;
            SplitBeams = new List<ThIfcLineBeam>();
        }
        public void Split()
        {
            List<Polyline> results = new List<Polyline>();
            ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(LineBeam.Outline as Polyline, SplitSegment.Outline);
            if (thCADCoreNTSRelate.IsCovers)
            {
                HandleCovers();
            }
            else if (thCADCoreNTSRelate.IsOverlaps)
            {
                HandleOverLaps();
            }
            else
            {
                return;
            }
        }
        public void SplitTType()
        {
            Polyline extentOutline = SplitSegment.Extend(2.0 * LineBeam.ActualWidth);
            var intersectPts = ThGeometryTool.IntersectWithEx(LineBeam.Outline as Polyline, extentOutline);
            if (intersectPts.Count == 4 && 
                IsPerpendicular(intersectPts))
            {
                var breakPts = ProjectionPoints(intersectPts);
                var midPt = ThGeometryTool.GetMidPt(breakPts[0], breakPts[breakPts.Count-1]);
                SplitBeam(midPt, midPt);
            }
        }
        private bool IsClosedBeamPort(Point3dCollection intersectPts)
        {           
            var lineBeamOutlinePts = (LineBeam.Outline as Polyline).Vertices();
            if (intersectPts.Count == 4)
            {
                bool isClose = intersectPts.Cast<Point3d>().Where(m =>
                {
                    return lineBeamOutlinePts.Cast<Point3d>().Where(n => m.DistanceTo(n) <= 1.0).Any();
                }).Any();
                return isClose ? false : true;
            }
            return false;
        }
        private bool IsPerpendicular(Point3dCollection intersectPts)
        {
            if(SplitSegment is ThLinearSegment thLinearSegment)
            {
                double rad = LineBeam.Direction.GetAngleTo(thLinearSegment.Direction);
                double angle = rad / Math.PI * 180.0;
                return Math.Abs(angle - 90.0) <= 10.0 || Math.Abs(angle - 270.0) <= 10.0 ? true : false;
            }
            else if(SplitSegment is ThArcSegment thArcSegment)
            {
                double rad = 0.0;
                if(thArcSegment.StartPoint.DistanceTo(intersectPts[0])<
                    thArcSegment.EndPoint.DistanceTo(intersectPts[0]))
                {
                    rad = LineBeam.Direction.GetAngleTo(thArcSegment.StartTangent);
                }
                else
                {
                    rad = LineBeam.Direction.GetAngleTo(thArcSegment.EndTangent);
                }
                double angle = rad / Math.PI * 180.0;
                return Math.Abs(angle - 90.0) <= 10.0 || Math.Abs(angle - 270.0) <= 10.0 ? true : false;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void HandleCovers()
        {
            Point3dCollection pts = SplitSegment.Outline.Vertices();
            var breakPts= ProjectionPoints(pts);
            if(breakPts.Count>0)
            {
                SplitBeam(breakPts[0], breakPts[breakPts.Count - 1]);
            }
        }
        private void HandleOverLaps()
        {
            var pts = LineBeam.Outline.IntersectWithEx(SplitSegment.Outline);
            var breakPts = ProjectionPoints(pts);
            if(breakPts.Count==1)
            {
                breakPts.AddRange(CollectRectInnerPoints());
                breakPts = breakPts.OrderBy(o => o.DistanceTo(LineBeam.StartPoint)).ToList();
            }
            if (breakPts.Count > 0)
            {
                SplitBeam(breakPts[0], breakPts[breakPts.Count - 1]);
            }
        }

        private List<Point3d> CollectRectInnerPoints()
        {
            Point3dCollection rectPts = SplitSegment.Outline.Vertices();
            var innerPts = rectPts.Cast<Point3d>().Where(o =>
            (LineBeam.Outline as Polyline).PointInPolylineEx(o, 1.0) == 1).ToList();
            Point3dCollection innerPtCollection = new Point3dCollection();
            innerPts.ForEach(o => innerPtCollection.Add(o));
            List<Point3d> results = ProjectionPoints(innerPtCollection);
            return results;
        }

        private List<Point3d> ProjectionPoints(Point3dCollection points)
        {
            List<Point3d> projectionPts = new List<Point3d>();
            Plane plane = new Plane(LineBeam.StartPoint, LineBeam.Direction.GetNormal());
            Matrix3d worldToPlane = Matrix3d.WorldToPlane(plane);
            Matrix3d planeToWorld = Matrix3d.PlaneToWorld(plane);            
            points.Cast<Point3d>().ForEach(o =>
            {
                Point3d pt = o.TransformBy(worldToPlane);
                pt = new Point3d(0, 0, pt.Z);
                pt = pt.TransformBy(planeToWorld);
                if (!projectionPts.Where(m => m.DistanceTo(pt) <= 1.0).Any())
                {
                    projectionPts.Add(pt);
                }
            });
            projectionPts = projectionPts.OrderBy(o => o.DistanceTo(LineBeam.StartPoint)).ToList();
            return projectionPts;
        }

        private void SplitBeam(Point3d firstPt, Point3d secondPt)
        {
            if (LineBeam.StartPoint.DistanceTo(firstPt) > 0.0)
            {
                var firstOutline = ThIfcLineBeam.CreatOutline(LineBeam.StartPoint, firstPt, LineBeam.ActualWidth);
                var clone = LineBeam.Clone() as ThIfcLineBeam;
                clone.EndPoint = firstPt;
                clone.Outline = firstOutline;
                SplitBeams.Add(clone);
            }
            if (secondPt.DistanceTo(LineBeam.EndPoint) > 0.0)
            {
                var secondOutline = ThIfcLineBeam.CreatOutline(secondPt, LineBeam.EndPoint, LineBeam.ActualWidth);
                var clone = LineBeam.Clone() as ThIfcLineBeam;
                clone.StartPoint = secondPt;
                clone.Outline = secondOutline;
                SplitBeams.Add(clone);
            }
            SplitBeams = SplitBeams.Where(o =>
              {
                  ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(SplitSegment.Outline, o.Outline as Polyline);
                  return !thCADCoreNTSRelate.IsCovers;
              }).ToList();
        }
    }
}
