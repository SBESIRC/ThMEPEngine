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
        public static IfcColumn CreateColumn(IfcStore model, ThTCHColumn column, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Column"))
            {
                var ret = model.Instances.New<IfcColumn>();

                //create representation
                var profile = GetProfile(model, column);
                var solid = model.ToIfcExtrudedAreaSolid(profile, column.ExtrudedDirection, column.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(column, floor_origin);
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

        private static Matrix3d GetTransfrom(ThTCHColumn column, Point3d floor_origin)
        {
            var offset = floor_origin.GetAsVector();
            if (column.Outline is Polyline pline)
            {
                offset += column.ExtrudedDirection.MultiplyBy(pline.Elevation);
            }
            return ThMatrix3dExtension.MultipleTransformFroms(1.0, column.XVector, column.Origin + offset);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHColumn column)
        {
            if (column.Outline is Polyline pline)
            {
                return model.ToIfcArbitraryClosedProfileDef(pline);
            }
            else
            {
                return model.ToIfcRectangleProfileDef(column.Length, column.Width);
            }
        }
    }
}
