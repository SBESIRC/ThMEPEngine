using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutMonitoringService
    {
        /// <summary>
        /// 吊装布置
        /// </summary>
        /// <param name="room"></param>
        /// <param name="doors"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public List<LayoutModel> HoistingLayout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls, bool isInfrared)
        {
            Polyline room = thRoom.Boundary as Polyline;
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            GetLayoutStructureService layoutStructureService = new GetLayoutStructureService();
            var structs = layoutStructureService.CalLayoutStruc(door, columns, walls);
            if (structs.Count <= 0)
            {
                return layoutModels;
            }

            //计算门信息
            var doorInfo = layoutStructureService.GetDoorCenterPointOnRoom(room, door);

            //布置控制器
            LayoutControllerService layoutControllerService = new LayoutControllerService();
            var controller = layoutControllerService.LayoutController(structs, room, doorInfo.Item1, doorInfo.Item2);

            //布置探测器
            LayoutHositingDetectorService layoutHositingDetectorService = new LayoutHositingDetectorService();
            var detector = layoutHositingDetectorService.LayoutDetector(doorInfo.Item1, doorInfo.Item2, door);

            layoutModels.Add(controller);
            if (isInfrared)
            {
                layoutModels.Add(detector as InfraredHositingDetectorModel);
            }
            else
            {
                layoutModels.Add(detector as DoubleHositingDetectorModel);
            }
            return layoutModels;
        }

        /// <summary>
        /// 壁装布置
        /// </summary>
        /// <param name="room"></param>
        /// <param name="doors"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public List<LayoutModel> WallMountingLayout(ThIfcRoom thRoom, Polyline door, List<Polyline> columns, List<Polyline> walls, bool isInfrared)
        {
            Polyline room = thRoom.Boundary as Polyline;
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            GetLayoutStructureService layoutStructureService = new GetLayoutStructureService();
            var structs = layoutStructureService.CalLayoutStruc(door, columns, walls);

            //计算门信息
            var doorInfo = layoutStructureService.GetDoorCenterPointOnRoom(room, door);

            //布置控制器
            LayoutControllerService layoutControllerService = new LayoutControllerService();
            var controller = layoutControllerService.LayoutController(structs, room, doorInfo.Item1, doorInfo.Item2);

            //布置探测器
            LayoutWallMountingDetectorService wallMountingDetectorService = new LayoutWallMountingDetectorService();
            var detector = wallMountingDetectorService.LayoutDetector(doorInfo.Item1, doorInfo.Item2, door, doorInfo.Item3, columns, walls, controller);

            layoutModels.Add(controller);
            if (isInfrared)
            {
                layoutModels.Add(detector as InfraredWallDetectorModel);
            }
            else
            {
                layoutModels.Add(detector as DoubleWallDetectorModel);
            }
            return layoutModels;
        }
    }
}
