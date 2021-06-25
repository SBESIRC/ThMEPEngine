using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService
{
    class LayoutGuardTourService
    {
        public void Layout(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls, List<Line> lanes)
        {
            var guardTourRooms = rooms.Where(x => HandleGuardTourRoomService.GTRoom.Any(y => y.Contains(x.Name))).ToList();
            var stairRooms = rooms.Where(x => HandleGuardTourRoomService.StairRoom.Any(y => y.Contains(x.Name))).ToList();

            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            foreach (var thRoom in guardTourRooms)
            {
                var room = thRoom.Boundary as Polyline;
                var bufferRoom = room.Buffer(5)[0] as Polyline;
                var nDoors = getLayoutStructureService.GetNeedDoors(doors, bufferRoom);
                var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
                var nLanes = getLayoutStructureService.GetNeedLanes(lanes, bufferRoom);

                LayoutStairwellsGTService layoutStairwellsGTService = new LayoutStairwellsGTService();
                var layoutPts = layoutStairwellsGTService.Layout(thRoom, stairRooms, doors, columns, walls);
            }
        }
    }
}
