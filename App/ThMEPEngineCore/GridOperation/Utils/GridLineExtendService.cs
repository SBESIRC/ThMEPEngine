using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public static class GridLineExtendService
    {
        public static Dictionary<Vector3d, List<Line>> ExtendGrid(Dictionary<Vector3d, List<Line>> lineGroup)
        {
            var resGroup = new Dictionary<Vector3d, List<Line>>();
            foreach (var group in lineGroup)
            {
                var extendLine = ExtendGroup(group, lineGroup);
                resGroup.Add(extendLine.Key, extendLine.Value);
            }
            return resGroup;
        }

        private static KeyValuePair<Vector3d, List<Line>> ExtendGroup(KeyValuePair<Vector3d, List<Line>> group, Dictionary<Vector3d, List<Line>> lineGroup)
        {
            var otherGroups = lineGroup.Where(x => x.Key != group.Key).ToDictionary(x => x.Key, y => y.Value);
            var resDics = new KeyValuePair<Vector3d, List<Line>>(group.Key , new List<Line>());
            foreach (var line in group.Value)
            {
                var extendLine = ExtendLine(line, otherGroups);
                resDics.Value.Add(extendLine);
            }

            return resDics;
        }

        private static Line ExtendLine(Line line, Dictionary<Vector3d, List<Line>> lineGroup) 
        {
            Ray sRay = new Ray();
            sRay.BasePoint = line.StartPoint;
            sRay.UnitDir = (line.StartPoint - line.EndPoint).GetNormal();
            var sPt = GetIntersectPts(sRay, lineGroup);
            if (sPt == null)
            {
                sPt = line.StartPoint;
            }

            Ray eRay = new Ray();
            eRay.BasePoint = line.EndPoint;
            eRay.UnitDir = (line.EndPoint - line.StartPoint).GetNormal();
            var ePt = GetIntersectPts(eRay, lineGroup);
            if (ePt == null)
            {
                ePt = line.EndPoint;
            }
            return new Line(sPt.Value, ePt.Value);
        }

        private static Point3d? GetIntersectPts(Ray Ray, Dictionary<Vector3d, List<Line>> lineGroup)
        {
            var resPt = new List<Point3d>();
            foreach (var group in lineGroup)
            {
                var intersectPt = group.Value.Select(x =>
                {
                    Point3dCollection point3DCollection = new Point3dCollection();
                    x.IntersectWith(Ray, Intersect.ExtendArgument, point3DCollection, (IntPtr)0, (IntPtr)0);
                    if (point3DCollection.Count > 0)
                    {
                        return point3DCollection[0] as Point3d?;
                    }
                    return null;
                })
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .OrderBy(x => x.DistanceTo(Ray.BasePoint))
                    .FirstOrDefault();
                if (intersectPt != null)
                {
                    resPt.Add(intersectPt);
                }
            }

            return resPt.OrderByDescending(x => x.DistanceTo(Ray.BasePoint)).FirstOrDefault();
        }
    }
}
