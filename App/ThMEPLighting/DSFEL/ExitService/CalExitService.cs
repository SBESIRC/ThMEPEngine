using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Model;
using ThMEPLighting.DSFEL.Model;

namespace ThMEPLighting.DSFEL.ExitService
{
    public class CalExitService
    {
        readonly double tol = 5;
        readonly string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
        readonly string roomConfidName = "房间名称处理";
        readonly double moveBlockTol = 250;
        /// <summary>
        /// 计算出口
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <param name="doors"></param>
        public List<ExitModel> CalExit(List<ThIfcRoom> roomInfo, List<Polyline> doors)
        {
            var configData = GetExcelContent(roomConfigUrl);
            var roomTree = RoomConfigTreeService.CreateRoomTree(configData.Tables[roomConfidName]);
            DSFELConfigCommon dSFELConfig = new DSFELConfigCommon(roomTree);

            List<ExitModel> exitModels = new List<ExitModel>();
            List<Polyline> bufferDoors = doors.Select(x => x.Buffer(tol)[0] as Polyline).ToList();
            foreach (var door in bufferDoors)
            {
                var intersectRooms = roomInfo.Where(x => (x.Boundary as Polyline).Intersects(door)).ToList();
                ExitModel exit = new ExitModel();
                if (intersectRooms.Count == 1)
                {  
                    Polyline roomBound = intersectRooms[0].Boundary as Polyline;
                    if (true/*CalDoorOpenDir(door, roomBound)*/)
                    {
                        exit.exitType = ExitType.SafetyExit;
                        exit.room = roomBound;
                        exit.positin = GetLayoutPosition(roomBound, door);
                        exit.direction = GetLayoutDir(roomBound, door);
                        exitModels.Add(exit);
                    }
                }
                else if (intersectRooms.Count > 1)
                {
                    foreach (var room in intersectRooms)
                    {
                        Polyline roomBound = room.Boundary as Polyline;
                        if (dSFELConfig.CheckExitRoom(room, intersectRooms.Where(x=>x.Boundary != room.Boundary).ToList()))
                        {
                            exit.exitType = ExitType.EvacuationExit;
                            exit.room = roomBound;
                            exit.positin = GetLayoutPosition(roomBound, door);
                            exit.direction = GetLayoutDir(roomBound, door);
                            exitModels.Add(exit);
                            break;
                        }
                    }
                }
            }

            return exitModels;
        }

        /// <summary>
        /// 判断门的朝向（true is out向外开，false is in 向内开）
        /// </summary>
        /// <param name="door"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private bool CalDoorOpenDir(Polyline door, Polyline room)
        {
            Polyline intersecArea = door.Intersection(new DBObjectCollection() { room }).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            if (intersecArea != null)
            {
                var isIn = intersecArea.Area / door.Area;
                return isIn > 0.5;
            }

            return true;
        }

        /// <summary>
        /// 计算疏散灯放置点位
        /// </summary>
        /// <param name="door"></param>
        private Point3d GetLayoutPosition(Polyline room, Polyline door)
        {
            List<Point3d> pts = door.Vertices().Cast<Point3d>().ToList();
            pts = pts.OrderBy(x => room.Distance(x)).ToList();
            var pt1 = pts[0];
            pts = pts.Where(x => !x.IsEqualTo(pt1, new Tolerance(0.01, 0.01))).ToList();
            var pt2 = pts[0];
            pts = pts.OrderBy(x => pt1.DistanceTo(x)).ToList();
            var moveDir = (pt1 - pts[0]).GetNormal();
            var layoutPt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            layoutPt = layoutPt + moveDir * (moveBlockTol / 2);
            return layoutPt;
        }

        /// <summary>
        /// 计算疏散指示灯布置方向
        /// </summary>
        /// <param name="door"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private Vector3d GetLayoutDir(Polyline room, Polyline door)
        {
            List<Point3d> pts = door.Vertices().Cast<Point3d>().ToList();
            pts = pts.OrderBy(x => room.Distance(x)).ToList();
            var pt1 = pts[0];
            pts = pts.Where(x => !x.IsEqualTo(pt1, new Tolerance(0.01, 0.01))).ToList();
            var pt2 = pts[0];
            var dir = (pt1 - pt2).GetNormal();
            if (dir.DotProduct(Vector3d.XAxis) < 0)
            {
                dir = -dir;
            }
            return dir;
        }

        /// <summary>
        /// 读取excel内容
        /// </summary>
        /// <returns></returns>
        private DataSet GetExcelContent(string path)
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            return excelSrevice.ReadExcelToDataSet(path, true);
        }
    }
}
