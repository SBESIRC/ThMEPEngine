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
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService
{
    public class LayoutVideo
    {
        double bufferWidth = 300;
        public LayoutModel Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> walls, List<Polyline> columns)
        {
            Polyline room = thRoom.Boundary as Polyline; 
            //找到可布置构建
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var roomPtInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var poly = getLayoutStructureService.GetLayoutRange(roomPtInfo.Item1, roomPtInfo.Item2);
            if (poly != null)
            {
                return null;
            }
            var nCols = getLayoutStructureService.GetNeedColumns(columns, room, poly);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, room, poly);

            //计算布置点位
            var pts = CreateClomunLayoutPt(roomPtInfo.Item1, nCols, walls);
            pts.AddRange(CreateWallLayoutPt(roomPtInfo.Item1, nWalls, walls));

            var layoutPt = CalLayoutPt(pts, roomPtInfo.Item2, roomPtInfo.Item1);
            var layoutDir = (roomPtInfo.Item1 - layoutPt).GetNormal();

            LayoutModel layoutModel = new LayoutModel();
            layoutModel.layoutPt = layoutPt;
            layoutModel.layoutDir = layoutDir;
            return layoutModel;
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
