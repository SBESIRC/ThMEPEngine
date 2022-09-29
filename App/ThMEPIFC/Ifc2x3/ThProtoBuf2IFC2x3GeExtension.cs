using Xbim.Ifc;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThProtoBuf2IFC2x3GeExtension
    {
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, ThTCHPoint3d point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXYZ(point.X, point.Y, point.Z);
            return pt;
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, ThTCHPoint3d point)
        {
            var placement = model.Instances.New<IfcAxis2Placement3D>();
            placement.Location = model.ToIfcCartesianPoint(point);
            return placement;
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model, 
            ThTCHPoint3d origin, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(origin);
            });
        }
    }
}
