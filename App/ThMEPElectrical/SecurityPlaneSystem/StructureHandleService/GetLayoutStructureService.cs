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
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.StructureHandleService
{
    public class GetLayoutStructureService
    {
        double length = 200;
        /// <summary>
        /// 计算房间内能够布置摄像头的范围
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        public Polyline GetLayoutRange(Point3d pt, Vector3d dir, double angle, double layoutRange, double blindArea)
        {
            var rotateAngle = (angle / 2) * Math.PI / 180;
            var dir1 = dir.RotateBy(rotateAngle, Vector3d.ZAxis);
            var dir2 = dir.RotateBy(rotateAngle, -Vector3d.ZAxis);

            Circle circle = new Circle(pt, Vector3d.ZAxis, layoutRange);
            var criclePoly = circle.Tessellate(length);

            Ray ray = new Ray() { BasePoint = pt, UnitDir = dir1 };
            Point3dCollection pts = new Point3dCollection();
            circle.IntersectWith(ray, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            var pt1 = pts[0];

            ray.UnitDir = dir2;
            pts = new Point3dCollection();
            circle.IntersectWith(ray, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            var pt2 = pts[0];

            var objCollection = criclePoly.GetAllLinesInPolyline();
            objCollection.Add(new Line(pt, pt1));
            objCollection.Add(new Line(pt, pt2));
            List<Polyline> polygons = objCollection.ToCollection().PolygonsEx().Cast<Polyline>().ToList();
            var resPoly = polygons.OrderBy(x => x.Area).FirstOrDefault();

            Circle blindCircle = new Circle(pt, Vector3d.ZAxis, blindArea);
            var blindCriclePoly = blindCircle.Tessellate(length);
            var result = resPoly.Difference(blindCriclePoly);
            if(result.Count == 0)
            {
                return null;
            }
            return result[0] as Polyline;
        }

        /// <summary>
        /// 找到门在房间框线上的中心点信息
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        public Tuple<Point3d, Point3d, Vector3d, double, double> GetDoorCenterPointOnRoom(Polyline room, Polyline door)
        {
            door = door.DPSimplify(1);
            var lines = door.GetAllLinesInPolyline().OrderByDescending(x => x.Length).ToList();
            double doorLength = lines.First().Length;
            double doorWidth = lines.Last().Length;
            Point3d pt1 = new Point3d((lines[0].StartPoint.X + lines[0].EndPoint.X) / 2, (lines[0].StartPoint.Y + lines[0].EndPoint.Y) / 2, 0);
            Point3d pt2 = new Point3d((lines[1].StartPoint.X + lines[1].EndPoint.X) / 2, (lines[1].StartPoint.Y + lines[1].EndPoint.Y) / 2, 0);
            var roomPt = room.GetClosestPointTo(pt1, false);

            var ep = roomPt.DistanceTo(pt1) < roomPt.DistanceTo(pt2) ? pt1 : pt2;
            var sp = roomPt.DistanceTo(pt2) > roomPt.DistanceTo(pt1) ? pt2 : pt1;
            var dir = (ep - sp).GetNormal();

            return Tuple.Create(ep, sp, dir, doorLength, doorWidth);
        }

        /// <summary>
        /// 计算门中心点
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        public Point3d GetDoorCenterPt(Polyline door)
        {
            door = door.DPSimplify(1);
            var lines = door.GetAllLinesInPolyline().OrderByDescending(x => x.Length).ToList();
            Point3d pt1 = new Point3d((lines[0].StartPoint.X + lines[0].EndPoint.X) / 2, (lines[0].StartPoint.Y + lines[0].EndPoint.Y) / 2, 0);
            Point3d pt2 = new Point3d((lines[1].StartPoint.X + lines[1].EndPoint.X) / 2, (lines[1].StartPoint.Y + lines[1].EndPoint.Y) / 2, 0);
            var centerPt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            return centerPt;
        }

        /// <summary>
        /// 找到与门相交的房间
        /// </summary>
        /// <param name="door"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        public List<ThIfcRoom> GetNeedTHRooms(Polyline door, List<ThIfcRoom> rooms)
        {
            List<ThIfcRoom> needRooms = new List<ThIfcRoom>();
            foreach (var room in rooms)
            {
                if (door.Intersects(room.Boundary))
                {
                    needRooms.Add(room);
                }
            }

            return needRooms;
        }

        /// <summary>
        /// 找到需要的房间内的柱
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> GetNeedColumns(List<Polyline> columns, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            var roomColumns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room);
            return roomColumns.Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到需要的房间内的柱
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> GetNeedColumns(List<Polyline> columns, Polyline room, Polyline range)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
            var roomColumns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room);
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(roomColumns);
            return thCADCoreNTSSpatialIndex.SelectCrossingPolygon(range).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到需要的与房间相交的门
        /// </summary>
        /// <param name="doors"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> GetNeedDoors(List<Polyline> doors, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(doors.ToCollection());
            return thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到房间内需要的墙
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> GetNeedWalls(List<Polyline> walls, Polyline room, Polyline range)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
            var needWalls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room);
            if (needWalls.Count <= 0)
            {
                return new List<Polyline>();
            }
            needWalls = room.Intersection(needWalls);
            return range.Intersection(needWalls).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到房间内需要的墙
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> GetNeedWalls(List<Polyline> walls, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
            var needWalls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room);

            return needWalls.Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 找到房间内相交的洞口
        /// </summary>
        /// <param name="walls"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Polyline> GetNeedHoles(List<Polyline> holes, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(holes.ToCollection());
            var needWalls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room);

            return needWalls.Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 获取空间内的车道线或者中心线
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Line> GetNeedLanes(List<Line> lanes, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(lanes.ToCollection());
            var needLanes = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Line>().ToList();
            return needLanes.SelectMany(x => room.Trim(x).Cast<Polyline>().Select(y => new Line(y.StartPoint, y.EndPoint))).ToList();
        }

        /// <summary>
        /// 获取空间内的车道线或者中心线
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<List<Line>> GetNeedLanes(List<List<Line>> lanes, Polyline room)
        {
            List<List<Line>> resLines = new List<List<Line>>();
            foreach (var laneLst in lanes)
            {
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(laneLst.ToCollection());
                var needLanes = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Line>().ToList();
                var trimLanes = needLanes.SelectMany(x => room.Trim(x).Cast<Polyline>().Select(y => new Line(y.StartPoint, y.EndPoint))).ToList();
                if (trimLanes.Count() > 0)
                {
                    resLines.Add(trimLanes);
                }

            }

            return resLines; 
        }

        /// <summary>
        /// 找到可能可以布置控制器的构建
        /// </summary>
        /// <param name="door"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public List<Polyline> CalLayoutStruc(Polyline door, List<Polyline> columns, List<Polyline> walls)
        {
            var bufferDoor = door.Buffer(5)[0] as Polyline;
            List<Polyline> structs = new List<Polyline>();
            foreach (var column in columns)
            {
                if (column.Intersects(bufferDoor))
                {
                    structs.Add(column);
                }
            }

            foreach (var wall in walls)
            {
                if (wall.Intersects(bufferDoor))
                {
                    structs.Add(wall);
                }
            }

            return structs;
        }

        /// <summary>
        /// 找到可能可以布置壁装探测器的构建
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public List<Polyline> GetWallLayoutStruc(Polyline circle, List<Polyline> columns, List<Polyline> walls)
        {
            List<Polyline> structs = new List<Polyline>();
            foreach (var column in columns)
            {
                if (column.Intersects(circle))
                {
                    structs.Add(column);
                }
            }

            structs.AddRange(circle.Intersection(walls.ToCollection()).Cast<Polyline>().ToList());
            return structs;
        }

        /// <summary>
        /// 找到合适的房间框线
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        public Polyline GetUseRoomBoundary(ThIfcRoom room, Polyline door)
        {
            Polyline polyRoom = null;
            if (room.Boundary is Polyline polyline)
            {
                polyRoom = polyline;
            }
            else if (room.Boundary is MPolygon mPolygon)
            {
                var bufferDoor = door.Buffer(10)[0] as Polyline;
                foreach (Polyline loop in mPolygon.Loops())
                {
                    if (loop.Intersects(bufferDoor))
                    {
                        return loop;
                    }
                }
            }
            return polyRoom;
        }

        /// <summary>
        /// 找到合适的房间框线
        /// </summary>
        /// <param name="room"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        public Polyline GetUseRoomBoundary(ThIfcRoom room)
        {
            Polyline polyRoom = null;
            if (room.Boundary is Polyline polyline)
            {
                polyRoom = polyline;
            }
            else if (room.Boundary is MPolygon mPolygon)
            {
                return mPolygon.Loops()[0];
            }
            return polyRoom;
        }
    }
}