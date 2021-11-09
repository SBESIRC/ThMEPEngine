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
        public const string EmgLightLayerName = "E-LITE-LITE";    //消防应急灯图层
        public const short EmgLightLayerColor = 3;             //消防应急灯图层颜色
        public const string EPowerLayerName = "E-POWR-EQPM";

        public const string EmgLightBlockName = "E-BFEL810";      //消防应急灯图块名,size:3
        public const string EmgLightDoubleBlockName = "E-BFEL500"; //消防应急灯图块名,size:1

        public const string EvacRBlockName = "E-BFEL200";     //疏散指示灯图块,size:1
        public const string EvacLRBlockName = "E-BFEL210";     //疏散指示灯图块,size:1

        public const string ExitEBlockName = "E-BFEL100";           //紧急出口图块,size:1
        public const string ExitSBlockName = "E-BFEL102";           //紧急出口图块,size:1

        public const string ExitECeilingBlockName = "E-BFEL101";//紧急出口吊装,size:2
        public const string ExitSCeilingBlockName = "E-BFEL103";//紧急出口吊装,size:2

        public const string EnterBlockName = "E-BFEL140";//出口/禁止,size:1
        public const string EnterCeilingBlockName = "E-BFEL141";//出口/禁止吊装,size:2

        public const string EvacCeilingBlockName = "E-BFEL201";//疏散指示灯吊装,size:2
        public const string EvacR2LineCeilingBlockName = "E-BFEL201-1";//疏散指示灯吊装,size:2
        public const string EvacLR2LineCeilingBlockName = "E-BFEL211-1";//疏散指示灯吊装,size:2


        //new
        public const string EnterNBlockName = "E-BFEL130";
        public const string EnterNCeilingBlockName = "E-BFEL131";
        public const string FloorBlockName = "E-BFEL110";
        public const string FloorCeilingBlockName = "E-BFEL111";
        public const string FloorEvacBlockName = "E-BFEL161";
        public const string FloorEvacCeilingBlockName = "E-BFEL161-1";
        public const string EvacUpCeilingBlockName = "E-BFEL221";
        public const string EvacPostBlockName = "E-BFEL223";
        public const string EvacSqBlockName = "E-BFEL240";
        public const string EvacSqDBlockName = "E-BFEL241";
        public const string EvacCirBlockName = "E-BFEL250";
        public const string EvacCirDBlockName = "E-BFEL251";
        public const string RefugeBlockName = "E-BFEL120";
        public const string RefugeCeilingBlockName = "E-BFEL121";
        public const string RefugeEBlockName = "E-BFEL122";
        public const string RefugeECeilingBlockName = "E-BFEL123";
        public const string EvacLRCeilingBlockName = "E-BFEL211";

        public const string EmgLightConnectLayerName = "E-LITE-WIRE2";
        public const string EmgLightConnectLineType = "HIDDEN";
        public const short EmgLightConnectLayerColor = 4;

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

        //疏散方向标志灯
        public const string PILOTLAMP_WALL_ONEWAY_SINGLESIDE = "E-BFEL200";         //疏散方向指示灯，壁装-> 单向 ->单面指示
        public const string PILOTLAMP_HOST_ONEWAY_SINGLESIDE = "E-BFEL201";         //疏散方向指示灯，吊装-> 单向 ->单面指示
        public const string PILOTLAMP_HOST_ONEWAY_DOUBLESIDE = "E-BFEL201-1";       //疏散方向指示灯，吊装-> 单向 ->双面指示
        public const string PILOTLAMP_WALL_TWOWAY_SINGLESIDE = "E-BFEL210";         //疏散方向指示灯，壁装->/<- 双向 ->单面指示
        public const string PILOTLAMP_HOST_TWOWAY_SINGLESIDE = "E-BFEL211";         //疏散方向指示灯，吊装->/<- 双向 ->单面指示
        public const string PILOTLAMP_HOST_TWOWAY_DOUBLESIDE = "E-BFEL211-1";     //疏散方向指示灯，吊装->/<- 双向 ->双面指示

        //地上疏散指示灯
        public const string ROOM_LAYER = "房间框线";                                //房间框线图层
        public const string ROOM_TEXT_NAME_LAYER = "房间名称";                      //房间名称图层
        public const string DOOR_LAYER = "AI-门";                                //门框线图层
        public const string CENTER_LINE_LAYER = "AI-中心线";                           //中心线图层

        //辅助、标识
        public const string REVCLOUD_LAYER = "AI-圈注";           //圈注图层，云线使用图层
        public static int EMGPILOTREVCLOUD_CORLOR_INDEX = 11;
    }
}
