using ThMEPEngineCore;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerRoomService : ThMEPEngineCoreRoomService
    {
        /// <summary>
        /// 判断房间是否为不可布区域，若区域不可布，则返回true
        /// </summary>
        /// <param name="labels"></param>
        /// <returns></returns>
        public bool CannotLayoutArea(ThIfcRoom room)
        {
            var cannotLayoutArea = new List<string> 
            {
                "充电桩配电房",
                "电表间",
                "电气机房",
                "电子设备间",
                "公变配电房",
                "井道",
                "控制室",
                "楼梯间",
                "强弱电间",
                "涉水运动场所",
                "消防电梯",
                "消防水池",
                "专变配电房",
            };
            var specialArea = new List<string> 
            {
                "水管井",
                "水井",
                "水",
                "给水",
                "排水",
            };
            foreach (var roomName in room.Tags)
            {
                if (JudgeRoomType(Tree, roomName, cannotLayoutArea))
                {
                    if (specialArea.Contains(roomName) && (room.Boundary as Polyline).Area >= 1200000.0)
                    {
                        return false;
                    }
                    else if(roomName.Contains("消防电梯前室"))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
