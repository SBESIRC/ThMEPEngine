using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public static class Parallel
    {
        public static bool IsParallelTo(this double angle1, double angle2)
        {
            return Math.Abs(angle1 - angle2) < 0.035 ||
                   Math.Abs(Math.Abs(angle1 - angle2) - Math.PI) < 0.035 ||
                   Math.Abs(Math.Abs(angle1 - angle2) - Math.PI * 2) < 0.035;
        }
    }
}
