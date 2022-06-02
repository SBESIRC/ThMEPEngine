using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThCreateHydrantPathService
    {
        private Point3d StartPoint;//起始点
        private Dictionary<Line,bool> DicTermLines;//终点线
        private List<Line> ObstacleRooms;//可穿越区域，但是代价大
        private List<Polyline> ObstacleHoles;//不可穿越区域
        private List<Polyline> StairsRooms;//楼梯间
        private List<Polyline> HydrantPipes;//立管
        private ThCADCoreNTSSpatialIndex HoleIndex;
        private ThCADCoreNTSSpatialIndex LineIndex;
        private ThCADCoreNTSSpatialIndex RoomIndex;
        public int AStarCount { set; get; }
        public ThCreateHydrantPathService()
        {
            
            StartPoint = new Point3d(0,0,0);
            DicTermLines = new Dictionary<Line, bool>();
            ObstacleRooms = new List<Line>();
            ObstacleHoles = new List<Polyline>();
            StairsRooms = new List<Polyline>();
            HydrantPipes = new List<Polyline>();
        }
        public void Clear()
        {
            foreach(var item in ObstacleRooms)
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
//            HoleIndex = new ThCADCoreNTSSpatialIndex(ObstacleHoles.ToCollection());
            LineIndex = new ThCADCoreNTSSpatialIndex(DicTermLines.Keys.ToList().ToCollection());
            RoomIndex = new ThCADCoreNTSSpatialIndex(ObstacleRooms.ToCollection());
        }
        public void ClearData()
        {
            foreach(var k in DicTermLines.Keys.ToList())
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
            if(room != null)
            {
                if(room is MPolygon)
                {
                    var pg = room as MPolygon;
                    var loops = pg.Loops();
                    foreach(var loop in loops)
                    {
                        var lines = loop.ToLines();
                        ObstacleRooms.AddRange(lines);
                    }
                }
                else if(room is Polyline)
                {
                    var lines = (room as Polyline).ToLines();
                    ObstacleRooms.AddRange(lines);
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
            if(hole != null)
            {
                if(hole is Polyline)
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
            foreach(var line in lines)
            {
                DicTermLines.Add(line, false);
            }
        }
        public void SetStartPoint(Point3d pt)
        {
            StartPoint = pt;
        }
        public Polyline CreateHydrantPath()
        {
            foreach (var hole in ObstacleHoles)
            {
                if (hole.Contains(StartPoint))
                {
                    return null;
                }
            }

            var holes = new List<Polyline>(ObstacleHoles);

            foreach (var pipe in HydrantPipes)
            {
                if(!pipe.Contains(StartPoint))
                {
                    holes.Add(pipe);
                }
            }
            foreach(var room in StairsRooms)
            {
                if(!room.Contains(StartPoint))
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
        private List<Point3d> StartOneStep(Point3d pt,List<Polyline> holes, List<Entity> rooms, Line line)
        {
            double step = 500.0;
            List<Point3d> returnPts = new List<Point3d>();
            double dist = line.GetDistToPoint(pt);//获取pt到 line 的距离
            if(dist < step)
            {
                returnPts.Add(new Point3d(pt.X,pt.Y,pt.Z));
                return returnPts;
            }

            //寻找 pt 附件的障碍线
            var holeLines = new List<Line>();
            foreach(var hole in holes)
            {
                if(hole != null)
                {
                    holeLines.AddRange(hole.ToLines());
                }
            }
            foreach(var room in rooms)
            {
                if(room != null)
                {
                    var lineObject = new DBObjectCollection();
                    room.Explode(lineObject);
                    foreach(var l in lineObject)
                    {
                        if(l is Line)
                        {
                            holeLines.Add(l as Line);
                        }
                    }
                }
            }
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pt, 200);
            var holeLineIndex = new ThCADCoreNTSSpatialIndex(holeLines.ToCollection());
            var dbObjects = holeLineIndex.SelectCrossingPolygon(frame);
            var nearHoleLine = new List<Line>();
            foreach (var l in dbObjects)
            {
                if (l is Line)
                {
                    nearHoleLine.Add(l as Line);
                }
            }

            if(nearHoleLine.Count == 0)
            {
                returnPts.Add(new Point3d(pt.X, pt.Y, pt.Z));
                return returnPts;
            }

            //判断障碍线与 line 是否有平行或者垂直的情况
            Vector3d v1 = line.StartPoint.GetVectorTo(line.EndPoint);
            foreach(var l in nearHoleLine)
            {
                Vector3d v2 = l.StartPoint.GetVectorTo(l.EndPoint);
                double inAngle = v1.GetAngleTo(v2);
                if (Math.Abs(inAngle - Math.PI/2.0) < 0.1 || Math.Abs(inAngle - Math.PI) < 0.1)
                {
                    returnPts.Add(new Point3d(pt.X, pt.Y, pt.Z));
                    return returnPts;
                }
            }

            foreach (var l in nearHoleLine)
            {
                Vector3d v2 = l.StartPoint.GetVectorTo(l.EndPoint).GetNormal();
                v2 = v2 * step;
                var tmpPt1 = pt + v2;
                var tmpPt2 = pt - v2;
                var tmpLine1 = new Line(pt, tmpPt1);
                var tmpLine2 = new Line(pt, tmpPt2);
                bool isIntersect1 = false;
                bool isIntersect2 = false;

                foreach (var o in holeLines)
                {
                    if(ThHydrantConnectPipeUtils.IsIntersect(tmpLine1,o))
                    {
                        isIntersect1 = true;
                    }
                }
                foreach (var o in holeLines)
                {
                    if (ThHydrantConnectPipeUtils.IsIntersect(tmpLine2, o))
                    {
                        isIntersect2 = true;
                    }
                }

                if(!isIntersect1)
                {
                    returnPts.Add(tmpPt1);
                }

                if (!isIntersect2)
                {
                    returnPts.Add(tmpPt2);
                }
            }
            return returnPts;
        }
        private Polyline DirectHydrantPath(Point3d pt)
        {
            var tmpLines = ThHydrantConnectPipeUtils.GetNearbyLine(StartPoint, DicTermLines.Keys.ToList(), 10);
            int lineCount = tmpLines.Count;
            if(lineCount > 10)
            {
                lineCount = 10;
            }
            for (int i = 0; i < lineCount; i++)
            {
                var line = DirectHydrantPath(pt, tmpLines[i]);
                if(line != null)
                {
                    return line;
                }
            }
            
            return null;

        }
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
            var dbRooms = RoomIndex.SelectCrossingPolygon(frame);
            var rooms = new List<Line>();
            foreach (var room in dbRooms)
            {
                if (room is Line)
                {
                    rooms.Add(room as Line);
                }
            }
            //----简单的一条延伸线且不穿洞
            var resLine = CreateSymbolLine(frame, line, pt, holes, rooms);
            return resLine;
        }
        private Polyline HydrantPath(Point3d pt)
        {
            Polyline hydrantPath = new Polyline();
            var hydrantpaths = new List<Polyline>();
            var lines = ThHydrantConnectPipeUtils.GetNearbyLine(StartPoint, DicTermLines.Keys.ToList(),2);
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
            if(lines.Count > 10)
            {
                lineCount = 10;
            }

            var hydrantpaths = new List<Polyline>();
            for(int i = 0;i < lineCount; i++)
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

            //foreach (var line in lines)
            //{
            //    if (DicTermLines[line])
            //    {
            //        continue;
            //    }
            //    var tmpPath = HydrantPath(pt, line);
            //    if(tmpPath != null)
            //    {
            //        hydrantpaths.Add(tmpPath);
            //    }
            //    DicTermLines[line] = true;
            //}

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
        private Polyline HydrantPath(Point3d pt,Line line)
        {
            if(line.Length < 100.0)
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
            var dbRooms = RoomIndex.SelectCrossingPolygon(frame);
            var rooms = new List<Line>();
            foreach (var room in dbRooms)
            {
                if (room is Line)
                {
                    rooms.Add(room as Line);
                }
            }
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
        private Polyline CreateSymbolLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Line> rooms)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)))
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, closetPt.ToPoint2D(), 0, 0, 0);
                if (!ThHydrantConnectPipeUtils.LineIntersctBySelect(holes, line,50)
                    && !ThHydrantConnectPipeUtils.LineIntersctBySelect(rooms, line) 
                    && !ThHydrantConnectPipeUtils.CheckIntersectWithFrame(line, frame))
                {
                    return line;
                }
            }

            return null;
        }
        public Polyline GetPathByAStar(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Line> rooms)
        {
            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(frame, dir, closetLane, 400, 300, 50);
            var costGetter = new ToLineCostGetterEx();
            var pathAdjuster = new ThHydrantConnectPipeAdjustPath();
            aStarRoute.costGetter = costGetter;
            aStarRoute.PathAdjuster = pathAdjuster;
            //----设置障碍物
            aStarRoute.SetObstacle2(holes);
            //----设置房间
            aStarRoute.SetRoom(rooms);
            //----计算路径
            var path = aStarRoute.Plan(startPt);
            return path;
        }
        private double PathCost(Polyline polyLine)
        {
            double cost = polyLine.PolyLineLength();
            foreach(var room in ObstacleRooms)
            {
                if(room != null)
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
