using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.BeamInfo.Utils
{
    public class ThBeamSpanZoneCalculator
    {
        public static AcPolygon CalculateProtectedRegion(AcPolygon polygon)
        {
            return Smooth(polygon);
        }

        public static AcPolygon CalculateDistributableRegion(AcPolygon polygon)
        {
            return Smooth(polygon);
        }

        private static AcPolygon Smooth(AcPolygon polygon)
        {
            return null;
        }
    }
}
