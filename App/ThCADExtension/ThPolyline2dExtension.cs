using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThCADExtension
{
    public static class ThPolyline2dExtension
    {
        public static Polyline ToPolyline(this Polyline2d polyline2d)
        {
            var curve3d = polyline2d.ToCurve3d();
            return Curve.CreateFromGeCurve(curve3d) as Polyline;
        }
    }
}
