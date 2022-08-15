using ThCADExtension;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.SharedBldgElements;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
    {

        public static IfcWall CreateWall(IfcStore model, ThTCHWall wall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var ret = model.Instances.New<IfcWall>();
                ret.Name = "A Standard rectangular wall";

                //create representation
                var profile = GetProfile(model, wall);
                var solid = model.ToIfcExtrudedAreaSolid(profile, wall.ExtrudedDirection, wall.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(wall, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform.CoordinateSystem3d);

                // add properties
                //model.Instances.New<IfcRelDefinesByProperties>(rel =>
                //{
                //    rel.Name = "THifc properties";
                //    rel.RelatedObjects.Add(ret);
                //    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                //    {
                //        pset.Name = "Basic set of THifc properties";
                //        foreach (var item in wall.Properties)
                //        {
                //            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                //            {
                //                p.Name = item.Key;
                //                p.NominalValue = new IfcText(item.Value.ToString());
                //            }));
                //        }
                //    });
                //});

                txn.Commit();
                return ret;
            }
        }

        private static Matrix3d GetTransfrom(ThTCHWall wall, Point3d floor_origin)
        {
            var offset = floor_origin.GetAsVector();
            if (wall.Outline is Polyline pline)
            {
                offset += wall.ExtrudedDirection.MultiplyBy(pline.Elevation);
            }
            return ThMatrix3dExtension.MultipleTransformFroms(1.0, wall.XVector, wall.Origin + offset);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWall wall)
        {
            if (wall.Outline is Polyline pline)
            {
                return model.ToIfcArbitraryClosedProfileDef(pline);
            }
            else
            {
                return model.ToIfcRectangleProfileDef(wall.Length, wall.Width);
            }
        }
    }
}
