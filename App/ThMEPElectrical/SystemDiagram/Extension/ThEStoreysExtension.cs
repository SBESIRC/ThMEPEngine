using System;
using System.Linq;

namespace ThMEPElectrical.SystemDiagram.Extension
{
    public static class ThEStoreysExtension
    {
        public static int GetFloorNumber(this string Floorstring)
        {
            int FloorNum = int.Parse(System.Text.RegularExpressions.Regex.Replace(Floorstring, @"[^0-9]+", "")) * 2;
            if (Floorstring.Contains('B'))
                FloorNum *= -1;
            if (Floorstring.Contains('M'))
                FloorNum += 1;
            return FloorNum;
        }
    }
}
