using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public static class Parallel
    {
        public static bool IsParallelTo(this double angle1, double angle2, double tor = 0.035)
        {
            var min = Math.Min(angle1, angle2);
            var max = Math.Max(angle1, angle2);
            while(min <= max + tor)
            {
                if(Math.Abs(min - max) < tor)
                {
                    return true;
                }
                min += Math.PI;
            }
            return false;
        }
    }
}
