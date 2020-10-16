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
        public List<ThIfcLineBeam> SplitBeams { get; private set; }
        public ThLinealBeamSplitterExtension(ThIfcLineBeam lineBeam)
        {
            LineBeam = lineBeam;
            SplitBeams = new List<ThIfcLineBeam>();
        }
        public void Split(Polyline outline)
        {
            List<Polyline> results = new List<Polyline>();
            ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(LineBeam.Outline as Polyline, outline);
            if (thCADCoreNTSRelate.IsCovers || thCADCoreNTSRelate.IsOverlaps)
            {
                if(thCADCoreNTSRelate.IsCovers)
                {
                    HandleCovers(outline);
                }
                else
                {
                    HandleOverLaps(outline);
                }
                SplitBeams=SplitBeams.Where(o =>
                {
                    ThCADCoreNTSRelate cadCoreNTSRelateOne = new ThCADCoreNTSRelate(outline, o.Outline as Polyline);
                    return !cadCoreNTSRelateOne.IsCovers && !cadCoreNTSRelateOne.IsWithIn;
                }).ToList();
                SplitBeams=SplitBeams.Where(o => Math.Abs(o.ActualWidth - LineBeam.ActualWidth) <= 1.0).ToList();
                SplitBeams= SplitBeams.Where(o=> Math.Abs(o.Length - LineBeam.Length) > 1.0).ToList();
            }            
        }        
        public void SplitTType(ThIfcBeam splitBeam)
        {
            Polyline extentOutline = splitBeam.ExtendBoth(2.0 * LineBeam.ActualWidth, 2.0 * LineBeam.ActualWidth);
            var intersectPts = ThGeometryTool.IntersectWithEx(LineBeam.Outline as Polyline, extentOutline);
            if (intersectPts.Count == 4 && 
                IsPerpendicular(splitBeam, intersectPts))
            {
                var breakPts = ProjectionPoints(intersectPts);
                var midPt = ThGeometryTool.GetMidPt(breakPts[0], breakPts[breakPts.Count-1]);
                SplitBeam(new List<Point3d> { midPt });
            }
        }        
        private bool IsPerpendicular(ThIfcBeam splitBeam, Point3dCollection intersectPts)
        {
            if(splitBeam is ThIfcLineBeam thIfcLineBeam)
            {
                double rad = LineBeam.Direction.GetAngleTo(thIfcLineBeam.Direction);
                double angle = rad / Math.PI * 180.0;
                return Math.Abs(angle - 90.0) <= 10.0 || Math.Abs(angle - 270.0) <= 10.0 ? true : false;
            }
            else if(splitBeam is ThIfcArcBeam thArcBeam)
            {
                double rad = 0.0;
                if(thArcBeam.StartPoint.DistanceTo(intersectPts[0])<
                    thArcBeam.EndPoint.DistanceTo(intersectPts[0]))
                {
                    rad = LineBeam.Direction.GetAngleTo(thArcBeam.StartTangent);
                }
                else
                {
                    rad = LineBeam.Direction.GetAngleTo(thArcBeam.EndTangent);
                }
                double angle = rad / Math.PI * 180.0;
                return Math.Abs(angle - 90.0) <= 10.0 || Math.Abs(angle - 270.0) <= 10.0 ? true : false;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void HandleCovers(Polyline outline)
        {
            Point3dCollection pts = outline.Vertices();
            var breakPts= ProjectionPoints(pts);
            SplitBeam(breakPts);
        }
        
        private void HandleOverLaps(Polyline outline)
        {
            var pts = LineBeam.Outline.IntersectWithEx(outline);
            if (pts.Count == 1)
            {
                return;
            }
            DBObjectCollection outlines = new DBObjectCollection();
            outlines.Add(outline);
            var lineBeamOutline = LineBeam.Outline as Polyline;
            var differObjs = ThCADCoreNTSPolygonExtension.Difference(lineBeamOutline, outlines);
            var newBeamOutlines = differObjs.Cast<Polyline>().ToList();
            if(newBeamOutlines.Count==1 && newBeamOutlines[0].Area== lineBeamOutline.Area)
            {
                return;
            }
            newBeamOutlines.ForEach(o =>
            {
                var rectangle = o.GetMinimumRectangle();
                SplitBeams.Add(ThIfcLineBeam.Create(rectangle));
            });
        }
        private List<Point3d> ProjectionPoints(Point3dCollection points)
        {
            List<Point3d> projectionPts = new List<Point3d>(); 
            points.Cast<Point3d>().ForEach(o =>
            {
                Point3d pt = ProjectionPoint(LineBeam.StartPoint,LineBeam.EndPoint,o);               
                if (!projectionPts.Where(m => m.DistanceTo(pt) <= 1.0).Any())
                {
                    projectionPts.Add(pt);
                }
            });
            projectionPts = projectionPts.OrderBy(o => o.DistanceTo(LineBeam.StartPoint)).ToList();
            return projectionPts;
        }
        private Point3d ProjectionPoint(Point3d startPt,Point3d endPt,Point3d outerPt)
        {
            Plane plane = new Plane(startPt, startPt.GetVectorTo(endPt).GetNormal());
            Matrix3d worldToPlane = Matrix3d.WorldToPlane(plane);
            Matrix3d planeToWorld = Matrix3d.PlaneToWorld(plane);
            Point3d pt = outerPt.TransformBy(worldToPlane);
            pt = new Point3d(0, 0, pt.Z);
            pt = pt.TransformBy(planeToWorld);
            return pt;
        }
        private void SplitBeam(List<Point3d> breakPts)
        {
            if (breakPts.Count > 4)
            {
                return;
            }
            breakPts = breakPts.Where(o => !(o.DistanceTo(LineBeam.StartPoint) <= 1.0
              || o.DistanceTo(LineBeam.EndPoint) <= 1.0)).ToList();
            breakPts = breakPts.Distinct().ToList();
            Point3d startPt = LineBeam.StartPoint;
            breakPts.ForEach(o =>
            {                
                SplitBeams.Add(CreateLineBeam(startPt,o));
                startPt = o;
            });
            SplitBeams.Add(CreateLineBeam(startPt, LineBeam.EndPoint));
        }
        private ThIfcLineBeam CreateLineBeam(Point3d startPt,Point3d endPt)
        {
            var beamOutline = ThIfcLineBeam.CreatOutline(startPt, endPt, LineBeam.ActualWidth);
            var clone = LineBeam.Clone() as ThIfcLineBeam;
            clone.StartPoint = startPt;
            clone.EndPoint = endPt;
            clone.Outline = beamOutline;
            return clone;
        }
    }
}
