namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCommon
    {
        public const double default_voltage = 380;
        public const double radian_tolerance = 1e-6;
        public const double default_fire_valve_width = 0.0;
        public const double default_fire_valve_length = 320.0;

        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_NAME = "块名";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_POSITION = "位置";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER = "目标图层";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID = "负载编号";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE = "是否炸开";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL = "内含图块";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY = "来源专业";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_EQUIMENT = "设备类型";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY = "可见性";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_POWER = "负载电量";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_RELATIONSHIP = "主备关系";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_POWER_SUPPLY = "电源类别";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_POSITION_MODE = "计算模式";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER = "外形图层";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_DESCRIPTION = "负载用途";
        public const string BLOCK_MAP_ATTRIBUTES_BLOCK_ROTATION_CORRECT = "旋转矫正";

        public const string PROPERTY_LOAD_FILP = "翻转状态1";
        public const string PROPERTY_POWER_VOLTAGE = "电压";
        public const string PROPERTY_LOAD_USAGE = "负载用途";
        public const string PROPERTY_POWER_QUANTITY = "电量";
        public const string PROPERTY_DUAL_FREQUENCY = "双频";
        public const string PROPERTY_LOAD_NUMBER = "负载编号";
        public const string PROPERTY_FIXED_FREQUENCY = "定频";
        public const string PROPERTY_LOAD_FILP_1 = "翻转状态1";
        public const string PROPERTY_TABLE_WIDTH = "标注表格宽度";
        public const string PROPERTY_VARIABLE_FREQUENCY = "变频";
        public const string PROPERTY_EQUIPMENT_SYMBOL = "设备符号";
        public const string PROPERTY_FIRE_POWER_SUPPLY = "消防电源";
        public const string PROPERTY_STOREY_AND_NUMBER = "楼层-编号";
        public const string PROPERTY_LOAD_POWER_QUANTITY = "负载电量";
        public const string PROPERTY_ALL_FREQUENCY = "变频、双速或定频";
        public const string PROPERTY_FIRE_POWER_SUPPLY2 = "消防电源或非消防电源";

        public const string PROPERTY_VALUE_FIRE_POWER = "消防电源";
        public const string PROPERTY_VALUE_NON_FIRE_POWER = "非消防电源";

        public const string PROPERTY_FAN_TYPE = "风机类型";
        public const string PROPERTY_FAN_USAGE = "风机功能";
        public const string PROPERTY_EQUIPMENT_NAME = "设备名称";

        public const string BLOCK_MAP_RULES_TABLE_TITLE_WEAK = "天华弱电提资转换对应表";
        public const string BLOCK_MAP_RULES_TABLE_TITLE_STRONG = "天华强电提资转换对应表";

        public const string COLLECTING_WELL = "集水井提资表表身";

        public const string WEAK_CURRENT = "弱电";
        public const string STRONG_CURRENT = "强电";

        public const string BLOCK_LOAD_DIMENSION = "负载标注";
        public const string LABEL_STYLE_BORDERLESS = "标注无边框";
        public const string BLOCK_MOTOR_AND_LOAD_DIMENSION = "电动机及负载标注";
        public const string BLOCK_PUMP_LABEL = "水泵标注";
        public const string BLOCK_PUMP_LABEL_LAYER = "E-UNIV-NOTE";
        public const string BLOCK_SUBMERSIBLE_PUMP = "潜水泵";
        public const string BLOCK_AI_SUBMERSIBLE_PUMP = "潜水泵-AI";

        public const string SINGLE_SUBMERSIBLE_PUMP = "单台潜水泵";
        public const string MANUAL_ACTUATOR_OF_SMOKE_EXHAUST_VALVE = "手动执行机构";

        public const string HIDING_LAYER = "AI-提资比对";

        public const string LINE_TYPE_HIDDEN = "HIDDEN";
        public const string LINE_TYPE_CONTINUOUS = "CONTINUOUS";
    }
}
