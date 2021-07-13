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

namespace ThMEPElectrical.VideoMonitoringSystem
{
    public class LayoutService
    {
        LayoutVideo layoutVideo = new LayoutVideo();
        LayoutVideoByLine layoutVideoByLine = new LayoutVideoByLine();
        public List<LayoutModel> LayoutFactory(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, List<Line> lanes, ThStoreys floor)
        {
            //填充数据
            HandleVideoMonitoringRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.videoMonitoringSystemTable);
            
            //设置参数
            layoutVideo.blindArea = ThElectricalUIService.Instance.Parameter.videoBlindArea;
            layoutVideo.layoutRange = ThElectricalUIService.Instance.Parameter.videaMaxArea;
            layoutVideoByLine.distance = ThElectricalUIService.Instance.Parameter.videoDistance;
            List<LayoutModel> models = new List<LayoutModel>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();

            //沿线均布布置模式
            var layoutRooms = GetLayoutRooms(rooms);
            foreach (var lRoom in layoutRooms)
            {
                var layoutVByLine = layoutVideoByLine.Layout(lanes, doors, lRoom.Key);
                models.AddRange(CreateModels(lRoom.Value, layoutVByLine)); 
            }
            
            //入口控制布置模式
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
                    if (CalTwoConnectRoom(connectRooms[1], connectRooms[0], floor.StoreyTypeString, out layoutAType, out layoutBType))
                    {
                        models.AddRange(DoLayout(layoutAType, connectRooms[0], door, columns, walls));
                        models.AddRange(DoLayout(layoutBType, connectRooms[1], door, columns, walls));
                    }
                }
            }

            return models;
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
        private List<LayoutModel> DoLayout(LayoutType layoutType, ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            var layoutModel = new List<LayoutModel>();
            switch (layoutType)
            {
                case LayoutType.EntranceGunCamera:
                case LayoutType.EntranceDomeCamera:
                case LayoutType.EntranceGunCameraWithShield:
                case LayoutType.EntranceFaceRecognitionCamera:
                    layoutModel.Add(layoutVideo.Layout(thRoom, door, walls, columns));
                    break;
                case LayoutType.EntranceGunCameraFlip:
                case LayoutType.EntranceDomeCameraFlip:
                case LayoutType.EntranceGunCameraWithShieldFlip:
                case LayoutType.EntranceFaceRecognitionCameraFlip:
                    layoutModel.Add(layoutVideo.Layout(thRoom, door, walls, columns));
                    layoutModel.ForEach(x => x.layoutDir = -x.layoutDir);
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
            var roomAInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => connectRoom.Tags.Any(y => x.roomA.Contains(y))).ToList();
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
            var roomBInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => connectRoom.Tags.Any(y => x.roomB.Contains(y))).ToList();
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
            var roomAInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => roomA.Tags.Any(y => x.roomA.Contains(y))).ToList();
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
                            if (roomB.Tags.Any(y => roomAInfo.roomB.Contains(y)))
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
                    if (room.Tags.Any(x=> nRName.roomA.Contains(x)))
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
