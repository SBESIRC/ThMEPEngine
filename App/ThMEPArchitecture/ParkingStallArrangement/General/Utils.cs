using System;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    internal class Utils
    {
        public static int RandInt(int range)
        {
            var guid = Guid.NewGuid();
            var rand = new Random(guid.GetHashCode());
            int i = rand.Next(range);
            return i;
        }

        public static double RandDouble()
        {
            var guid = Guid.NewGuid();
            var rand = new Random(guid.GetHashCode());
            var d = rand.NextDouble();
            return d;
        }
    }
}
