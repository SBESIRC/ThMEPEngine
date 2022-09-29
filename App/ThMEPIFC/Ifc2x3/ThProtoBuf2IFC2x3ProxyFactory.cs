using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThProtoBuf2IFC2x3Factory
    {
        public static IfcBuildingElementProxy CreateProxy(IfcStore model, ThSUPolygonMesh mesh, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Proxy"))
            {
                var ret = model.Instances.New<IfcBuildingElementProxy>();

                //create representation
                var surface = model.ToIfcFaceBasedSurface(mesh);
                var shape = CreateFaceBasedSurfaceBody(model, surface);
                ret.Representation = CreateProductDefinitionShape(model, shape);

                //object placement
                ret.ObjectPlacement = model.ToIfcLocalPlacement(floor_origin);

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
    }
}
