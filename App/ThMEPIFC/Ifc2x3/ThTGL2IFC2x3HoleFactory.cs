using ThMEPTCH.Model;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.ProductExtension;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
    {
        public static IfcOpeningElement CreateHole(IfcStore model, ThTCHWall wall, ThTCHDoor door, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Hole"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "Door Hole";

                //create representation
                var profile = GetProfile(model, wall, door);
                var solid = model.ToIfcExtrudedAreaSolid(profile, door.ExtrudedDirection, door.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(door, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform.CoordinateSystem3d);

                txn.Commit();
                return ret;
            }
        }

        public static IfcOpeningElement CreateHole(IfcStore model, ThTCHWall wall, ThTCHWindow window, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Hole"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "Window Hole";

                //create representation
                var profile = GetProfile(model, wall, window);
                var solid = model.ToIfcExtrudedAreaSolid(profile, window.ExtrudedDirection, window.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(window, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform.CoordinateSystem3d);

                txn.Commit();
                return ret;
            }
        }

        public static IfcOpeningElement CreateHole(IfcStore model, ThTCHOpening hole, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Hole"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "Generic Hole";

                //create representation
                var profile = GetProfile(model, hole);
                var solid = model.ToIfcExtrudedAreaSolid(profile, hole.ExtrudedDirection, hole.Height);

                //object placement
                var transform = GetTransfrom(hole, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform.CoordinateSystem3d);

                txn.Commit();
                return ret;
            }
        }

        private static Matrix3d GetTransfrom(ThTCHOpening hole, Point3d floor_origin)
        {
            var offset = floor_origin.GetAsVector();
            return ThMatrix3dExtension.MultipleTransformFroms(1.0, hole.XVector, hole.Origin + offset);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHOpening hole)
        {
            return model.ToIfcRectangleProfileDef(hole.Length, hole.Width);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWall wall, ThTCHWindow window)
        {
            // 为了确保开洞完全贯通墙，
            // 洞的宽度需要在墙的厚度的基础上增加一定的延伸
            // TODO：
            //  1）暂时只支持直墙和弧墙
            //  2）弧墙的延伸量需要考虑弧的半径以及墙的厚度，这里暂时给一个经验值
            return model.ToIfcRectangleProfileDef(window.Length, wall.Width + 120);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWall wall, ThTCHDoor door)
        {
            // 为了确保开洞完全贯通墙，
            // 洞的宽度需要在墙的厚度的基础上增加一定的延伸
            // TODO：
            //  1）暂时只支持直墙和弧墙
            //  2）弧墙的延伸量需要考虑弧的半径以及墙的厚度，这里暂时给一个经验值
            return model.ToIfcRectangleProfileDef(door.Length, wall.Width + 120);
        }
    }
}
