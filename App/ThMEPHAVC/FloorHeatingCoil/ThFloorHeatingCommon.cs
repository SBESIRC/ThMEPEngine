using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class ThFloorHeatingCommon
    {
        public static List<string> ObstacleTypeList = new List<string>(){ "坐便器" ,
                                                                        "单盆洗手台" ,
                                                                        "厨房洗涤盆" ,
                                                                        "淋浴器" ,
                                                                        "洗衣机" ,
                                                                        "拖把池" ,
                                                                        "浴缸" ,
                                                                        };

        public static string Layer_RoomSeparate = "AI-地暖伸缩缝";
        public static string Layer_Obstacle = "AI-地暖障碍物";
        public static string Layer_RoomSetFrame = "AI-套型";//!!!!!!
        public static string Layer_RoomSuggest = "AI-地暖推荐间距";
        public static string Layer_WaterSeparator = "H-EQUP-HEAT";
        public static string Layer_ShowRoute = "H-PIPE-DIMS";
        public static string Layer_Coil = "H-PIPE-FHS";

        public static string BlkName_WaterSeparator = "AI-地暖分集水器";
        public static string BlkName_BathRadiator = "AI-散热器";
        public static string BlkName_RoomSuggest = "AI-地暖回路指定";
        public static string BlkName_ShowRoute = "AI-地暖回路标注";

        public static Dictionary<string, string> BlkLayerDict = new Dictionary<string, string>() {
                                            {BlkName_RoomSuggest,Layer_RoomSuggest },
                                             {BlkName_ShowRoute, Layer_ShowRoute},
                                             {BlkName_WaterSeparator,Layer_WaterSeparator},
                                             {BlkName_BathRadiator,Layer_WaterSeparator },
                                                                };

        public static string BlkSettingAttrName_WaterSeparator = "回路数";
        public static string BlkSettingAttrName_Radiator_x1 = "水管连接点1 X";
        public static string BlkSettingAttrName_Radiator_y1 = "水管连接点1 Y";
        public static string BlkSettingAttrName_Radiator_x2 = "水管连接点2 X";
        public static string BlkSettingAttrName_Radiator_y2 = "水管连接点2 Y";

        public static string BlkSettingAttrName_RoomSuggest_Route = "回路编号";
        public static string BlkSettingAttrName_RoomSuggest_Dist = "盘管间距";
        public static string BlkSettingAttrName_RoomSuggest_Length = "回路长度";

        public static double DefaultValue_SuggestDist = 200;

        public static string Error_privateOneDoor = "住宅模式暂不支持分集水器所在房间有多个门";
    }
}
