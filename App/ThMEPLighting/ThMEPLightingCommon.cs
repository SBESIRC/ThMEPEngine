using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting
{
    public class ThMEPLightingCommon
    {
        //应急照明
        public static readonly string EmgLightLayerName = "E-LITE-LITE";    //消防应急灯图层
        public static readonly short EmgLightLayerColor = 3;             //消防应急灯图层颜色
        public static readonly string EmgLightBlockName = "E-BFEL810";      //消防应急灯图块名

        //车道线
        public const string LANELINE_LAYER_NAME = "E-LANE-CENTER";

        //疏散指示灯
        public const string FEI_EXIT_NAME100 = "E-BFEL100";      //疏散出入口块名100
        public const string FEI_EXIT_NAME101 = "E-BFEL101";      //疏散出入口块名101
        public const string FEI_EXIT_NAME102 = "E-BFEL102";      //疏散出入口块名102
        public const string FEI_EXIT_NAME103 = "E-BFEL103";      //疏散出入口块名103
        public const string FEI_EXIT_NAME140 = "E-BFEL140";      //疏散出入口块名140
        public const string FEI_EXIT_NAME141 = "E-BFEL141";      //疏散出入口块名141
    }
}
