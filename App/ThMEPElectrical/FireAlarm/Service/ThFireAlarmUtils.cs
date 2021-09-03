using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.FireAlarm.Service
{
    public static class ThFireAlarmUtils
    {
        //
        public static bool IsPositiveInfinity(this Point3d pt)
        {
            return double.IsPositiveInfinity(pt.X) ||
                double.IsPositiveInfinity(pt.Y) ||
                double.IsPositiveInfinity(pt.Z);
        }
    }
}
