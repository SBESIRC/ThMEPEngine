using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using ThMEPLighting.DSFEL.Model;

namespace ThMEPLighting.DSFEL
{
    public class DSFELConfigCommon
    {
        readonly string exitConfigUrl = ThCADCommon.SupportPath() + "\\疏散方向判断.xlsx";
        readonly string roomAColumn = "房间A";
        readonly string roomBColumn = "房间B";
        readonly string floorColumn = "楼层";
        readonly string openDoorColumn = "门的开启方向";
        readonly string EvacuationTag = "疏散指示";
        readonly string roomTagValue = "Y";
        public List<string> LayoutRoomTextConfig = new List<string>();
        public List<string> EvacuationExitAreaConfig = new List<string>();
        public List<string> EvacuationRoomConfig = new List<string>();
        public List<ExitRoomModel> GTRooms = new List<ExitRoomModel>();
        public List<string> floorNames = new List<string>();
        public DSFELConfigCommon(List<RoomTableTree> roomTree, ThEStoreys floor)
        {
            var configData = GetExcelContent(exitConfigUrl).Tables[0];
            HandleRoomInfo(configData, roomTree);

            //提取楼层信息
            HandleFloorInfo(floor);

            //获得所有需要疏散的房间
            EvacuationRoomConfig = roomTree.CalRoomLstByTag(EvacuationTag, roomTagValue);

            GTRooms = GTRooms.Where(x => floorNames.Any(y => y == x.floorName)).ToList();
        }

        /// <param name="floor"></param>
        /// <summary>
        /// 判断房间是否放置疏散指示灯
        /// *如何房间符合roomA的规则，则往roomB中疏散
        /// *如果该房间需要疏散，但是不在roomA中，则roomA中疏散
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

            var compareRoom = GTRooms.Where(x => x.roomA.Any(y => room.Tags.Any(z => RoomConfigTreeService.CompareRoom(y, z)))).ToList();
            if (compareRoom.Count <= 0)
            {
                if (GTRooms.Any(x => x.roomA.Any(y => otherRooms.SelectMany(z => z.Tags).Any(z => RoomConfigTreeService.CompareRoom(y, z))))
                    && !GTRooms.Any(x => x.roomB.Any(y => room.Tags.Any(z => RoomConfigTreeService.CompareRoom(y, z)))))
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (GTRooms.Any(x => x.roomB.Any(y => otherRooms.SelectMany(z => z.Tags).Any(z => RoomConfigTreeService.CompareRoom(y, z)))))
                {
                    return true;
                }
                return false;
            }
        }
        
        /// <summary>
        /// 处理楼层信息
        /// </summary>
        /// <param name="floor"></param>
        private void HandleFloorInfo(ThEStoreys floor)
        {
            floorNames.Clear();
            if(floor.StoreyType == EStoreyType.LargeRoof || floor.StoreyType == EStoreyType.SmallRoof)
            {
                floorNames.Add("屋面");
            }
            else
            {
                foreach (var storey in floor.Storeys)
                {
                    if (storey.Contains("B"))
                    {
                        floorNames.Add("地下");
                    }
                    else if (storey.Contains("1"))
                    {
                        floorNames.Add("1层");
                    }
                    else
                    {
                        floorNames.Add("2层以上");
                    }
                }
            }
        }

        /// <summary>
        /// 处理房间疏散信息
        /// </summary>
        /// <param name="table"></param>
        /// <param name="roomTree"></param>
        private void HandleRoomInfo(DataTable table, List<RoomTableTree> roomTree)
        {
            GTRooms.Clear();
            foreach (DataRow row in table.Rows)
            {
                var roomANames = roomTree.CalRoomLst(row[roomAColumn].ToString());
                var roomBNames = roomTree.CalRoomLst(row[roomBColumn].ToString());
                ExitRoomModel roomInfo = new ExitRoomModel();
                roomInfo.roomA = roomANames;
                roomInfo.roomB = roomBNames;
                roomInfo.openDoor = row[openDoorColumn].ToString();
                roomInfo.floorName = row[floorColumn].ToString();

                GTRooms.Add(roomInfo);
            }
        }

        /// <summary>
        /// 读取excel内容
        /// </summary>
        /// <returns></returns>
        private DataSet GetExcelContent(string path)
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            return excelSrevice.ReadExcelToDataSet(path, true);
        }
    }
}