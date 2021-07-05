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
            HandleVideoMonitoringRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.accessControlSystemTable);
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            List<LayoutModel> models = new List<LayoutModel>();
            List<ThIfcRoom> readyRooms = new List<ThIfcRoom>();
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
                    if (!readyRooms.Contains(connectRooms[0]))
                    {
                        var layoutType = CalNoCennectRoom(connectRooms[0], floor.StoreyTypeString);
                        models.AddRange(DoLayout(layoutType, connectRooms[0], door, doors, columns, walls, lanes));
                        readyRooms.Add(connectRooms[0]);
                    }
                }
                else if (connectRooms.Count >= 2)
                {
                    if (CalTwoConnectRoom(connectRooms[0], connectRooms[1], floor.StoreyTypeString, out LayoutType layoutAType, out LayoutType layoutBType))
                    {
                        if (!readyRooms.Contains(connectRooms[0]))
                        {
                            models.AddRange(DoLayout(layoutAType, connectRooms[0], door, doors, columns, walls, lanes));
                            readyRooms.Add(connectRooms[0]);
                        }
                        if (!readyRooms.Contains(connectRooms[1]))
                        {
                            models.AddRange(DoLayout(layoutBType, connectRooms[1], door, doors, columns, walls, lanes));
                            readyRooms.Add(connectRooms[1]);
                        }
                    }
                    else if (CalTwoConnectRoom(connectRooms[1], connectRooms[0], floor.StoreyTypeString, out layoutAType, out layoutBType))
                    {
                        if (!readyRooms.Contains(connectRooms[0]))
                        {
                            models.AddRange(DoLayout(layoutAType, connectRooms[0], door, doors, columns, walls, lanes));
                            readyRooms.Add(connectRooms[0]);
                        }
                        if (!readyRooms.Contains(connectRooms[1]))
                        {
                            models.AddRange(DoLayout(layoutBType, connectRooms[1], door, doors, columns, walls, lanes));
                            readyRooms.Add(connectRooms[1]);
                        }
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
        private List<LayoutModel> DoLayout(LayoutType layoutType, ThIfcRoom thRoom, Polyline door, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, List<Line> lanes)
        {
            var layoutModel = new List<LayoutModel>();
            switch (layoutType)
            {
                case LayoutType.AlongLineGunCamera:
                case LayoutType.AlongLinePanTiltCamera:
                case LayoutType.AlongLineDomeCamera:
                case LayoutType.AlongLineGunCameraWithShield:
                    layoutModel = layoutVideoByLine.Layout(lanes, doors, thRoom);
                    break;
                case LayoutType.EntranceGunCamera:
                case LayoutType.EntranceDomeCamera:
                case LayoutType.EntranceGunCameraWithShield:
                case LayoutType.EntranceFaceRecognitionCamera:
                    layoutModel.Add(layoutVideo.Layout(thRoom, door, walls, columns));
                    break;
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
            var roomAInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => x.roomA.Contains(connectRoom.Name)).ToList();
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
            var roomBInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => x.roomA.Contains(connectRoom.Name)).ToList();
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
        private bool CalTwoConnectRoom(ThIfcRoom roomA, ThIfcRoom roomB, string floor, out LayoutType roomAType, out LayoutType roomBType)
        {
            roomAType = LayoutType.Nothing;
            roomBType = LayoutType.Nothing;

            bool findRule = false;
            var roomAInfos = HandleVideoMonitoringRoomService.GTRooms.Where(x => x.roomA.Contains(roomA.Name)).ToList();
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
