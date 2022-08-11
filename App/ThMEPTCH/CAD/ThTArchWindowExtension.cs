using DotNetARX;
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
            var scale = window.Scale();
            var rotation = window.Rotation();
            var displacement = window.Displacement();
            profile.TransformBy(scale.PreMultiplyBy(rotation).PreMultiplyBy(displacement));
            return profile;
        }

        private static Matrix3d Scale(this TArchWindow window)
        {
            return Matrix3d.Scaling(1.0, Point3d.Origin);
        }

        private static Matrix3d Rotation(this TArchWindow window)
        {
            return Matrix3d.Rotation(window.Rotation, Vector3d.XAxis, Point3d.Origin);
        }

        private static Matrix3d Displacement(this TArchWindow window)
        {
            return Matrix3d.Displacement(new Vector3d(window.BasePointX, window.BasePointY, window.BasePointZ));
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
            window.Rotation = direction.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis);
        }
    }
}
