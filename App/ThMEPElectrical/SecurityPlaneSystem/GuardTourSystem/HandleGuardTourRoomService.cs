using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Service;
using ThMEPEngineCore.Config;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem
{
    public static class HandleGuardTourRoomService
    {
        static string roomNameColumn = "房间名称";
        static string insideRoomColumn = "房间内";
        static string outsideRoomColumn = "房间外";
        static string floorColumn = "楼层";
        public static List<RoomInfoModel> GTRooms = new List<RoomInfoModel>();      //房间内是否需要布置
        public static List<RoomInfoModel> otherRooms = new List<RoomInfoModel>();   //房间外是否要布置

        public static void HandleRoomInfo(DataTable table)
        {
            string columnName = null;
            string insideRoom = null;
            string outsideRoom = null;
            string floor = null;
            foreach (DataColumn column in table.Columns)
            {
                if (column.ColumnName.Contains(roomNameColumn))
                {
                    columnName = column.ColumnName;
                }
                else if (column.ColumnName.Contains(insideRoomColumn))
                {
                    insideRoom = column.ColumnName;
                }
                else if (column.ColumnName.Contains(outsideRoomColumn))
                {
                    outsideRoom = column.ColumnName;
                }
                else if (column.ColumnName.Contains(floorColumn))
                {
                    floor = column.ColumnName;
                }
            }

            GTRooms.Clear();
            otherRooms.Clear();
            foreach (DataRow row in table.Rows)
            {
                if (true == (row[0] as bool?).Value)
                {
                    var roomNames = RoomConfigTreeService.CalRoomLst(ThElectricalUIService.Instance.Parameter.RoomInfoMappingTree, row[columnName].ToString());
                    RoomInfoModel roomInfoModel = new RoomInfoModel();
                    roomInfoModel.roomName = roomNames;
                    if (row[floor].ToString() != "All")
                    {
                        roomInfoModel.floor = row[floor].ToString();
                    }
                    if (row[insideRoom].ToString() == "是")
                    {
                        GTRooms.Add(roomInfoModel);
                    }
                    if (row[outsideRoom].ToString() == "是")
                    {
                        otherRooms.Add(roomInfoModel);
                    }
                }
            }
        }
    }

    public class RoomInfoModel
    {
        /// <summary>
        /// 房间名
        /// </summary>
        public List<string> roomName { get; set; }

        /// <summary>
        /// 楼层名
        /// </summary>
        public string floor { get; set; }
    }
}
