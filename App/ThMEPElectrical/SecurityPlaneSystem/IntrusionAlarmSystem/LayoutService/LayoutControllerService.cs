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
        public ControllerModel LayoutController(List<Polyline> structs, Point3d doorPt, Vector3d doorDir)
        {
            var layoutInfo = CalControllerLayoutPt(structs, doorDir, doorPt).First();
            var controller = CalControllerInfo(layoutInfo, doorDir);

            return controller;
        }

        /// <summary>
        /// 计算控制器布置点位
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="dir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private Dictionary<Line, Point3d> CalControllerLayoutPt(List<Polyline> structs, Vector3d dir, Point3d doorPt)
        {
            Dictionary<Line, Point3d> resLayoutInfo = new Dictionary<Line, Point3d>();
            foreach (var str in structs)
            {
                var allLines = str.GetAllLinesInPolyline();
                foreach (var line in allLines)
                {
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    if (!dir.IsParallelWithTolerance(lineDir, angle) && line.Length > blockWidth)
                    {
                        var pt = line.StartPoint.DistanceTo(doorPt) < line.EndPoint.DistanceTo(doorPt) ? line.StartPoint : line.EndPoint;
                        var checkDir = (pt - doorPt).GetNormal();
                        if (checkDir.DotProduct(lineDir) < 0)
                        {
                            lineDir = -lineDir;
                        }

                        var layoutPt = pt + lineDir * (blockWidth / 2);
                        resLayoutInfo.Add(line, layoutPt);
                    }
                }
            }

            return resLayoutInfo.OrderBy(x => x.Value.DistanceTo(doorPt)).ToDictionary(x => x.Key, y => y.Value);
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
