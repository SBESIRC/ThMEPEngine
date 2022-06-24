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
        public const string HOSE_BLOCK_NAME = "风机软接";

        // 中心线线型
        public const string CENTERLINE_LINETYPE = "CENTER2";
        public const string CONTINUES_LINETYPE = "CONTINUOUS";
        public const string DASH_LINETYPE = "DASH";

        // XDATA
        public const string RegAppName_FanSelection = "THCAD_FAN_SELECTION";
        public const string RegAppName_FanSelectionEx = "THCAD_FAN_SELECTION_EX";
        public const string RegAppName_Model_Foundation = "THCAD_FAN_FOUNDATION";
        public const string RegAppName_Duct_Info = "THCAD_HVAC_DUCT_INFO";
        public const string RegAppName_Duct_Param = "THCAD_HVAC_DUCT_PARAM";

        // 图层
        public const string WALLHOLE_LAYER = "H-HOLE";
        public const string FOUNDATION_LAYER = "H-BASE";

        // 阀图层
        public const string VALVE_LAYER_DUAL = "H-DAPP-DDAMP";
        public const string VALVE_LAYER_FIRE = "H-DAPP-FDAMP";
        public const string VALVE_LAYER_EQUP = "H-DAPP-DAMP";

        // 中心线图层
        public const string CENTERLINE_LAYER_DUAL = "H-DUCT-DUAL-MID";
        public const string CENTERLINE_LAYER_FIRE = "H-DUCT-FIRE-MID";
        public const string CENTERLINE_LAYER_VENT = "H-DUCT-VENT-MID";

        // 法兰图层
        public const string FLANGE_LAYER_DUAL = "H-DAPP-DAPP";
        public const string FLANGE_LAYER_FIRE = "H-DAPP-FAPP";
        public const string FLANGE_LAYER_VENT = "H-DAPP-AAPP";

        // 风管图层
        public const string DUCT_LAYER_DUAL = "H-DUCT-DUAL";
        public const string DUCT_LAYER_FIRE = "H-DUCT-FIRE";
        public const string DUCT_LAYER_VENT = "H-DUCT-VENT";

        // 风管文字图层
        public const string DUCT_TEXT_LAYER_DUAL = "H-DIMS-DUAL";
        public const string DUCT_TEXT_LAYER_FIRE = "H-DIMS-FIRE";
        public const string DUCT_TEXT_LAYER_VENT = "H-DIMS-DUCT";

        //风管文字样式名
        public const string DUCT_TEXT_STYLE = "TH-STYLE3";

        // 风机图层
        public const string MODEL_LAYER_DUAL = "H-DUAL-FBOX";
        public const string MODEL_LAYER_FIRE = "H-FIRE-FBOX";
        public const string MODEL_LAYER_EQUP = "H-EQUP-FBOX";

        // 风机块属性名
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

        // 风机块动态属性名
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
        public const string BLOCK_DYNAMIC_PROPERTY_LENGTH = "风机长度";
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
        public const string BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE_FIRE = "字旋转";
        public const string BLOCK_DYNAMIC_PROPERTY_INSTALL_STYLE = "安装方式";

        public const string BLOCK_DYNAMIC_PROPERTY_FAN_ANGLE = "风机角度";
        public const string BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_X = "标注位置 X";
        public const string BLOCK_DYNMAIC_PROPERTY_ANNOTATION_POSITION_POINT_Y = "标注位置 Y";

        //阀块动态属性名
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY = "可见性";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT = "高度";
        public const string BLOCK_DYNAMIC_PROPERTY_CHECK_VALVE_HEIGHT = "阀门长度";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA = "宽度或直径";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTH = "宽度";
        public const string BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH = "长度";
        public const string BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT = "字高";
        public const string BLOCK_DYNAMIC_PROPERTY_ROTATE_NONE = "未翻转";
        public const string BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE = "字旋转";

        //阀块可见性属性值
        public const string BLOCK_VALVE_VISIBILITY_FIRE_280 = "280度防火阀（反馈）FDH";
        public const string BLOCK_VALVE_VISIBILITY_FIRE_150 = "150度防火阀";
        public const string BLOCK_VALVE_VISIBILITY_FIRE_70 = "70度防火阀（反馈）FDS";
        public const string BLOCK_VALVE_VISIBILITY_CHECK = "风管止回阀";
        public const string BLOCK_VALVE_VISIBILITY_SILENCER_100 = "ZP100";
        public const string BLOCK_VALVE_VISIBILITY_SILENCER_200 = "ZP200";
        public const string BLOCK_VALVE_VISIBILITY_ELECTRIC = "电动多叶调节风阀";
        public const string BLOCK_VALVE_VISIBILITY_FIRE_MEC = "电/手动排烟阀（电磁、手动、常闭）MEC";
        public const string BLOCK_VALVE_VISIBILITY_FIRE_BEC = "电/手动排烟阀（电磁、反馈，就地手动、常闭）BEC";
        public const string BLOCK_VALVE_VISIBILITY_FIRE_MECH_280 = "280度电/手动防火阀（电磁，反馈，常闭）MECH";
        public const string BLOCK_VALVE_VISIBILITY_FIRE_BECH_280 = "280度电/手动防火阀（电磁，反馈，就地手动，常闭）BECH";


        //风口动态块属性名
        public const string BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER = "风口长度";
        public const string BLOCK_DYNAMIC_PORT_HEIGHT = "风口宽度";
        public const string BLOCK_DYNAMIC_PORT_RANGE = "风口类型";

        //风管立管动态块属性名
        public const string BLOCK_DYNAMIC_VERTICAL_PIPE_WIDTH = "风管宽度";
        public const string BLOCK_DYNAMIC_VERTICAL_PIPE_LENGTH = "风管宽度";
        public const string BLOCK_DYNAMIC_VERTICAL_PIPE_AIRVOLUME = "风量";
        public const string BLOCK_DYNAMIC_VERTICAL_PIPE_CUT_SIZE = "截面尺寸";
        public const string BLOCK_DYNAMIC_VERTICAL_PIPE_Code = "风管代号";

        //风管断线动态块属性名
        public const string BLOCK_DYNAMIC_BROKEN_LEN = "风管宽度";

        //45°下翻动态块属性名
        public const string BLOCK_DYNAMIC_FLIP_DOWN_45_WIDTH = "风管宽度";
        public const string BLOCK_DYNAMIC_FLIP_DOWN_45_HEIGHT = "风管高度";

        //风口标注动态块属性名
        public const string BLOCK_DYNAMIC_PORT_NAME = "风口名称";
        public const string BLOCK_DYNAMIC_PORT_SIZE = "尺寸";
        public const string BLOCK_DYNAMIC_PORT_NUM = "数量";
        public const string BLOCK_DYNAMIC_PORT_AIRVOLUME = "风量";

        //组成连接件的个数 在ThDuctPortsFactory.cs
        public const int R_TEE_GEO_NUM = 8;
        public const int R_TEE_FLG_NUM = 3;
        public const int R_TEE_CENTERLINE_NUM = 3;
        public const int R_TEE_ALL_NUM = (R_TEE_GEO_NUM + R_TEE_FLG_NUM + R_TEE_CENTERLINE_NUM);
        public const int V_TEE_GEO_NUM = 10;// 11或10个
        public const int V_TEE_FLG_NUM = 3;
        public const int V_TEE_CENTERLINE_NUM = 3;
        public const int V_TEE_ALL_NUM = (V_TEE_GEO_NUM + V_TEE_FLG_NUM + V_TEE_CENTERLINE_NUM);
        public const int ELBOW_GEO_NUM = 6;
        public const int ELBOW_FLG_NUM = 2;
        public const int ELBOW_CENTERLINE_NUM = 3;
        public const int ELBOW_ALL_NUM = (ELBOW_GEO_NUM + ELBOW_FLG_NUM + ELBOW_CENTERLINE_NUM);
        public const int CROSS_GEO_NUM = 12;
        public const int CROSS_FLG_NUM = 4;
        public const int CROSS_CENTERLINE_NUM = 4;
        public const int CROSS_ALL_NUM = (CROSS_GEO_NUM + CROSS_FLG_NUM + CROSS_CENTERLINE_NUM);
        public const int REDUCING_GEO_NUM = 2;
        public const int REDUCING_FLG_NUM = 2;
        public const int REDUCING_CENTERLINE_NUM = 1;
        public const int REDUCING_ALL_NUM = (REDUCING_GEO_NUM + REDUCING_FLG_NUM + REDUCING_CENTERLINE_NUM);
        public const int AXIS_REDUCING_GEO_NUM = 4;
        public const int AXIS_REDUCING_FLG_NUM = 2;
        public const int AXIS_REDUCING_CENTERLINE_NUM = 1;
        public const int AXIS_REDUCING_ALL_NUM = (AXIS_REDUCING_GEO_NUM + AXIS_REDUCING_FLG_NUM + AXIS_REDUCING_CENTERLINE_NUM);

        public const string AI_PORT = "AI-风口";
        public const string AI_BROKEN_LINE = "AI-风管断线";
        public const string AI_VERTICAL_PIPE = "AI-风管立管";

        public const string AI_ROOM_BOUNDS = "AI-房间框线";
        public const string AI_DUCT_ROUTINE = "AI-风管路由";
        public const string AI_SMOKE_BROKE = "AI-挡烟垂壁";

        public const string AI_HOLE = "AI-洞口";
        public const string AI_HOLE_WIDTH = "洞口宽度";
        public const string AI_HOLE_LENGTH = "洞口长度";
        public const string AI_HOLE_ROTATION = "洞口角度";
        public const string AI_HOLE_TEXT_HEIGHT = "文字高度";
    }
}