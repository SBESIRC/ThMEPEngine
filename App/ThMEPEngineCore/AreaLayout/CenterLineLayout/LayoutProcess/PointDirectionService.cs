using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.LayoutProcess
{
    internal class PointDirectionService
    {
        /// <summary>
        /// 获取布置点的方向
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="holeList"></param>
        /// <param name="points"></param>
        /// <param name="pointsWithDirection"></param>
        public static void PointsWithDirection(Polyline frame, List<Polyline> holeList, List<Point3d> points, Dictionary<Point3d, Vector3d> pointsWithDirection)
        {
            var lines = new List<Line>();
            var dbObjs = new DBObjectCollection();
            frame.Explode(dbObjs);
            foreach (var pl in holeList)
            {
                lines.AddRange(pl.ToLines());
            }
            foreach (var curve in dbObjs)
            {
                if (curve is Line line)
                {
                    if (line.StartPoint != line.EndPoint)
                    {
                        lines.Add(line);
                    }
                }
                else if (curve is Polyline poly)
                {
                    lines.AddRange(poly.ToLines());
                }
                else if (curve is Circle circle)
                {
                    lines.AddRange(circle.Tessellate(100.0).ToLines());
                }
            }
            foreach (var pt in points)
            {
                var closestLine = lines.OrderBy(o => o.GetClosestPointTo(pt, false).DistanceTo(pt)).First();
                //HostApplicationServices.WorkingDatabase.AddToModelSpace(new Line(pt, closestLine.StartPoint));//--------------------显示链接线
                pointsWithDirection.Add(pt, (closestLine.EndPoint - closestLine.StartPoint).GetNormal());
            }
        }

    }
}
