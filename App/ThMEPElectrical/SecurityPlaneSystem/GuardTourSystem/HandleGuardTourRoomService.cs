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
        static string stairRoomColumn = "楼梯间";
        public static List<string> GTRoom = new List<string>()
        {
            "车库",
        };

        public static List<string> StairRoom = new List<string>()
        {
            "楼梯",
        };

        public static void HandleRoomInfo(DataTable table)
        {
            string columnName = null;
            foreach (DataColumn column in table.Columns)
            {
                if (column.ColumnName.Contains(roomNameColumn))
                {
                    columnName = column.ColumnName;
                    break;
                }
            }

            if (columnName != null)
            {
                GTRoom.Clear();
                foreach (DataRow row in table.Rows)
                {
                    GTRoom.AddRange(CommonRoomHandleService.HandleRoom(row[columnName].ToString()));
                }
            }

            StairRoom.Clear();
            StairRoom.AddRange(CommonRoomHandleService.HandleRoom(stairRoomColumn));
        }
    }
}
