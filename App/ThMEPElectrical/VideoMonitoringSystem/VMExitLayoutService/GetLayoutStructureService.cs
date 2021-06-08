using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.VideoMonitoringSystem.Model;
using ThMEPElectrical.VideoMonitoringSystem.Utls;

namespace ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService
{
    public class GetLayoutStructureService
    {
        double tol = 10;
        double angle = 135;
        double layoutRange = 10000;
        double blindArea = 1250;
        double length = 200;

        public List<LayoutInfoModel> GetStructureService(List<Polyline> rooms, List<Polyline> doors, List<Polyline> columns, List<Polyline> walls)
        {
            List<LayoutInfoModel> roomInfo = new List<LayoutInfoModel>();
            foreach (var room in rooms)
            {
                room.Closed = true;
                var bufferRoom = room.Buffer(tol)[0] as Polyline;
                var needDoors = GetNeedDoors(doors, bufferRoom);

                foreach (var nDoor in needDoors)
                {
                    LayoutInfoModel layoutInfo = new LayoutInfoModel();
                    var roomPtInfo = GetDoorCenterPointOnRoom(room, nDoor);
                    var poly = GetLayoutRange(roomPtInfo);
                    if (poly != null)
                    {
                        var nCols = GetNeedColumns(columns, poly);
                        var nWalls = GetNeedWalls(walls, poly);
                        layoutInfo.room = room;
                        layoutInfo.doorCenterPoint = roomPtInfo.Key;
                        layoutInfo.doorDir = roomPtInfo.Value;
                        layoutInfo.walls = nWalls;
                        layoutInfo.colums = nCols;
                        roomInfo.Add(layoutInfo);
                    }
                }
            }

            return roomInfo;
        }

        /// <summary>
        /// 计算房间内能够布置摄像头的范围
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        private Polyline GetLayoutRange(KeyValuePair<Point3d, Vector3d> roomPtInfo)
        {
            var rotateAngle = (angle / 2) * Math.PI / 180;
            var dir1 = roomPtInfo.Value.RotateBy(rotateAngle, Vector3d.ZAxis);
            var dir2 = roomPtInfo.Value.RotateBy(rotateAngle, -Vector3d.ZAxis);

            Circle circle = new Circle(roomPtInfo.Key, Vector3d.ZAxis, layoutRange);
            var criclePoly = circle.Tessellate(length);

            Ray ray = new Ray() { BasePoint = roomPtInfo.Key, UnitDir = dir1 };
            Point3dCollection pts = new Point3dCollection();
            circle.IntersectWith(ray, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            var pt1 = pts.Cast<Point3d>().Where(x => x.IsEqualTo(roomPtInfo.Key, new Tolerance(1, 1))).FirstOrDefault();

            ray.UnitDir = dir2;
            pts = new Point3dCollection();
            circle.IntersectWith(ray, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            var pt2 = pts.Cast<Point3d>().Where(x => x.IsEqualTo(roomPtInfo.Key, new Tolerance(1, 1))).FirstOrDefault();

            var objCollection = criclePoly.GetAllLinesInPolyline();
            objCollection.Add(new Line(roomPtInfo.Key, pt1));
            objCollection.Add(new Line(roomPtInfo.Key, pt2));
            List<Polyline> polygons = objCollection.ToCollection().Polygons().Cast<Polyline>().ToList();
            var resPoly = polygons.OrderBy(x => x.Area).FirstOrDefault();

            Circle blindCircle = new Circle(roomPtInfo.Key, Vector3d.ZAxis, blindArea);
            var blindCriclePoly = blindCircle.Tessellate(length);

            return resPoly.Difference(blindCriclePoly)[0] as Polyline;
        }

        /// <summary>
        /// 找到门在房间框线上的中心点信息
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        private KeyValuePair<Point3d, Vector3d> GetDoorCenterPointOnRoom(Polyline room, Polyline door)
        {
            var lines = door.GetAllLinesInPolyline().OrderByDescending(x => x.Length).ToList();
            Point3d pt1 = new Point3d((lines[0].StartPoint.X + lines[0].StartPoint.X) / 2, (lines[0].StartPoint.Y + lines[0].StartPoint.Y) / 2, 0);
            Point3d pt2 = new Point3d((lines[1].StartPoint.X + lines[1].StartPoint.X) / 2, (lines[1].StartPoint.Y + lines[1].StartPoint.Y) / 2, 0);
            var roomPt = room.GetClosestPointTo(pt1, false);

            var ep = roomPt.DistanceTo(pt1) < roomPt.DistanceTo(pt2) ? pt1 : pt2;
            var sp = roomPt.DistanceTo(pt2) > roomPt.DistanceTo(pt1) ? pt2 : pt1;
            var dir = (ep - sp).GetNormal();

            return new KeyValuePair<Point3d, Vector3d>(roomPt, dir);
        }

        /// <summary>
        /// 找到需要的与房间相交的门
        /// </summary>
        /// <param name="doors"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Polyline> GetNeedDoors(List<Polyline> doors, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(doors.ToCollection());
            return thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到需要的房间内的柱
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Polyline> GetNeedColumns(List<Polyline> columns, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            return thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到房间内需要的墙
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Polyline> GetNeedWalls(List<Polyline> walls, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
            var needWalls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();

            List<Polyline> resWalls = new List<Polyline>();
            foreach (var wall in needWalls)
            {
                resWalls.AddRange(room.Difference(wall).Cast<Polyline>());
            }
            return resWalls;
        }
    }
}

