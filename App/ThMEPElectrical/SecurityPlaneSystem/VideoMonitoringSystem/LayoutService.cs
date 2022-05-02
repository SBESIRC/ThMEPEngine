using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService;
using ThMEPEngineCore.Model;
using System.Linq;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;
using ThMEPEngineCore.Config;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.VMExitLayoutService;
using System;
using ThMEPEngineCore.AFASRegion.Utls;

namespace ThMEPElectrical.VideoMonitoringSystem
{
    public class LayoutService
    {
        LayoutVideo layoutVideo = new LayoutVideo();
        LayoutVideoByLine layoutVideoByLine = new LayoutVideoByLine();
        public List<LayoutModel> LayoutFactory(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, List<Line> lanes, ThEStoreys floor)
        {
            //填充数据
            HandleVideoMonitoringRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.videoMonitoringSystemTable);

            //设置参数
            layoutVideo.blindArea = ThElectricalUIService.Instance.Parameter.videoBlindArea;
            layoutVideo.layoutRange = ThElectricalUIService.Instance.Parameter.videaMaxArea;
            layoutVideoByLine.distance = ThElectricalUIService.Instance.Parameter.videoDistance;
            List<LayoutModel> models = new List<LayoutModel>();
            Dictionary<ThIfcRoom, List<LayoutModel>> vmInfos = new Dictionary<ThIfcRoom, List<LayoutModel>>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();

            //沿线均布布置模式
            var layoutRooms = GetLayoutRooms(rooms);
            foreach (var lRoom in layoutRooms)
            {
                var layoutVByLine = layoutVideoByLine.Layout(lanes, doors, lRoom.Key);
                models.AddRange(CreateModels(lRoom.Value, layoutVByLine));
            }

