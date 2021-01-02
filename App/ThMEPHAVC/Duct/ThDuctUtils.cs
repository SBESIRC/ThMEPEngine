using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.Duct
{
    public class ThDuctUtils
    {
        public static string DuctLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.DUCT_LAYER_DUAL;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.DUCT_LAYER_FIRE;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.DUCT_LAYER_VENT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string DuctCenterLineLayerName(string scenario)
        {
            if (scenario.Contains("消防"))
            {
                return ThHvacCommon.FIRE_CENTERLINE_LAYER;
            }
            else if (scenario.Contains("兼用"))
            {
                return ThHvacCommon.DUAL_CENTERLINE_LAYER;
            }
            else
            {
                return ThHvacCommon.VENT_CENTERLINE_LAYER;
            }
        }


        public static string FireValveLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.H_DAPP_DDAMP;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.H_DAPP_FDAMP;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.H_DAPP_EDAMP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string AirValveLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.H_DAPP_DDAMP;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.H_DAPP_FDAMP;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.H_DAPP_EDAMP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string SilencerLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.H_DAPP_DDAMP;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.H_DAPP_FDAMP;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.H_DAPP_EDAMP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string HoseLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.H_DAPP_DDAMP;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.H_DAPP_FDAMP;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.H_DAPP_EDAMP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string FlangeLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.H_DAPP_DAPP;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.H_DAPP_FAPP;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.H_DAPP_AAPP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string CenterlineLayerName(string ductLayer)
        {
            switch (ductLayer)
            {
                case ThHvacCommon.DUCT_LAYER_DUAL:
                    return ThHvacCommon.DUCT_MID_LAYER_DUAL;
                case ThHvacCommon.DUCT_LAYER_FIRE:
                    return ThHvacCommon.DUCT_MID_LAYER_FIRE;
                case ThHvacCommon.DUCT_LAYER_VENT:
                    return ThHvacCommon.DUCT_MID_LAYER_VENT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string FireValveBlockName()
        {
            return ThHvacCommon.FILEVALVE_BLOCK_NAME;
        }

        public static string FireValveModelName(string scenario)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "消防排烟兼平时排风":
                    return "280度排烟阀（带输出信号）FDSH";
                case "厨房排油烟":
                    return "150度防火阀";
                default:
                    return "70度排烟阀（带输出信号）FDS";
            }
        }

        public static string CheckValveModelName()
        {
            return "风管止回阀";
        }

        public static string ElectricValveModelName()
        {
            return "电动多叶调节阀";
        }

        public static string HoleLayerName()
        {
            return ThHvacCommon.WALLHOLE_LAYER;
        }

        public static double GetHoseLength(string scenario)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    return 0;
                default:
                    return 200;
            }

        }
    }
}
