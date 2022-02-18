using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSPolylineService
    {
        //public static Polyline CreateSquare(this Point3d center, double length)
        //{
        //    var pline = new Polyline
        //    {
        //        Closed = true
        //    };
        //    var pts = new Point3dCollection
        //    {
        //        center + length * Vector3d.XAxis + length * Vector3d.YAxis,
        //        center - length * Vector3d.XAxis + length * Vector3d.YAxis,
        //        center - length * Vector3d.XAxis - length * Vector3d.YAxis,
        //        center + length * Vector3d.XAxis - length * Vector3d.YAxis,
        //    };
        //    pline.CreatePolyline(pts);
        //    return pline;
        //}
    }
}
