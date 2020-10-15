using System;
using System.Linq;
using ThCADCore.NTS;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.BeamInfo.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcLineBeam : ThIfcBeam, ICloneable
    {
        public static ThIfcLineBeam Create(Polyline polyline)
        {
            var segments = new PolylineSegmentCollection(polyline);
            var enumerable = segments.Where(o => o.IsLinear).OrderByDescending(o => o.ToLineSegment().Length);
            var beam = new LineBeam(
                new Line(enumerable.ElementAt(0).StartPoint.ToPoint3d(), enumerable.ElementAt(0).EndPoint.ToPoint3d()),
                new Line(enumerable.ElementAt(1).StartPoint.ToPoint3d(), enumerable.ElementAt(1).EndPoint.ToPoint3d()));
            return new ThIfcLineBeam()
            {
                EndPoint = beam.EndPoint,
                Normal = beam.BeamNormal,
                Outline = beam.BeamBoundary,
                StartPoint = beam.StartPoint,
                Uuid = Guid.NewGuid().ToString(),
            };
        }

        public static ThIfcLineBeam Create(ThIfcBeamAnnotation annotation)
        {
            var outline = CreatOutline(annotation.StartPoint, annotation.EndPoint, annotation.Size.X);
            outline.TransformBy(annotation.Matrix);
            return Create(outline);
        }

        public object Clone()
        {
            return new ThIfcLineBeam()
            {
                StartPoint = this.StartPoint,
                EndPoint = this.EndPoint,
                Normal = this.Normal,
                Uuid = Guid.NewGuid().ToString(),
                Outline = this.Outline.Clone() as Entity,
                Width = this.Width,
                Height = this.Height,
                ComponentType = this.ComponentType,
            };
        }

        public override Curve Centerline()
        {
            return new Line(StartPoint, EndPoint);
        }

        public Vector3d Direction 
        { 
            get
            {
                return this.StartPoint.GetVectorTo(this.EndPoint);
            }
        }
        public double Length
        {
            get
            {
                return StartPoint.DistanceTo(EndPoint);
            }
        }
        public override Polyline Extend(double lengthIncrement,double widthIncrement)
        {
            Polyline outline = this.Outline as Polyline;
            double actualwidth = outline.GetPoint3dAt(0).DistanceTo(outline.GetPoint3dAt(1));
            StartPoint = StartPoint - Direction.GetNormal().MultiplyBy(lengthIncrement);
            EndPoint = EndPoint + Direction.GetNormal().MultiplyBy(lengthIncrement);           
            return CreatOutline(StartPoint, EndPoint, actualwidth);
        }
        public override Polyline ExtendBoth(double startExtendLength, double endExtendLength)
        {
            Polyline outline = this.Outline as Polyline;
            Vector3d perpendDir = outline.GetPoint3dAt(0).GetVectorTo(outline.GetPoint3dAt(1));
            double actualwidth = perpendDir.Length;
            StartPoint = this.StartPoint - Direction.GetNormal().MultiplyBy(startExtendLength);
            EndPoint = this.EndPoint + Direction.GetNormal().MultiplyBy(endExtendLength);
            return CreatOutline(StartPoint, EndPoint, actualwidth);
        }
        public static Polyline CreatOutline(Point3d startPt, Point3d endPt,double width)
        {
            Vector3d direction = startPt.GetVectorTo(endPt);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            Point3d pt1 = startPt - perpendDir.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt2 = startPt + perpendDir.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt3 = pt2 + direction.GetNormal().MultiplyBy(startPt.DistanceTo(endPt));
            Point3d pt4 = pt1 + direction.GetNormal().MultiplyBy(startPt.DistanceTo(endPt));
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return pts.CreatePolyline();
        }
    }
}
