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

namespace ThMEPEngineCore.Service
{
    public class ThLinealBeamSplitterExtension
    {
        private ThIfcLineBeam LineBeam { get; set; }
        private Polyline SplitRectangle { get; set; }
        public List<ThIfcLineBeam> SplitBeams { get; private set; }
        public ThLinealBeamSplitterExtension(ThIfcLineBeam lineBeam,Polyline polyline)
        {
            LineBeam = lineBeam;
            SplitRectangle = polyline;
            SplitBeams = new List<ThIfcLineBeam>();
        }
        public void Split()
        {
            List<Polyline> results = new List<Polyline>();
            ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(LineBeam.Outline as Polyline, SplitRectangle);
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
        private void HandleCovers()
        {
            Point3dCollection pts = SplitRectangle.Vertices();
            var breakPts= ProjectionPoints(pts);
            if(breakPts.Count>0)
            {
                SplitBeam(breakPts[0], breakPts[breakPts.Count - 1]);
            }
        }
        private void HandleOverLaps()
        {
            var pts = LineBeam.Outline.IntersectWithEx(SplitRectangle);
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
            Point3dCollection rectPts = SplitRectangle.Vertices();
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
                  ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(SplitRectangle, o.Outline as Polyline);
                  return !thCADCoreNTSRelate.IsCovers;
              }).ToList();
        }
    }
}
