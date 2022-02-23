using System;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class AngleTools
    {
        public static bool IsParallel(this double ang1, double ang2, double tor = 0.035)
        {
            var minAngle = Math.Min(ang1, ang2);
            var maxAngle = Math.Max(ang1, ang2);
            while (minAngle < maxAngle + tor)
            {
                if (Math.Abs(minAngle - maxAngle) < tor)
                {
                    return true;
                }
                minAngle += Math.PI;
            }
            return false;
        }
    }
}
