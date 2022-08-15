using ThMEPTCH.Model;
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

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWall wall, ThTCHWindow window)
        {
            return model.ToIfcRectangleProfileDef(window.Length, wall.Width);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWall wall, ThTCHDoor door)
        {
            return model.ToIfcRectangleProfileDef(door.Length, wall.Width);
        }
    }
}
