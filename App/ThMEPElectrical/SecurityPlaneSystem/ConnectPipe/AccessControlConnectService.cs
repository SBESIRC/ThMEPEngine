using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Model;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe
{
    public class AccessControlConnectService
    {
        readonly double tol = 10;
        readonly double avoidStep = 300;
        readonly double tolLength = 5000;
        readonly double pathDis = 150;
        public List<Polyline> ConnectPipe(List<BlockReference> IABlock, List<ThIfcRoom> rooms, List<Polyline> columns, List<Polyline> doors, ThEStoreys floor)
        {
            var roomBlockInfos = SetBlockInRoom(IABlock, rooms);
            var doorBlockInfos = SetBlockInDoor(IABlock, doors);
            List<Polyline> resPipe = new List<Polyline>();
            HandleAccessControlRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.accessControlSystemTable);
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            LayoutAccessControlService layoutAccessControlService = new LayoutAccessControlService();
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
                    var layoutType = layoutAccessControlService.CalNoCennectRoom(connectRooms[0], floor.StoreyTypeString);
                    resPipe.AddRange(CalLayout(layoutType, connectRooms[0], connectRooms[0], door, roomBlockInfos, doorBlockInfos, columns));
                }
                else if (connectRooms.Count >= 2)
                {
                    if (layoutAccessControlService.CalTwoConnectRoom(connectRooms[0], connectRooms[1], floor.StoreyTypeString, out LayoutType layoutAType, out LayoutType layoutBType))
                    {
                        resPipe.AddRange(CalLayout(layoutAType, connectRooms[0], connectRooms[1], door, roomBlockInfos, doorBlockInfos, columns));
                        resPipe.AddRange(CalLayout(layoutBType, connectRooms[1], connectRooms[0], door, roomBlockInfos, doorBlockInfos, columns));
                    }
                    else if (layoutAccessControlService.CalTwoConnectRoom(connectRooms[1], connectRooms[0], floor.StoreyTypeString, out layoutAType, out layoutBType))
                    {
                        resPipe.AddRange(CalLayout(layoutAType, connectRooms[1], connectRooms[0], door, roomBlockInfos, doorBlockInfos, columns));
                        resPipe.AddRange(CalLayout(layoutBType, connectRooms[0], connectRooms[1], door, roomBlockInfos, doorBlockInfos, columns));
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
        private List<Polyline> CalLayout(LayoutType layoutType, ThIfcRoom thRoomA, ThIfcRoom thRoomB, Polyline door, Dictionary<ThIfcRoom, List<ACModel>> roomModelInfos
            , Dictionary<Polyline, List<ACModel>> doorModelInfos, List<Polyline> holes)
        {
            List<Polyline> resPolys = new List<Polyline>();
            if (!doorModelInfos.Keys.Contains(door) || !roomModelInfos.Keys.Contains(thRoomA) || !roomModelInfos.Keys.Contains(thRoomB))
            {
                return resPolys;
            }

            var useAModels = roomModelInfos[thRoomA];
            var useBModels = roomModelInfos[thRoomB];
            var useDoorModels = doorModelInfos[door];
            switch (layoutType)
            {
                case LayoutType.Nothing:
                    break;
                case LayoutType.OneWayAuthentication:
                    var button = useAModels.Where(x => x is ACButtun).ToList();
                    var cardReader = useBModels.Where(x => x is ACCardReader).ToList();
                    if (cardReader.Count <= 0)
                    {
                        return resPolys;
                    }
                    var electricLock = useDoorModels.Where(x => x is ACElectricLock).First();
                    resPolys.AddRange(ConnectACPipe(button, cardReader, electricLock, thRoomA, thRoomB, holes));
                    break;
                case LayoutType.TwoWayAuthentication:
                    var cardReaderA = useAModels.Where(x => x is ACCardReader).ToList();
                    var cardReaderB = useBModels.Where(x => x is ACCardReader).ToList();
                    if (cardReaderB.Count <= 0)
                    {
                        return resPolys;
                    }
                    electricLock = useDoorModels.Where(x => x is ACElectricLock).First();
                    resPolys.AddRange(ConnectACPipe(cardReaderA, cardReaderB, electricLock, thRoomA, thRoomB, holes));
                    break;
                case LayoutType.OneWayVisitorTalk:
                    button = useAModels.Where(x => x is ACButtun).ToList();
                    var intercom = useBModels.Where(x => x is ACIntercom).ToList();
                    if (intercom.Count <= 0)
                    {
                        return resPolys;
                    }
                    electricLock = useDoorModels.Where(x => x is ACElectricLock).First();
                    resPolys.AddRange(ConnectACPipe(button, intercom, electricLock, thRoomA, thRoomB, holes));
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
        private List<Polyline> ConnectACPipe(List<ACModel> roomAModel, List<ACModel> roomBModel, ACModel electricLock, ThIfcRoom roomA, ThIfcRoom roomB, List<Polyline> holes)
        {
            List<Polyline> resPaths = new List<Polyline>();
            var useHoles = new List<Polyline>(holes);
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            PipePathService pipePath = new PipePathService();
            ConnectBlockService connectBlockService = new ConnectBlockService();

            Polyline aPath = null;
            Polyline ePath = null;
            var bModel = roomBModel.OrderBy(x => x.position.DistanceTo(electricLock.position)).First();
            var roomPoly = CreateMap(roomA, roomB, bModel.layoutDir).Buffer(avoidStep * 2)[0] as Polyline;
            var otherBModels = roomBModel.Where(x => x != bModel).ToList();
            useHoles.AddRange(otherBModels.Select(x => UtilService.GetBoungdingBox(x.position, bModel.layoutDir, avoidStep)));
            //连接房间a和房间b的块
            ACModel aModel = null;
            if (roomAModel.Count > 0)
            {
                aModel = roomAModel.OrderBy(x => x.position.DistanceTo(electricLock.position)).First();
                var otherAModels = roomAModel.Where(x => x != aModel).ToList();
                useHoles.AddRange(otherAModels.Select(x => UtilService.GetBoungdingBox(x.position, bModel.layoutDir, avoidStep)));
                var useAHoles = new List<Polyline>(useHoles);
                useAHoles.Add(UtilService.GetBoungdingBox(electricLock.position, bModel.layoutDir, avoidStep));
                aPath = pipePath.CreatePipePath(roomPoly, aModel.position, bModel.position, bModel.layoutDir, useAHoles);
                //修正连接线
                if (aPath != null)
                {
                    double range = 2 * ThElectricalUIService.Instance.Parameter.scale;
                    if(bModel.Boundary.Contains(aPath.GetPoint3dAt(aPath.NumberOfVertices - 2)))
                    {
                        aPath.RemoveVertexAt(aPath.NumberOfVertices - 1);
                    }
                    if (aPath.NumberOfVertices >= 2)
                    {
                        aPath = connectBlockService.ConnectByPoint(bModel.ConnectPts, aPath);
                        aPath.ReverseCurve();
                        aPath = connectBlockService.ConnectByCircle(new List<Point3d>() { aModel.position }, aPath, range);
                        resPaths.Add(aPath);
                    }
                }
            }

            //连接电锁和房间b的块
            var useEHoles = new List<Polyline>(useHoles);
            if (aModel != null)
            {
                useEHoles.Add(UtilService.GetBoungdingBox(aModel.position, bModel.layoutDir, avoidStep));
            }
            if(resPaths.Count > 0)
            {
                useEHoles.AddRange(resPaths.Select(o => o.BufferFlatPL(10).Cast<Polyline>().First()));
            }
            ePath = pipePath.CreatePipePath(roomPoly, electricLock.position, bModel.position, bModel.layoutDir, useEHoles);
            //修正连接线
            ePath = connectBlockService.AjustPathIntersection(ePath, aPath, electricLock.position, bModel.position, pathDis);
            if (ePath != null)
            {
                if (bModel.Boundary.Contains(ePath.GetPoint3dAt(ePath.NumberOfVertices - 2)))
                {
                    ePath.RemoveVertexAt(ePath.NumberOfVertices - 1);
                }
                if (ePath.NumberOfVertices >= 2)
                {
                    ePath = connectBlockService.ConnectByPoint(bModel.ConnectPts, ePath);
                    ePath.ReverseCurve();
                    if (electricLock.Boundary.Contains(ePath.GetPoint3dAt(ePath.NumberOfVertices - 2)))
                    {
                        ePath.RemoveVertexAt(ePath.NumberOfVertices - 1);
                    }
                    if (ePath.NumberOfVertices >= 2)
                    {
                        ePath = connectBlockService.ConnectByPoint(electricLock.ConnectPts, ePath);
                        resPaths.Add(ePath);
                    }
                }
            }
            
            return resPaths.Where(x => x.Length < tolLength).ToList();
        }

        /// <summary>
        /// 创建地图
        /// </summary>
        /// <param name="roomA"></param>
        /// <param name="roomB"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Polyline CreateMap(ThIfcRoom roomA, ThIfcRoom roomB, Vector3d dir)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var polyA = getLayoutStructureService.GetUseRoomBoundary(roomA);
            var polyB = getLayoutStructureService.GetUseRoomBoundary(roomB); 
            List<Point3d> pts = new List<Point3d>(polyA.Vertices().Cast<Point3d>());
            pts.AddRange(polyB.Vertices().Cast<Point3d>());
            return UtilService.GetBoungdingBox(pts, dir);
        }

        /// <summary>
        /// 计算门内块的信息
        /// </summary>
        /// <param name="IABlock"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<ACModel>> SetBlockInDoor(List<BlockReference> IABlock, List<Polyline> doors)
        {
            Dictionary<Polyline, List<ACModel>> blockGroup = new Dictionary<Polyline, List<ACModel>>();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(IABlock.ToCollection());
            foreach (var door in doors)
            {
                var bufferRoom = door.Buffer(-tol)[0] as Polyline;
                var blocks = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferRoom);
                if (blocks.Count > 0)
                {
                    var iaBlocks = ModelClassifyService.ClassifyACBlock(blocks.Cast<BlockReference>().ToList());
                    blockGroup.Add(door, iaBlocks);
                }
            }

            return blockGroup;
        }

        /// <summary>
        /// 计算房间内块的信息
        /// </summary>
        /// <param name="IABlock"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        private Dictionary<ThIfcRoom, List<ACModel>> SetBlockInRoom(List<BlockReference> IABlock, List<ThIfcRoom> rooms)
        {
            Dictionary<ThIfcRoom, List<ACModel>> blockGroup = new Dictionary<ThIfcRoom, List<ACModel>>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(IABlock.ToCollection());
            foreach (var room in rooms)
            {
                var bufferRoom = getLayoutStructureService.GetUseRoomBoundary(room).Buffer(avoidStep)[0] as Polyline;
                var blocks = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferRoom);
                if (blocks.Count > 0)
                {
                    var iaBlocks = ModelClassifyService.ClassifyACBlock(blocks.Cast<BlockReference>().ToList());
                    blockGroup.Add(room, iaBlocks);
                }
            }

            return blockGroup;
        }
    }
}
