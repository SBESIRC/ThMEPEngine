using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using AcRectangle = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPTCH.CAD
{
    public static class ThTArchWindowExtension
    {
        public static void TransformBy(this TArchWindow window, Matrix3d matrix)
        {
            var profile = window.Profile();
            profile.TransformBy(matrix);
            window.SyncWithProfile(profile);
        }

        private static AcRectangle Profile(this TArchWindow window)
        {
            var profile = new AcRectangle()
            {
                Closed = true,
            };
            var vertices = new Point2dCollection()
            {
                new Point2d(-window.Width/2.0, -window.Thickness/2.0),
                new Point2d(-window.Width/2.0, window.Thickness/2.0),
                new Point2d(window.Width/2.0, window.Thickness/2.0),
                new Point2d(window.Width/2.0, -window.Thickness/2.0)
            };
            profile.CreatePolyline(vertices);
            var move = new Vector3d(window.BasePointX, window.BasePointY, window.BasePointZ);
            profile.TransformBy(ThMatrix3dExtension.MultipleTransformFroms(1.0, window.Rotation, move));
            return profile;
        }

        private static void SyncWithProfile(this TArchWindow window, AcRectangle profile)
        {
            var leftSide = profile.GetLineSegmentAt(0);
            var rightSide = profile.GetLineSegmentAt(2);
            var direction = leftSide.MidPoint.GetVectorTo(rightSide.MidPoint);
            var basePoint = leftSide.MidPoint + direction / 2.0;
            window.BasePointX = basePoint.X;
            window.BasePointY = basePoint.Y;
            window.BasePointZ = basePoint.Z;
            window.Rotation = Vector3d.XAxis.GetAngleTo(direction.GetNormal(), Vector3d.ZAxis);
        }
    }
}
