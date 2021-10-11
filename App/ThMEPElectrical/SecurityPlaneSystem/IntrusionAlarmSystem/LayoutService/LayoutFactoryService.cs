using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutFactoryService
    {
        LayoutDisabledToiletService layoutDisabledToiletService = new LayoutDisabledToiletService();
        LayoutEmergencyAlarmService layoutEmergencyAlarmService = new LayoutEmergencyAlarmService();
        LayoutMonitoringService layoutMonitoringService = new LayoutMonitoringService();

        public List<LayoutModel> LayoutFactory(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, ThEStoreys floor)
        {
            HandleIntrusionAlarmRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.intrusionAlarmSystemTable);
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            List<LayoutModel> models = new List<LayoutModel>();
            foreach (var door in doors)
            {
                var bufferDoor = door.Buffer(15)[0] as Polyline;
                var connectRooms = getLayoutStructureService.GetNeedTHRooms(bufferDoor, rooms);
                if (connectRooms.Count <= 0)
                {
                    continue;
                }
                else if (connectRooms.Count == 1)
                {
                    var layoutType = CalNoCennectRoom(connectRooms[0], floor.StoreyTypeString);
                    models.AddRange(DoLayout(layoutType, connectRooms[0], door, columns, walls));
                }
                else if (connectRooms.Count >= 2)
                {
                    if (CalTwoConnectRoom(connectRooms[0], connectRooms[1], floor.StoreyTypeString, out LayoutType layoutAType, out LayoutType layoutBType))
                    {
                        models.AddRange(DoLayout(layoutAType, connectRooms[0], door, columns, walls));
                        models.AddRange(DoLayout(layoutBType, connectRooms[1], door, columns, walls));
                    }
                    else if (CalTwoConnectRoom(connectRooms[1], connectRooms[0], floor.StoreyTypeString, out layoutAType, out layoutBType))
                    {
                        models.AddRange(DoLayout(layoutAType, connectRooms[1], door, columns, walls));
                        models.AddRange(DoLayout(layoutBType, connectRooms[0], door, columns, walls));
                    } 
                }
            }

            //控制每个房间只需要一个控制器
            var controller = models.Where(x => x is ControllerModel).ToList();
            var classifyController = controller.GroupBy(x => x.Room).Select(x => x.First()).ToList();
            List<LayoutModel> resModels = models.Except(controller).ToList();
            resModels.AddRange(classifyController);
            return resModels;
        }

        /// <summary>
        /// 执行布置
        /// </summary>
        /// <param name="layoutType"></param>
        private List<LayoutModel> DoLayout(LayoutType layoutType, ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            List<LayoutModel> models = new List<LayoutModel>();
            switch (layoutType)
            {
                case LayoutType.Nothing:
                    break;
                case LayoutType.DisabledToiletAlarm:
                    models = layoutDisabledToiletService.Layout(thRoom, door, columns, walls);
                    break;
                case LayoutType.EmergencyAlarm:
                    models = layoutEmergencyAlarmService.Layout(thRoom, door, columns, walls);
                    break;
                case LayoutType.InfraredWallMounting:
                    models = layoutMonitoringService.WallMountingLayout(thRoom, door, columns, walls, true);
                    break;
                case LayoutType.InfraredHoisting:
                    models = layoutMonitoringService.HoistingLayout(thRoom, door, columns, walls, true);
                    break;
                case LayoutType.DoubleWallMounting:
                    models = layoutMonitoringService.WallMountingLayout(thRoom, door, columns, walls, false);
                    break;
                case LayoutType.DoubleHositing:
                    models = layoutMonitoringService.HoistingLayout(thRoom, door, columns, walls, false);
                    break;
                default:
                    break;
            }

            return models;
        }

        /// <summary>
        /// 计算门只连接一个房间时的布置方式
        /// </summary>
        /// <param name="connectRoom"></param>
        /// <returns></returns>
        public LayoutType CalNoCennectRoom(ThIfcRoom connectRoom, string floor)
        {
            var roomAInfos = HandleIntrusionAlarmRoomService.GTRooms.Where(x => x.connectType == ConnectType.AllConnect || x.connectType == ConnectType.NoCennect).Where(x => connectRoom.Tags.Any(y => x.roomA.Any(z => RoomConfigTreeService.CompareRoom(z, y)))).ToList();
            if (roomAInfos.Count > 0)
            {
                foreach (var roomAInfo in roomAInfos)
                {
                    if (string.IsNullOrEmpty(roomAInfo.floorName) || roomAInfo.floorName == floor)
                    {
                        if (roomAInfo.connectType == ConnectType.NoCennect || roomAInfo.connectType == ConnectType.AllConnect)
                        {
                            return roomAInfo.roomAHandle;
                        }
                    }
                }
            }
            var roomBInfos = HandleIntrusionAlarmRoomService.GTRooms.Where(x => x.connectType == ConnectType.AllConnect || x.connectType == ConnectType.NoCennect).Where(x => connectRoom.Tags.Any(y => x.roomB.Any(z => RoomConfigTreeService.CompareRoom(z, y)))).ToList();
            if (roomBInfos.Count > 0)
            {
                foreach (var roomBInfo in roomBInfos)
                {
                    if (string.IsNullOrEmpty(roomBInfo.floorName) || roomBInfo.floorName == floor)
                    {
                        if (roomBInfo.connectType == ConnectType.NoCennect || roomBInfo.connectType == ConnectType.AllConnect)
                        {
                            return roomBInfo.roomBHandle;
                        }
                    }
                }
            }

            return LayoutType.Nothing;
        }

        /// <summary>
        /// 计算门两边都连接房间的时候的布置方式
        /// </summary>
        /// <param name="roomA"></param>
        /// <param name="roomB"></param>
        /// <param name="roomAType"></param>
        /// <param name="roomBType"></param>
        /// <returns></returns>
        public bool CalTwoConnectRoom(ThIfcRoom roomA, ThIfcRoom roomB, string floor, out LayoutType roomAType, out LayoutType roomBType)
        {
            roomAType = LayoutType.Nothing;
            roomBType = LayoutType.Nothing;

            bool findRule = false;
            var roomAInfos = HandleIntrusionAlarmRoomService.GTRooms.Where(x => roomA.Tags.Any(y => x.roomA.Any(z => RoomConfigTreeService.CompareRoom(z, y)))).ToList();
            if (roomAInfos.Count > 0)
            {
                foreach (var roomAInfo in roomAInfos)
                {
                    if (string.IsNullOrEmpty(roomAInfo.floorName) || roomAInfo.floorName == floor)
                    {
                        if (roomAInfo.connectType == ConnectType.AllConnect)
                        {
                            roomAType = roomAInfo.roomAHandle;
                            roomBType = roomAInfo.roomBHandle;
                            findRule = true;
                        }
                        else if (roomAInfo.connectType == ConnectType.Normal)
                        {
                            if (roomAInfo.roomB.Contains(roomB.Name))
                            {
                                roomAType = roomAInfo.roomAHandle;
                                roomBType = roomAInfo.roomBHandle;
                                findRule = true;
                            }
                        }
                    }
                }
            }

            return findRule;
        }
    }
}
