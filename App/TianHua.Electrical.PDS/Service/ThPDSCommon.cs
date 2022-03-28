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
        public static readonly string FIRE_POWER_SUPPLY = "消防电源";
        public static readonly string NON_FIRE_POWER_SUPPLY = "非消防电源";
        public static readonly string FREQUENCY_CONVERSION = "变频";

        public static readonly string DISTRIBUTION_BOX_ID = "配电箱编号";
        public static readonly string APPLICATION = "Description";
        public static readonly string FIRE_LOAD = "Fire Load"; 
        public static readonly string OVERALL_DIMENSIONS = "Overall Dimensions";
        public static readonly string LOCATION = "Location";
        public static readonly string INSTALLMETHOD = "Install Method";
        public static readonly string ENTER_CIRCUIT_ID = "回路编号";

        public static readonly string ENTER_CIRCUIT_QL = "QL";
        public static readonly string ENTER_CIRCUIT_QL_250_4P = "QL 250/4P";
        public static readonly string ENTER_CIRCUIT_ATSE_320A_4P = "ATSE 320A 4P"; 

        public static readonly string OUT_CIRCUIT_CB = "CB";
        public static readonly string OUT_CIRCUIT_CB1 = "CB1";
        public static readonly string OUT_CIRCUIT_CB2 = "CB2";
        public static readonly string OUT_CIRCUIT_RCD = "RCD";
        public static readonly string OUT_CIRCUIT_QAC = "QAC";
        public static readonly string OUT_CIRCUIT_KH = "KH";
        public static readonly string OUT_CIRCUIT_CONDUCTOR = "Conductor";
        public static readonly string OUT_CIRCUIT_CIRCUIT_NUMBER = "回路编号";
        public static readonly string OUT_CIRCUIT_PHSAE = "相序";
        public static readonly string OUT_CIRCUIT_POWER = "功率";
        public static readonly string OUT_CIRCUIT_LOAD_ID = "负载编号";
        public static readonly string OUT_CIRCUIT_DESCRIPTION = "功能用途";

        public const double ALLOWABLE_TOLERANCE = 25.0;  //允许公差
        public const double STOREY_TOLERANCE = 200.0;  //允许楼层位置偏差

        public static readonly string MOTOR_AND_LOAD_LABELS = "电动机及负载标注";
        public static readonly string LOAD_LABELS = "负载标注";
        public static readonly string LOAD_DETAILS = "负载明细";
        public static readonly string PUMP_LABELS = "水泵标注";

        public static readonly string SYSTEM_DIAGRAM_TABLE_HEADER = "系统图内框及标题栏";
        public static readonly string SYSTEM_DIAGRAM_TABLE_TITLE = "系统图标题栏";
        public static readonly string SYSTEM_DIAGRAM_TABLE_TAIL_SINGLE_PHASE = "单相计算表";
        public static readonly string SYSTEM_DIAGRAM_TABLE_TAIL_THREE_PHASE = "三相计算表";

        public static readonly string DEFAULT_ISOLATING_SWITCH = "E-BQL102";
        public static readonly string DEFAULT_TRANSFER_SWITCH = "E-BTS101";

        public static readonly string DEFAULT_CIRCUIT_BREAKER = "E-BCB101";
        public static readonly string DEFAULT_RESIDUAL_CURRENT_DEVICE = "E-BCB102";
        public static readonly string DEFAULT_CONTACTOR = "E-BKM101";
        public static readonly string DEFAULT_THERMAL_RELAY = "E-BKH102";
    }
}
