using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem
{
    public static class HandleGuardTourRoomService
    {
        static string roomNameColumn = "房间名称";
        static string insideRoomColumn = "房间内";
        static string outsideRoomColumn = "房间外";
        public static List<string> GTRooms = new List<string>()
        {
            "车库",
        };

        public static List<string> otherRooms = new List<string>()
        {
            "楼梯",
        };

        public static void HandleRoomInfo(DataTable table)
        {
            string columnName = null;
            string insideRoom = null;
            string outsideRoom = null;
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
            }

            if (columnName != null)
            {
                GTRooms.Clear();
                otherRooms.Clear();
                foreach (DataRow row in table.Rows)
                {
                    var roomNames = CommonRoomHandleService.HandleRoom(row[columnName].ToString());
                    if (row[insideRoom].ToString() == "是")
                    {
                        GTRooms.AddRange(roomNames);
                    }
                    if (row[outsideRoom].ToString() == "是")
                    {
                        otherRooms.AddRange(roomNames);
                    }
                }
            }
        }
    }
}
