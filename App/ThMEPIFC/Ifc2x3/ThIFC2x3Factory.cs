using Xbim.Ifc;
using Xbim.Common.Step21;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.RepresentationResource;

namespace ThMEPIFC.Ifc2x3
{
    public class ThIFC2x3Factory
    {
        public static IfcShapeRepresentation CreateBrepBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "Brep";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcShapeRepresentation CreateFaceBasedSurfaceBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "SurfaceModel";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcProductDefinitionShape CreateProductDefinitionShape(IfcStore model, IfcShapeRepresentation representation)
        {
            return model.Instances.New<IfcProductDefinitionShape>(s =>
            {
                s.Representations.Add(representation);
            });
        }

        public static IfcGeometricRepresentationContext GetGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.FirstOrDefault<IfcGeometricRepresentationContext>();
        }

        public static IfcStore CreateMemoryModel()
        {
            return IfcStore.Create(IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
        }

        public static IfcCompositeCurve CreateIfcCompositeCurve(IfcStore model)
        {
            return model.Instances.New<IfcCompositeCurve>();
        }

        public static IfcCompositeCurveSegment CreateIfcCompositeCurveSegment(IfcStore model)
        {
            return model.Instances.New<IfcCompositeCurveSegment>(s =>
            {
                s.SameSense = true;
            });
        }
    }
}
