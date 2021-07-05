using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem
{
    public static class HandleAccessControlRoomService
    {
        static string roomAColumn = "房间A";
        static string roomBColumn = "房间B";
        static string floorColumn = "楼层";
        static string roomAEventsColumn = "房间A采取的措施";
        static string roomBEventsColumn = "房间B采取的措施";

        public static List<RoomInfoModel> GTRooms = new List<RoomInfoModel>();

        public static void HandleRoomInfo(DataTable table)
        {
            string roomA = null;
            string roomB = null;
            string floor = null;
            string roomAEvent = null;
            string roomBEvent = null;
            foreach (DataColumn column in table.Columns)
            {
                if (column.ColumnName.Contains(roomAColumn))
                {
                    roomA = column.ColumnName;
                }
                else if (column.ColumnName.Contains(roomBColumn))
                {
                    roomB = column.ColumnName;
                }
                else if (column.ColumnName.Contains(floorColumn))
                {
                    floor = column.ColumnName;
                }
                else if (column.ColumnName.Contains(roomAEventsColumn))
                {
                    roomAEvent = column.ColumnName;
                }
                else if (column.ColumnName.Contains(roomBEventsColumn))
                {
                    roomBEvent = column.ColumnName;
                }
            }

            foreach (DataRow row in table.Rows)
            {
                var roomANames = CommonRoomHandleService.HandleRoom(row[roomA].ToString());
                var roomBNames = CommonRoomHandleService.HandleRoom(row[roomB].ToString());
                RoomInfoModel roomInfo = new RoomInfoModel();
                roomInfo.roomA = roomANames;
                roomInfo.roomB = roomBNames;
                roomInfo.roomAHandle = GetLayoutType(row[roomAEvent].ToString());
                roomInfo.roomBHandle = GetLayoutType(row[roomBEvent].ToString());
                if (row[floor].ToString() != "All")
                {
                    roomInfo.floorName = row[floor].ToString();
                }
                roomInfo.connectType = GetConnectType(row[roomBEvent].ToString());

                GTRooms.Add(roomInfo);
            }
        }
        
        /// <summary>
        /// 判断房间应该的连接类型
        /// </summary>
        /// <param name="roomB"></param>
        /// <returns></returns>
        private static ConnectType GetConnectType(string roomB)
        {
            if (roomB.Contains("无"))
            {
                return ConnectType.NoCennect;
            }
            else if (roomB.Contains("All"))
            {
                return ConnectType.AllConnect;
            }

            return ConnectType.Normal;
        }

        /// <summary>
        /// 获取布置类型
        /// </summary>
        /// <param name="typeString"></param>
        /// <returns></returns>
        private static LayoutType GetLayoutType(string typeString)
        {
            if (typeString.Contains("无"))
            {
                return LayoutType.Nothing;
            }
            else if (typeString.Contains("单向认证进入"))
            {
                return LayoutType.OneWayAuthentication;
            }
            else if (typeString.Contains("双向认证进入"))
            {
                return LayoutType.TwoWayAuthentication;
            }
            else if (typeString.Contains("单向访客对讲"))
            {
                return LayoutType.OneWayVisitorTalk;
            }

            return LayoutType.Nothing;
        }
    }
}
