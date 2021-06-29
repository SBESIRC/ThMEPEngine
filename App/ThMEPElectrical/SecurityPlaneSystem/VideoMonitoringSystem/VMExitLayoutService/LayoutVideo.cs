using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.Utls;

namespace ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService
{
    public class LayoutVideo
    {
        double bufferWidth = 300;
        public KeyValuePair<Point3d, Vector3d> Layout(Point3d doorPt, Vector3d dir, List<Polyline> walls, List<Polyline> columns)
        {
            var pts = CreateClomunLayoutPt(doorPt, columns, walls);
            pts.AddRange(CreateWallLayoutPt(doorPt, columns, walls));

            var layoutPt = CalLayoutPt(pts, dir, doorPt);
            var layoutDir = (doorPt - layoutPt).GetNormal();

            return new KeyValuePair<Point3d, Vector3d>(layoutPt, layoutDir);
        }

        /// <summary>
        /// 找到合适的可布置点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="dir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        private Point3d CalLayoutPt(List<Point3d> pts, Vector3d dir, Point3d doorPt)
        {
            return pts.Distinct().ToDictionary(x =>x,y =>
            {
                var layoutDir = (y - doorPt).GetNormal();
                double angle = layoutDir.GetAngleTo(dir);
                if (angle > Math.PI)
                {
                    angle = Math.PI * 2 - angle;
                }

                return angle;
            })
            .OrderBy(x=>x.Value)
            .Select(x=>x.Key)
            .FirstOrDefault();
        }

        /// <summary>
        /// 找到柱上的可布置点
        /// </summary>
        /// <param name="doorPt"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        private List<Point3d> CreateClomunLayoutPt(Point3d doorPt, List<Polyline> columns, List<Polyline> walls)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (var column in columns)
            {
                var bufferColumn = (column.Buffer(bufferWidth)[0] as Polyline).DPSimplify(1);
                var allLines = UtilService.GetAllLinesInPolyline(bufferColumn);
                foreach (var line in allLines)
                {
                    var pt = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);
                    var checkLine = new Line(pt, doorPt);
                    if (CheckIntersectWithStruc(checkLine, walls, columns))
                    {
                        pts.Add(pt);
                    }
                }
            }

            return pts;
        }

        /// <summary>
        /// 找到墙上的可布置点
        /// </summary>
        /// <param name="doorPt"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        private List<Point3d> CreateWallLayoutPt(Point3d doorPt, List<Polyline> columns, List<Polyline> walls)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (var wall in walls)
            {
                var bufferWall = wall.Buffer(bufferWidth)[0] as Polyline;
                var allPts = bufferWall.Vertices();
                foreach (Point3d pt in allPts)
                {
                    var checkLine = new Line(pt, doorPt);
                    if (CheckIntersectWithStruc(checkLine, walls, columns))
                    {
                        var allLines = UtilService.GetAllLinesInPolyline(bufferWall);
                        var interPts = allLines.Select(x => x.GetClosestPointTo(pt, false)).OrderBy(x => x.DistanceTo(pt)).ToList();
                        pts.Add(interPts[0]);
                        pts.Add(interPts[1]);
                    }
                }
            }

            return pts;
        }

        /// <summary>
        /// 检查是否和墙或者柱相交(true:不相交，false：相交)
        /// </summary>
        /// <param name="checkLine"></param>
        /// <param name="walls"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private bool CheckIntersectWithStruc(Line checkLine, List<Polyline> walls, List<Polyline> columns)
        {
            foreach (var wall in walls)
            {
                if (wall.Intersects(checkLine))
                {
                    return false;
                }
            }

            foreach (var column in columns)
            {
                if (column.Intersects(checkLine))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
