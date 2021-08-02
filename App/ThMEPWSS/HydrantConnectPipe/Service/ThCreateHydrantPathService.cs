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
        private double HydrantAngle;
        private Point3d StartPoint;
        private List<Line> Terminationlines;
        private List<Entity> ObstacleRooms;//可穿越区域，但是代价大
        private List<Polyline> ObstacleHoles;//不可穿越区域
        private ThCADCoreNTSSpatialIndex HoleIndex;
        private ThCADCoreNTSSpatialIndex LineIndex;
        private ThCADCoreNTSSpatialIndex RoomIndex;
        public ThCreateHydrantPathService()
        {
            HydrantAngle = 0.0;
            StartPoint = new Point3d(0,0,0);
            Terminationlines = new List<Line>();
            ObstacleRooms = new List<Entity>();
            ObstacleHoles = new List<Polyline>();
        }
        public void InitData()
        {
            HoleIndex = new ThCADCoreNTSSpatialIndex(ObstacleHoles.ToCollection());
            LineIndex = new ThCADCoreNTSSpatialIndex(Terminationlines.ToCollection());
            RoomIndex = new ThCADCoreNTSSpatialIndex(ObstacleRooms.ToCollection());
            //foreach (var room in ObstacleRooms)
            //{
            //    room.ColorIndex = 6;
            //    Draw.AddToCurrentSpace(room);
            //}
            foreach (var line in Terminationlines)
            {
                line.ColorIndex = 4;
                Draw.AddToCurrentSpace(line);
            }
            foreach (var hole in ObstacleHoles)
            {
                hole.ColorIndex = 3;
                Draw.AddToCurrentSpace(hole);
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
        public void SetRoom(Entity room)
        {
            if(room != null)
            {
                ObstacleRooms.Add(room);
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
        public void SetTermination(Line line)
        {
            if(line != null)
            {
                Terminationlines.Add(line);
            }
        }
        public void SetTermination(List<Line> lines)
        {
            Terminationlines.AddRange(lines);
        }
        public void SetHydrantAngle(double angle)
        {
            HydrantAngle = angle;
        }
        public void SetStartPoint(Point3d pt)
        {
            StartPoint = pt;
        }
        public Polyline CreateHydrantPath(bool flag)
        {
            Polyline hydrantPath = new Polyline();
            foreach(var hole in ObstacleHoles)
            {
                if(hole.Contains(StartPoint))
                {
                    return null;
                }
            }
            if(flag)//有消火栓
            {
                var hydrantpaths = new List<Polyline>();
                for (int i = 0; i < 4; i++)
                {
                    Vector3d vec = new Vector3d(Math.Sin(HydrantAngle + Math.PI / 2.0 * i), Math.Cos(HydrantAngle + Math.PI / 2.0 * i), 0);
                    vec *= 500;
                    var tmpPoint = StartPoint + vec;
                    bool isInObstacle = false;
                    foreach (var obstacle in ObstacleHoles)
                    {
                        if (obstacle.Contains(tmpPoint))
                        {
                            isInObstacle = true;
                            break;
                        }
                    }
                    if (isInObstacle)
                    {
                        continue;
                    }
                    var path = HydrantPath(tmpPoint);
                    hydrantpaths.Add(path);
                }
                hydrantpaths = hydrantpaths.OrderBy(o => PathCost(o)).ToList();
                hydrantPath = hydrantpaths.First();
                hydrantPath.AddVertexAt(0, StartPoint.ToPoint2d(), 0, 0, 0);
            }
            else
            {
                hydrantPath = HydrantPath(StartPoint);
                if(hydrantPath != null)
                {
                    if(PathCost(hydrantPath) > (hydrantPath.PolyLineLength()+5000))
                    {
                        var tmpPath = CreateHydrantPath(StartPoint, 30000);
                        if(tmpPath == null)
                        {
                            return hydrantPath;
                        }
                        return (PathCost(hydrantPath) > PathCost(tmpPath))? tmpPath : hydrantPath;
                    }
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
        private Polyline CreateHydrantPath(Point3d pt, double radius)
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
            var holeObjs = HoleIndex.SelectCrossingPolygon(frame);
            var holes = new List<Polyline>();
            foreach (var hole in holeObjs)
            {
                if (hole is Polyline)
                {
                    holes.Add(hole as Polyline);
                }
            }
            var dbRooms = RoomIndex.SelectCrossingPolygon(frame);
            var rooms = new List<Entity>();
            foreach (var room in dbRooms)
            {
                if (room is Entity)
                {
                    rooms.Add(room as Entity);
                }
            }
            var hydrantpaths = new List<Polyline>();
            foreach (Line line in lines)
            {
                var polyLine = CreateSymbolLine(frame, line, pt, holes, rooms);
                if (polyLine != null)
                {
                    hydrantpaths.Add(polyLine);
                }
                else
                {
                    var tmpPts = StartOneStep(pt, holes, rooms, line);
                    foreach (var tmpPt in tmpPts)
                    {
                        var tmpPath = GetPathByAStar(frame, line, tmpPt, holes, rooms);
                        if (null == tmpPath)
                        {
                            continue;
                        }
                        if (tmpPt.DistanceTo(pt) > 1)
                        {
                            tmpPath.AddVertexAt(0, pt.ToPoint2D(), 0, 0, 0);
                        }
                        hydrantpaths.Add(tmpPath);
                    }
                }
            }
            if (hydrantpaths.Count == 0)
            {
                return null;
            }
            hydrantpaths = hydrantpaths.OrderBy(o => PathCost(o)).ToList();
            hydrantPath = hydrantpaths.First();
            return hydrantPath;
        }
        private Polyline HydrantPath(Point3d pt)
        {
            Polyline hydrantPath = new Polyline();
            var lines = ThHydrantConnectPipeUtils.GetNearbyLine4(StartPoint, Terminationlines);
            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            foreach (var line in lines)
            {
                pts.Add(line.GetClosestPointTo(pt, false));
//                pts.Add(line.StartPoint);
//                pts.Add(line.EndPoint);
            }
            //构造frame
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pts, 5000);
            var dbObjects = HoleIndex.SelectCrossingPolygon(frame);
            var rst = new List<Polyline>();
            foreach (var dbobject in dbObjects)
            {
                if (dbobject is Polyline)
                {
                    rst.Add(dbobject as Polyline);
                }
            }

            var dbRooms = RoomIndex.SelectCrossingPolygon(frame);
            var rooms = new List<Entity>();
            foreach (var room in dbRooms)
            {
                if (room is Entity)
                {
                    rooms.Add(room as Entity);
                }
            }


            var hydrantpaths = new List<Polyline>();
            foreach (Line line in lines)
            {
                //----简单的一条延伸线且不穿洞
                var polyLine = CreateSymbolLine(frame, line, pt, rst, rooms);
                if(polyLine != null)
                {
                    hydrantpaths.Add(polyLine);
                }
                else
                {
                    var tmpPts = StartOneStep(pt, rst, rooms, line);
                    foreach (var tmpPt in tmpPts)
                    {
                        var tmpPath = GetPathByAStar(frame, line, tmpPt, rst, rooms);
                        if (null == tmpPath)
                        {
                            continue;
                        }
                        if (tmpPt.DistanceTo(pt) > 1)
                        {
                            tmpPath.AddVertexAt(0, pt.ToPoint2D(), 0, 0, 0);
                        }
                        hydrantpaths.Add(tmpPath);
                    }
                }
            }
            if (hydrantpaths.Count == 0)
            {
                return null;
            }
            hydrantpaths = hydrantpaths.OrderBy(o => PathCost(o)).ToList();
            hydrantPath = hydrantpaths.First();
            return hydrantPath;
        }

        private Polyline CreateStartLines(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Entity> rooms)
        {
            //创建延伸线
            //----简单的一条延伸线且不穿洞
            var polyLine = CreateSymbolLine(frame, closetLane, startPt, holes, rooms);
            if (polyLine != null)
            {
                return polyLine;
            }
            else
            {
                //----用a*算法计算路径躲洞
                return GetPathByAStar(frame, closetLane, startPt, holes, rooms);
            }
        }
        /// <summary>
        /// 计算简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private Polyline CreateSymbolLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Entity> rooms)
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
        private Polyline GetPathByAStar(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Entity> rooms)
        {
            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(frame, dir, closetLane, 400, 300, 50);

            //var costGetter = new ToLineCostGetterEx();
            //aStarRoute.costGetter = costGetter;
            //----设置障碍物
            aStarRoute.SetObstacle(holes);
            //----设置房间
            //aStarRoute.SetRoom(rooms);
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
                    cost += pts.Count * 5000;
                }
            }
            return cost;
        }
    }
}
