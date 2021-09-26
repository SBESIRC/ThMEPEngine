using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public static class ThSprinklerUtils
    {
        public static void MoveToOrigin(this ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3WindowVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3RailingVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3CurtainWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorMarkVisitor.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
                transformer.Transform(o.Geometry);
            });
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
        }

        public static Point3d VerticalPoint(Point3d first, Point3d second, double distance)
        {
            var verticalVerctor = second - first;
            return CenterPoint(first, second) + verticalVerctor / verticalVerctor.Length * distance;
        }

        public static Point3d CenterPoint(Point3d first, Point3d second)
        {
            return new Point3d((first.X + second.X) / 2, (first.Y + second.Y) / 2, 0);
        }
    }
}
