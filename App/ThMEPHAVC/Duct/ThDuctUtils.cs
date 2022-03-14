using System;
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
                case ThHvacCommon.MODEL_LAYER_EQUP:
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
                case ThHvacCommon.MODEL_LAYER_EQUP:
                    return ThHvacCommon.CENTERLINE_LAYER_VENT;
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
                case ThHvacCommon.MODEL_LAYER_EQUP:
                    return ThHvacCommon.VALVE_LAYER_EQUP;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string SilencerLayerName(string modelLayer)
        {
            return FlangeLayerName(modelLayer);
        }

        public static string HoseLayerName(string modelLayer)
        {
            return FlangeLayerName(modelLayer);
        }

        public static string FlangeLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case ThHvacCommon.MODEL_LAYER_DUAL:
                    return ThHvacCommon.FLANGE_LAYER_DUAL;
                case ThHvacCommon.MODEL_LAYER_FIRE:
                    return ThHvacCommon.FLANGE_LAYER_FIRE;
                case ThHvacCommon.MODEL_LAYER_EQUP:
                    return ThHvacCommon.FLANGE_LAYER_VENT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string DuctTextLayerName(string modelLayer)
        {
            switch (modelLayer)
            {
                case ThHvacCommon.MODEL_LAYER_DUAL:
                    return ThHvacCommon.DUCT_TEXT_LAYER_DUAL;
                case ThHvacCommon.MODEL_LAYER_FIRE:
                    return ThHvacCommon.DUCT_TEXT_LAYER_FIRE;
                case ThHvacCommon.MODEL_LAYER_EQUP:
                    return ThHvacCommon.DUCT_TEXT_LAYER_VENT;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string FireValveBlockName()
        {
            return ThHvacCommon.FILEVALVE_BLOCK_NAME;
        }

        public static string SilencerBlockName()
        {
            return ThHvacCommon.SILENCER_BLOCK_NAME;
        }

        public static string FireValveModelName(string scenario)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "消防排烟兼平时排风":
                    return ThHvacCommon.BLOCK_VALVE_VISIBILITY_FIRE_280;
                case "厨房排油烟":
                    return ThHvacCommon.BLOCK_VALVE_VISIBILITY_FIRE_150;
                default:
                    return ThHvacCommon.BLOCK_VALVE_VISIBILITY_FIRE_70;
            }
        }

        public static string CheckValveModelName()
        {
            return ThHvacCommon.BLOCK_VALVE_VISIBILITY_CHECK;
        }

        public static string SilencerModelName()
        {
            return ThHvacCommon.BLOCK_VALVE_VISIBILITY_SILENCER_100;
        }

        public static string ElectricValveModelName()
        {
            return ThHvacCommon.BLOCK_VALVE_VISIBILITY_ELECTRIC;
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
            return ThHvacCommon.AI_HOLE == name;
        }
    }
}
