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
        public static IfcBeam CreateBeam(IfcStore model, ThTCHBeam beam, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Beam"))
            {
                var ret = model.Instances.New<IfcBeam>();

                //create representation
                var profile = model.ToIfcRectangleProfileDef(beam.Width, beam.Height);
                profile.ProfileName = $"Rec_{beam.Width}*{beam.Height}";
                var solid = model.ToIfcExtrudedAreaSolid(profile, beam.ExtrudedDirection, beam.Length);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = Matrix3d.Rotation(System.Math.PI / 2, Vector3d.ZAxis, Point3d.Origin).PreMultiplyBy(Matrix3d.Rotation(System.Math.PI / 2, Vector3d.YAxis, Point3d.Origin).PreMultiplyBy(Matrix3d.Displacement(new Vector3d(-beam.Length / 2, 0, beam.Height / 2))).PreMultiplyBy(GetTransfrom(beam, floor_origin)));
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

        private static Matrix3d GetTransfrom(ThTCHBeam beam, Point3d floor_origin)
        {
            var offset = floor_origin.GetAsVector();
            return ThMatrix3dExtension.MultipleTransformFroms(1.0, beam.XVector, beam.Origin + offset);
        }
    }
}
