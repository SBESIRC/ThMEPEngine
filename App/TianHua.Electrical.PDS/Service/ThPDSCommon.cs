using System;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSCommon
    {
        public static readonly string BLOCK = "Block";
        public static readonly string DISTRIBUTION_BOX = "配电箱";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_NAME = "块名";
        public static readonly string LOAD_ID = "负载编号";
        public static readonly string DESCRIPTION = "负载用途";
        public static readonly string ELECTRICITY = "电量";
        public static readonly string POWER_CATEGORY = "电源类别";
        public static readonly string PROPERTY_VALUE_FIRE_POWER = "消防电源";
        public static readonly string NON_PROPERTY_VALUE_FIRE_POWER = "非消防电源";
        public static readonly string FREQUENCY_CONVERSION = "变频";

        public static readonly string DISTRIBUTION_BOX_ID = "配电箱编号";
        public static readonly string APPLICATION = "Description";
        public static readonly string FIRE_LOAD = "Fire Load";
        public static readonly string OVERALL_DIMENSIONS = "Overall Dimensions";
        public static readonly string LOCATION = "Location";
        public static readonly string INSTALLMETHOD = "Install Method";
        public static readonly string ENTER_CIRCUIT_ID = "回路编号";

        public static readonly string ENTER_CIRCUIT_QL = "QL";
        public static readonly string ENTER_CIRCUIT_QL_25_1P = "QL 25/1P";
        public static readonly string ENTER_CIRCUIT_QL_250_4P = "QL 250/4P";
        public static readonly string ENTER_CIRCUIT_ATSE_320A_4P = "ATSE 320A 4P";
        public static readonly string ENTER_CIRCUIT_MTSE_320A_4P = "MTSE 320A 4P";

        public static readonly string OUT_CIRCUIT_CB = "CB";
        public static readonly string OUT_CIRCUIT_CB1 = "CB1";
        public static readonly string OUT_CIRCUIT_CB2 = "CB2";
        public static readonly string OUT_CIRCUIT_RCD = "RCD";
        public static readonly string OUT_CIRCUIT_QAC = "QAC";
        public static readonly string OUT_CIRCUIT_QAC1 = "QAC1";
        public static readonly string OUT_CIRCUIT_QAC2 = "QAC2";
        public static readonly string OUT_CIRCUIT_QAC3 = "QAC3";
        public static readonly string OUT_CIRCUIT_KH = "KH";
        public static readonly string OUT_CIRCUIT_KH1 = "KH1";
        public static readonly string OUT_CIRCUIT_KH2 = "KH2";
        public static readonly string OUT_CIRCUIT_CT = "CT";
        public static readonly string OUT_CIRCUIT_MT = "MT";
        public static readonly string OUT_CIRCUIT_CPS = "CPS";
        public static readonly string OUT_CIRCUIT_CPS1 = "CPS1";
        public static readonly string OUT_CIRCUIT_CPS2 = "CPS2";
        public static readonly string OUT_CIRCUIT_CONDUCTOR = "Conductor";
        public static readonly string OUT_CIRCUIT_CONDUCTOR1 = "Conductor1";
        public static readonly string OUT_CIRCUIT_CONDUCTOR2 = "Conductor2";
        public static readonly string OUT_CIRCUIT_CIRCUIT_NUMBER = "回路编号";
        public static readonly string OUT_CIRCUIT_PHSAE = "相序";
        public static readonly string OUT_CIRCUIT_POWER = "功率";
        public static readonly string OUT_CIRCUIT_LOW_POWER = "功率(低)";
        public static readonly string OUT_CIRCUIT_HIGH_POWER = "功率(高)";
        public static readonly string OUT_CIRCUIT_LOAD_ID = "负载编号";
        public static readonly string OUT_CIRCUIT_DESCRIPTION = "功能用途";
        public static readonly string CONTROL_CIRCUIT_DESCRIPTION = "控制回路";

        public const double ALLOWABLE_TOLERANCE = 26.0;  //允许公差
        public const double STOREY_TOLERANCE = 200.0;  //允许楼层位置偏差
        public const double INNER_TOLERANCE = 57400.0;  //内框高度

        public static readonly string MOTOR_AND_LOAD_LABELS = "电动机及负载标注";
        public static readonly string LOAD_LABELS = "负载标注";
        public static readonly string LOAD_DETAILS = "E-电力平面-负荷明细";
        public static readonly string PUMP_LABELS = "水泵标注";
        public static readonly string LIGHTING_LOAD = "E-BL001";

        public static readonly string SYSTEM_DIAGRAM_TABLE_FRAME = "THAPE_A1L_inner";
        public static readonly string SYSTEM_DIAGRAM_TABLE_HEADER = "系统图内框及标题栏";
        public static readonly string SYSTEM_DIAGRAM_TABLE_TITLE = "系统图标题栏";
        public static readonly string SYSTEM_DIAGRAM_TABLE_TAIL_SINGLE_PHASE = "单相计算表";
        public static readonly string SYSTEM_DIAGRAM_TABLE_TAIL_THREE_PHASE = "三相计算表";
        public static readonly string SYSTEM_DIAGRAM_SECONDARY_JUNCTION = "二次结线说明";

        public static readonly string DEFAULT_ISOLATING_SWITCH = "E-BQL102";
        public static readonly string DEFAULT_ISOLATING_SWITCH_1 = "E-BQL102-1";
        public static readonly string DEFAULT_TRANSFER_SWITCH = "E-BTS101";
        public static readonly string DEFAULT_MANUAL_TRANSFER_SWITCH = "E-BTS102";

        public static readonly string DEFAULT_CIRCUIT_BREAKER = "E-BCB101";
        public static readonly string DEFAULT_RESIDUAL_CURRENT_DEVICE = "E-BCB102";
        public static readonly string DEFAULT_CPS = "E-BCB103";
        public static readonly string DEFAULT_CONTACTOR = "E-BKM101";
        public static readonly string DEFAULT_THERMAL_RELAY = "E-BKH102";
        public static readonly string DEFAULT_CURRENT_TRANSFORMER = "E-BCT102";

        public static readonly string CONTROL_CIRCUIT_BELONG_TO_CPS = "控制（从属CPS）";
        public static readonly string CONTROL_CIRCUIT_BELONG_TO_QAC = "控制（从属接触器）";
        public static readonly string SMALL_BUSBAR = "分支小母排";
        public static readonly string SMALL_BUSBAR_Circuit = "小母排分支";
        public static readonly string SURGE_PROTECTION = "SPD";

        public static readonly string FIRE_POWER_MONITORING_1 = "E-BPMFE201-1";
        public static readonly string FIRE_POWER_MONITORING_2 = "E-BPMFE201-2";
        public static readonly string ELECTRICAL_FIRE_MONITORING_1 = "E-BEFPS201-1";
        public static readonly string ELECTRICAL_FIRE_MONITORING_2 = "E-BEFPS201-2";

        public static readonly Tuple<string, short> AI_POWR_AUXL1 = Tuple.Create("AI-POWR-AUXL1", (short)2); 
        public static readonly Tuple<string, short> AI_POWR_AUXL2 = Tuple.Create("AI-POWR-AUXL2", (short)1); 
        public static readonly Tuple<string, short> AI_POWR_AUXL3 = Tuple.Create("AI-POWR-AUXL3", (short)3); 
    
        public static readonly string LOAD_DIMENSION = "AI-负载标注";
        public static readonly string LOAD_DIMENSION_R = "AI-负载标注-R";
        public static readonly string LOAD_DIMENSION_L = "AI-负载标注-L";
        public static readonly string CIRCUIT_DIMENSION = "AI-回路标注";
        public static readonly string CIRCUIT_DIMENSION_R = "AI-回路标注-R";
        public static readonly string CIRCUIT_DIMENSION_L = "AI-回路标注-L";

        public static readonly string POSITION_1_X = "位置1 X";
        public static readonly string POSITION_1_Y = "位置1 Y"; 
        public static readonly string PROPERTY_LOAD_FILP = "翻转状态1"; 
        public static readonly string PROPERTY_TABLE_WIDTH = "标注表格宽度";

        public static readonly string LOAD_ID_OR_PURPOSE = "设备编号或用途";
        public static readonly string LOAD_POWER = "设备功率"; 
        public static readonly string PRIMARY_AND_SPARE_AVAIL = "主备关系"; 

        public static readonly string SECONDARY_JUNCTION_TEXT1 = "Text1"; 
        public static readonly string SECONDARY_JUNCTION_TEXT2 = "Text2";
        public static readonly string SECONDARY_JUNCTION_TEXT3 = "Text3";
        public static readonly string SECONDARY_JUNCTION_TEXT4 = "Text4";
        public static readonly string SECONDARY_JUNCTION_TEXT5 = "Text5";
        public static readonly string SECONDARY_JUNCTION_TEXT6 = "Text6";
    }
}
