using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Service.Hvac
{
    public class ThHvacCommon
    {
        // 块名
        public const string HTFC_TYPE_NAME = "离心";
        public const string AXIAL_TYPE_NAME = "轴流";
        public const string HTFC_BLOCK_NAME = "离心风机";
        public const string AXIAL_BLOCK_NAME = "轴流风机";
        public const string WALLHOLE_BLOCK_NAME = "洞口";
        public const string AIRVALVE_BLOCK_NAME = "风阀";
        public const string SILENCER_BLOCK_NAME = "阻抗复合式消声器";
        public const string FILEVALVE_BLOCK_NAME = "防火阀";

        // XDATA
        public const string RegAppName_FanSelection = "THCAD_FAN_SELECTION";
        public const string RegAppName_Model_Foundation = "THCAD_FAN_FOUNDATION";

        // 图层
        public const string FOUNDATION_LAYER = "H-BASE";
        public const string SILENCER_LAYER = "H-DUCT-APPE";
        public const string WALLHOLE_LAYER = "H-HOLE";
        public const string AIRVALVE_LAYER = "H-DAPP-DAMP";

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
        public const string BLOCK_ATTRIBUTE_VALUE_MOUNT_STRIP = "落地条形";
        public const string BLOCK_ATTRIBUTE_VALUE_MOUNT_SQUARE = "落地方形";
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
        public const string BLOCK_DYNAMIC_PROPERTY_DIAMETER = "风机直径";
        public const string BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_X = "标注基点 X";
        public const string BLOCK_DYNMAIC_PROPERTY_ANNOTATION_BASE_POINT_Y = "标注基点 Y";
        public const string BLOCK_DYNAMIC_PROPERTY_INLET_X = "进风口 X";
        public const string BLOCK_DYNAMIC_PROPERTY_INLET_Y = "进风口 Y";
        public const string BLOCK_DYNAMIC_PROPERTY_OUTLET_X = "出风口 X";
        public const string BLOCK_DYNAMIC_PROPERTY_OUTLET_Y = "出风口 Y";
        public const string BLOCK_DYNAMIC_PROPERTY_INLET_VERTICAL = "进风口竖";
        public const string BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL = "进风口横";
        public const string BLOCK_DYNAMIC_PROPERTY_OUTLET_VERTICAL = "出风口竖";
        public const string BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL = "出风口横";

        //阀块动态属性
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY = "可见性";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT = "高度";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA = "宽度或直径";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTH = "宽度";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH = "长度";
    }
}
