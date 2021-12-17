using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService
{
    public class LayoutTwoWayAuthenticationService
    {
        double blockTol = 300;
        double cardReaderWidth = 4 * ThElectricalUIService.Instance.Parameter.scale;
        double cardReaderLength = 5 * ThElectricalUIService.Instance.Parameter.scale;
        public List<AccessControlModel> Layout(ThIfcRoom thRoomA, ThIfcRoom thRoomB, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var roomA = getLayoutStructureService.GetUseRoomBoundary(thRoomA, door);
            var roomB = thRoomB == null ? null : getLayoutStructureService.GetUseRoomBoundary(thRoomB, door);

            //计算门信息
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(roomA, door);

            //获取构建信息
            var bufferRoom = roomA.Buffer(15)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);

            List<AccessControlModel> accessControlModels = new List<AccessControlModel>();
            if (structs.Count <= 0)
            {
                var bufferDoor = door.Buffer(5)[0] as Polyline;
                Polyline roomBoundary = roomA;
                var bufferRoomPL = roomBoundary.BufferPL(200)[0] as Polyline;
                DBObjectCollection objs = new DBObjectCollection();
                objs.Add(roomBoundary);
                objs.Add(bufferDoor);
                var rooms = ThCADCoreNTSEntityExtension.Difference(bufferRoomPL, objs).Cast<Polyline>().ToList();
                structs.AddRange(rooms);
                if (structs.Count <= 0)
                {
                    return accessControlModels;
                }
            }
            var inCardReader = CalLayoutCardReader(structs, roomDoorInfo.Item1, roomA, roomB, door, true);
            var outCardReader = CalLayoutCardReader(structs, roomDoorInfo.Item2, roomA, roomB, door, false);
            if (inCardReader != null) accessControlModels.Add(inCardReader);
            if (outCardReader != null) accessControlModels.Add(outCardReader);
            if (inCardReader.IsNull() || outCardReader.IsNull())
            {
                var bufferDoor = door.Buffer(5)[0] as Polyline;
                Polyline roomBoundary = roomA;
                var bufferRoomPL = roomBoundary.BufferPL(200)[0] as Polyline;
                DBObjectCollection objs = new DBObjectCollection();
                objs.Add(roomBoundary);
                objs.Add(bufferDoor);
                var rooms = ThCADCoreNTSEntityExtension.Difference(bufferRoomPL, objs).Cast<Polyline>().ToList();
                structs.AddRange(rooms);
                if (inCardReader.IsNull())
                {
                    inCardReader = CalLayoutCardReader(structs, roomDoorInfo.Item1, roomA, roomB, door, true);
                    if (inCardReader != null) accessControlModels.Add(inCardReader);
                }
                if (outCardReader.IsNull())
                {
                    outCardReader = CalLayoutCardReader(structs, roomDoorInfo.Item2, roomA, roomB, door, false);
                    if (outCardReader != null) accessControlModels.Add(outCardReader);
                }
            }
            accessControlModels.Add(CalLayoutElectricLock(roomDoorInfo.Item1, roomDoorInfo.Item3));

            return accessControlModels;
        }

        /// <summary>
        /// 计算电子锁布置信息
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private ElectricLock CalLayoutElectricLock(Point3d pt, Vector3d dir)
        {
            ElectricLock electricLock = new ElectricLock();
            electricLock.layoutDir = dir;
            electricLock.layoutPt = pt;
            return electricLock;
        }


        /// <summary>
        /// 计算读卡器布置信息
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private CardReader CalLayoutCardReader(List<Polyline> structs, Point3d doorUsePt, Polyline roomA, Polyline roomB, Polyline door, bool inRoom)
        {
            var resInfos = new List<KeyValuePair<Point3d, Vector3d>>();
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorUsePt, roomA, cardReaderWidth, blockTol, inRoom);
            var bufferDoor = door.Buffer(10)[0] as Polyline;
            var firLayoutInfos = layoutInfo.Where(x => x.Key.IsIntersects(bufferDoor)).ToDictionary(x => x.Key, y => y.Value);
            if (firLayoutInfos.Count > 0)
            {
                layoutInfo = firLayoutInfos;
            }
            foreach (var lInfo in layoutInfo)
            {
                var dir = Vector3d.ZAxis.CrossProduct(lInfo.Key.EndPoint - lInfo.Key.StartPoint).GetNormal();
                var layoutPt = lInfo.Value + dir * (cardReaderLength / 2);
                if ((inRoom && !roomA.Contains(layoutPt)) || (!inRoom && roomA.Contains(layoutPt)))
                {
                    layoutPt = lInfo.Value - dir * (cardReaderLength / 2);
                    dir = -dir;
                }
                resInfos.Add(new KeyValuePair<Point3d, Vector3d>(layoutPt, dir));
            }
            if (!inRoom && roomB != null)
            {
                resInfos = resInfos.Where(x => roomB.Contains(x.Key)).ToList();
            }
            resInfos = resInfos.OrderBy(x => x.Key.DistanceTo(doorUsePt)).ToList();
            if (resInfos.Count <= 0)
            {
                return null;
            }
            CardReader cardReader = new CardReader();
            cardReader.layoutDir = resInfos.First().Value;
            cardReader.layoutPt = resInfos.First().Key;

            return cardReader;
        }
    }
}
