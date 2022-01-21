namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCommon
    {
        public static readonly double default_voltage = 380;
        public static readonly double radian_tolerance = 1e-6;
        public const double default_fire_valve_width = 0.0;
        public const double default_fire_valve_length = 320.0;
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_NAME = "块名";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER = "目标图层";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE = "是否炸开";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY = "可见性";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL = "内含图块";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_INSERT_MODE = "插入模式";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER = "外形图层";

        public static readonly string PROPERTY_POWER_VOLTAGE = "电压";
        public static readonly string PROPERTY_POWER_QUANTITY = "电量";
        public static readonly string PROPERTY_LOAD_USAGE = "负载用途";
        public static readonly string PROPERTY_LOAD_NUMBER = "负载编号";
        public static readonly string PROPERTY_EQUIPMENT_SYMBOL = "设备符号";
        public static readonly string PROPERTY_STOREY_AND_NUMBER = "楼层-编号";
        public static readonly string PROPERTY_FIRE_POWER_SUPPLY = "消防电源";
        public static readonly string PROPERTY_FIRE_POWER_SUPPLY2 = "消防电源或非消防电源";
        public static readonly string PROPERTY_FIXED_FREQUENCY = "定频";
        public static readonly string PROPERTY_VARIABLE_FREQUENCY = "变频";
        public static readonly string PROPERTY_DUAL_FREQUENCY = "双频";
        public static readonly string PROPERTY_ALL_FREQUENCY = "变频、双速或定频";
        public static readonly string PROPERTY_LOAD_FILP = "翻转状态1";

        public static readonly string PROPERTY_VALUE_FIRE_POWER = "消防电源";
        public static readonly string PROPERTY_VALUE_NON_FIRE_POWER = "非消防电源";

        public static readonly string PROPERTY_FAN_TYPE = "风机类型";
        public static readonly string PROPERTY_FAN_USAGE = "风机功能";
        public static readonly string PROPERTY_EQUIPMENT_NAME = "设备名称";

        public static readonly string BLOCK_MAP_RULES_TABLE_TITLE_WEAK = "天华弱电提资转换对应表";
        public static readonly string BLOCK_MAP_RULES_TABLE_TITLE_STRONG = "天华强电提资转换对应表";

        public static readonly string COLLECTING_WELL = "集水井提资表表身";

        public static readonly string STRONG_CURRENT = "强电";
        public static readonly string WEAK_CURRENT = "弱电";
    }
}
