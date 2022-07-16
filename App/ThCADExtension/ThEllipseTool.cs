using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThEllipseTool
    {
        public static Ellipse ToEllipse(Plane plane, EllipticalArc2d ellipse2d)
        {
            // https://www.theswamp.org/index.php?topic=54014.msg586554#msg586554
            var curve3d = new EllipticalArc3d(
                new Point3d(plane, ellipse2d.Center),
                new Vector3d(plane, ellipse2d.MajorAxis),
                new Vector3d(plane, ellipse2d.MinorAxis),
                ellipse2d.MajorRadius, ellipse2d.MinorRadius,
                ellipse2d.StartAngle, ellipse2d.EndAngle);
            return Curve.CreateFromGeCurve(curve3d) as Ellipse;
        }
    }
}
