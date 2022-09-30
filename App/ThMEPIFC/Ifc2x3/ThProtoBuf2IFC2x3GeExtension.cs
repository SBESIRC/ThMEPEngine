using Xbim.Ifc;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using ThMEPIFC.Geometry;
using Xbim.Common.Geometry;

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

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,
            ThTCHMatrix3d matrix, IfcObjectPlacement relative_to = null)
        {
            var transform = matrix.ToXbimMatrix3D();
            var cs = new ThXbimCoordinateSystem3D(transform);
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(cs);
            });
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, XbimVector3D vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXYZ(vector.X, vector.Y, vector.Z);
            return direction;
        }

        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, XbimPoint3D point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXYZ(point.X, point.Y, point.Z);
            return pt;
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, ThXbimCoordinateSystem3D cs)
        {
            return model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Axis = model.ToIfcDirection(cs.CS.ZAxis.ToXbimVector3D());
                p.RefDirection = model.ToIfcDirection(cs.CS.XAxis.ToXbimVector3D());
                p.Location = model.ToIfcCartesianPoint(cs.CS.Origin.ToXbimPoint3D());
            });
        }
    }
}
