using Xbim.Ifc;
using Xbim.Ifc2x3.GeometryResource;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThTGL2IFC2x3GeExtension
    {
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, Point3d point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXYZ(point.X, point.Y, point.Z);
            return pt;
        }
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, Point2d point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXY(point.X, point.Y);
            return pt;
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, Point3d point)
        {
            var placement = model.Instances.New<IfcAxis2Placement3D>();
            placement.Location = model.ToIfcCartesianPoint(point);
            return placement;
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this IfcStore model, Point2d point)
        {
            var placement = model.Instances.New<IfcAxis2Placement2D>();
            placement.Location = model.ToIfcCartesianPoint(point);
            return placement;
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this IfcStore model, Point2d point, Vector2d direction)
        {
            return model.Instances.New<IfcAxis2Placement2D>(p =>
            {
                p.Location = model.ToIfcCartesianPoint(point);
                p.RefDirection = model.ToIfcDirection(direction);
            });
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, Vector3d vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXYZ(vector.X, vector.Y, vector.Z);
            return direction;
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, Vector2d vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXY(vector.X, vector.Y);
            return direction;
        }

        public static Point3d ToAcGePoint3d(this IfcStore model, IfcCartesianPoint point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }
    }
}