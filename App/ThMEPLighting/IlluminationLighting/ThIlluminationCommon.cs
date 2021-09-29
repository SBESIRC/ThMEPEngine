using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThMEPLighting.Lighting.ViewModels;

namespace ThMEPLighting.IlluminationLighting
{
    public class ThIlluminationCommon
    {
        public static string BlkName_CircleCeiling = "E-BL302";
        public static string BlkName_DomeCeiling = "E-BL302-2";
        public static string BlkName_InductionCeiling = "E-BL302-3";
        public static string BlkName_Downlight = "E-BL201";
        public static string BlkName_EmergencyLight = "E-BFEL800";

        public static List<string> BlkNameListAreaLayout = new List<string>() {
                                                                      BlkName_CircleCeiling,
                                                                      BlkName_DomeCeiling,
                                                                      BlkName_InductionCeiling,
                                                                      BlkName_Downlight,
                                                                      BlkName_EmergencyLight
                                                                    };

        public static Dictionary<string, (double, double)> blk_size = new Dictionary<string, (double, double)>()
                                                                {
                                                                    {BlkName_CircleCeiling,(1,1)},
                                                                    {BlkName_DomeCeiling, (1,1)},
                                                                    {BlkName_InductionCeiling, (1,1)},
                                                                    {BlkName_Downlight, (1,1)},
                                                                    {BlkName_EmergencyLight,(1,1)},
                                                                };

        public static Dictionary<string, string> blk_layer = new Dictionary<string, string>()
                                                                {
                                                                    {BlkName_CircleCeiling,"E-FAS-DEVC"},
                                                                    {BlkName_DomeCeiling, "E-FAS-DEVC"},
                                                                    {BlkName_InductionCeiling, "E-FAS-DEVC"},
                                                                    {BlkName_Downlight, "E-FAS-DEVC"},
                                                                    {BlkName_EmergencyLight,"E-FAS-DEVC"},
                                                                 };

        public static Dictionary<LightTypeEnum, string> lightTypeDict = new Dictionary<LightTypeEnum, string>()
                                                                {
                                                                    {LightTypeEnum.circleCeiling , BlkName_CircleCeiling},
                                                                    {LightTypeEnum.domeCeiling  ,BlkName_DomeCeiling },
                                                                    {LightTypeEnum.inductionCeiling ,BlkName_InductionCeiling},
                                                                    {LightTypeEnum.downlight ,BlkName_Downlight},
                                                                    //{LightTypeEnum.emergencyLight ,BlkName_EmergencyLight},
                                                                 };
        public enum layoutType
        {
            //疏散照明,正常照明
            evacuation = 0,
            normal = 1,
            normalEvac = 2,
            stair = 3,
            noName = 4,
        }

        public static string stairName = "楼梯间";
        public static string normalTag = "正常照明";
        public static string evacuationTag = "疏散照明";

        public static string RoomNameControl = "房间名称处理";


    }
}
