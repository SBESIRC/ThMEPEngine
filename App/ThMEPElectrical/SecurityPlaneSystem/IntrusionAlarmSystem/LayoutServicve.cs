using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutService
    {
        public void WallLayoutService(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            foreach (var thRoom in rooms)
            {
                var room = thRoom.Boundary as Polyline;
                var bufferRoom = room.Buffer(5)[0] as Polyline;
                var nDoors = getLayoutStructureService.GetNeedDoors(doors, bufferRoom);
                var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
                var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);

                LayoutMonitoringService layoutMonitoringService = new LayoutMonitoringService();
                var layoutInfo = layoutMonitoringService.WallMountingLayout(room, nDoors, nColumns, nWalls);

                using (Linq2Acad.AcadDatabase dv = Linq2Acad.AcadDatabase.Active())
                {
                    foreach (var item in layoutInfo)
                    {
                        dv.ModelSpace.Add(new Line(item.controller.LayoutPoint, item.controller.LayoutPoint + item.controller.LayoutDir * 500));
                        dv.ModelSpace.Add(new Line(item.detector.LayoutPoint, item.detector.LayoutPoint + item.detector.LayoutDir * 500));
                    }
                }
            }
        }

        public void HositingLayoutService(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            foreach (var thRoom in rooms)
            {
                var room = thRoom.Boundary as Polyline;
                var bufferRoom = room.Buffer(5)[0] as Polyline;
                var nDoors = getLayoutStructureService.GetNeedDoors(doors, bufferRoom);
                var nColumns = getLayoutStructureService.GetNeedColumns(columns, bufferRoom);
                var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom);

                LayoutMonitoringService layoutMonitoringService = new LayoutMonitoringService();
                var layoutInfo = layoutMonitoringService.HoistingLayout(room, nDoors, nColumns, nWalls);

                using (Linq2Acad.AcadDatabase dv = Linq2Acad.AcadDatabase.Active())
                {
                    foreach (var item in layoutInfo)
                    {
                        dv.ModelSpace.Add(new Line(item.controller.LayoutPoint, item.controller.LayoutPoint + item.controller.LayoutDir * 500));
                        dv.ModelSpace.Add(new Circle(item.detector.LayoutPoint, Vector3d.ZAxis, 500));
                    }
                }
            }
        }
    }
}
