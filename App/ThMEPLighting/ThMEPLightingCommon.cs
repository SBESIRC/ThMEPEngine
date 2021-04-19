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
        public static readonly string EPowerLayerName = "E-POWR-EQPM";

        public static readonly string EmgLightBlockName = "E-BFEL810";      //消防应急灯图块名
        public static readonly string EvacRBlockName = "E-BFEL200";     //疏散指示灯图块
        public static readonly string EvacLRBlockName = "E-BFEL210";     //疏散指示灯图块

        public static readonly string ExitEBlockName = "E-BFEL100";           //紧急出口图块
        public static readonly string ExitSBlockName = "E-BFEL102";           //紧急出口图块

        public static readonly string ExitECeilingBlockName = "E-BFEL101";//紧急出口吊装
        public static readonly string ExitSCeilingBlockName = "E-BFEL103";//紧急出口吊装

        public static readonly string EnterBlockName = "E-BFEL140";//出口/禁止
        public static readonly string EnterCeilingBlockName = "E-BFEL141";//出口/禁止吊装

        public static readonly string EvacCeilingBlockName = "E-BFEL201";//疏散指示灯吊装
        public static readonly string EvacR2LineCeilingBlockName = "E-BFEL201-1";//疏散指示灯吊装
        public static readonly string EvacLR2LineCeilingBlockName = "E-BFEL211-1";//疏散指示灯吊装

        //public static readonly string ALBlockName = "E-BDB001";
        //public static readonly string ALEBlockName = "E-BDB003";

        //车道线
        public const string LANELINE_LAYER_NAME = "E-LANE-CENTER";

        //疏散指示灯
        public const string FEI_EXIT_NAME100 = "E-BFEL100";      //疏散出入口块名100
        public const string FEI_EXIT_NAME101 = "E-BFEL101";      //疏散出入口块名101
        public const string FEI_EXIT_NAME102 = "E-BFEL102";      //疏散出入口块名102
        public const string FEI_EXIT_NAME103 = "E-BFEL103";      //疏散出入口块名103
        public const string FEI_EXIT_NAME140 = "E-BFEL140";      //疏散出入口块名140
        public const string FEI_EXIT_NAME141 = "E-BFEL141";      //疏散出入口块名141

        //疏散指示路径
        public const string MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME = "预估主要疏散路径-吊装";      //预估主要疏散路径-吊装
        public const string MAIN_EVACUATIONPATH_BYWALL_LAYERNAME = "预估主要疏散路径-壁装";      //预估主要疏散路径-壁装
        public const string AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME = "预估辅助疏散路径-吊装";      //预估辅助疏散路径-吊装
        public const string AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME = "预估辅助疏散路径-壁装";      //预估辅助疏散路径-壁装
        //public const string MAIN_EVACUATIONPATH_BYHOISTING_LINETYPE = "预估主要疏散路径-吊装";      //预估主要疏散路径-吊装
        //public const string MAIN_EVACUATIONPATH_BYWALL_LINETYPE = "预估主要疏散路径-壁装";      //预估主要疏散路径-壁装
        //public const string AUXILIARY_EVACUATIONPATH_BYHOISTING_LINETYPE = "预估辅助疏散路径-吊装";      //预估辅助疏散路径-吊装
        //public const string AUXILIARY_EVACUATIONPATH_BYWALL_LINETYPE = "预估辅助疏散路径-壁装";      //预估辅助疏散路径-壁装


        //疏散方向标志灯
        public const string PILOTLAMP_WALL_ONEWAY_SINGLESIDE = "E-BFEL200";         //疏散方向指示灯，壁装-> 单向 ->单面指示
        public const string PILOTLAMP_HOST_ONEWAY_SINGLESIDE = "E-BFEL201";         //疏散方向指示灯，吊装-> 单向 ->单面指示
        public const string PILOTLAMP_HOST_ONEWAY_DOUBLESIDE = "E-BFEL201-1";       //疏散方向指示灯，吊装-> 单向 ->双面指示
        public const string PILOTLAMP_WALL_TWOWAY_SINGLESIDE = "E-BFEL210";         //疏散方向指示灯，壁装->/<- 双向 ->单面指示
        public const string PILOTLAMP_HOST_TWOWAY_SINGLESIDE = "E-BFEL211";         //疏散方向指示灯，吊装->/<- 双向 ->单面指示
        public const string PILOTLAMP_HOST_TWOWAY_DOUBLESIDE = "E-BFEL211-1";     //疏散方向指示灯，吊装->/<- 双向 ->双面指示
    }
}
