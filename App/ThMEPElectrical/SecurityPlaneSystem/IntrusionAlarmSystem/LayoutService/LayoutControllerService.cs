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
using ThMEPElectrical.Service;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutControllerService
    {
        double angle = 45;
        double blockTol = 300;
        double blockWidth = 3 * ThElectricalUIService.Instance.Parameter.scale;
        double blockLength = 5 * ThElectricalUIService.Instance.Parameter.scale;
        public ControllerModel LayoutController(List<Polyline> structs, Polyline room, Polyline door, Point3d doorPt, Vector3d doorDir)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, doorDir, doorPt, door, angle, blockTol, true)
                .Where(x => room.Contains(x.Value) || room.Distance(x.Value) < 10).FirstOrDefault();
            var checkDir = doorDir;
            if (layoutInfo.Key == null)
            {
                var crossDir = Vector3d.ZAxis.CrossProduct(doorDir);
                layoutInfo = UtilService.CalLayoutInfo(structs, crossDir, doorPt, door, angle, blockTol)
                    .Where(x => room.Contains(x.Value) || room.Distance(x.Value) < 10).FirstOrDefault();
                if (layoutInfo.Key == null)
                {
                    layoutInfo = UtilService.CalLayoutInfo(structs, -crossDir, doorPt, door, angle, blockTol)
                        .Where(x => room.Contains(x.Value) || room.Distance(x.Value) < 10).FirstOrDefault();
                }
                checkDir = (doorPt - layoutInfo.Value).GetNormal();
            }
            if (layoutInfo.Key == null)
            {
                return null;
            }
            var controller = CalControllerInfo(layoutInfo, checkDir);

            return controller;
        }

        /// <summary>
        /// 计算控制器信息
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        private ControllerModel CalControllerInfo(KeyValuePair<Line, Point3d> layoutInfo, Vector3d doorDir)
        {
            //计算控制器排布信息
            ControllerModel controller = new ControllerModel();
            var controllerLayoutDir = CalControllerLayoutDir(layoutInfo.Key, doorDir);
            controller.LayoutDir = controllerLayoutDir;
            controller.LayoutPoint = layoutInfo.Value + controllerLayoutDir * (blockLength / 2);

            return controller;
        }

        /// <summary>
        /// 计算探测器排布方向
        /// </summary>
        /// <param name="line"></param>
        /// <param name="checkDir"></param>
        /// <returns></returns>
        private Vector3d CalControllerLayoutDir(Line line, Vector3d checkDir)
        {
            var dir = Vector3d.ZAxis.CrossProduct(line.EndPoint - line.StartPoint).GetNormal();
            if (checkDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }
            return dir;
        }
    }
}
