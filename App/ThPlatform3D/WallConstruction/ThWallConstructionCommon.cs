using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThPlatform3D.WallConstruction
{
    public class ThWallConstructionCommon
    {
        public static string Layer_Wall = "AE-WALL";
        public static List<string> Layer_Door = new List<string>() { "AE-DOOR-INSD", "AE-WIND" };
        public static string Layer_Moldings = "AE-FNSH";
        public static List<string> Layer_Axis = new List<string> { "AD-AXIS-AXIS", "XD-AXIS-AXIS" };
        public static List<string> Layer_FloorLevel = new List<string> { "AD-LEVL-HIGH", "AD-ARCH-AXIS" };
        public static string Layer_FloorNum = "AD-LEVL-HIGH";
        public static string Layer_BreakLine = "AD-SIGN";


    }
}
