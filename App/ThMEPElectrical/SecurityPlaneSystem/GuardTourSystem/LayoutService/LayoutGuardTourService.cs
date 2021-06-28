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

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService
{
    class LayoutGuardTourService
    {
        public List<(Point3d, Vector3d)> Layout(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, List<List<Line>> lanes)
        {
            HandleGuardTourRoomService.HandleRoomInfo(ThElectricalUIService.Instance.Parameter.guardTourSystemTable);
            var otherRooms = rooms.Where(x => HandleGuardTourRoomService.otherRooms.Any(y => y.Contains(x.Name))).ToList();

            List<(Point3d, Vector3d)> layoutInfo = new List<(Point3d, Vector3d)>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            foreach (var thRoom in rooms)
            {
                var roomInfo = HandleGuardTourRoomService.GTRooms.Where(y => y.Contains(thRoom.Name));
                if (roomInfo.Count() <= 0)
                {
                    continue;
                }

                var room = thRoom.Boundary as Polyline;
                var bufferRoom = room.Buffer(5)[0] as Polyline;
                var nDoors = getLayoutStructureService.GetNeedDoors(doors, bufferRoom);
                var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
                var nLanes = getLayoutStructureService.GetNeedLanes(lanes, bufferRoom);
                 
                LayoutOtherGTService layoutStairwellsGTService = new LayoutOtherGTService();
                var layoutPts = layoutStairwellsGTService.Layout(thRoom, otherRooms, doors, columns, walls);

                LayoutGTAlongLaneService layoutGTAlongLaneService = new LayoutGTAlongLaneService();
                var laneLayoutPts = layoutGTAlongLaneService.Layout(lanes, columns, layoutPts.Select(x => x.Item1).ToList());

                layoutInfo.AddRange(layoutPts);
                layoutInfo.AddRange(laneLayoutPts);
            }

            return layoutInfo;
        }
    }
}
