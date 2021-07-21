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
    public class LayoutTwoWayAuthenticationService
    {
        double cardReaderWidth = 400;
        double cardReaderLength = 500;
        double angle = 45;
        public List<AccessControlModel> Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door);

            //计算门信息
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(door);
            var otherDoorPt = doorCenterPt - roomDoorInfo.Item2 * (roomDoorInfo.Item3 / 2);

            //获取构建信息
            var bufferRoom = room.Buffer(5)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);

            List<AccessControlModel> accessControlModels = new List<AccessControlModel>();
            var inCardReader = CalLayoutCardReader(structs, door, roomDoorInfo.Item2, roomDoorInfo.Item1);
            var outCardReader = CalLayoutCardReader(structs, door, -roomDoorInfo.Item2, otherDoorPt);
            if (inCardReader != null) accessControlModels.Add(inCardReader);
            if (outCardReader != null) accessControlModels.Add(outCardReader);
            accessControlModels.Add(CalLayoutElectricLock(roomDoorInfo.Item1, roomDoorInfo.Item2));

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
        private CardReader CalLayoutCardReader(List<Polyline> structs, Polyline door, Vector3d doorDir, Point3d doorPt)
        {
            var checkDir = doorDir;
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorDir, doorPt, door, angle, cardReaderWidth, true).FirstOrDefault();
            if (layoutInfo.Key == null)
            {
                var crossDir = Vector3d.ZAxis.CrossProduct(doorDir);
                layoutInfo = UtilService.CalLayoutInfo(structs, crossDir, doorPt, door, angle, cardReaderWidth * 2)
                    .Where(x => (x.Value - doorPt).DotProduct(doorDir) > 0)
                    .FirstOrDefault();
                checkDir = (doorPt - layoutInfo.Value).GetNormal();
            }
            if (layoutInfo.Key == null)
            {
                return null;
            }
            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();
            if (checkDir.DotProduct(dir) < 0)
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
