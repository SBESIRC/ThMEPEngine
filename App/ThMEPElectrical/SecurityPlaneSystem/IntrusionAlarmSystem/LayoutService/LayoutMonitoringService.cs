﻿using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutMonitoringService
    {
        /// <summary>
        /// 吊装布置
        /// </summary>
        /// <param name="room"></param>
        /// <param name="doors"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public List<LayoutModel> HoistingLayout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls, bool isInfrared)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door);
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            var structs = getLayoutStructureService.CalLayoutStruc(door, columns, walls);
            if (structs.Count <= 0)
            {
                var bufferDoor = door.Buffer(5)[0] as Polyline;
                Polyline roomBoundary = new Polyline();
                if(thRoom.Boundary is Polyline polyline)
                {
                    roomBoundary = polyline;
                }
                else if(thRoom.Boundary is MPolygon mPolygon)
                {
                    roomBoundary = mPolygon.Shell();
                }
                var bufferRoom = roomBoundary.BufferPL(200)[0] as Polyline;
                DBObjectCollection objs = new DBObjectCollection();
                objs.Add(roomBoundary);
                objs.Add(bufferDoor);
                var rooms = ThCADCoreNTSEntityExtension.Difference(bufferRoom, objs).Cast<Polyline>().ToList();
                structs.AddRange(rooms);
                if (structs.Count <= 0)
                {
                    return layoutModels;
                }
            }
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in structs)
                {
                    //db.ModelSpace.Add(item);
                }
            }
            //计算门信息
            var doorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(door);

            //布置控制器
            LayoutControllerService layoutControllerService = new LayoutControllerService();
            var controller = layoutControllerService.LayoutController(structs, room, door, doorCenterPt, doorInfo.Item3);

            //布置探测器
            LayoutHositingDetectorService layoutHositingDetectorService = new LayoutHositingDetectorService();
            var detector = layoutHositingDetectorService.LayoutDetector(doorInfo.Item1, doorInfo.Item3, door);
            if (isInfrared)
            {
                layoutModels.Add(new InfraredHositingDetectorModel() { LayoutPoint = detector.LayoutPoint, LayoutDir = detector.LayoutDir, Room = thRoom });
            }
            else
            {
                layoutModels.Add(new DoubleHositingDetectorModel() { LayoutPoint = detector.LayoutPoint, LayoutDir = detector.LayoutDir, Room = thRoom });
            }

            if (controller == null)
            {
                return layoutModels;
            }
            layoutModels.Add(controller);
            controller.Room = thRoom;
            return layoutModels;
        }

        /// <summary>
        /// 壁装布置
        /// </summary>
        /// <param name="room"></param>
        /// <param name="doors"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public List<LayoutModel> WallMountingLayout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls, bool isInfrared)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door);
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            var structs = getLayoutStructureService.CalLayoutStruc(door, columns, walls);
            if (structs.Count <= 0)
            {
                var bufferDoor = door.Buffer(5)[0] as Polyline;
                Polyline roomBoundary = new Polyline();
                if (thRoom.Boundary is Polyline polyline)
                {
                    roomBoundary = polyline;
                }
                else if (thRoom.Boundary is MPolygon mPolygon)
                {
                    roomBoundary = mPolygon.Shell();
                }
                var bufferRoom = roomBoundary.BufferPL(200)[0] as Polyline;
                DBObjectCollection objs = new DBObjectCollection();
                objs.Add(roomBoundary);
                objs.Add(bufferDoor);
                var rooms = ThCADCoreNTSEntityExtension.Difference(bufferRoom, objs).Cast<Polyline>().ToList();
                structs.AddRange(rooms);
                if (structs.Count <= 0)
                {
                    return layoutModels;
                }
                return layoutModels;
            }

            //计算门信息
            var doorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);

            //布置控制器
            LayoutControllerService layoutControllerService = new LayoutControllerService();
            var controller = layoutControllerService.LayoutController(structs, room, door, doorInfo.Item1, doorInfo.Item3);
            controller.Room = thRoom;

            //布置探测器
            LayoutWallMountingDetectorService wallMountingDetectorService = new LayoutWallMountingDetectorService();
            var detector = wallMountingDetectorService.LayoutDetector(doorInfo.Item1, doorInfo.Item3, door, doorInfo.Item4, columns, walls, controller);

            layoutModels.Add(controller);
            if (isInfrared)
            {
                layoutModels.Add(new InfraredWallDetectorModel() { LayoutPoint = detector.LayoutPoint, LayoutDir = detector.LayoutDir, Room = thRoom });
            }
            else
            {
                layoutModels.Add(new DoubleWallDetectorModel() { LayoutPoint = detector.LayoutPoint, LayoutDir = detector.LayoutDir, Room = thRoom });
            }
            return layoutModels;
        }
    }
}
