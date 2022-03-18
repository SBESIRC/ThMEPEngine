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
        public static readonly string FREQUENCY_CONVERSION = "变频";

        public const double ALLOWABLE_TOLERANCE = 25.0;  //允许公差
        public const double STOREY_TOLERANCE = 200.0;  //允许楼层位置偏差

        public static readonly string MOTOR_AND_LOAD_LABELS = "电动机及负载标注";
        public static readonly string LOAD_LABELS = "负载标注";
        public static readonly string LOAD_DETAILS = "负载明细";
        public static readonly string PUMP_LABELS = "水泵标注";
        
    }
}
