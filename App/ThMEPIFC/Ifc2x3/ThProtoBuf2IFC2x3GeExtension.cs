using Xbim.Ifc;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using ThMEPIFC.Geometry;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.IO.Memory;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThProtoBuf2IFC2x3GeExtension
    {
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, ThTCHPoint3d point)
        {
            return model.Instances.New<IfcCartesianPoint>(c =>
            {
                c.SetXYZ(point.X, point.Y, point.Z);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,  
            IfcObjectPlacement relative_to = null)
        {
            var cs = ThXbimCoordinateSystem3D.Identity;
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(cs);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,
            ThTCHMatrix3d matrix, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(matrix);
            });
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, XbimVector3D vector)
        {
            return model.Instances.New<IfcDirection>(d =>
            {
                d.SetXYZ(vector.X, vector.Y, vector.Z);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,
            ThTCHPoint3d origin, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(origin.ToXbimPoint3D());
            });
        }

        public static IfcExtrudedAreaSolid ToIfcExtrudedAreaSolid(this IfcStore model, IfcProfileDef profile, XbimVector3D direction, double depth)
        {
            return model.Instances.New<IfcExtrudedAreaSolid>(s =>
            {
                s.Depth = depth;
                s.SweptArea = profile;
                s.ExtrudedDirection = model.ToIfcDirection(direction);
                s.Position = model.ToIfcAxis2Placement3D(XbimPoint3D.Zero);
            });
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, XbimPoint3D point)
        {
            return model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Location = ToIfcCartesianPoint(model, point);
            });
        }

        private static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, ThTCHMatrix3d m)
        {
            return model.ToIfcAxis2Placement3D(new ThXbimCoordinateSystem3D(m));
        }

        private static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, ThXbimCoordinateSystem3D cs)
        {
            return model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Axis = model.ToIfcDirection(cs.CS.ZAxis.ToXbimVector3D());
                p.RefDirection = model.ToIfcDirection(cs.CS.XAxis.ToXbimVector3D());
                p.Location = model.ToIfcCartesianPoint(cs.CS.Origin.ToXbimPoint3D());
            });
        }

        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, XbimPoint3D point)
        {
            return model.Instances.New<IfcCartesianPoint>(c =>
            {
                c.SetXYZ(point.X, point.Y, point.Z);
            });
        }
    }
}
