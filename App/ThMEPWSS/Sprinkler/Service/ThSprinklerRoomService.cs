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
        public override bool CannotLayoutArea(ThIfcRoom room)
        {
            var cannotLayoutArea = new List<string> { 
                "室外区域", 
                "井道", 
                "控制室", 
                "电气机房", 
                "电子设备间", 
                "游泳池", 
                "溜冰场", 
                "滑雪场" };
            var specialArea = new List<string> { 
                "水管井", 
                "水井", 
                "水", 
                "给水", 
                "排水" };
            foreach (var roomName in room.Tags)
            {
                if (JudgeRoomType(Tree, roomName, cannotLayoutArea))
                {
                    if(specialArea.Contains(roomName) && (room.Boundary as Polyline).Area >= 1200000)
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
