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
        public double angle = 135;
        public double layoutRange = 5000;
        public double blindArea = 1250;
        double bufferWidth = 300;
        public LayoutModel Layout(ThIfcRoom thRoom, Polyline door, List<Polyline> walls, List<Polyline> columns, List<Polyline> doors)
        {
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = getLayoutStructureService.GetUseRoomBoundary(thRoom, door);

            //找到可布置构建
            var roomPtInfo = getLayoutStructureService.GetDoorCenterPointOnRoom(room, door);
            var poly = getLayoutStructureService.GetLayoutRange(roomPtInfo.Item1, roomPtInfo.Item3, angle, layoutRange, blindArea);
            if (poly == null)
            {
                return null;
            }
            var bufferRoom = room.Buffer(50)[0] as Polyline;
            var nCols = getLayoutStructureService.GetNeedColumns(columns, bufferRoom, poly);
            var nWalls = getLayoutStructureService.GetNeedWalls(walls, bufferRoom, poly);
            //using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            //{
            //    db.ModelSpace.Add(poly);
            //    foreach (var item in nWalls)
            //    {
            //        db.ModelSpace.Add(item);
            //    }
            //}
            //计算布置点位
            var checkDoors = new List<Polyline>(doors);
            checkDoors.Remove(door);
            checkDoors = checkDoors.Select(x => x.Buffer(-5)[0] as Polyline).ToList();
            var pts = CreateClomunLayoutPt(room,roomPtInfo.Item1, nCols, nWalls, checkDoors);
            pts.AddRange(CreateWallLayoutPt(room,roomPtInfo.Item1, nCols, nWalls, checkDoors));
            var checkRoom = room.Buffer(-10)[0] as Polyline;
            pts = pts.Where(x => poly.Contains(x) && checkRoom.Contains(x)).ToList();

            var layoutPt = CalLayoutPt(pts, roomPtInfo.Item3, roomPtInfo.Item1);
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
        private List<Point3d> CreateClomunLayoutPt(Polyline room, Point3d doorPt, List<Polyline> columns, List<Polyline> walls, List<Polyline> doors)
        {
            List<Point3d> pts = new List<Point3d>();
            List<Polyline> checkStru = new List<Polyline>(columns);
            checkStru.AddRange(walls);
            checkStru.AddRange(doors);
            foreach (var column in columns)
            {
                var bufferColumn = (column.Buffer(bufferWidth)[0] as Polyline).DPSimplify(1);
                var allLines = UtilService.GetAllLinesInPolyline(bufferColumn);
                foreach (var line in allLines)
                {
                    var pt = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);
                    var checkLine = new Line(pt, doorPt);
                    if (CheckIntersectWithStruc(checkLine, checkStru, room))
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
        private List<Point3d> CreateWallLayoutPt(Polyline room, Point3d doorPt, List<Polyline> columns, List<Polyline> walls, List<Polyline> doors)
        {
            List<Point3d> pts = new List<Point3d>();
            List<Polyline> checkStru = new List<Polyline>(walls);
            checkStru.AddRange(columns);
            checkStru.AddRange(doors);
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var s in doors)
                {
                    //db.ModelSpace.Add(s);
                }
            }
            foreach (var wall in walls)
            {
                var bufferWall = wall.Buffer(bufferWidth)[0] as Polyline;
                var allPts = wall.Vertices();
                var allLines = UtilService.GetAllLinesInPolyline(bufferWall).Where(x => x.Length > bufferWidth * 2).ToList();
                foreach (Point3d pt in allPts)
                {
                    var interPts = allLines.Select(x => x.GetClosestPointTo(pt, false)).OrderBy(x => x.DistanceTo(pt)).ToList();
                    if (CheckIntersectWithStruc(new Line(interPts[0], doorPt), checkStru, room))
                    {
                        pts.Add(interPts[0]);
                    }
                    if (CheckIntersectWithStruc(new Line(interPts[1], doorPt), checkStru, room))
                    {
                        pts.Add(interPts[1]);
                    }
                }
            }

            return pts.Distinct().ToList();
        }

        /// <summary>
        /// 检查是否和墙或者柱相交(true:不相交，false：相交)
        /// </summary>
        /// <param name="checkLine"></param>
        /// <param name="walls"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private bool CheckIntersectWithStruc(Line checkLine, List<Polyline> strus, Polyline room)
        {
            var leftMatrix = Matrix3d.Rotation(Math.PI / 180 * 2, Vector3d.ZAxis, checkLine.StartPoint);
            var leftLine = checkLine.Clone() as Line;
            leftLine.TransformBy(leftMatrix.Inverse());
            var rightMatrix = Matrix3d.Rotation(Math.PI / 180 * -2, Vector3d.ZAxis, checkLine.StartPoint);
            var rightLine = checkLine.Clone() as Line;
            rightLine.TransformBy(rightMatrix.Inverse());
            using (Linq2Acad.AcadDatabase db =Linq2Acad.AcadDatabase.Active())
            {
                //db.ModelSpace.Add(leftLine);
                //db.ModelSpace.Add(rightLine);
                //foreach (var item in strus)
                //{
                //    db.ModelSpace.Add(item);
                //}
            }
            Point3dCollection pts = new Point3dCollection();
            checkLine.IntersectWith(room, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count > 1)
            {
                return false;
            }
            pts = new Point3dCollection();
            leftLine.IntersectWith(room, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count > 1)
            {
                return false;
            }
            pts = new Point3dCollection();
            rightLine.IntersectWith(room, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count > 1)
            {
                return false;
            }

            foreach (var wall in strus)
            {
                if (wall.Intersects(checkLine))
                {
                    return false;
                }
                if (wall.Intersects(leftLine))
                {
                    return false;
                }
                if (wall.Intersects(rightLine))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
