using Autodesk.AutoCAD.Geometry;
using Xbim.IO;
using Xbim.Ifc;
using Xbim.Common.Step21;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.RepresentationResource;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
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

        public static IfcShapeRepresentation CreateSweptSolidBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "SweptSolid";
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

        private static IfcGeometricRepresentationContext GetGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.FirstOrDefault<IfcGeometricRepresentationContext>();
        }

        private static IfcStore CreateModel()
        {
            return IfcStore.Create(IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
        }

        private static IfcGeometricRepresentationContext CreateGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.New<IfcGeometricRepresentationContext>(c =>
            {
                c.Precision = ThTGL2IFCCommon.PRECISION;
                c.CoordinateSpaceDimension = new IfcDimensionCount(3);
                c.WorldCoordinateSystem = model.ToIfcAxis2Placement3D(Point3d.Origin);
            });
        }
    }
}
