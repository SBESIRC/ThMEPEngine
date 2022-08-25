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
        public static string Layer_RoomSuggestDist = "AI-地暖推荐间距";
        public static string Layer_RoomSetFrame = "AI-套型";
        public static string Layer_RoomSuggest = "AI-房间功能";
        public static string Layer_Coil = "AI-地暖盘管";

        public static string BlkName_WaterSeparator = "AI-集分水器";
        public static string BlkName_WaterSeparator2 = "AI-地暖分集水器";
        public static string BlkName_BathRadiator = "AI-散热器";
        public static string BlkName_RoomSuggest = "AI-回路指定";
        public static string BlkName_ShowRoute = "AI-地暖回路标注";

        public static Dictionary<string, string> BlkLayerDict = new Dictionary<string, string>() {
                                            {BlkName_RoomSuggest,Layer_RoomSuggest },
                                             { BlkName_ShowRoute, Layer_RoomSuggest}
                                                                };

        public static string BlkSettingAttrName_WaterSeparator = "设备宽度";
        public static string BlkSettingAttrName_Radiator_x1 = "水管连接点1 X";
        public static string BlkSettingAttrName_Radiator_y1 = "水管连接点1 Y";
        public static string BlkSettingAttrName_Radiator_x2 = "水管连接点2 X";
        public static string BlkSettingAttrName_Radiator_y2 = "水管连接点2 Y";

        public static string BlkSettingAttrName_RoomSuggest_Route = "回路编号";
        public static string BlkSettingAttrName_RoomSuggest_Dist = "盘管间距";
        public static string BlkSettingAttrName_RoomSuggest_Length = "回路长度";

        public static double DefaultValue_SuggestDist = 200;
    }
}
