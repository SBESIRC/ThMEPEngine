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
                    return ThHvacCommon.DUCT_LAYER_EQUP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string FireValveLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case "H-DUAL-FBOX":
                    return ThHvacCommon.FIRE_VALVE_LAYER_DUAL;
                case "H-FIRE-FBOX":
                    return ThHvacCommon.FIRE_VALVE_LAYER_FIRE;
                case "H-EQUP-FBOX":
                    return ThHvacCommon.FIRE_VALVE_LAYER_EQUP;
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
                    return "280度排烟阀（带输出信号）FDHS";
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
    }
}
