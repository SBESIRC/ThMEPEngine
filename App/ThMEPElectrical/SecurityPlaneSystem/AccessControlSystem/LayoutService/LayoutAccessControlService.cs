using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService
{
    public class LayoutAccessControlService
    {
        public void LayoutFactory(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            HandleAccessControlRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.accessControlSystemTable);
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            List<AccessControlModel> models = new List<AccessControlModel>();
            foreach (var door in doors)
            {
                var bufferDoor = door.Buffer(5)[0] as Polyline;
                var connectRooms = getLayoutStructureService.GetNeedTHRooms(bufferDoor, rooms);
                if (connectRooms.Count <= 0)
                {
                    continue;
                }
                else if (connectRooms.Count == 1)
                {
                    var layoutType = CalNoCennectRoom(connectRooms[0]);
                    models.AddRange(DoLayout(layoutType, connectRooms[0], door, columns, walls));
                }
                else if (connectRooms.Count >= 2)
                {
                    if (CalTwoConnectRoom(connectRooms[0], connectRooms[1], out LayoutType layoutAType, out LayoutType layoutBType))
                    {
                        models.AddRange(DoLayout(layoutAType, connectRooms[0], door, columns, walls));
                        models.AddRange(DoLayout(layoutBType, connectRooms[1], door, columns, walls));
                    }
                    else if (CalTwoConnectRoom(connectRooms[1], connectRooms[0], out layoutAType, out layoutBType))
                    {
                        models.AddRange(DoLayout(layoutAType, connectRooms[1], door, columns, walls));
                        models.AddRange(DoLayout(layoutBType, connectRooms[0], door, columns, walls));
                    }
                }
            }
        }

        /// <summary>
        /// 执行布置
        /// </summary>
        /// <param name="layoutType"></param>
        private List<AccessControlModel> DoLayout(LayoutType layoutType, ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            List<AccessControlModel> models = new List<AccessControlModel>(); 
            switch (layoutType)
            {
                case LayoutType.Nothing:
                    break;
                case LayoutType.OneWayAuthentication:
                    LayoutOneWayAuthenticationService layoutOneWayAuthenticationService = new LayoutOneWayAuthenticationService();
                    models = layoutOneWayAuthenticationService.Layout(thRoom, door, columns, walls);
                    break;
                case LayoutType.TwoWayAuthentication:
                    LayoutTwoWayAuthenticationService layoutTwoWayAuthenticationService = new LayoutTwoWayAuthenticationService();
                    models = layoutTwoWayAuthenticationService.Layout(thRoom, door, columns, walls);
                    break;
                case LayoutType.OneWayVisitorTalk:
                    LayoutOneWayVisitorTalkService layoutOneWayVisitorTalkService = new LayoutOneWayVisitorTalkService();
                    models = layoutOneWayVisitorTalkService.Layout(thRoom, door, columns, walls);
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
        private LayoutType CalNoCennectRoom(ThIfcRoom connectRoom)
        {
            var roomAInfos = HandleAccessControlRoomService.GTRooms.Where(x => x.roomA.Contains(connectRoom.Name)).ToList();
            if (roomAInfos.Count > 0)
            {
                foreach (var roomAInfo in roomAInfos)
                {
                    if (roomAInfo.connectType == ConnectType.NoCennect || roomAInfo.connectType == ConnectType.AllConnect)
                    {
                        return roomAInfo.roomAHandle;
                    }
                }
            }
            var roomBInfos = HandleAccessControlRoomService.GTRooms.Where(x => x.roomA.Contains(connectRoom.Name)).ToList();
            if (roomBInfos.Count > 0)
            {
                foreach (var roomBInfo in roomBInfos)
                {
                    if (roomBInfo.connectType == ConnectType.NoCennect || roomBInfo.connectType == ConnectType.AllConnect)
                    {
                        return roomBInfo.roomBHandle;
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
        private bool CalTwoConnectRoom(ThIfcRoom roomA, ThIfcRoom roomB, out LayoutType roomAType, out LayoutType roomBType)
        {
            roomAType = LayoutType.Nothing;
            roomBType = LayoutType.Nothing;

            bool findRule = false;
            var roomAInfos = HandleAccessControlRoomService.GTRooms.Where(x => x.roomA.Contains(roomA.Name)).ToList();
            if (roomAInfos.Count > 0)
            {
                foreach (var roomAInfo in roomAInfos)
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

            return findRule;
        }
    }
}
