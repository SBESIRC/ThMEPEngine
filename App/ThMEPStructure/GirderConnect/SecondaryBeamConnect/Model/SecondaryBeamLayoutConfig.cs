using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class SecondaryBeamLayoutConfig
    {
        public static string LayerName = "TH_AICL_BEAM";
        public static short ColorIndex = 6;

        public static double Da = 5500;//mm
        public static double Db = 6300;//mm
        public static double Dc = 3300;//mm
        public static double Er = 2.0;

        public static double AngleTolerance = 30; //容差：30°
    }
}
