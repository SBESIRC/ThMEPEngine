using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ThMEPHVAC.CAD;

namespace TianHua.Hvac.UI
{
    public class ThHvacUIService
    {
        public static void LimitAirSpeedRange(string scenario, ref double airSpeed, ref double airSpeedMin, ref double airSpeedMax)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    airSpeed = 15;
                    airSpeedMin = 5;
                    airSpeedMax = 20;
                    break;
                case "厨房排油烟":
                case "厨房排油烟补风":
                case "事故排风":
                case "事故补风":
                case "平时送风":
                case "平时排风":
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                case "平时送风兼事故补风":
                case "平时排风兼事故排风":
                    airSpeed = 8;
                    airSpeedMin = 5;
                    airSpeedMax = 10;
                    break;
                default:
                    throw new NotImplementedException("Check scenario!!!");
            }
        }
        public static void PortInit(string scenario, out string downPortName, out string sidePortName)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "厨房排油烟":
                case "平时排风":
                case "消防排烟兼平时排风":
                case "事故排风":
                case "平时排风兼事故排风":
                    downPortName = "下回单层百叶";
                    sidePortName = "侧回单层百叶";
                    break;
                case "消防补风":
                case "消防加压送风":
                case "厨房排油烟补风":
                case "平时送风":
                case "消防补风兼平时送风":
                case "事故补风":
                case "平时送风兼事故补风":
                    downPortName = "下送单层百叶";
                    sidePortName = "侧送单层百叶";
                    break;
                default:
                    throw new NotImplementedException("Check scenario!!!");
            }
        }
        public static double CalcAirSpeed(double airVolume, double ductWidth, double ductHeight)
        {
            return airVolume / 3600 / (ductWidth * ductHeight / 1000000);
        }
        public static void LimitAirSpeed(double ceiling,
                                           double floor,
                                           out bool isHigh,
                                           ref double airSpeed)
        {
            isHigh = false;
            if (Math.Abs(airSpeed) < 1e-3)
                return;
            if (airSpeed > ceiling)
            {
                isHigh = true;
                airSpeed = ceiling;
            }
            if (airSpeed < floor)
                airSpeed = floor;
        }
        public static void LimitAirVolume(out bool isHigh, ref double airVolume)
        {
            isHigh = false;
            if (Math.Abs(airVolume) < 1e-3)
                return;
            double airVolumeFloor = 1500;
            double airVolumeCeiling = 60000;
            if (airVolume > airVolumeCeiling)
            {
                isHigh = true;
                airVolume = airVolumeCeiling;
            }
            if (airVolume < airVolumeFloor)
                airVolume = airVolumeFloor;
        }
        public static bool IsFloat2Decimal(string text)
        {
            string reg = "^[0-9]*[.]?[0-9]{0,2}$";
            return Regex.Match(text, reg).Success;
        }
        public static bool IsIntegerStr(string text)
        {
            string reg = "^[0-9]*$";
            return Regex.Match(text, reg).Success;
        }
        public static bool IsDoubleVolume(string strVolume)
        {
            return strVolume.Contains("/");
        }
    }
}
