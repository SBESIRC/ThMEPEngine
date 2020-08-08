using DotNetARX;
using NetTopologySuite.Simplify;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.AutoCAD.Utility.ExtensionTools;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSSimplify
    {
        public static Polyline Simplify(this Polyline pline, double distanceTol)
        {
            var coordinates = VWLineSimplifier.Simplify(pline.Vertices().ToNTSCoordinates(), distanceTol);
            var result = new Polyline()
            {
                Closed = pline.Closed,
            };
            result.CreatePolyline(coordinates.ToAcGePoint3ds());
            return result;
        }
    }
}
