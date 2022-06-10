using System;
using GeometryExtensions;
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
            throw new NotImplementedException();
        }

        public static IfcCompositeCurveSegment ToIfcCompositeCurveSegment(IfcStore model, PolylineSegment segment)
        {
            var curveSegement = model.Instances.New<IfcCompositeCurveSegment>();
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

        private static IfcTrimmedCurve ToIfcTrimmedCurve(IfcStore model, CircularArc2d circularArc)
        {
            var trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
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
