using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.StructureHandleService;

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
        public List<LayoutModel> HoistingLayout(Polyline room, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            foreach (var door in doors)
            {
                GetLayoutStructureService layoutStructureService = new GetLayoutStructureService();
                var structs = layoutStructureService.CalLayoutStruc(door, columns, walls);
                if (structs.Count <= 0)
                {
                    continue;
                }

                //计算门信息
                var doorInfo = layoutStructureService.GetDoorCenterPointOnRoom(room, door);
                
                //布置控制器
                LayoutControllerService layoutControllerService = new LayoutControllerService();
                var controller = layoutControllerService.LayoutController(structs, room, doorInfo.Item1, doorInfo.Item2);

                //布置探测器
                LayoutHositingDetectorService layoutHositingDetectorService = new LayoutHositingDetectorService();
                var detector = layoutHositingDetectorService.LayoutDetector(doorInfo.Item1, doorInfo.Item2, door);

                LayoutModel layoutModel = new LayoutModel();
                layoutModel.detector = detector;
                layoutModel.controller = controller;
                layoutModels.Add(layoutModel);
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
        public List<LayoutModel> WallMountingLayout(Polyline room, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            List<LayoutModel> layoutModels = new List<LayoutModel>();
            foreach (var door in doors)
            {
                GetLayoutStructureService layoutStructureService = new GetLayoutStructureService();
                var structs = layoutStructureService.CalLayoutStruc(door, columns, walls);
                if (structs.Count <= 0)
                {
                    continue;
                }

                //计算门信息
                var doorInfo = layoutStructureService.GetDoorCenterPointOnRoom(room, door);

                //布置控制器
                LayoutControllerService layoutControllerService = new LayoutControllerService();
                var controller = layoutControllerService.LayoutController(structs, room, doorInfo.Item1, doorInfo.Item2);

                //布置探测器
                LayoutWallMountingDetectorService wallMountingDetectorService = new LayoutWallMountingDetectorService();
                var detector = wallMountingDetectorService.LayoutDetector(doorInfo.Item1, doorInfo.Item2, door, doorInfo.Item3, columns, walls, controller);
               
                LayoutModel layoutModel = new LayoutModel();
                layoutModel.controller = controller;
                layoutModel.detector = detector;
                layoutModels.Add(layoutModel);
            }

            return layoutModels;
        }
    }
}