            walls.AddRange(doors);  //门也当作墙障碍
            //入口控制布置模式
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
                    var pts = door.GetAllLinesInPolyline().OrderByDescending(o => o.Length).Take(2).Select(o => new Point3d((o.StartPoint.X + o.EndPoint.X)/2, (o.StartPoint.Y + o.EndPoint.Y) / 2, 0)).ToList();
                    if (connectRooms.First().Boundary is Polyline polyline)
                    {
                        if (pts.All(o => polyline.Contains(o)))
                        {
                            continue;
                        }
                    }
                    if (connectRooms.First().Boundary is MPolygon mPolygon)
                    {
                        if (pts.All(o => mPolygon.Contains(o)))
                        {
                            continue;
                        }
                    }
                    var layoutType = CalNoCennectRoom(connectRooms[0], floor.StoreyTypeString);
                    SetVideaoInRoom(vmInfos, connectRooms[0], DoLayout(layoutType, connectRooms[0], door, columns, walls, doors));
                }
                else if (connectRooms.Count >= 2)
                {
                    if (CalTwoConnectRoom(connectRooms[0], connectRooms[1], floor.StoreyTypeString, out LayoutType layoutAType, out LayoutType layoutBType))
                    {
                        SetVideaoInRoom(vmInfos, connectRooms[0], DoLayout(layoutAType, connectRooms[0], door, columns, walls, doors));
                        SetVideaoInRoom(vmInfos, connectRooms[1], DoLayout(layoutBType, connectRooms[1], door, columns, walls, doors));
                    }
                    if (CalTwoConnectRoom(connectRooms[1], connectRooms[0], floor.StoreyTypeString, out layoutAType, out layoutBType))
                    {
                        SetVideaoInRoom(vmInfos, connectRooms[1], DoLayout(layoutAType, connectRooms[1], door, columns, walls, doors));
                        SetVideaoInRoom(vmInfos, connectRooms[0], DoLayout(layoutBType, connectRooms[0], door, columns, walls, doors));
                    }
                }
            }

            //调整房间内的摄像头
            LayoutVideaoAdjust videaoAdjust = new LayoutVideaoAdjust();
            models.AddRange(videaoAdjust.ClearClostVideao(vmInfos));

            return models.Where(x => !x.layoutPt.IsEqualTo(Point3d.Origin)).ToList();
        }

        /// <summary>
        /// 将布置的摄像头根据房间分类
        /// </summary>
        /// <param name="vmInfos"></param>
        /// <param name="room"></param>
        /// <param name="models"></param>
        private void SetVideaoInRoom(Dictionary<ThIfcRoom, List<LayoutModel>> vmInfos, ThIfcRoom room, List<LayoutModel> models)
        {
            if (models.Count <= 0)
            {
                return;
            }
            if (vmInfos.Keys.Contains(room))
            {
                vmInfos[room].AddRange(models);
            }
            else
            {
                vmInfos.Add(room, models);
            }
        }

        /// <summary>
        /// 执行布置
        /// </summary>
        /// <param name="layoutType"></param>
        /// <param name="thRoom"></param>
        /// <param name="door"></param>
        /// <param name="doors"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private List<LayoutModel> DoLayout(LayoutType layoutType, ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls, List<Polyline> doors)
        {
            var layoutModel = new List<LayoutModel>();
            switch (layoutType)
            {
                case LayoutType.EntranceGunCamera:
                case LayoutType.EntranceDomeCamera:
                case LayoutType.EntranceGunCameraWithShield:
                case LayoutType.EntranceFaceRecognitionCamera:
                    layoutModel.Add(layoutVideo.Layout(thRoom, door, walls, columns, doors));
                    break;
                case LayoutType.EntranceGunCameraFlip:
                case LayoutType.EntranceDomeCameraFlip:
                case LayoutType.EntranceGunCameraWithShieldFlip:
                case LayoutType.EntranceFaceRecognitionCameraFlip:
                    layoutModel.Add(layoutVideo.Layout(thRoom, door, walls, columns, doors));
                    GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
                    var doorPt = getLayoutStructureService.GetDoorCenterPt(door);
                    layoutModel.ForEach(x =>
                    {
                        x.layoutDir = -x.layoutDir;
                        x.layoutPt = new Point3d((x.layoutPt.X + doorPt.X) / 2, (x.layoutPt.Y + doorPt.Y) / 2, 0);
                    });
                    break;
                case LayoutType.AlongLineGunCamera:
                case LayoutType.AlongLinePanTiltCamera:
                case LayoutType.AlongLineDomeCamera:
                case LayoutType.AlongLineGunCameraWithShield:
                case LayoutType.Nothing:
                default:
                    break;
            }
            return CreateModels(layoutType, layoutModel);
        }

        /// <summary>
        /// 创建布置模型
        /// </summary>
        /// <param name="layoutType"></param>
        /// <param name="layoutModels"></param>
        /// <returns></returns>
        private List<LayoutModel> CreateModels(LayoutType layoutType, List<LayoutModel> layoutModels)
        {
            List<LayoutModel> resModels = new List<LayoutModel>();
            switch (layoutType)
            {
                case LayoutType.EntranceGunCameraFlip:
                case LayoutType.EntranceGunCamera:
                case LayoutType.AlongLineGunCamera:
                    layoutModels.ForEach(x =>
                    {
                        GunCameraModel gunCameraModel = new GunCameraModel();
                        gunCameraModel.layoutPt = x.layoutPt;
                        gunCameraModel.layoutDir = x.layoutDir;
                        resModels.Add(gunCameraModel);
                    });
                    break;
                case LayoutType.AlongLinePanTiltCamera:
                    layoutModels.ForEach(x =>
                    {
                        PanTiltCameraModel panTiltCameraModel = new PanTiltCameraModel();
                        panTiltCameraModel.layoutPt = x.layoutPt;
                        panTiltCameraModel.layoutDir = x.layoutDir;
                        resModels.Add(panTiltCameraModel);
                    });
                    break;
                case LayoutType.EntranceDomeCameraFlip:
                case LayoutType.EntranceDomeCamera:
                case LayoutType.AlongLineDomeCamera:
                    layoutModels.ForEach(x =>
                    {
                        DomeCameraModel domeCameraModel = new DomeCameraModel();
                        domeCameraModel.layoutPt = x.layoutPt;
                        domeCameraModel.layoutDir = x.layoutDir;
                        resModels.Add(domeCameraModel);
                    });
                    break;
                case LayoutType.EntranceGunCameraWithShieldFlip:
                case LayoutType.EntranceGunCameraWithShield:
                case LayoutType.AlongLineGunCameraWithShield:
                    layoutModels.ForEach(x =>
                    {
                        GunCameraWithShieldModel gunCameraWithShieldModel = new GunCameraWithShieldModel();
                        gunCameraWithShieldModel.layoutPt = x.layoutPt;
                        gunCameraWithShieldModel.layoutDir = x.layoutDir;
                        resModels.Add(gunCameraWithShieldModel);
                    });
                    break;
                case LayoutType.EntranceFaceRecognitionCameraFlip:
                case LayoutType.EntranceFaceRecognitionCamera:
                    layoutModels.ForEach(x =>
                    {
                        FaceRecognitionCameraModel faceRecognitionCameraModel = new FaceRecognitionCameraModel();
                        faceRecognitionCameraModel.layoutPt = x.layoutPt;
                        faceRecognitionCameraModel.layoutDir = x.layoutDir;
                        resModels.Add(faceRecognitionCameraModel);
                    });
                    break;
                case LayoutType.Nothing:
                default:
                    break;
            }

            return resModels;
        }

        /// <summary>
        /// 计算门只连接一个房间时的布置方式
        /// </summary>
        /// <param name="connectRoom"></param>
        /// <returns></returns>
        private LayoutType CalNoCennectRoom(ThIfcRoom connectRoom, string floor)
        {
            var roomAInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x=>x.connectType == ConnectType.AllConnect || x.connectType == ConnectType.NoCennect).Where(x => connectRoom.Tags.Any(y => x.roomA.Any(z => RoomConfigTreeService.CompareRoom(z, y)))).ToList();
            if (roomAInfos.Count > 0)
            {
                foreach (var roomAInfo in roomAInfos)
                {
                    if (string.IsNullOrEmpty(roomAInfo.floorName) || roomAInfo.floorName == floor)
                    {
                        if (roomAInfo.connectType == ConnectType.NoCennect || roomAInfo.connectType == ConnectType.AllConnect || CheckEntranceLayout(roomAInfo.roomAHandle))
                        {
                            return roomAInfo.roomAHandle;
                        }
                    }
                }
            }
            var roomBInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => x.connectType == ConnectType.AllConnect || x.connectType == ConnectType.NoCennect).Where(x => connectRoom.Tags.Any(y => x.roomB.Any(z => RoomConfigTreeService.CompareRoom(z, y)))).ToList();
            if (roomBInfos.Count > 0)
            {
                foreach (var roomBInfo in roomBInfos)
                {
                    if (string.IsNullOrEmpty(roomBInfo.floorName) || roomBInfo.floorName == floor)
                    {
                        if (roomBInfo.connectType == ConnectType.NoCennect || roomBInfo.connectType == ConnectType.AllConnect || CheckEntranceLayout(roomBInfo.roomAHandle))
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
        private bool CalTwoConnectRoom(ThIfcRoom roomA, ThIfcRoom roomB, string floor, out LayoutType roomAType, out LayoutType roomBType)
        {
            roomAType = LayoutType.Nothing;
            roomBType = LayoutType.Nothing;

            bool findRule = false;
            var roomAInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => roomA.Tags.Any(y => x.roomA.Any(z => RoomConfigTreeService.CompareRoom(z, y)))).ToList();
            foreach (var roomAInfo in roomAInfos)
            {
                if (string.IsNullOrEmpty(roomAInfo.floorName) || roomAInfo.floorName == floor)
                {
                    if (CheckEntranceLayout(roomAInfo.roomAHandle))
                    {
                        if (roomAInfo.connectType == ConnectType.AllConnect)
                        {
                            roomAType = roomAInfo.roomAHandle;
                            roomBType = roomAInfo.roomBHandle;
                            findRule = true;
                            break;
                        }
                        else if (roomAInfo.connectType == ConnectType.Normal)
                        {
                            if (roomB.Tags.Any(y => roomAInfo.roomB.Any(z => RoomConfigTreeService.CompareRoom(z,y))))
                            {
                                roomAType = roomAInfo.roomAHandle;
                                roomBType = roomAInfo.roomBHandle;
                                findRule = true;
                                break;
                            }
                        }
                    }
                }
            }

            return findRule;
        }


        /// <summary>
        /// 判断是否是入口控制布置模式
        /// </summary>
        /// <param name="layoutType"></param>
        /// <returns></returns>
        private bool CheckEntranceLayout(LayoutType layoutType)
        {
            bool isLayout = false;
            switch (layoutType)
            {
                case LayoutType.EntranceGunCamera:
                case LayoutType.EntranceDomeCamera:
                case LayoutType.EntranceGunCameraWithShield:
                case LayoutType.EntranceFaceRecognitionCamera:
                    isLayout = true;
                    break;
                case LayoutType.Nothing:
                case LayoutType.AlongLineGunCamera:
                case LayoutType.AlongLinePanTiltCamera:
                case LayoutType.AlongLineDomeCamera:
                case LayoutType.AlongLineGunCameraWithShield:
                default:
                    break;
            }

            return isLayout;
        }

        /// <summary>
        /// 获取沿线布置模式需要布置的房间
        /// </summary>
        /// <param name="rooms"></param>
        /// <returns></returns>
        private Dictionary<ThIfcRoom, LayoutType> GetLayoutRooms(List<ThIfcRoom> rooms)
        {
            var needRoomNames = HandleVideoMonitoringRoomService.GTRooms.Where(x => x.roomAHandle == LayoutType.AlongLineDomeCamera ||
                x.roomAHandle == LayoutType.AlongLineGunCamera ||
                x.roomAHandle == LayoutType.AlongLineGunCameraWithShield ||
                x.roomAHandle == LayoutType.AlongLinePanTiltCamera)
                .ToList();
            var needRooms = new Dictionary<ThIfcRoom, LayoutType>();
            foreach (var room in rooms)
            {
                foreach (var nRName in needRoomNames)
                {
                    if (room.Tags.Any(x => nRName.roomA.Contains(x)))
                    {
                        needRooms.Add(room, nRName.roomAHandle);
                        break;
                    }
                }
            }
            return needRooms;
        }
    }
}
