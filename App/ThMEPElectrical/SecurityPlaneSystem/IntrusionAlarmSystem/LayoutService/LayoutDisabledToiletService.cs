using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutDisabledToiletService
    {
        double alarmWidth = 300;
        double angle = 45;
        public List<LayoutModel> Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            List<LayoutModel> layoutModels = new List<LayoutModel>();
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
            var roomStructs = new List<Polyline>(nColumns);
            roomStructs.AddRange(nWalls);

            layoutModels.Add(LayoutSoundLightAlarm(structs, door, roomDoorInfo.Item3, doorCenterPt));
            layoutModels.Add(LayoutDisabledAlarmButtun(roomStructs, roomDoorInfo.Item3, roomDoorInfo.Item1));

            return layoutModels;
        }

        /// <summary>
        /// 布置声光报警器
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="polyline"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private SoundLightAlarm LayoutSoundLightAlarm(List<Polyline> structs, Polyline polyline, Vector3d doorDir, Point3d doorPt)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorDir, doorPt, polyline, angle, alarmWidth).First();

            var dir = Vector3d.ZAxis.CrossProduct(layoutInfo.Key.EndPoint - layoutInfo.Key.StartPoint).GetNormal();
            if (doorDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }

            SoundLightAlarm soundLightAlarm = new SoundLightAlarm();
            soundLightAlarm.LayoutDir = dir;
            soundLightAlarm.LayoutPoint = layoutInfo.Value;
            return soundLightAlarm;
        }

        /// <summary>
        /// 布置残卫报警按钮
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        public DisabledAlarmButtun LayoutDisabledAlarmButtun(List<Polyline> structs, Vector3d doorDir, Point3d doorPt)
        {
            Ray ray = new Ray() { BasePoint = doorPt, UnitDir = doorDir };
            var layoutPts = new List<KeyValuePair<Point3d, Line>>();
            foreach (var stru in structs)
            {
                Point3dCollection pts = new Point3dCollection();
                ray.IntersectWith(stru, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    var pt = pts.Cast<Point3d>().OrderBy(x => x.DistanceTo(doorPt)).First();
                    var allStruLines = stru.GetAllLinesInPolyline()
                        .Where(x => x.GetClosestPointTo(pt, false).DistanceTo(pt) < 1)
                        .ToList();
                    if (allStruLines.Count > 1)
                    {
                        var line = allStruLines.Where(x => !(x.EndPoint - x.StartPoint).GetNormal().IsParallelWithTolerance(doorDir, angle)).First();
                        layoutPts.Add(new KeyValuePair<Point3d, Line>(pt, line));
                    }
                    else if (allStruLines.Count == 1)
                    {
                        layoutPts.Add(new KeyValuePair<Point3d, Line>(pt, allStruLines[0]));
                    }
                }
            }

            if (layoutPts.Count > 0)
            {
                var layoutInfo = layoutPts.OrderBy(x => x.Key.DistanceTo(doorPt)).First();
                var dir = Vector3d.ZAxis.CrossProduct((layoutInfo.Value.EndPoint - layoutInfo.Value.StartPoint).GetNormal());
                if (dir.DotProduct(doorDir) < 0)
                {
                    dir = -dir;
                }
                DisabledAlarmButtun disabledAlarmButtun = new DisabledAlarmButtun();
                disabledAlarmButtun.LayoutPoint = layoutInfo.Key;
                disabledAlarmButtun.LayoutDir = dir;
                return disabledAlarmButtun;
            }

            return null;
        }
    }
}
