using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model;
using ThMEPElectrical.Service;
using ThMEPEngineCore.Config;

namespace ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem
{
    public static class HandleVideoMonitoringRoomService
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
                if (column.ColumnName == roomAColumn)
                {
                    roomA = column.ColumnName;
                }
                else if (column.ColumnName == roomBColumn)
                {
                    roomB = column.ColumnName;
                }
                else if (column.ColumnName == floorColumn)
                {
                    floor = column.ColumnName;
                }
                else if (column.ColumnName == roomAEventsColumn)
                {
                    roomAEvent = column.ColumnName;
                }
                else if (column.ColumnName == roomBEventsColumn)
                {
                    roomBEvent = column.ColumnName;
                }
            }

            GTRooms.Clear();
            foreach (DataRow row in table.Rows)
            {
                var roomANames = RoomConfigTreeService.CalRoomLst(ThElectricalUIService.Instance.Parameter.RoomInfoMappingTree, row[roomA].ToString());
                var roomBNames = RoomConfigTreeService.CalRoomLst(ThElectricalUIService.Instance.Parameter.RoomInfoMappingTree, row[roomB].ToString());
                RoomInfoModel roomInfo = new RoomInfoModel();
                roomInfo.roomA = roomANames;
                roomInfo.roomB = roomBNames;
                roomInfo.roomAHandle = GetLayoutType(row[roomAEvent].ToString());
                roomInfo.roomBHandle = GetLayoutType(row[roomBEvent].ToString());
                if (row[floor].ToString() != "All")
                {
                    roomInfo.floorName = row[floor].ToString();
                }
                roomInfo.connectType = GetConnectType(row[roomB].ToString());

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
            else if (typeString == "沿线均布-枪式摄像机")
            {
                return LayoutType.AlongLineGunCamera;
            }
            else if (typeString == "沿线均布-云台摄像机")
            {
                return LayoutType.AlongLinePanTiltCamera;
            }
            else if (typeString == "沿线均布-半球摄像机")
            {
                return LayoutType.AlongLineDomeCamera;
            }
            else if (typeString == "沿线均布-枪式摄像机（带室内防护罩）")
            {
                return LayoutType.AlongLineGunCameraWithShield;
            }
            else if (typeString == "入口控制-枪式摄像机")
            {
                return LayoutType.EntranceGunCamera;
            }
            else if (typeString == "入口控制-枪式摄像机（翻转）")
            {
                return LayoutType.EntranceGunCamera;
            }
            else if (typeString == "入口覆盖-半球摄像机")
            {
                return LayoutType.EntranceDomeCamera;
            }
            else if (typeString == "入口覆盖-半球摄像机（翻转）")
            {
                return LayoutType.EntranceDomeCamera;
            }
            else if (typeString == "入口覆盖-枪式摄像机（带室内防护罩）")
            {
                return LayoutType.EntranceGunCameraWithShield;
            }
            else if (typeString == "入口覆盖-枪式摄像机（带室内防护罩）（翻转）")
            {
                return LayoutType.EntranceGunCameraWithShield;
            }
            else if (typeString == "入口覆盖-人脸识别摄像机")
            {
                return LayoutType.EntranceFaceRecognitionCamera;
            }
            else if (typeString == "入口覆盖-人脸识别摄像机（翻转）")
            {
                return LayoutType.EntranceFaceRecognitionCamera;
            }

            return LayoutType.Nothing;
        }
    }
}
