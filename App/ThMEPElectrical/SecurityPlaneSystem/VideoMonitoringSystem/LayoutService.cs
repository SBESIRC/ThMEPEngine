using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.VideoMonitoringSystem
{
    public class LayoutService
    {
        public List<KeyValuePair<Point3d, Vector3d>> ExitLayoutService(List<ThIfcRoom> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            //找到可布置构建
            GetLayoutStructureService layoutStructureService = new GetLayoutStructureService();
            var strucInfo = layoutStructureService.GetStructureService(rooms, doors, columns, walls);

            List<KeyValuePair<Point3d, Vector3d>> layoutInfo = new List<KeyValuePair<Point3d, Vector3d>>();
            LayoutVideo layout = new LayoutVideo();
            foreach (var info in strucInfo)
            {
                layoutInfo.Add(layout.Layout(info.doorCenterPoint, info.doorDir, info.walls, info.colums));
            }

            return layoutInfo;
        }

        public List<KeyValuePair<Point3d, Vector3d>> LaneLayoutService(List<Line> lanes, List<Polyline> doors, List<ThIfcRoom> rooms)
        {

            LayoutVideoByLine layout = new LayoutVideoByLine();
            var layoutInfo = layout.Layout(lanes, doors, rooms);

            return layoutInfo;
        }
    }
}
