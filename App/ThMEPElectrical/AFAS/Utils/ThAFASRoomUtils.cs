using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Config;

namespace ThMEPElectrical.AFAS.Utils
{
    public class ThAFASRoomUtils
    {
        /// <summary>
        /// 读取房间配置表
        /// </summary>
        public static List<RoomTableTree> ReadRoomConfigTable(string roomConfigUrl)
        {
            var roomTableConfig = new List<RoomTableTree>();
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var roomNameControl = "房间名称处理";
            var table = dataSet.Tables[roomNameControl];
            if (table != null)
            {
                roomTableConfig = RoomConfigTreeService.CreateRoomTree(table);
            }

            return roomTableConfig;
        }

        public static bool IsRoom(List<RoomTableTree> roomTableTree, string name, string standardName)
        {
            var bReturn = false;
            var standardNameList = RoomConfigTreeService.CalRoomLst(roomTableTree, standardName);

            //if (nameList.Contains(name))
            //{
            //    bReturn = true;
            //}
            var names = name.Split(',');
            foreach (var n in names)
            {
              var isRoom=  RoomConfigTreeService.CompareRoom(standardNameList, name);
                if (isRoom == true)
                {
                    bReturn = true;
                    break;
                }
            }

            return bReturn;
        }

        public static bool IsRoom(List<RoomTableTree> roomTableTree, string name, List<string> standardName)
        {
            var bReturn = false;
            var standardNameList = RoomConfigTreeService.CalRoomLst(roomTableTree, standardName);

            var names = name.Split(',');
            foreach (var n in names)
            {
                var isRoom = RoomConfigTreeService.CompareRoom(standardNameList, name);
                if (isRoom == true)
                {
                    bReturn = true;
                    break;
                }
            }

            return bReturn;
        }



    }
}
