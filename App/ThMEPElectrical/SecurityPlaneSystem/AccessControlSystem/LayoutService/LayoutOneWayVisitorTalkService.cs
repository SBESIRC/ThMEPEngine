﻿using Autodesk.AutoCAD.DatabaseServices;
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
    public class LayoutOneWayVisitorTalkService
    {
        double blockTol = 300;
        double buttunWidth = 4 * ThElectricalUIService.Instance.Parameter.scale;
        double cardReaderWidth = 4 * ThElectricalUIService.Instance.Parameter.scale;
        double cardReaderLength = 8 * ThElectricalUIService.Instance.Parameter.scale;
        public List<AccessControlModel> Layout(ThIfcRoom thRoomA, ThIfcRoom thRoomB, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var roomA = getLayoutStructureService.GetUseRoomBoundary(thRoomA, door);
            var roomB = thRoomB == null ? null : getLayoutStructureService.GetUseRoomBoundary(thRoomB, door);

            //计算门信息
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(roomA, door);
            var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(door);

            //获取构建信息
            var bufferRoom = roomA.Buffer(15)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in structs)
                {
                    //db.ModelSpace.Add(item);
                }
            }
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
            var intercom = CalLayoutIntercom(structs, roomDoorInfo.Item2, roomA, roomB, door);
            var button = CalLayoutButton(structs, roomDoorInfo.Item1, roomA, door);
            if (intercom != null) accessControlModels.Add(intercom);
            if (button != null) accessControlModels.Add(button);
            accessControlModels.Add(CalLayoutElectricLock(doorCenterPt, roomDoorInfo.Item3));

            return accessControlModels;
        }

        /// <summary>
        /// 计算电子按钮布置信息
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private Buttun CalLayoutButton(List<Polyline> structs, Point3d doorUsePt, Polyline room, Polyline door)
        {
            var resInfos = new List<KeyValuePair<Point3d, Vector3d>>();
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorUsePt, room, buttunWidth, blockTol);
            var bufferDoor = door.Buffer(10)[0] as Polyline;
            var firLayoutInfos = layoutInfo.Where(x => x.Key.IsIntersects(bufferDoor)).ToDictionary(x => x.Key, y => y.Value);
            if (firLayoutInfos.Count > 0)
            {
                layoutInfo = firLayoutInfos;
            }
            foreach (var lInfo in layoutInfo)
            {
                var dir = Vector3d.ZAxis.CrossProduct(lInfo.Key.EndPoint - lInfo.Key.StartPoint).GetNormal();
                var layoutPt = lInfo.Value + dir * (buttunWidth / 2);
                if (!room.Contains(layoutPt))
                {
                    layoutPt = lInfo.Value - dir * (buttunWidth / 2);
                    dir = -dir;
                }
                resInfos.Add(new KeyValuePair<Point3d, Vector3d>(layoutPt, dir));
            }
            resInfos = resInfos.OrderBy(x => x.Key.DistanceTo(doorUsePt)).ToList();
            if (resInfos.Count <= 0)
            {
                return null;
            }

            Buttun buttun = new Buttun();
            buttun.layoutDir = resInfos.First().Value;
            buttun.layoutPt = resInfos.First().Key;

            return buttun;
        }

        /// <summary>
        /// 计算电子锁布置信息
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private ElectricLock CalLayoutElectricLock(Point3d pt, Vector3d dir)
        {
            ElectricLock electricLock = new ElectricLock();
            electricLock.layoutPt = pt;
            electricLock.layoutDir = dir;
            return electricLock;
        }


        /// <summary>
        /// 计算读卡器布置信息
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private Intercom CalLayoutIntercom(List<Polyline> structs, Point3d doorUsePt, Polyline roomA, Polyline roomB, Polyline door)
        {
            var resInfos = new List<KeyValuePair<Point3d, Vector3d>>();
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorUsePt, roomA, cardReaderWidth, blockTol, false);
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
                if (roomA.Contains(layoutPt))
                {
                    layoutPt = lInfo.Value - dir * (cardReaderLength / 2);
                    dir = -dir;
                }
                resInfos.Add(new KeyValuePair<Point3d, Vector3d>(layoutPt, dir));
            }
            resInfos = resInfos.OrderBy(x => x.Key.DistanceTo(doorUsePt)).ToList();
            if (roomB != null)
            {
                resInfos = resInfos.Where(x => roomB.Contains(x.Key)).ToList();
            }
            if (resInfos.Count <= 0)
            {
                return null;
            }

            Intercom intercom = new Intercom();
            intercom.layoutDir = resInfos.First().Value;
            intercom.layoutPt = resInfos.First().Key;
            return intercom;
        }
    }
}
