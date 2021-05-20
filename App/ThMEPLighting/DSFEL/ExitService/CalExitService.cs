using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.DSFEL.Model;

namespace ThMEPLighting.DSFEL.ExitService
{
    public class CalExitService
    {
        readonly double tol = 5;

        /// <summary>
        /// 计算出口
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <param name="doors"></param>
        public List<ExitModel> CalExit(List<KeyValuePair<Polyline, string>> roomInfo, List<Polyline> doors)
        {
            List<ExitModel> exitModels = new List<ExitModel>();
            List<Polyline> bufferDoors = doors.Select(x => x.Buffer(tol)[0] as Polyline).ToList();
            foreach (var door in bufferDoors)
            {
                var intersectRooms = roomInfo.Where(x => x.Key.Intersects(door)).ToList();
                ExitModel exit = new ExitModel();
                if (intersectRooms.Count == 1)
                {
                    if(CalDoorOpenDir(door, intersectRooms[0].Key))
                    {
                        exit.exitType = ExitType.SafetyExit;
                        exit.room = intersectRooms[0].Key;
                        exitModels.Add(exit);
                    }
                }
                else if (intersectRooms.Count > 1)
                {
                    foreach (var room in intersectRooms)
                    {
                        if (CalDoorOpenDir(door, intersectRooms[0].Key) && CheckIsExit(intersectRooms.Where(x=>x.Key != x.Key).ToList()))
                        {
                            exit.exitType = ExitType.EvacuationExit;
                            exit.room = room.Key;
                            exitModels.Add(exit);
                            break;
                        }
                    }
                }
            }

            return exitModels;
        }

        /// <summary>
        /// 检查是否是疏散出口
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        private bool CheckIsExit(List<KeyValuePair<Polyline, string>> roomInfo)
        {
            return roomInfo.Any(z => DSFELConfigCommon.EvacuationExitArea.Where(y => y.Contains(z.Value)).Count() > 0);
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
    }
}
