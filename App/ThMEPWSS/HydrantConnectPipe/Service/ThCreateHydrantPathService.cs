using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.GlobleAStarAlgorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThCreateHydrantPathService
    {
        private Point3d StartPoint;//起始点
        private Dictionary<Line, bool> DicTermLines;//终点线
        private List<Polyline> ObstacleRooms;//可穿越区域，但是代价大
        private List<Polyline> ObstacleHoles;//不可穿越区域
        private List<Polyline> StairsRooms;//楼梯间
        private List<Polyline> HydrantPipes;//立管
        private ThCADCoreNTSSpatialIndex HoleIndex;
        private ThCADCoreNTSSpatialIndex LineIndex;
        private ThCADCoreNTSSpatialIndex RoomIndex;
        public int AStarCount { set; get; }
        public ThCreateHydrantPathService()
        {

            StartPoint = new Point3d(0, 0, 0);
            DicTermLines = new Dictionary<Line, bool>();
            ObstacleRooms = new List<Polyline>();
            ObstacleHoles = new List<Polyline>();
            StairsRooms = new List<Polyline>();
            HydrantPipes = new List<Polyline>();
        }
        public void Clear()
        {
            foreach (var item in ObstacleRooms)
            {
                item.Dispose();
            }
            foreach (var item in ObstacleHoles)
            {
                item.Dispose();
            }
            foreach (var item in StairsRooms)
            {
                item.Dispose();
            }
            foreach (var item in HydrantPipes)
            {
                item.Dispose();
            }
        }

        public void InitData()
        {
            AStarCount = 0;
            LineIndex = new ThCADCoreNTSSpatialIndex(DicTermLines.Keys.ToList().ToCollection());
            RoomIndex = new ThCADCoreNTSSpatialIndex(ObstacleRooms.ToCollection());
        }
        public void ClearData()
        {
            foreach (var k in DicTermLines.Keys.ToList())
            {
                DicTermLines[k] = false;
            }
        }
        public void AddObstacle(Entity hole)
        {
            var adds = new DBObjectCollection();
            if (hole != null)
            {
                if (hole is Polyline)
                {
                    adds.Add(hole as Polyline);
                    ObstacleHoles.Add(hole as Polyline);
                    HoleIndex.Update(adds, new DBObjectCollection());
                }
            }
        }
        public void SetBuildRoom(Entity room)
        {
            if (room != null)
            {
                if (room is MPolygon)
                {
                    var pg = room as MPolygon;
                    ObstacleRooms.Add(pg.Shell());
                }
                else if (room is Polyline roomPoly)
                {
                    ObstacleRooms.Add(roomPoly);
                }
            }
        }
        public void SetStairsRoom(Entity room)
        {
            if (room != null)
            {
                if (room is Polyline)
                {
                    StairsRooms.Add(room as Polyline);
                }
            }
        }
        public void SetHydrantPipe(Entity pipe)
        {
            if (pipe != null)
            {
                if (pipe is Polyline)
                {
                    HydrantPipes.Add(pipe as Polyline);
                }
            }
        }
        public void SetObstacle(Entity hole)
        {
            if (hole != null)
            {
                if (hole is Polyline)
                {
                    ObstacleHoles.Add(hole as Polyline);
                }
            }
        }
        public int GetHoleCount()
        {
            return ObstacleHoles.Count;
        }
        public void SetTermination(List<Line> lines)
        {
            foreach (var line in lines)
            {
                DicTermLines.Add(line, false);
            }
        }
        public void SetStartPoint(Point3d pt)
        {
            StartPoint = pt;
        }

        /// <summary>
        /// 创建连接管线
        /// </summary>
        /// <returns></returns>
        public Polyline CreateHydrantPath()
        {
            var holes = new List<Polyline>(ObstacleHoles);
            foreach (var pipe in HydrantPipes)
            {
                if (!pipe.Contains(StartPoint))
                {
                    holes.Add(pipe);
                }
            }
            foreach (var room in StairsRooms)
            {
                if (!room.Contains(StartPoint))
                {
                    holes.Add(room);
                }
            }

            HoleIndex = new ThCADCoreNTSSpatialIndex(holes.ToCollection());
            ClearData();

            var hydrantPath = HydrantPath(StartPoint);
            if (hydrantPath != null)
            {
                var directPath = DirectHydrantPath(StartPoint);//取直连的路径
                if (PathCost(hydrantPath) > (hydrantPath.PolyLineLength() + 5000))
                {
                    var tmpPath = HydrantPath(StartPoint, 30000);
                    if (tmpPath != null)
                    {
                        hydrantPath = (PathCost(hydrantPath) > PathCost(tmpPath)) ? tmpPath : hydrantPath;
                    }
                }

                if (directPath != null)
                {
                    hydrantPath = (PathCost(directPath) > PathCost(hydrantPath)) ? hydrantPath : directPath;
                }
            }

            return hydrantPath;
        }

        /// <summary>
        /// 计算直连连接点位
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Polyline DirectHydrantPath(Point3d pt)
        {
            var tmpLines = ThHydrantConnectPipeUtils.GetNearbyLine(StartPoint, DicTermLines.Keys.ToList(), 10);
            int lineCount = tmpLines.Count;
            if (lineCount > 10)
            {
                lineCount = 10;
            }
            for (int i = 0; i < lineCount; i++)
            {
                var line = DirectHydrantPath(pt, tmpLines[i]);
                if (line != null)
                {
                    return line;
                }
            }

            return null;

        }

        /// <summary>
        /// 计算直连连接线
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private Polyline DirectHydrantPath(Point3d pt, Line line)
        {
            if (line.Length < 1000.0)
            {
                return null;
            }

            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            pts.Add(line.GetClosestPointTo(pt, false));

            //构造frame
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pts, 10000);
            if (frame == null)
            {
                return null;
            }
            var dbHoles = HoleIndex.SelectCrossingPolygon(frame);
            var holes = new List<Polyline>();
            foreach (var dbHole in dbHoles)
            {
                if (dbHole is Polyline)
                {
                    holes.Add(dbHole as Polyline);
                }
            }
            var rooms = RoomIndex.SelectCrossingPolygon(frame).OfType<Polyline>().ToList();
            //----简单的一条延伸线且不穿洞
            var resLine = CreateSymbolLine(frame, line, pt, holes, rooms);
            return resLine;
        }

        /// <summary>
        /// 计算消火栓连线
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Polyline HydrantPath(Point3d pt)
        {
            Polyline hydrantPath = new Polyline();
            var hydrantpaths = new List<Polyline>();
            var lines = ThHydrantConnectPipeUtils.GetNearbyLine(StartPoint, DicTermLines.Keys.ToList(), 2);
            foreach (var line in lines)
            {
                var tmpPath = HydrantPath(pt, line);
                if (tmpPath != null)
                {
                    hydrantpaths.Add(tmpPath);
                }
                DicTermLines[line] = true;
            }

            if (hydrantpaths.Count == 0)
            {
                return null;
            }
            hydrantpaths = hydrantpaths.OrderBy(o => PathCost(o)).ToList();
            hydrantPath = hydrantpaths.First();
            for (int i = 1; i < hydrantpaths.Count; i++)
            {
                hydrantpaths[i].Dispose();
            }
            return hydrantPath;
        }

        /// <summary>
        /// 找到范围内的路径
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private Polyline HydrantPath(Point3d pt, double radius)
        {
            Polyline hydrantPath = new Polyline();
            //构造frame
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pt, radius);
            var lineObjs = LineIndex.SelectCrossingPolygon(frame);
            var lines = new List<Line>();
            foreach (var line in lineObjs)
            {
                if (line is Line)
                {
                    lines.Add(line as Line);
                }
            }
            lines = lines.OrderBy(o => o.DistanceToPoint(pt)).ToList();
            int lineCount = lines.Count;
            if (lines.Count > 10)
            {
                lineCount = 10;
            }

            var hydrantpaths = new List<Polyline>();
            for (int i = 0; i < lineCount; i++)
            {
                if (DicTermLines[lines[i]])
                {
                    continue;
                }

                var tmpPath = HydrantPath(pt, lines[i]);
                if (tmpPath != null)
                {
                    hydrantpaths.Add(tmpPath);
                }
                DicTermLines[lines[i]] = true;
            }

            if (hydrantpaths.Count == 0)
            {
                return null;
            }
            hydrantpaths = hydrantpaths.OrderBy(o => PathCost(o)).ToList();
            hydrantPath = hydrantpaths.First();
            for (int i = 1; i < hydrantpaths.Count; i++)
            {
                hydrantpaths[i].Dispose();
            }
            return hydrantPath;
        }

        /// <summary>
        /// 计算消火栓连线(点到线的寻路)
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Polyline HydrantPath(Point3d pt, Line line)
        {
            if (line.Length < 100.0)
            {
                return null;
            }

            List<Point3d> pts = new List<Point3d>();// { line.StartPoint, line.EndPoint };
            pts.Add(pt);
            pts.Add(line.GetClosestPointTo(pt, false));

            //构造frame
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pts, 10000);
            if (frame == null)
            {
                return null;
            }
            var dbHoles = HoleIndex.SelectCrossingPolygon(frame);
            var holes = dbHoles.OfType<Polyline>().ToList();
            var rooms = RoomIndex.SelectCrossingPolygon(frame).OfType<Polyline>().ToList();
            
            //----简单的一条延伸线且不穿洞
            var resLine = CreateSymbolLine(frame, line, pt, holes, rooms);
            if (resLine != null)
            {
                return resLine;
            }
            else
            {
                resLine = GetPathByAStar(frame, line, pt, holes, rooms);
                AStarCount++;
            }

            return resLine;
        }

        /// <summary>
        /// 计算简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private Polyline CreateSymbolLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Polyline> rooms)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)))
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, closetPt.ToPoint2D(), 0, 0, 0);
                if (!ThHydrantConnectPipeUtils.LineIntersctBySelect(holes, line, 50)
                    && !ThHydrantConnectPipeUtils.LineIntersctBySelect(rooms, line)
                    && !ThHydrantConnectPipeUtils.CheckIntersectWithFrame(line, frame))
                {
                    return line;
                }
            }

            return null;
        }

        /// <summary>
        /// A*跑连接路径
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        public Polyline GetPathByAStar(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Polyline> rooms)
        {
            //计算逃生路径(用A*算法)
            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            GlobleAStarRoutePlanner<Line> aStarRoute = new GlobleAStarRoutePlanner<Line>(frame, dir, closetLane, 300, 300, 55, 20);

            //----设置障碍物
            aStarRoute.SetObstacle(holes.Select(x => x.ToNTSPolygon().ToDbMPolygon()).ToList(), Double.MaxValue);
            var mPolygonRooms = rooms.Select(x =>
            {
                var buuferCol = x.Buffer(-350);
                var outRoom = x.Buffer(10)[0] as Polyline;
                if (buuferCol.Count > 0)
                {
                    var bufferPoly = buuferCol[0] as Polyline;
                    return ThMPolygonTool.CreateMPolygon(outRoom, new List<Curve>() { bufferPoly });
                }
                return ThMPolygonTool.CreateMPolygon(outRoom);
            }).ToList();
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                foreach (var item in mPolygonRooms)
                {
                    //db.ModelSpace.Add(item);
                }
                foreach (var item in holes.Select(x => x.ToNTSPolygon().ToDbMPolygon()).ToList())
                {
                    //db.ModelSpace.Add(item);
                }
            }
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                //db.ModelSpace.Add(frame);
                //db.ModelSpace.Add(closetLane.Clone() as Line);
            }
            aStarRoute.SetObstacle(mPolygonRooms, 100, 0);

            //----计算路径
            var path = aStarRoute.Plan(startPt);

            return path;
        }

        /// <summary>
        /// 计算消火栓连线的消耗
        /// </summary>
        /// <param name="polyLine"></param>
        /// <returns></returns>
        private double PathCost(Polyline polyLine)
        {
            double cost = polyLine.PolyLineLength();
            foreach (var room in ObstacleRooms)          //穿过房间权重增加
            {
                if (room != null)
                {
                    Point3dCollection pts = new Point3dCollection();
                    polyLine.IntersectWith(room, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    cost += pts.Count * 10000;
                }
            }
            return cost;
        }
    }
}
