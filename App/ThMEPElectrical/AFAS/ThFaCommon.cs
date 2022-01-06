using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AFAS
{
    public class ThFaCommon
    {
        #region  固定布点。显示器监视器电话
        public static string BlkName_Display_District = "E-BFAS030";
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

        #region 手报
        public static string BlkName_ManualAlarm = "E-BFAS212";
        public static string BlkName_SoundLightAlarm = "E-BFAS330";
        #endregion

        #region 广播
        public static string BlkName_Broadcast_Ceiling = "E-BFAS410-2";
        public static string BlkName_Broadcast_Wall = "E-BFAS410-4";
        #endregion

        #region 其他
        public static string stairName = "楼梯间";
        #endregion

        public static List<string> BlkNameList = new List<string>() {
                                                                      BlkName_Display_District,
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
                                                                      BlkName_EmergencyLight,
                                                                      BlkName_ManualAlarm,
                                                                      BlkName_SoundLightAlarm,
                                                                      BlkName_Broadcast_Ceiling,
                                                                      BlkName_Broadcast_Wall,
                                                                    };

        public static Dictionary<string, (double, double)> blk_size = new Dictionary<string, (double, double)>()
                                                                {
                                                                    {BlkName_Display_District,(5,3)},
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
                                                                    {BlkName_ManualAlarm,(3,3) },
                                                                    {BlkName_SoundLightAlarm,(5,3) },
                                                                    {BlkName_Broadcast_Ceiling,(5,3) },
                                                                    {BlkName_Broadcast_Wall,(5,3) },
                                                                };

        public static Dictionary<string, string> Blk_Layer = new Dictionary<string, string>()
                                                                {
                                                                    {BlkName_Display_District,"E-FAS-EQPM"},
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
                                                                    { BlkName_ManualAlarm,"E-FAS-DEVC" },
                                                                    { BlkName_SoundLightAlarm,"E-FAS-DEVC" },
                                                                    { BlkName_Broadcast_Ceiling,"E-FAS-DEVC" },
                                                                    { BlkName_Broadcast_Wall,"E-FAS-DEVC" },
                                                                 };

        //烟温感（0）广播（1）楼层显示器（2）消防电话（3）可燃气体探测（4）手动报警按钮（5）防火门监控（6）
        //普通照明（7） 应急照明（8）
        public static Dictionary<int, List<string>> LayoutBlkList = new Dictionary<int, List<string>>()
        {
            {(int)LayoutItemType.Smoke , new List<string>() { BlkName_Smoke, BlkName_Heat, BlkName_Smoke_ExplosionProf, BlkName_Heat_ExplosionProf } },
            {(int)LayoutItemType.Broadcast, new List<string>() { BlkName_Broadcast_Ceiling, BlkName_Broadcast_Wall } },
            {(int)LayoutItemType.Display, new List<string>() { BlkName_Display_District, BlkName_Display_Floor } },
            {(int)LayoutItemType.Tel, new List<string>() { BlkName_FireTel } },
            {(int)LayoutItemType.Gas, new List<string>() { BlkName_Gas, BlkName_Gas_ExplosionProf } },
            {(int)LayoutItemType.ManualAlarm, new List<string>() { BlkName_ManualAlarm, BlkName_SoundLightAlarm } },
            {(int)LayoutItemType.Monitor, new List<string>() { BlkName_Monitor } },
            {(int)LayoutItemType.NormalLighting,new List<string>(){BlkName_CircleCeiling,BlkName_DomeCeiling,BlkName_InductionCeiling,BlkName_Downlight, } },
            {(int)LayoutItemType.EmergencyLighting,new List<string>(){ BlkName_EmergencyLight, } }

        };

        public enum LayoutItemType
        {
            Smoke = 0,
            Broadcast = 1,
            Display = 2,
            Tel = 3,
            Gas = 4,
            ManualAlarm = 5,
            Monitor = 6,
            NormalLighting = 7,
            EmergencyLighting = 8,
        }

    }
}
