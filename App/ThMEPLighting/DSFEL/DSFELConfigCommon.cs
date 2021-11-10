using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Model;

namespace ThMEPLighting.DSFEL
{
    public class DSFELConfigCommon
    {
        /*LayoutRoomText中的符合这个房间名称的房间，全部都往EvacuationExitArea中的房间疏散
         *如果配置表中符合安装疏散指示灯的房间，但是不在LayoutRoomText中的房间，则全部往LayoutRoomText中的房间疏散
         */
        private static List<string> LayoutRoomText = new List<string>() {
            "走道",
            "走廊",
            "连廊",
            "过道",
            "外廊",
            "前室",   //前室、合用前室、消防电梯前室、防烟前室
            "楼梯间",
            "避难",   //避难层、避难间、避难走道
            "非机动车库",
            "电梯厅",
        };

        private static List<string> EvacuationExitArea = new List<string>() {
            "楼梯间",
            "避难",  //避难层、避难间、避难走道
            "前室",  //合用前室
        };

        readonly string EvacuationTag = "疏散指示";
        public List<string> LayoutRoomTextConfig = new List<string>();
        public List<string> EvacuationExitAreaConfig = new List<string>();
        public List<string> EvacuationRoomConfig = new List<string>();
        public DSFELConfigCommon(List<RoomTableTree> roomTree)
        {
            EvacuationRoomConfig = roomTree.CalRoomLstByTag(EvacuationTag);
            LayoutRoomTextConfig = roomTree.CalRoomLst(LayoutRoomText);
            EvacuationExitAreaConfig = roomTree.CalRoomLst(EvacuationExitArea);
        }

        /// <summary>
        /// 判断房间是否放置疏散指示灯
        /// </summary>
        /// <param name="room"></param>
        /// <param name="otherRooms"></param>
        /// <returns></returns>
        public bool CheckExitRoom(ThIfcRoom room, List<ThIfcRoom> otherRooms)
        {
            if (!EvacuationRoomConfig.Any(x => room.Tags.Any(y => RoomConfigTreeService.CompareRoom(x, y))))
            {
                return false;
            }

            if (LayoutRoomTextConfig.Any(x => room.Tags.Any(y => RoomConfigTreeService.CompareRoom(x, y))))
            {
                if (EvacuationExitAreaConfig.Any(x => otherRooms.SelectMany(z => z.Tags).Any(y => RoomConfigTreeService.CompareRoom(x, y))))
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (LayoutRoomTextConfig.Any(x => otherRooms.SelectMany(z => z.Tags).Any(y => RoomConfigTreeService.CompareRoom(x, y))))
                {
                    return true;
                }
                return false;
            }
        }
    }
}
