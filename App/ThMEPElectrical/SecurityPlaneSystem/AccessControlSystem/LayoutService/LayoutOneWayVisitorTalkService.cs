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
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService
{
    public class LayoutOneWayVisitorTalkService
    {
        double buttunWidth = 400;
        double cardReaderWidth = 400;
        double cardReaderLength = 800;
        double angle = 45;
        public List<AccessControlModel> Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door);

            //计算门信息
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(door);
            var otherDoorPt = doorCenterPt - roomDoorInfo.Item2 * (roomDoorInfo.Item4 / 2);

            //获取构建信息
            var bufferRoom = room.Buffer(5)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in structs)
                {
                    db.ModelSpace.Add(item);
                }
            }
            List<AccessControlModel> accessControlModels = new List<AccessControlModel>();
            if (structs.Count <= 0)
            {
                return accessControlModels;
            }
            var intercom = CalLayoutIntercom(structs, door, -roomDoorInfo.Item2, otherDoorPt);
            var button = CalLayoutButton(structs, door, roomDoorInfo.Item2, roomDoorInfo.Item1);
            if (intercom != null) accessControlModels.Add(intercom);
            if (button != null) accessControlModels.Add(button);
            accessControlModels.Add(CalLayoutElectricLock(doorCenterPt, roomDoorInfo.Item2));

            return accessControlModels;
        }

        /// <summary>
        /// 计算电子按钮布置信息
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private Buttun CalLayoutButton(List<Polyline> structs, Polyline door, Vector3d doorDir, Point3d doorPt)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorDir, doorPt, door, angle, buttunWidth, true).FirstOrDefault();
            if (layoutInfo.Key == null)
            {
                return null;
            }
            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();
            if (doorDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }

            Buttun buttun = new Buttun();
            buttun.layoutDir = dir;
            buttun.layoutPt = layoutInfo.Value + dir * (buttunWidth / 2);

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
        private Intercom CalLayoutIntercom(List<Polyline> structs, Polyline door, Vector3d doorDir, Point3d doorPt)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorDir, doorPt, door, angle, cardReaderWidth, true).FirstOrDefault();
            if (layoutInfo.Key == null)
            {
                return null;
            }
            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();
            if (doorDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }

            Intercom intercom = new Intercom();
            intercom.layoutDir = dir;
            intercom.layoutPt = layoutInfo.Value + dir * (cardReaderLength / 2);
            return intercom;
        }
    }
}
