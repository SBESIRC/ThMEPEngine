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
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService
{
    public class LayoutOneWayAuthenticationService
    {
        double buttunWidth = 400;
        double cardReaderWidth = 400;
        double cardReaderLength = 500;
        double angle = 45;

        public List<AccessControlModel> Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            var room = thRoom.Boundary as Polyline;

            //计算门信息
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(door);
            var otherDoorPt = doorCenterPt - roomDoorInfo.Item2 * (roomDoorInfo.Item4 / 2);

            //获取构建信息
            var bufferRoom = room.Buffer(5)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);

            List<AccessControlModel> accessControlModels = new List<AccessControlModel>();
            accessControlModels.Add(CalLayoutButton(structs, bufferRoom, roomDoorInfo.Item2, roomDoorInfo.Item1));
            accessControlModels.Add(CalLayoutCardReader(structs, bufferRoom, -roomDoorInfo.Item2, otherDoorPt));
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
        private Buttun CalLayoutButton(List<Polyline> structs, Polyline polyline, Vector3d doorDir, Point3d doorPt)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, polyline, doorDir, doorPt, angle, buttunWidth).First();

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
        private CardReader CalLayoutCardReader(List<Polyline> structs, Polyline polyline, Vector3d doorDir, Point3d doorPt)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, polyline, doorDir, doorPt, angle, cardReaderWidth).First();

            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();
            if (doorDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }

            CardReader cardReader = new CardReader();
            cardReader.layoutDir = dir;
            cardReader.layoutPt = layoutInfo.Value + dir * (cardReaderLength / 2);

            return cardReader;
        }
    }
}
