using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.FEI.Service
{
    public static class CheckService
    {
        /// <summary>
        /// 检查是否和洞口以及柱相交(true有相交，false没有相交)
        /// </summary>
        /// <param name="line"></param>
        /// <param name="holes"></param>
        /// <param name="intersectHoles"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithHols(Line line, List<Polyline> holes, out List<Polyline> intersectHoles)
        {
            intersectHoles = new List<Polyline>();
            foreach (var hole in holes)
            {
                if (hole.IsIntersects(line))
                {
                    intersectHoles.Add(hole);
                }
            }

            if (intersectHoles.Count < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否和洞口以及柱相交(true有相交，false没有相交)
        /// </summary>
        /// <param name="line"></param>
        /// <param name="holes"></param>
        /// <param name="intersectHoles"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithHols(Ray ray, List<Polyline> holes, out List<Polyline> intersectHoles, out Point3d interPt)
        {
            intersectHoles = new List<Polyline>();
            interPt = ray.BasePoint;
            List<Point3d> allPts = new List<Point3d>();
            foreach (var hole in holes)
            {
                Point3dCollection intersectPts = new Point3dCollection();
                ray.IntersectWith(hole, Intersect.OnBothOperands, intersectPts, (IntPtr)0, (IntPtr)0);
                if (intersectPts.Count > 0)
                {
                    allPts.AddRange(intersectPts.Cast<Point3d>().ToList());
                }
            }

            if (intersectHoles.Count < 0)
            {
                return true;
            }
            else
            {
                interPt = allPts.OrderBy(x => x.DistanceTo(ray.BasePoint)).First();
                return false;
            }
        }
    }
}
