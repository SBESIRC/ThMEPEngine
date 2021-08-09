using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
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
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door); 

            //计算门信息
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var doorCenterPt = getLayoutStructureService.GetDoorCenterPt(door);

            //获取构建信息
            var bufferRoom = room.Buffer(15)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);
            structs = structs.ToCollection().UnionPolygons().Cast<Polyline>().ToList();
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in structs)
                {
                    //db.ModelSpace.Add(item);
                }
            }
            List<AccessControlModel> accessControlModels = new List<AccessControlModel>();
            var button = CalLayoutButton(structs, roomDoorInfo.Item1, room);
            var cardReader = CalLayoutCardReader(structs, roomDoorInfo.Item2, room);
            if (button != null) accessControlModels.Add(button);
            if (cardReader != null) accessControlModels.Add(cardReader);
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
        private Buttun CalLayoutButton(List<Polyline> structs, Point3d doorUsePt, Polyline room)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs,doorUsePt, room, buttunWidth).FirstOrDefault();
            if (layoutInfo.Key == null)
            {
                return null;
            }
            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();

            var layoutPt = layoutInfo.Value + dir * (buttunWidth / 2);
            if (!room.Contains(layoutPt))
            {
                layoutPt = layoutInfo.Value - dir * (buttunWidth / 2);
                dir = -dir;
            }
            Buttun buttun = new Buttun();
            buttun.layoutDir = dir;
            buttun.layoutPt = layoutPt;

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
        private CardReader CalLayoutCardReader(List<Polyline> structs, Point3d doorUsePt, Polyline room)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorUsePt, room, buttunWidth).FirstOrDefault();
            if (layoutInfo.Key == null)
            {
                return null;
            }
            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();

            var layoutPt = layoutInfo.Value + dir * (buttunWidth / 2);
            if (!room.Contains(layoutPt))
            {
                layoutPt = layoutInfo.Value - dir * (buttunWidth / 2);
                dir = -dir;
            }

            CardReader cardReader = new CardReader();
            cardReader.layoutDir = dir;
            cardReader.layoutPt = layoutPt;

            return cardReader;
        }
    }
}
