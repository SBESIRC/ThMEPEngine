using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class Check
    {
        public static bool IsCurrentFloor(this string floor)
        {
            var f =  floor.Trim();
            return f.StartsWith("X") || f.StartsWith("B") || f.StartsWith("D")||f.Count()==0;
        }
    }
}
