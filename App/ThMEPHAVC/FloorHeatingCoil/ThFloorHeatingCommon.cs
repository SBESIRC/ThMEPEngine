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
        public static string BlkName_WaterSeparator = "AI-集分水器";

    }
}
