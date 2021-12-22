using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.GirderConnect.Data
{
    public static class BeamConfig
    {
        public static string MainBeamLayerName = "TH_AIZL_S_BEAM";
        public static string MainBeamTextLayerName = "TH_AIZL_S_BEAM_TEXT";
        public static string HouseBoundLayerName = "TH_AI_HOUSEBOUND";
        public static string WallBoundLayerName = "TH_AI_WALLBOUND";

        public static string SecondaryBeamLayerName = "TH_AICL_S_BEAM";
        public static string SecondaryBeamTextLayerName = "TH_AICL_S_BEAM_TEXT";

        public static string BeamTextStyleName = "TH-STYLE3";
        public static string ErrorLayerName = "TH_AICL_未封闭区格_error";
    }
}
