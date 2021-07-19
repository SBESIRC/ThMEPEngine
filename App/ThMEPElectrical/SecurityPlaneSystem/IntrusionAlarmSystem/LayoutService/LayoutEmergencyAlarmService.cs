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
    public class LayoutEmergencyAlarmService
    {
        double angle = 45;
        public List<LayoutModel> Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door);

            //计算门信息
            var roomDoorInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);

            //获取构建信息
            var bufferRoom = room.Buffer(5)[0] as Polyline;
            var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
            var structs = getLayoutStructureService.CalLayoutStruc(door, nColumns, nWalls);
            var roomStructs = new List<Polyline>(nColumns);
            roomStructs.AddRange(nWalls);

            layoutModels.Add(LayoutEmergencyAlarmButton(roomStructs, roomDoorInfo.Item2, roomDoorInfo.Item1));

            return layoutModels;
        }

        /// <summary>
        /// 布置残卫报警按钮
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="doorDir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        public EmergencyAlarmButton LayoutEmergencyAlarmButton(List<Polyline> structs, Vector3d doorDir, Point3d doorPt)
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
                EmergencyAlarmButton emergencyAlarmButton = new EmergencyAlarmButton();
                emergencyAlarmButton.LayoutPoint = layoutInfo.Key;
                emergencyAlarmButton.LayoutDir = dir;
                return emergencyAlarmButton;
            }

            return null;
        }
    }
}
