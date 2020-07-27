using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSLineExtension
    {
        public static bool CoveredBy(this Line line, Line other)
        {
            return line.ToNTSLineString().CoveredBy(other.ToNTSLineString());
        }

        public static bool CoveredBy(this Line line, Polyline other)
        {
            return line.ToNTSLineString().CoveredBy(other.ToNTSLineString());
        }
    }
}
