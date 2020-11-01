
namespace TianHua.FanSelection
{
    public class ThFanSelectionCommon
    {
        // 命令
        public const string CMD_MODEL_EDIT = "THFJEDIT";

        public const string AXIAL_TYPE_NAME = "轴流";
        public const string AXIAL_BLOCK_NAME = "轴流风机";
        public const string AXIAL_MODEL_NAME_SUFFIX = "无基础";
        public const string HTFC_TYPE_NAME = "离心";
        public const string HTFC_BACKWARD_NAME = "后倾";
        public const string HTFC_BLOCK_NAME = "离心风机";
        public const string MOTOR_POWER = "电机功率.json";
        public const string MOTOR_POWER_Double = "电机功率-双速.json";
        public const string BLOCK_FAN_FILE = "暖通.选型.风机.dwg";
        public const string HTFC_Selection = "离心风机选型.json";
        public const string HTFC_Parameters = "离心-前倾-单速.json";
        public const string HTFC_Parameters_Double = "离心-前倾-双速.json";
        public const string HTFC_Parameters_Single = "离心-后倾-单速.json";
        
        public const string AXIAL_Selection = "轴流风机选型.json";
        public const string AXIAL_Parameters = "轴流-单速.json";
        public const string AXIAL_Parameters_Double = "轴流-双速.json";
        public const string HTFC_Efficiency = "离心风机效率.json";
        public const string AXIAL_Efficiency = "轴流风机效率.json";
        public const string RegAppName_FanSelection = "THCAD_FAN_SELECTION";

        // 图层
        public const string BLOCK_LAYER_FIRE = "H-FIRE-FBOX";
        public const string BLOCK_LAYER_DUAL = "H-DUAL-FBOX";
        public const string BLOCK_LAYER_EQUP = "H-EQUP-FBOX";


        // 风机块属性
        public const string BLOCK_ATTRIBUTE_EQUIPMENT_SYMBOL = "设备符号";
        public const string BLOCK_ATTRIBUTE_STOREY_AND_NUMBER = "楼层-编号";
        public const string BLOCK_ATTRIBUTE_FAN_USAGE = "风机功能";
        public const string BLOCK_ATTRIBUTE_FAN_VOLUME = "风量";
        public const string BLOCK_ATTRIBUTE_FAN_PRESSURE = "全压";
        public const string BLOCK_ATTRIBUTE_FAN_CHARGE = "电量";
        public const string BLOCK_ATTRIBUTE_FAN_REMARK = "备注";
        public const string BLOCK_ATTRIBUTE_MOUNT_TYPE = "安装方式";
        public const string BLOCK_ATTRIBUTE_FIXED_FREQUENCY = "定频";
        public const string BLOCK_ATTRIBUTE_FIRE_POWER_SUPPLY = "消防电源";

        // 风机块属性值
        public const string BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST = "吊装";
        public const string BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR = "落地";
        public const string BLOCK_ATTRIBUTE_VALUE_FIXED_FREQUENCY = "定频";
        public const string BLOCK_ATTRIBUTE_VALUE_VARIABLE_FREQUENCY = "变频";
        public const string BLOCK_ATTRIBUTE_VALUE_DUAL_FREQUENCY = "双频";
        public const string BLOCK_ATTRIBUTE_VALUE_SINGLE_SPEED = "单速";
        public const string BLOCK_ATTRIBUTE_VALUE_DOUBLE_SPEED = "双速";
        public const string BLOCK_ATTRIBUTE_VALUE_FIRE_POWER = "消防电源";
        public const string BLOCK_ATTRIBUTE_VALUE_NON_FIRE_POWER = "非消防电源";

        // 风机块动态属性
        public const string BLOCK_DYNAMIC_PROPERTY_ANGLE1 = "角度1";
        public const string BLOCK_DYNAMIC_PROPERTY_ANGLE2 = "角度2";
        public const string BLOCK_DYNAMIC_PROPERTY_ROTATE1 = "翻转状态1";
        public const string BLOCK_DYNAMIC_PROPERTY_ROTATE2 = "翻转状态2";
        public const string BLOCK_DYNAMIC_PROPERTY_POSITION1_X = "位置1 X";
        public const string BLOCK_DYNAMIC_PROPERTY_POSITION1_Y = "位置1 Y";
        public const string BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X = "设备基点 X";
        public const string BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y = "设备基点 Y";
        public const string BLOCK_DYNAMIC_PROPERTY_SPECIFICATION_MODEL = "规格及型号";
        public const string BLOCK_DYNAMIC_PROPERTY_MODEL_TEXT_HEIGHT = "型号字高";
        public const string BLOCK_DYNAMIC_PROPERTY_ANNOTATION_TEXT_HEIGHT = "标注字高";
    }
}
