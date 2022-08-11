using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using AcRectangle = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPTCH.CAD
{
    public static class ThTArchDoorExtension
    {
        public static void TransformBy(this TArchDoor door, Matrix3d matrix)
        {
            var profile = door.Profile();
            profile.TransformBy(matrix);
            door.SyncWithProfile(profile);
        }

        private static AcRectangle Profile(this TArchDoor door)
        {
            var profile = new AcRectangle()
            {
                Closed = true,
            };
            var vertices = new Point2dCollection()
            {
                new Point2d(-door.Width/2.0, -door.Thickness/2.0),
                new Point2d(-door.Width/2.0, door.Thickness/2.0),
                new Point2d(door.Width/2.0, door.Thickness/2.0),
                new Point2d(door.Width/2.0, -door.Thickness/2.0)
            };
            profile.CreatePolyline(vertices);
            var scale = Matrix3d.Scaling(1.0, Point3d.Origin);
            var rotation = Matrix3d.Rotation(door.Rotation, Vector3d.XAxis, Point3d.Origin);
            var displacement = Matrix3d.Displacement(new Vector3d(door.BasePointX, door.BasePointY, door.BasePointZ));
            profile.TransformBy(scale.PreMultiplyBy(rotation).PreMultiplyBy(displacement));
            return profile;
        }

        private static void SyncWithProfile(this TArchDoor door, AcRectangle profile)
        {
            var leftSide = profile.GetLineSegmentAt(0);
            var rightSide = profile.GetLineSegmentAt(2);
            var direction = leftSide.MidPoint.GetVectorTo(rightSide.MidPoint);
            var basePoint = leftSide.MidPoint + direction / 2.0;
            door.BasePointX = basePoint.X;
            door.BasePointY = basePoint.Y;
            door.BasePointZ = basePoint.Z;
            door.Rotation = direction.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis);
        }
    }
}
