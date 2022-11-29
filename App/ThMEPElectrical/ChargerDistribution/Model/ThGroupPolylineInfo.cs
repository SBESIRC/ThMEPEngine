using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.ChargerDistribution.Model
{
    public class ThGroupPolylineInfo
    {
        public Polyline Polyline { get; set; }

        public Point3d Centroid { get; set; }

        public ThGroupPolylineInfo(Polyline polyline)
        {
            Polyline = polyline;
            Centroid = polyline.GetCenter();
        }
    }
}
