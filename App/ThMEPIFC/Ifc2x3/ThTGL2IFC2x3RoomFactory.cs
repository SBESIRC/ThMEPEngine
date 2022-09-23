using ThCADExtension;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ProductExtension;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
    {
        public static IfcSpace CreateRoom(IfcStore model, ThTCHSpace space, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Room"))
            {
                var ret = model.Instances.New<IfcSpace>();
                ret.Name = "Room";
                ret.Description = space.Name;

                //create representation
                var profile = model.ToIfcArbitraryProfileDefWithVoids(space.Outline);
                var solid = model.ToIfcExtrudedAreaSolid(profile, space.ExtrudedDirection, space.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(floor_origin);
                txn.Commit();
                return ret;
            }

        }
    }
}
