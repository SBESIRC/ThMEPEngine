using System.Linq;
using ThCADExtension;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.Ifc;
using Xbim.Ifc2x3.GeometryResource;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThTGL2IFC2x3DbExtension
    {
        public static IfcCompositeCurve ToIfcCompositeCurve(IfcStore model, Polyline polyline)
        {
            var compositeCurve = CreateIfcCompositeCurve(model);
            var segments = new PolylineSegmentCollection(polyline);
            segments.OfType<PolylineSegment>().ForEach(s =>
            {
                compositeCurve.Segments.Add(ToIfcCompositeCurveSegment(model, s));
            });
            return compositeCurve;
        }

        public static IfcCompositeCurve ToIfcCompositeCurve(IfcStore model, MPolygon mp)
        {
            return ToIfcCompositeCurve(model, ThMPolygonExtension.Shell(mp));
        }

        private static IfcCompositeCurve CreateIfcCompositeCurve(IfcStore model)
        {
            return model.Instances.New<IfcCompositeCurve>();
        }

        private static IfcCompositeCurveSegment CreateIfcCompositeCurveSegment(IfcStore model)
        {
            return model.Instances.New<IfcCompositeCurveSegment>(s =>
            {
                s.SameSense = true;
            });
        }

        private static IfcCompositeCurveSegment ToIfcCompositeCurveSegment(IfcStore model, PolylineSegment segment)
        {
            var curveSegement = CreateIfcCompositeCurveSegment(model);
            if (segment.IsLinear)
            {
                curveSegement.ParentCurve = ToIfcPolyline(model, segment.ToLineSegment());
            }
            else
            {
                curveSegement.ParentCurve = ToIfcTrimmedCurve(model, segment.ToCircularArc());
            }
            return curveSegement;
        }

        private static IfcPolyline ToIfcPolyline(IfcStore model, LineSegment2d lineSegment)
        {
            var poly = model.Instances.New<IfcPolyline>();
            poly.Points.Add(ToIfcCartesianPoint(model, lineSegment.StartPoint));
            poly.Points.Add(ToIfcCartesianPoint(model, lineSegment.EndPoint));
            return poly;
        }

        private static IfcCircle ToIfcCircle(IfcStore model, CircularArc2d circularArc)
        {
            return model.Instances.New<IfcCircle>(c =>
            {
                c.Radius = circularArc.Radius;
                c.Position = model.ToIfcAxis2Placement2D(circularArc.Center, Vector2d.XAxis);
            });
        }

        private static IfcTrimmedCurve ToIfcTrimmedCurve(IfcStore model, CircularArc2d circularArc)
        {
            var trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
            trimmedCurve.BasisCurve = ToIfcCircle(model, circularArc);
            trimmedCurve.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
            trimmedCurve.SenseAgreement = !circularArc.IsClockWise;
            trimmedCurve.Trim1.Add(model.ToIfcCartesianPoint(circularArc.StartPoint));
            trimmedCurve.Trim2.Add(model.ToIfcCartesianPoint(circularArc.EndPoint));
            return trimmedCurve;
        }

        private static IfcCartesianPoint ToIfcCartesianPoint(IfcStore model, Point2d point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXY(point.X, point.Y);
            return pt;
        }
    }
}
