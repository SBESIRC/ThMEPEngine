namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCommon
    {
        public static readonly double default_voltage = 380;
        public static readonly double radian_tolerance = 1e-6;
        public static readonly string LAYER_FAN_DEVICE = "E-FAS-DEVC";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK = "Block";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_SCALE = "Scale";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY = "可见性";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_FIREPOWER = "FirePowerBlock";
        public static readonly string BLOCK_MAP_ATTRIBUTES_BLOCK_NON_FIREPOWER = "NonFirePowerBlock";

        public static readonly string PROPERTY_POWER_VOLTAGE = "电压";
        public static readonly string PROPERTY_POWER_QUANTITY = "电量";
        public static readonly string PROPERTY_LOAD_USAGE = "负载用途";
        public static readonly string PROPERTY_LOAD_NUMBER = "负载编号";
        public static readonly string PROPERTY_EQUIPMENT_SYMBOL = "设备符号";
        public static readonly string PROPERTY_STOREY_AND_NUMBER = "楼层-编号";
        public static readonly string PROPERTY_FIRE_POWER_SUPPLY = "消防电源";
        public static readonly string PROPERTY_FIXED_FREQUENCY = "定频";
        public static readonly string PROPERTY_VARIABLE_FREQUENCY = "变频";
        public static readonly string PROPERTY_DUAL_FREQUENCY = "双频";


        public static readonly string PROPERTY_FAN_TYPE = "风机类型";
        public static readonly string PROPERTY_FAN_USAGE = "风机功能";
        public static readonly string PROPERTY_EQUIPMENT_NAME = "设备名称";

        public static readonly string BLOCK_MAP_RULES_FILE = "提电气块转换配置表.机电一所.dwg";
        public static readonly string BLOCK_MAP_RULES_TABLE_TITLE_WEAK = "机电一所弱电设备转换对应表";
        public static readonly string BLOCK_MAP_RULES_TABLE_TITLE_STRONG = "机电一所强电设备转换对应表";
    }
}
