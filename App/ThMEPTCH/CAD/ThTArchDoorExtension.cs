using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using AcRectangle = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPTCH.CAD
{
    public static class ThTArchDoorExtension
    {
        public static AcRectangle Profile(this TArchDoor door)
        {
            // 根据参数计算其轮廓，暂时仅支持普通矩形门
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
            var move = door.BasePoint.GetAsVector();
            profile.TransformBy(ThMatrix3dExtension.MultipleTransformFroms(1.0, door.Rotation, move));
            return profile;
        }

        public static void SyncWithProfile(this TArchDoor door, AcRectangle profile)
        {
            // 根据轮廓同步其参数，暂时仅支持普通矩形门
            var leftSide = profile.GetLineSegmentAt(0);
            var rightSide = profile.GetLineSegmentAt(2);
            var direction = leftSide.MidPoint.GetVectorTo(rightSide.MidPoint);
            door.BasePoint = leftSide.MidPoint + direction / 2.0;
            door.Rotation = Vector3d.XAxis.GetAngleTo(direction.GetNormal(), Vector3d.ZAxis);
        }
    }
}
