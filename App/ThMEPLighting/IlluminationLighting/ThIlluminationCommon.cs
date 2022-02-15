using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThMEPElectrical.AFAS;
using ThMEPLighting.ViewModel;

namespace ThMEPLighting.IlluminationLighting
{
    public class ThIlluminationCommon
    {
        public static Dictionary<LightTypeEnum, string> lightTypeDict = new Dictionary<LightTypeEnum, string>()
                                                                {
                                                                    {LightTypeEnum.circleCeiling ,ThFaCommon. BlkName_CircleCeiling},
                                                                    {LightTypeEnum.domeCeiling  ,ThFaCommon.BlkName_DomeCeiling },
                                                                    {LightTypeEnum.inductionCeiling ,ThFaCommon.BlkName_InductionCeiling},
                                                                    {LightTypeEnum.downlight ,ThFaCommon.BlkName_Downlight},
                                                                    //{LightTypeEnum.emergencyLight ,ThFaCommon.BlkName_EmergencyLight},
                                                                 };
        public enum LayoutType
        {
            //疏散照明,正常照明
            evacuation = 0,
            normal = 1,
            normalEvac = 2,
            stair = 3,
            noName = 4,
        }

     
        public static string NormalTag = "正常照明";
        public static string EvacuationTag = "疏散照明";
        public static string Layer_Blind = "AI-照明盲区";
        public static int Color_Blind = 1;
        public enum LightTypeEnum
        {
            /// <summary>
            /// 圆形吸顶灯
            /// </summary>
            circleCeiling,
            /// <summary>
            /// 半球吸顶灯
            /// </summary>
            domeCeiling,
            /// <summary>
            /// 感应吸顶灯
            /// </summary>
            inductionCeiling,
            /// <summary>
            /// 筒灯
            /// </summary>
            downlight,
        }


    }
}
