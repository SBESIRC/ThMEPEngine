namespace ThMEPWSS.Pipe
{
    public class ThWPipeCommon
    {
        /// <summary>
        /// 洗衣机地漏到其他地漏最大距离（阳台区域）
        /// </summary>
        public const double MAX_BALCONYWASHINGFLOORDRAIN_TO_BALCONYFLOORDRAIN = 700;
        /// <summary>
        /// 洗衣机地漏到雨水管最大距离（阳台区域）
        /// </summary>
        public const double MAX_BALCONYWASHINGFLOORDRAIN_TO_RAINPIPE = 1700;
        /// <summary>
        /// 相邻设备平台最近距离
        /// </summary>
        public const double MAX_DEVICE_TO_DEVICE = 1700;
        /// <summary>
        /// 相邻设备平台距阳台最远距离
        /// </summary>
        public const double MAX_DEVICE_TO_BALCONY = 4000;
        /// <summary>
        /// 雨水管到地漏最大距离（阳台区域）
        /// </summary>
        public const double MAX_RAINPIPE_TO_BALCONYFLOORDRAIN = 2000;
        /// <summary>
        /// 新增场景新增排水管到洗衣机地漏最小距离（阳台区域）
        /// </summary>
        public const double MIN_DOWNSPOUT_TO_BALCONYFLOORDRAIN = 450;
        /// <summary>
        /// 冷凝管管到洗衣机最大距离（阳台区域）
        /// </summary>
        public const double MAX_CONDENSEPIPE_TO_WASHMACHINE = 2000;
        /// <summary>
        /// 洗衣机到台盆最大距离（阳台区域）
        /// </summary>
        public const double MAX_BALCONYWASHINGMACHINE_TO_BALCONYBASINLINE = 1300;
        /// <summary>
        /// 洗衣机地漏到排水管最大距离（阳台区域）
        /// </summary>
        public const double MAX_DOWNSPOUT_TO_BALCONYWASHINGFLOORDRAIN = 800;
        /// <summary>
        /// 新增场景洗衣机到雨水管最大距离（卫生间区域）
        /// </summary>
        public const double MAX__RAINPIPE_TO_WASHMACHINE = 550;
        /// <summary>
        /// 立管半径
        /// </summary>
        public const double COMMONRADIUS = 50;
        /// <summary>
        /// 阳台台盆到阳台最大距离
        /// </summary>
        public const double MAX_BALCONYBASIN_TO_BALCONY = 200;
        /// <summary>
        /// 阳台区域偏置距离
        /// </summary>
        public const double BALCONY_BUFFER_DISTANCE = 500;
        /// <summary>
        /// 阳台台盆到阳台最小距离
        /// </summary>
        public const double MIN_BALCONYBASIN_TO_BALCONY = 80;
        /// <summary>
        /// 标注Y方向偏移量
        /// </summary>
        public const double MAX_TAG_YPOSITION = 1260;
        /// <summary>
        /// 标注X方向偏移量
        /// </summary>
        public const double MAX_TAG_XPOSITION = 540;
        /// <summary>
        /// 标注线长度
        /// </summary>
        public const double MAX_TAG_LENGTH = 1660;
        /// <summary>
        /// 标注文字缩进
        /// </summary>
        public const double TEXT_INDENT = 40;
        /// <summary>
        /// 标注文字高度
        /// </summary>
        public const double TEXT_HEIGHT = 200;
        /// <summary>
        /// 侧入式雨水斗X方向缩进
        /// </summary>
        public const double SIDEWATERBUCKET_X_INDENT = 160;
        /// <summary>
        /// 侧入式雨水斗Y方向缩进
        /// </summary>
        public const double SIDEWATERBUCKET_Y_INDENT = 114;
        /// <summary>
        /// 管井到墙的偏移量
        /// </summary>
        public const double WELL_TO_WALL_OFFSET = 100;
        /// <summary>
        /// 卫生间管井间隔
        /// </summary>
        public const double TOILET_WELLS_INTERVAL = 200;
        /// <summary>
        /// 卫生间区域偏置距离
        /// </summary>
        public const double TOILET_BUFFER_DISTANCE = 500;
        /// <summary>
        /// 管井最大面积
        /// </summary>
        public const double WELLS_MAX_AREA = 0.15;
        /// <summary>
        /// 管井到马桶最短距离
        /// </summary>
        public const double MIN_WELL_TO_URINAL_DISTANCE = 60;
        /// <summary>
        /// 房间最大间隔
        /// </summary>
        public const double MAX_ROOM_INTERVAL = 200;
        /// <summary>
        /// 厨房区域偏置距离
        /// </summary>
        public const double KITCHEN_BUFFER_DISTANCE = 500;
        /// <summary>
        /// 卫生间到厨房最大距离
        /// </summary>
        public const double MAX_TOILET_TO_KITCHEN_DISTANCE = 3500;
        /// <summary>
        /// 特殊场景卫生间到厨房最大距离，应对新增距离远的情况
        /// </summary>
        public const double MAX_TOILET_TO_KITCHEN_DISTANCE1 = 5500;
        /// <summary>
        /// 厨房到雨水管最大距离
        /// </summary>
        public const double MAX_KITCHEN_TO_RAINPIPE_DISTANCE = 5500;
        /// <summary>
        /// 阳台到雨水管最大距离
        /// </summary>
        public const double MAX_BALCONY_TO_RAINPIPE_DISTANCE = 2000;
        /// <summary>
        /// 卫生间到冷凝管最大距离
        /// </summary>
        public const double MAX_TOILET_TO_CONDENSEPIPE_DISTANCE = 3000;
        /// <summary>
        /// 卫生间到地漏最大距离
        /// </summary>
        public const double MAX_TOILET_TO_FLOORDRAIN_DISTANCE = 2200;
        /// <summary>
        /// 新增特殊场景卫生间到地漏最大距离
        /// </summary>
        public const double MAX_TOILET_TO_FLOORDRAIN_DISTANCE2 = 2650;
        /// <summary>
        /// 新增特殊场景卫生间到地漏最大距离
        /// </summary>
        public const double MAX_TOILET_TO_FLOORDRAIN_DISTANCE1 = 4500;
        /// <summary>
        /// 阳台到设备平台最大距离
        /// </summary>
        public const double MAX_BALCONY_TO_DEVICEPLATFORM_DISTANCE = 6000;
        /// <summary>
        /// 阳台雨水管到阳台其他地漏最大距离
        /// </summary>
        public const double MAX_BALCONYRAINPIPE_TO_FLOORDRAIN_DISTANCE = 4900;
        /// <summary>
        /// 可并行相邻阳台最远距离
        /// </summary>
        public const double MAX_BALCONY_TO_BALCONY_DISTANCE = 4000;
        /// <summary>
        /// 厨房到阳台最大距离
        /// </summary>
        public const double MAX_KITCHEN_TO_BALCONY_DISTANCE = 9000;
        /// <summary>
        /// 设备平台区域最小面积
        /// </summary>
        public const double MIN_DEVICEPLATFORM_AREA = 0.4;
        /// <summary>
        /// 设备平台区域最大面积
        /// </summary>
        public const double MAX_DEVICEPLATFORM_AREA = 1.3;
        /// <summary>
        /// 基点圆区域最大面积
        /// </summary>
        public const double MAX_BASECIRCLE_AREA = 140;
        /// <summary>
        /// 角度公差
        /// </summary>
        public const double MAX_ANGEL_TOLERANCE = 0.1;
        /// <summary>
        /// 图层
        /// </summary>
        public const string AD_FLOOR_AREA = "AD-FLOOR-AREA";
        /// <summary>
        /// 雨水管标注
        /// </summary>
        public const string W_RAIN_NOTE = "W-RAIN-NOTE";
        /// <summary>
        /// 水管标注
        /// </summary>
        public static string W_DRAI_NOTE = "W-DRAI-NOTE";
        /// <summary>
        /// 雨水管立管
        /// </summary>
        public static string W_RAIN_EQPM = "W-RAIN-EQPM";
        /// <summary>
        /// 污水管立管
        /// </summary>
        public const string W_DRAI_EQPM = "W-DRAI-EQPM";
        /// <summary>
        /// 污废合流管道
        /// </summary>
        public const string W_DRAI_SEWA_PIPE = "W-DRAI-SEWA-PIPE"; 
        public const string W_DRAI_SEWA_PIPE1 = "W-DRAI-DOME-PIPE";
        /// <summary>
        /// 通气管
        /// </summary>
        public const string W_DRAI_VENT_PIPE = "W-DRAI-VENT-PIPE";
        /// <summary>
        /// 雨水管
        /// </summary>
        public const string W_RAIN_PIPE = "W-RAIN-PIPE";
        /// <summary>
        /// 地漏
        /// </summary>
        public const string W_DRAI_FLDR = "W-DRAI-FLDR";

        /// <summary>
        /// 楼层
        /// </summary>
        public const string STOREY_BLOCK_NAME = "楼层框定";
        public const string STOREY_LAYER_NAME = "AI-楼层框线";
        public const string STOREY_DYNAMIC_PROPERTY_TYPE = "楼层类型";
        public const string STOREY_ATTRIBUTE_VALUE_NUMBER = "楼层编号";
        public const string STOREY_DYNAMIC_PROPERTY_VALUE_ROOF_FLOOR = "大屋面";
        public const string STOREY_DYNAMIC_PROPERTY_VALUE_TOP_ROOF_FLOOR = "小屋面";
        public const string STOREY_DYNAMIC_PROPERTY_VALUE_STANDARD_FLOOR = "标准层";
        public const string STOREY_DYNAMIC_PROPERTY_VALUE_NON_STANDARD_FLOOR = "非标准层";
        public const string STOREY_DYNAMIC_PROPERTY_VALUE_NOT_STANDARD_FLOOR = "非标层";
    }
}
