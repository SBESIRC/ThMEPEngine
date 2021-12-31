using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.GridOperation.Utils    
{
    public static class GridArcExtendService
    {
        /// <summary>
        /// 延申轴网
        /// </summary>
        /// <param name="lineGroup"></param>
        /// <returns></returns>
        public static List<ArcGridModel> ExtendGrid(List<ArcGridModel> arcGroup)
        {
            var resGroup = new List<ArcGridModel>();
            foreach (var group in arcGroup)
            {
                var extendLine = ExtendGroup(group);
                resGroup.Add(extendLine);
            }
            return resGroup;
        }

        /// <summary>
        /// 组内延伸线
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private static ArcGridModel ExtendGroup(ArcGridModel group)
        {
            var resDics = new ArcGridModel()
            {
                centerPt = group.centerPt,
                arcLines = new List<Arc>(),
                lines = new List<Line>(),
            };

            List<double> angle = group.lines.Select(x =>
            {
                var dir = (x.StartPoint - group.centerPt).GetNormal();
                return dir.GetAngleTo(Vector3d.XAxis);
            })
                .OrderBy(x => x)
                .ToList();
            foreach (var arcLine in group.arcLines)
            {
                var extendLine = ExtendArcGrid(arcLine, angle, group.centerPt);
                resDics.arcLines.Add(extendLine);
            }
            foreach (var line in group.lines)
            {
                var extendLine = ExtendLine(line, group.arcLines);
                resDics.lines.Add(extendLine);
            }

            return resDics;
        }

        /// <summary>
        /// 延申轴网直线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="arcs"></param>
        /// <returns></returns>
        private static Line ExtendLine(Line line, List<Arc> arcs)
        {
             Ray sRay = new Ray();
            sRay.BasePoint = line.StartPoint;
            sRay.UnitDir = (line.StartPoint - line.EndPoint).GetNormal();
            var sPt = GetIntersectPts(sRay, arcs);
            if (sPt == null)
            {
                sPt = line.StartPoint;
            }

            Ray eRay = new Ray();
            eRay.BasePoint = line.EndPoint;
            eRay.UnitDir = (line.EndPoint - line.StartPoint).GetNormal();
            var ePt = GetIntersectPts(eRay, arcs);
            if (ePt == null)
            {
                ePt = line.EndPoint;
            }

            return new Line(sPt.Value, ePt.Value);
        }

        /// <summary>
        /// 获取相交点
        /// </summary>
        /// <param name="Ray"></param>
        /// <param name="lineGroup"></param>
        /// <returns></returns>
        private static Point3d? GetIntersectPts(Ray Ray, List<Arc> arcGroup)
        {
            var intersectPts = arcGroup.Select(x =>
            {
                Point3dCollection point3DCollection = new Point3dCollection();
                x.IntersectWith(Ray, Intersect.OnBothOperands, point3DCollection, (IntPtr)0, (IntPtr)0);
                if (point3DCollection.Count > 0)
                {
                    return point3DCollection[0] as Point3d?;
                }
                return null;
            })
            .Where(x => x != null)
            .ToList();
            if (intersectPts.Count <= 0)
            {
                return null;
            }

            var intersectPt = intersectPts.Select(x => x.Value)
            .OrderBy(x => x.DistanceTo(Ray.BasePoint))
            .FirstOrDefault();

            return intersectPt;
        }

        /// <summary>
        /// 延申弧形轴网线
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="lineAngle"></param>
        /// <param name="centerPt"></param>
        /// <returns></returns>
        private static Arc ExtendArcGrid(Arc arc, List<double> lineAngle, Point3d centerPt)
        {
            double sAngle = arc.StartAngle < arc.EndAngle ? arc.StartAngle : arc.EndAngle;
            double eAngle = arc.StartAngle > arc.EndAngle ? arc.StartAngle : arc.EndAngle;
            if (!(lineAngle.Any(x => Math.Abs(sAngle - x) < 0.001)))
            {
                var minAngles = lineAngle.Where(x => x < sAngle).ToList();
                if (minAngles.Count > 0)
                {
                    sAngle = minAngles.OrderBy(x => Math.Abs(x - sAngle)).First();
                }
            }
            if (!(lineAngle.Any(x => Math.Abs(eAngle - x) < 0.001)))
            {
                var maxAngles = lineAngle.Where(x => x > eAngle).ToList();
                if (maxAngles.Count > 0)
                {
                    eAngle = maxAngles.OrderBy(x => Math.Abs(x - sAngle)).First();
                }
            }

            return new Arc(centerPt, arc.Radius, sAngle, eAngle);
        }
    }
}
