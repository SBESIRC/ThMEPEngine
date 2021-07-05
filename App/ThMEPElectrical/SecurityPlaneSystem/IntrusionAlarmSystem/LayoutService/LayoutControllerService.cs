using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.Utls;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutControllerService
    {
        double angle = 45;
        double blockWidth = 300;
        public ControllerModel LayoutController(List<Polyline> structs, Polyline polylibe, Point3d doorPt, Vector3d doorDir)
        {
            var layoutInfo = UtilService.CalLayoutInfo(structs, polylibe, doorDir, doorPt, angle, blockWidth).First();
            var controller = CalControllerInfo(layoutInfo, doorDir);

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
            controller.LayoutPoint = layoutInfo.Value;
            controller.LayoutDir = controllerLayoutDir;

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
