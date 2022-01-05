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
    public static class GridLineExtendService
    {
        static double maxLength = 10000;
        /// <summary>
        /// 延申轴网
        /// </summary>
        /// <param name="lineGroup"></param>
        /// <returns></returns>
        public static List<LineGridModel> ExtendGrid(List<LineGridModel> lineGroup, double extendLength = 10000)
        {
            maxLength = extendLength;
            var resGroup = new List<LineGridModel>();
            foreach (var group in lineGroup)
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
        private static LineGridModel ExtendGroup(LineGridModel group)
        {
            var resDics = new LineGridModel()
            {
                vecter = group.vecter,
                xLines = new List<Line>(),
                yLines = new List<Line>(),
            };
            foreach (var line in group.xLines)
            {
                var extendLine = ExtendLine(line, group.yLines);
                resDics.xLines.Add(extendLine);
            }
            foreach (var line in group.yLines)
            {
                var extendLine = ExtendLine(line, group.xLines);
                resDics.yLines.Add(extendLine);
            }

            return resDics;
        }

        /// <summary>
        /// 延申一根线搭到最近的线上
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineGroup"></param>
        /// <returns></returns>
        private static Line ExtendLine(Line line, List<Line> lineGroup) 
        {
            Ray sRay = new Ray();
            sRay.BasePoint = line.StartPoint;
            sRay.UnitDir = (line.StartPoint - line.EndPoint).GetNormal();
            var sPt = GetIntersectPts(sRay, lineGroup);
            if (sPt == null || line.StartPoint.DistanceTo(sPt.Value) > maxLength)
            {
                sPt = line.StartPoint;
            }

            Ray eRay = new Ray();
            eRay.BasePoint = line.EndPoint;
            eRay.UnitDir = (line.EndPoint - line.StartPoint).GetNormal();
            var ePt = GetIntersectPts(eRay, lineGroup);
            if (ePt == null || line.EndPoint.DistanceTo(ePt.Value) > maxLength)
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
        private static Point3d? GetIntersectPts(Ray Ray, List<Line> lineGroup)
        {
            var intersectPts = lineGroup.Select(x =>
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
    }
}
