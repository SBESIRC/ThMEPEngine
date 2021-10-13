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
        #region  固定布点。显示器监视器电话
        public static string BlkName_Display_Fire = "E-BFAS030";
        public static string BlkName_Display_Floor = "E-BFAS031";
        public static string BlkName_Monitor = "E-BEFPS110";
        public static string BlkName_FireTel = "E-BFAS220";
        #endregion

        #region 烟温感
        public static string BlkName_Smoke = "E-BFAS110";
        public static string BlkName_Heat = "E-BFAS120";
        public static string BlkName_Smoke_ExplosionProf = "E-BFAS110-3";
        public static string BlkName_Heat_ExplosionProf = "E-BFAS120-3";
        #endregion

        #region 可燃气体
        public static string BlkName_Gas = "E-BCGS210";//--------------------------------------------------------------
        public static string BlkName_Gas_ExplosionProf = "E-BCGS210-2";//--------------------------------------------------------------
        #endregion

        #region 灯
        public static string BlkName_CircleCeiling = "E-BL302";
        public static string BlkName_DomeCeiling = "E-BL302-2";
        public static string BlkName_InductionCeiling = "E-BL302-3";
        public static string BlkName_Downlight = "E-BL201";
        public static string BlkName_EmergencyLight = "E-BFEL800";
        #endregion

        public static List<string> BlkNameList = new List<string>() {
                                                                      BlkName_Display_Fire,
                                                                      BlkName_Display_Floor,
                                                                      BlkName_Monitor,
                                                                      BlkName_FireTel,
                                                                      BlkName_Smoke,
                                                                      BlkName_Smoke_ExplosionProf,
                                                                      BlkName_Heat,
                                                                      BlkName_Heat_ExplosionProf,
                                                                      BlkName_Gas,
                                                                      BlkName_Gas_ExplosionProf,
                                                                      BlkName_CircleCeiling,
                                                                      BlkName_DomeCeiling,
                                                                      BlkName_InductionCeiling,
                                                                      BlkName_Downlight,
                                                                      BlkName_EmergencyLight
                                                                    };

        public static Dictionary<string, (double, double)> blk_size = new Dictionary<string, (double, double)>()
                                                                {
                                                                    {BlkName_Display_Fire,(5,3)},
                                                                    {BlkName_Display_Floor, (5,3)},
                                                                    {BlkName_Monitor, (5,3)},
                                                                    {BlkName_FireTel, (3, 3)},
                                                                    {BlkName_Smoke,(3, 3)},
                                                                    {BlkName_Heat, (3, 3) },
                                                                    {BlkName_Smoke_ExplosionProf,(3, 3)},
                                                                    {BlkName_Heat_ExplosionProf, (3, 3) },
                                                                    {BlkName_Gas, (3, 3) },
                                                                    {BlkName_Gas_ExplosionProf, (3, 3) },
                                                                    {BlkName_CircleCeiling,(3,3)},
                                                                    {BlkName_DomeCeiling, (3,3)},
                                                                    {BlkName_InductionCeiling, (3,3)},
                                                                    {BlkName_Downlight, (3,3)},
                                                                    {BlkName_EmergencyLight,(2.5,2.5)},
                                                                };

        public static Dictionary<string, string> blk_layer = new Dictionary<string, string>()
                                                                {
                                                                    {BlkName_Display_Fire,"E-FAS-EQPM"},
                                                                    {BlkName_Display_Floor, "E-FAS-EQPM"},
                                                                    {BlkName_Monitor, "E-EFPS-DEVC"},
                                                                    {BlkName_FireTel, "E-FAS-DEVC"},
                                                                    {BlkName_Smoke,"E-FAS-DEVC"},
                                                                    {BlkName_Heat, "E-FAS-DEVC" },
                                                                    {BlkName_Smoke_ExplosionProf,"E-FAS-DEVC"},
                                                                    {BlkName_Heat_ExplosionProf, "E-FAS-DEVC" },
                                                                    {BlkName_Gas,"E-FAS-DEVC"},
                                                                    {BlkName_Gas_ExplosionProf, "E-FAS-DEVC" },
                                                                    {BlkName_CircleCeiling,"E-LITE-LITE"},
                                                                    {BlkName_DomeCeiling, "E-LITE-LITE"},
                                                                    {BlkName_InductionCeiling, "E-LITE-LITE"},
                                                                    {BlkName_Downlight, "E-LITE-LITE"},
                                                                    {BlkName_EmergencyLight,"E-LITE-LITE"},
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
