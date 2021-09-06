using System.Linq;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class Check
    {
        public static bool IsCurrentFloor(this string floor)
        {
            if(floor is null)
            {
                return true;
            }
            var f =  floor?.Trim();
            return f.StartsWith("X") || f.StartsWith("B") || f.StartsWith("D")||f.Count()==0;
        }
    }
}
