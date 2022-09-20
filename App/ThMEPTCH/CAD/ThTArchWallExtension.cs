using DotNetARX;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.CAD
{
    public static class ThTArchWallExtension
    {
        public static Polyline Profile(this TArchWall wall)
        {
            return wall.IsArc ? wall.CircularProfile() : wall.LinearProfile();
        }

        private static Polyline CircularProfile(this TArchWall wall)
        {
            var centerCurve = new CircularArc2d(
                wall.StartPoint.ToPoint2D(),
                wall.EndPoint.ToPoint2D(),
                wall.Bulge,
                false);
            var leftCurve = new CircularArc2d(
                centerCurve.Center,
                centerCurve.Radius + wall.LeftWidth,
                centerCurve.StartAngle,
                centerCurve.EndAngle,
                centerCurve.ReferenceVector,
                centerCurve.IsClockWise);
            var rightCurve = new CircularArc2d(
                centerCurve.Center,
                centerCurve.Radius - wall.RightWidth,
                centerCurve.StartAngle,
                centerCurve.EndAngle,
                centerCurve.ReferenceVector,
                centerCurve.IsClockWise);
            var segements = new PolylineSegmentCollection()
            {
                new PolylineSegment(leftCurve),
                new PolylineSegment(leftCurve.EndPoint, rightCurve.EndPoint),
                new PolylineSegment(rightCurve),
                new PolylineSegment(rightCurve.StartPoint, rightCurve.StartPoint),
            };
            segements.Join();
            return segements.ToPolyline();
        }

        private static Polyline LinearProfile(this TArchWall wall)
        {
            var profile = new Polyline()
            {
                Closed = true,
            };

            var centerline = new Line(wall.StartPoint, wall.EndPoint);
            var vertices = new Point2dCollection()
            {
                new Point2d(-centerline.Length/2.0, -wall.RightWidth),
                new Point2d(-centerline.Length/2.0, -wall.LeftWidth),
                new Point2d(centerline.Length/2.0, wall.LeftWidth),
                new Point2d(centerline.Length/2.0, -wall.RightWidth),
            };
            profile.CreatePolyline(vertices);
            profile.TransformBy(ThMatrix3dExtension.MultipleTransformFroms(1.0, centerline.LineDirection(), centerline.GetMidpoint()));
            return profile;
        }
    }
}
