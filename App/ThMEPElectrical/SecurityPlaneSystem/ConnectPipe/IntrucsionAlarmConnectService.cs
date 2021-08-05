using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class IntrucsionAlarmConnectService
    {
        readonly double tol = 10;
        readonly double avoidStep = 400;
        public List<Polyline> ConnectPipe(List<BlockReference> IABlock, List<ThIfcRoom> rooms, List<Polyline> columns, List<Polyline> doors, ThEStoreys floor)
        {
            var roomBlockInfos = SetBlockInRoom(IABlock, rooms);
            HandleIntrusionAlarmRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.intrusionAlarmSystemTable);
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            LayoutFactoryService layoutFactoryService = new LayoutFactoryService();
            List<Polyline> resPipe = new List<Polyline>();
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
                    LayoutType layoutType = layoutFactoryService.CalNoCennectRoom(connectRooms[0], floor.StoreyTypeString);
                    resPipe.AddRange(CalLayout(layoutType, connectRooms[0], roomBlockInfos, columns));
                }
                else if (connectRooms.Count >= 2)
                {
                    if (layoutFactoryService.CalTwoConnectRoom(connectRooms[0], connectRooms[1], floor.StoreyTypeString, out LayoutType layoutAType, out LayoutType layoutBType))
                    {
                        resPipe.AddRange(CalLayout(layoutAType, connectRooms[0], roomBlockInfos, columns));
                        resPipe.AddRange(CalLayout(layoutBType, connectRooms[1], roomBlockInfos, columns));
                    }
                    else if (layoutFactoryService.CalTwoConnectRoom(connectRooms[1], connectRooms[0], floor.StoreyTypeString, out layoutAType, out layoutBType))
                    {
                        resPipe.AddRange(CalLayout(layoutAType, connectRooms[1], roomBlockInfos, columns));
                        resPipe.AddRange(CalLayout(layoutBType, connectRooms[0], roomBlockInfos, columns));
                    }
                }
            }

            return resPipe;
        }

        /// <summary>
        /// 计算连接信息
        /// </summary>
        /// <param name="layoutType"></param>
        /// <param name="thRoom"></param>
        /// <param name="roomModelInfos"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private List<Polyline> CalLayout(LayoutType layoutType, ThIfcRoom thRoom, Dictionary<ThIfcRoom, List<IAModel>> roomModelInfos, List<Polyline> holes)
        {
            List<Polyline> resPolys = new List<Polyline>();
            if (!roomModelInfos.Keys.Contains(thRoom))
            {
                return resPolys;
            }
            var useModels = roomModelInfos[thRoom];
            switch (layoutType)
            {
                case LayoutType.Nothing:
                    break;
                case LayoutType.DisabledToiletAlarm:
                    break;
                case LayoutType.EmergencyAlarm:
                    break;
                case LayoutType.InfraredWallMounting:
                    var controllers = useModels.Where(x => x is IAControllerModel).ToList();
                    var detectors = useModels.Where(x => x is IAInfraredWallDetectorModel).ToList();
                    resPolys.AddRange(ConnectIWMPipe(controllers, detectors, thRoom, holes));
                    break;
                case LayoutType.InfraredHoisting:
                    controllers = useModels.Where(x => x is IAControllerModel).ToList();
                    detectors = useModels.Where(x => x is IAInfraredHositingDetectorModel).ToList();
                    resPolys.AddRange(ConnectIWMPipe(controllers, detectors, thRoom, holes));
                    break;
                case LayoutType.DoubleWallMounting:
                case LayoutType.DoubleHositing:
                    controllers = useModels.Where(x => x is IAControllerModel).ToList();
                    detectors = useModels.Where(x => x is IADoubleDetectorModel).ToList();
                    resPolys.AddRange(ConnectIWMPipe(controllers, detectors, thRoom, holes));
                    break;
                default:
                    break;
            }

            return resPolys;
        }

        /// <summary>
        /// 创建连接管线
        /// </summary>
        /// <param name="controllers"></param>
        /// <param name="detectors"></param>
        /// <param name="room"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private List<Polyline> ConnectIWMPipe(List<IAModel> controllers, List<IAModel> detectors, ThIfcRoom room, List<Polyline> holes)
        {
            List<Polyline> resPaths = new List<Polyline>();
            if (controllers.Count <= 0)
            {
                return resPaths;
            }

            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var roomPoly = getLayoutStructureService.GetUseRoomBoundary(room);
            var control = controllers.First() as IAControllerModel;
            ConnectBlockService connectBlockService = new ConnectBlockService();
            foreach (var detec in detectors)
            {
                var otherDetec = detectors.Where(x => x != detec).ToList();
                var useHoles = new List<Polyline>(holes);
                useHoles.AddRange(otherDetec.Select(x => UtilService.GetBoungdingBox(x.position, control.layoutDir, avoidStep)));
                PipePathService pipePath = new PipePathService();
                var path = pipePath.CreatePipePath(roomPoly, detec.position, control.position, control.layoutDir, useHoles);
                if (path != null)
                {
                    path = connectBlockService.ConnectByPoint(control.ConnectPts, path);
                    path.ReverseCurve();
                    path = connectBlockService.ConnectByCircle(new List<Point3d>() { detec.position }, path, 150);
                    resPaths.Add(path);
                }
            }

            return resPaths;
        }

        /// <summary>
        /// 计算房间内块的信息
        /// </summary>
        /// <param name="IABlock"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        private Dictionary<ThIfcRoom, List<IAModel>> SetBlockInRoom(List<BlockReference> IABlock, List<ThIfcRoom> rooms)
        {
            Dictionary<ThIfcRoom, List<IAModel>> blockGroup = new Dictionary<ThIfcRoom, List<IAModel>>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(IABlock.ToCollection());
            foreach (var room in rooms)
            {
                var bufferRoom = getLayoutStructureService.GetUseRoomBoundary(room).Buffer(-tol)[0] as Polyline;
                var blocks = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferRoom);
                if (blocks.Count > 0)
                {
                    var iaBlocks = ClassifyBlock(blocks.Cast<BlockReference>().ToList());
                    blockGroup.Add(room, iaBlocks);
                }
            }

            return blockGroup;
        }

        /// <summary>
        /// 分类块
        /// </summary>
        /// <param name="IABlock"></param>
        /// <returns></returns>
        private List<IAModel> ClassifyBlock(List<BlockReference> IABlock)
        {
            List<IAModel> models = new List<IAModel>();
            foreach (var block in IABlock)
            {
                if (block.Name == ThMEPCommon.CONTROLLER_BLOCK_NAME) models.Add(new IAControllerModel(block));
                if (block.Name == ThMEPCommon.INFRAREDWALLDETECTOR_BLOCK_NAME) models.Add(new IAInfraredWallDetectorModel(block));
                if (block.Name == ThMEPCommon.DOUBLEDETECTOR_BLOCK_NAME) models.Add(new IADoubleDetectorModel(block));
                if (block.Name == ThMEPCommon.INFRAREDHOSITINGDETECTOR_BLOCK_NAME) models.Add(new IAInfraredHositingDetectorModel(block));
                if (block.Name == ThMEPCommon.DISABLEDALARM_BLOCK_NAME) models.Add(new IADisabledAlarmButtun(block));
                if (block.Name == ThMEPCommon.SOUNDLIGHTALARM_BLOCK_NAME) models.Add(new IASoundLightAlarm(block));
                if (block.Name == ThMEPCommon.EMERGENCYALARM_BLOCK_NAME) models.Add(new IAEmergencyAlarmButton(block));
            }

            return models;
        }
    }
}
