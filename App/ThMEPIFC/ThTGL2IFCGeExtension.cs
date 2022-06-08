using Xbim.Ifc;
using Xbim.Ifc4.GeometryResource;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC
{
    public static class ThTGL2IFCGeExtension
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

        public static Point3d ToAcGePoint3d(this IfcStore model, IfcCartesianPoint point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }
    }
}
