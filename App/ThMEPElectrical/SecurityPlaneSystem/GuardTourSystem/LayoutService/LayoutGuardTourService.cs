using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService
{
    public class LayoutGuardTourService
    {
        public List<(Point3d, Vector3d)> Layout(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, List<List<Line>> lanes, ThStoreys floor)
        {
            HandleGuardTourRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.guardTourSystemTable);
            var otherRooms = rooms.Where(x => HandleGuardTourRoomService.otherRooms.Any(y => x.Tags.Any(z => y.roomName.Contains(z)))).ToList();

            List<(Point3d, Vector3d)> layoutInfo = new List<(Point3d, Vector3d)>();
            //房间外需要布置的房间
            LayoutOtherGTService layoutStairwellsGTService = new LayoutOtherGTService();
            var layoutPts = layoutStairwellsGTService.Layout(otherRooms, doors, columns, walls);
            layoutInfo.AddRange(layoutPts.Select(x => x.Value));

            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            LayoutGTAlongLaneService layoutGTAlongLaneService = new LayoutGTAlongLaneService();
            layoutGTAlongLaneService.layoutSpace = ThElectricalUIService.Instance.Parameter.gtDistance;
            foreach (var thRoom in rooms)
            {
                var roomInfo = HandleGuardTourRoomService.GTRooms.Where(y => thRoom.Tags.Any(x => y.roomName.Contains(x)) &&
                    (string.IsNullOrEmpty(y.floor) || y.floor == floor.StoreyTypeString));
                if (roomInfo.Count() <= 0)
                {
                    continue;
                }

                var room = thRoom.Boundary as Polyline;
                var bufferRoom = room.Buffer(5)[0] as Polyline;
                var nDoors = getLayoutStructureService.GetNeedDoors(doors, bufferRoom);
                var nColumns = getLayoutStructureService.GetNeedColumns(columns, room);
                var nLanes = getLayoutStructureService.GetNeedLanes(lanes, bufferRoom);
                var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);
                var nLayoutPts = layoutPts.Where(x => nDoors.Contains(x.Key)).Select(x => x.Value.Item1).ToList();

                //筛出不和墙相交的柱
                nColumns = nColumns.Where(x => {
                    Point3dCollection pts = new Point3dCollection();
                    room.IntersectWith(x, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0) return false;
                    bufferRoom.IntersectWith(x, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0) return false;
                    return true;
                }).ToList();
                var laneLayoutPts = layoutGTAlongLaneService.Layout(nLanes, nColumns, nWalls, nLayoutPts);

                layoutInfo.AddRange(laneLayoutPts);
            }

            return layoutInfo;
        }
    }
}
