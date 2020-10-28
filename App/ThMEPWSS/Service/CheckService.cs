using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Noding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPWSS.Model;

namespace ThMEPWSS.Service
{
    public class CheckService
    {
        public List<ThIfcBeam> allBeams = new List<ThIfcBeam>();

        /// <summary>
        /// 判断是否距梁过近
        /// </summary>
        /// <param name="sprayPoly"></param>
        /// <param name="sprays"></param>
        /// <param name="dir"></param>
        /// <param name="dis"></param>
        /// <returns></returns>
        public bool CheckSprayData(Line sprayPoly, List<SprayLayoutData> sprays, Vector3d dir, double dis)
        {
            var polys = SprayDataOperateService.GetAllSanmeDirLines(dir, sprays);
            var pts = SprayDataOperateService.CalSprayPoint(polys, sprayPoly);
            foreach (var beam in allBeams)
            {
                foreach (var pt in pts)
                {
                    var closet = (beam.Outline as Polyline).GetClosestPointTo(pt, false);
                    if (closet.DistanceTo(pt) < dis)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 校验边界防止产生新盲区
        /// </summary>
        /// <param name="boundarys"></param>
        /// <param name="position"></param>
        /// <param name="newPosition"></param>
        /// <param name="maxLenghth"></param>
        /// <returns></returns>
        public bool CheckBoundaryLines(List<Line> boundarys, Point3d position, Point3d newPosition, double maxLenghth)
        {
            foreach (var bLine in boundarys)
            {
                Point3d closetPt = bLine.GetClosestPointTo(position, true);
                double length = closetPt.DistanceTo(position);

                Point3d newClosetPt = bLine.GetClosestPointTo(newPosition, true);
                double newLength = newClosetPt.DistanceTo(newPosition);

                if (length <= maxLenghth && newLength > maxLenghth)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断两个喷淋点是否满足间距
        /// </summary>
        /// <param name="position"></param>
        /// <param name="newPosition"></param>
        /// <param name="maxSpacing"></param>
        /// <param name="minSpacing"></param>
        /// <returns></returns>
        public bool CheckSprayPtDistance(Point3d position, Point3d newPosition, double maxSpacing, double minSpacing)
        {
            double distance = newPosition.DistanceTo(position);
            Vector3d compareDir = (position - newPosition).GetNormal();
            double compareX = Math.Abs(compareDir.X) > Math.Abs(compareDir.Y) ? Math.Abs(compareDir.X) : Math.Abs(compareDir.Y);
            double compareValue = distance * compareX;

            return compareValue >= minSpacing && compareValue <= maxSpacing;
        }
    }
}
