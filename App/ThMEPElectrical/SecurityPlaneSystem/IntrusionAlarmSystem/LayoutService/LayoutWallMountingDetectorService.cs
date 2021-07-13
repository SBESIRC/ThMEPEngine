using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.StructureHandleService;

namespace ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem
{
    public class LayoutWallMountingDetectorService
    {
        double blockWidth = 400;
        double blockLength = 400;
        double angle = 10;
        public DetectorModel LayoutDetector(Point3d doorPt, Vector3d doorDir, Polyline door, double doorLength, List<Polyline> columns, List<Polyline> walls, ControllerModel controller)
        {
            Circle circle = new Circle(doorPt, Vector3d.ZAxis, doorLength * 2);
            var circlePoly = circle.Tessellate(300);

            GetLayoutStructureService layoutStructureService = new GetLayoutStructureService();
            var structs = layoutStructureService.GetWallLayoutStruc(circlePoly, columns, walls);
            
            return CalWallMountingDetectorInfo(controller, structs, door, doorPt, doorDir);
        }

        /// <summary>
        /// 计算壁装探测器信息
        /// </summary>
        /// <param name="layoutInfo"></param>
        /// <param name="doorInfo"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private DetectorModel CalWallMountingDetectorInfo(ControllerModel controller, List<Polyline> structs, Polyline door, Point3d doorPt, Vector3d doorDir)
        {
            var movePt = CalMovePoint(doorPt, doorDir, door);
            var allLines = structs.SelectMany(x => x.GetAllLinesInPolyline()).ToList();

            var layoutPt = CalDetectorPointByWall(allLines, doorDir, movePt);
            if (layoutPt == null)
            {
                layoutPt = controller.LayoutPoint + doorDir * (blockWidth / 2);
            }

            //计算入侵报警控制器排布信息
            DetectorModel detector = new DetectorModel();
            var detectorLayoutDir = CalDetectorLayoutDir(doorPt, layoutPt.Value, doorDir);
            detector.LayoutDir = detectorLayoutDir;
            detector.LayoutPoint = layoutPt.Value + detectorLayoutDir * (blockLength / 3);

            return detector;
        }

        /// <summary>
        /// 计算移动投影点
        /// </summary>
        /// <param name="doorPt"></param>
        /// <param name="doorDir"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        private Point3d CalMovePoint(Point3d doorPt, Vector3d doorDir, Polyline door)
        {
            double distance = door.GetClosestPointTo(doorPt, false).DistanceTo(doorPt) + blockWidth;
            return doorPt + doorDir * distance;
        }

        /// <summary>
        /// 计算壁装探测器布置点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="doorPt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Point3d? CalDetectorPointByWall(List<Line> strucLines, Vector3d doorDir, Point3d movePt)
        {
            var usefulLines = strucLines.Where(x => x.Length >= blockWidth || (x.EndPoint - x.StartPoint).GetNormal().IsParallelWithTolerance(doorDir, angle)).ToList();

            var dir = Vector3d.ZAxis.CrossProduct(doorDir);
            Ray leftRay = new Ray() { BasePoint = movePt, UnitDir = dir };
            Ray rightRay = new Ray() { BasePoint = movePt, UnitDir = -dir };
            List<Point3d> layoutPts = new List<Point3d>();
            foreach (var line in usefulLines)
            {
                Point3dCollection pts = new Point3dCollection();
                leftRay.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    if (pts[0].DistanceTo(line.StartPoint) >= (blockWidth / 2 - 5) && pts[0].DistanceTo(line.EndPoint) > (blockWidth / 2 - 5))
                    {
                        layoutPts.Add(pts[0]);
                    }
                    continue;
                }

                pts = new Point3dCollection();
                rightRay.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    if (pts[0].DistanceTo(line.StartPoint) >= (blockWidth / 2 - 5) && pts[0].DistanceTo(line.EndPoint) > (blockWidth / 2 - 5))
                    {
                        layoutPts.Add(pts[0]);
                    }
                }
            }

            var resPt = layoutPts.OrderBy(x => x.DistanceTo(movePt)).FirstOrDefault();
            return resPt;
        }

        /// <summary>
        /// 计算壁装探测器布置方向
        /// </summary>
        /// <param name="doorPt"></param>
        /// <param name="layoutPt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Vector3d CalDetectorLayoutDir(Point3d doorPt, Point3d layoutPt, Vector3d dir)
        {
            var checkDir = (doorPt - layoutPt).GetNormal();
            var layoutDir = Vector3d.ZAxis.CrossProduct(dir);
            if (checkDir.DotProduct(layoutDir) < 0)
            {
                layoutDir = -layoutDir;
            }

            return layoutDir;
        }
    }
}
