using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Data
{
    public static class HandleStructService
    {
        /// <summary>
        /// 获取最大外包框
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="mainSewagePipes"></param>
        /// <param name="mainRainPipes"></param>
        /// <returns></returns>
        public static Polyline GetMaxFrame(List<Polyline> rooms, List<Polyline> mainSewagePipes, List<Polyline> mainRainPipes)
        {
            var allPts = rooms.SelectMany(x => GeometryUtils.GetAllPolylinePts(x)).ToList();
            allPts.AddRange(mainSewagePipes.SelectMany(x => GeometryUtils.GetAllPolylinePts(x)));
            allPts.AddRange(mainRainPipes.SelectMany(x => GeometryUtils.GetAllPolylinePts(x)));
            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;
            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            Polyline frame = new Polyline { Closed = true };
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);

            return frame.Buffer(1000)[0] as Polyline;
        }

        /// <summary>
        ///获取需要的地图框线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        public static Polyline GetNeedFrame(Line line, List<Polyline> rooms)
        {
            var xDir = (line.EndPoint - line.StartPoint).GetNormal();
            var yDir = Vector3d.ZAxis.CrossProduct(xDir);
            var zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            var allPts = rooms.SelectMany(x => GeometryUtils.GetAllPolylinePts(x))
                .Select(y => y.TransformBy(matrix.Inverse())).ToList();
            allPts.AddRange(new List<Point3d>() { line.StartPoint.TransformBy(matrix.Inverse()), line.EndPoint.TransformBy(matrix.Inverse()) });
            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            Polyline frame = new Polyline { Closed = true };
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);
            frame.TransformBy(matrix);

            return frame.Buffer(1000)[0] as Polyline;
        }

        /// <summary>
        ///获取需要的地图框线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        public static Polyline GetNeedFrame(Line line, Point3d pt)
        {
            var xDir = (line.EndPoint - line.StartPoint).GetNormal();
            var yDir = Vector3d.ZAxis.CrossProduct(xDir);
            var zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            var allPts = new List<Point3d>() { line.StartPoint, line.EndPoint };
            allPts.Add(pt);
            allPts = allPts.Select(y => y.TransformBy(matrix.Inverse())).ToList(); ;
            allPts = allPts.OrderBy(x => x.X).ToList();
            double minX = allPts.First().X;
            double maxX = allPts.Last().X;
            allPts = allPts.OrderBy(x => x.Y).ToList();
            double minY = allPts.First().Y;
            double maxY = allPts.Last().Y;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            Polyline frame = new Polyline { Closed = true };
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);
            frame.TransformBy(matrix);

            return frame.Buffer(1000)[0] as Polyline;
        }

        /// <summary>
        /// 找到需要的房间
        /// </summary>
        /// <param name="roomDic"></param>
        /// <param name="outUserFrame"></param>
        /// <param name="pipes"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, List<string>> GetNeedStruct(Dictionary<Polyline, List<string>> roomDic, List<Polyline> outUserFrame, List<VerticalPipeModel> pipes)
        {
            var roomDheckDic = new Dictionary<Polyline, List<string>>(roomDic);
            var deepRooms = new Dictionary<Polyline, List<string>>();
            var pipeRooms = roomDheckDic.Where(x => pipes.Any(y => x.Key.Contains(y.Position))).ToDictionary(x => x.Key, y => y.Value);
            while (pipeRooms.Count > 0)
            {
                var firRoomDic = pipeRooms.First();
                pipeRooms.Remove(firRoomDic.Key);
                var intersectRooms = FindIntersectRoom(roomDheckDic, outUserFrame, firRoomDic);
                foreach (var room in intersectRooms)
                {
                    deepRooms.Add(room.Key, room.Value);
                    pipeRooms.Remove(room.Key);
                }
            }

            return deepRooms;
        }

        /// <summary>
        /// 找到相交的房间
        /// </summary>
        /// <param name="roomDic"></param>
        /// <param name="outUserFrame"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private static Dictionary<Polyline, List<string>> FindIntersectRoom(Dictionary<Polyline, List<string>> roomDic, List<Polyline> outUserFrame, KeyValuePair<Polyline, List<string>> room)
        {
            var resRoomDic = new Dictionary<Polyline, List<string>>();
            resRoomDic.Add(room.Key, room.Value);
            roomDic.Remove(room.Key);
            var bufferRoom = room.Key.Buffer(50)[0] as Polyline;
            var getOutFrames = outUserFrame.Where(x => bufferRoom.Intersects(x)).ToList();
            var intersectRooms = roomDic.Where(x =>
            {
                var bufferPoly = x.Key.Buffer(50)[0] as Polyline;
                return getOutFrames.Any(y => y.Intersects(bufferPoly));
            }).ToDictionary(x => x.Key, y => y.Value);
            if (intersectRooms.Count > 0)
            {
                roomDic = roomDic.Except(intersectRooms).ToDictionary(x => x.Key, y => y.Value);
                foreach (var iRoom in intersectRooms)
                {
                    foreach (var fRoom in FindIntersectRoom(roomDic, outUserFrame, iRoom))
                    {
                        resRoomDic.Add(fRoom.Key, fRoom.Value);
                    }
                }
            }

            return resRoomDic;
        }
    }
}
