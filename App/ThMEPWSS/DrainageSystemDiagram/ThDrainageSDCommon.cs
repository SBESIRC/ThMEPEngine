using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ThMEPWSS.DrainageSystemDiagram
{
    public static class ThDrainageSDCommon
    {
        public static string LDrainageGivenSD = "卫生间分组";
        public static string ProAreaId = "AreaId";
        public static string ProGroupId = "GroupId";
        public static string ProDirection = "Direction";
        public static string ProId = "Id";
        public static string ProAlignmentVector = "AlignmentVector";
        public static string ProNeighborIds = "NeighborIds";
        public static string GJWaterSupplyPoint = "WaterSupplyPoint";
        public static string GJPipe = "Pipe";

        public static string GJSecPtSuffix = "-sec";

        public static double supplyCoolDalta75 = 75;
        public static double supplyCoolDalta0 = 0;
        public static double supplyCoolDalta200 = -200;
        public static double supplyCoolDalta250 = -250;
        public static double supplyCoolDalta150 = 150;
        public static double supplyCoolDalta120 = 120;
        public static double supplyCoolDalta350 = 200 + 75 + 75;
        public static double supplyCoolDaltaDoubleSinkLeftParameter = 4;
        public static double supplyCoolDaltaDoubleSinkRightParameter = 4.0 / 3.0;
        public static double supplyCoolDalta308 = 308.564 + 75 + 75;

        public static int TolSmallArea = 4 * 1000 * 1000;
        public static int TolToilateToWall = 800;
        public static int LengthSublink = 400;
        public static int MoveDistVirtualPt = 200;
        public static int MoveDistDimOutter = 400;
        public static int MoveDistDimInner = 200;
        public static int DimWidth = 350;
        public static string tagIsland = "island";
        public static string tagSmallRoom = "small";

        public static string Layer_Suffix = "-AI";
        public static string Layer_CoolPipe = "W-WSUP-COOL-PIPE-AI";
        public static string Layer_Stack = "W-WSUP-EQPM";
        public static string Layer_AngleValves = "W-WSUP-EQPM";
        public static string Layer_ShutValve = "W-WSUP-EQPM";
        public static string Layer_Dim = "W-WSUP-DIMS";
        public static string Style_Dim = "TH-DIM50-W";
        public static int Dia_Stack = 25;
        public static string Blk_AngleValves = "给水角阀平面";
        public static string Blk_ShutValves = "截止阀";
        public static double Blk_scale = 1;
        public static int Blk_Size = 150;
       
    }
}
