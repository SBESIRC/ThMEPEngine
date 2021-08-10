using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.Service;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutHositingDetectorService
    {
        double moveDistance = 300;
        public DetectorModel LayoutDetector(Point3d doorPt, Vector3d doorDir, Polyline door)
        {
            return CalHoistingDetectorInfo(doorPt, doorDir, door);
        }

        /// <summary>
        /// 计算吊装探测器信息
        /// </summary>
        /// <param name="layoutInfo"></param>
        /// <param name="doorInfo"></param>
        /// <param name="dir"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        private DetectorModel CalHoistingDetectorInfo(Point3d doorPt, Vector3d dir, Polyline door)
        {
            double distance = door.GetClosestPointTo(doorPt, false).DistanceTo(doorPt) + moveDistance;

            //计算探测器排布信息
            DetectorModel detector = new DetectorModel();
            var detectorLayoutPt = CalHositingDetectorLayoutPoint(distance, doorPt, dir);
            detector.LayoutDir = Vector3d.ZAxis;
            detector.LayoutPoint = detectorLayoutPt;

            return detector;
        }

        /// <summary>
        /// 计算吊装探测器布置点
        /// </summary>
        /// <param name="doorPt"></param>
        /// <param name="layoutPt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Point3d CalHositingDetectorLayoutPoint(double distance, Point3d doorPt, Vector3d dir)
        {
            return doorPt + dir * distance;
        }
    }
}
