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
                case ThHvacCommon.MODEL_LAYER_DUAL:
                    return ThHvacCommon.DUCT_LAYER_DUAL;
                case ThHvacCommon.MODEL_LAYER_FIRE:
                    return ThHvacCommon.DUCT_LAYER_FIRE;
                case ThHvacCommon.MODEL_LAYER_ERUP:
                    return ThHvacCommon.DUCT_LAYER_VENT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string DuctCenterLineLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case ThHvacCommon.MODEL_LAYER_DUAL:
                    return ThHvacCommon.CENTERLINE_LAYER_DUAL;
                case ThHvacCommon.MODEL_LAYER_FIRE:
                    return ThHvacCommon.CENTERLINE_LAYER_FIRE;
                case ThHvacCommon.MODEL_LAYER_ERUP:
                    return ThHvacCommon.FLANGE_LAYER_VENT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string ValveLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case ThHvacCommon.MODEL_LAYER_DUAL:
                    return ThHvacCommon.VALVE_LAYER_DUAL;
                case ThHvacCommon.MODEL_LAYER_FIRE:
                    return ThHvacCommon.VALVE_LAYER_FIRE;
                case ThHvacCommon.MODEL_LAYER_ERUP:
                    return ThHvacCommon.VALVE_LAYER_EQUP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string SilencerLayerName(string modelLayer)
        {
            return ValveLayerName(modelLayer);
        }

        public static string HoseLayerName(string modelLayer)
        {
            return ValveLayerName(modelLayer);
        }

        public static string FlangeLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case ThHvacCommon.MODEL_LAYER_DUAL:
                    return ThHvacCommon.FLANGE_LAYER_DUAL;
                case ThHvacCommon.MODEL_LAYER_FIRE:
                    return ThHvacCommon.FLANGE_LAYER_FIRE;
                case ThHvacCommon.MODEL_LAYER_ERUP:
                    return ThHvacCommon.FLANGE_LAYER_VENT;
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

        public static bool IsHoleModel(string name)
        {
            return ThHvacCommon.WALLHOLE_BLOCK_NAME == name;
        }
    }
}
