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

        public static string HoleLayerName()
        {
            return ThHvacCommon.WALLHOLE_LAYER;
        }
    }
}
