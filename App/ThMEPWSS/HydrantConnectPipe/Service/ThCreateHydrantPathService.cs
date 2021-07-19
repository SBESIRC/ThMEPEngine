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
        private List<Polyline> ObstacleRooms;//可穿越区域，但是代价大
        private List<Polyline> ObstacleHoles;//不可穿越区域
        private ThCADCoreNTSSpatialIndex HoleIndex;
        private ThCADCoreNTSSpatialIndex LineIndex;
        public ThCreateHydrantPathService()
        {
            HydrantAngle = 0.0;
            StartPoint = new Point3d(-300,600,0);
            Terminationlines = new List<Line>();
            ObstacleRooms = new List<Polyline>();
            ObstacleHoles = new List<Polyline>();
        }
        public void InitData()
        {
            HoleIndex = new ThCADCoreNTSSpatialIndex(ObstacleHoles.ToCollection());
            LineIndex = new ThCADCoreNTSSpatialIndex(Terminationlines.ToCollection());
            foreach(var hole in ObstacleHoles)
            {
                hole.ColorIndex = 3;
                Draw.AddToCurrentSpace(hole);
            }
            foreach (var line in Terminationlines)
            {
                line.ColorIndex = 4;
                Draw.AddToCurrentSpace(line);
            }
        }
        public void SetRoom(Polyline room)
        {
            ObstacleRooms.Add(room);
        }
        public void SetObstacle(Polyline polyline)
        {
            ObstacleHoles.Add(polyline);
        }
        public void SetTermination(Line line)
        {
            Terminationlines.Add(line);
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
                        var tmpPath = CreateHydrantPath(StartPoint, 15000);
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
        private List<Point3d> StartOneStep(Point3d pt,List<Polyline> holes, Line line)
        {
            List<Point3d> returnPts = new List<Point3d>();
            double dist = line.GetDistToPoint(pt);//获取pt到 line 的距离
            if(dist < 500)
            {
                returnPts.Add(new Point3d(pt.X,pt.Y,pt.Z));
                return returnPts;
            }

            //寻找 pt 附件的障碍线
            var tmpHoles = new List<Polyline>(holes);
            tmpHoles.AddRange(ObstacleRooms);
            var holeLines = new List<Line>();
            foreach(var hole in tmpHoles)
            {
                if(hole != null)
                {
                    holeLines.AddRange(hole.ToLines());
                }
            }
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pt, 100);
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
                v2 = v2 * 500;
                var tmpPt1 = pt + v2;
                var tmpPt2 = pt - v2;
                var tmpLine1 = new Line(pt, tmpPt1);
                var tmpLine2 = new Line(pt, tmpPt2);
                bool isIntersect1 = false;
                bool isIntersect2 = false;

                foreach (var o in nearHoleLine)
                {
                    if(ThHydrantConnectPipeUtils.IsIntersect(tmpLine1,o))
                    {
                        isIntersect1 = true;
                    }
                }
                foreach (var o in nearHoleLine)
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
            var hydrantpaths = new List<Polyline>();
            foreach (Line line in lines)
            {
                var tmpPts = StartOneStep(pt, holes, line);
                foreach (var tmpPt in tmpPts)
                {
                    var tmpPath = CreateStartLines(frame, line, tmpPt, holes);
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
                //pts.Add(line.GetClosestPointTo(pt, false));
                pts.Add(line.StartPoint);
                pts.Add(line.EndPoint);
            }
            //构造frame
            var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pts, 800);
            var dbObjects = HoleIndex.SelectCrossingPolygon(frame);
            var rst = new List<Polyline>();
            foreach (var dbobject in dbObjects)
            {
                if (dbobject is Polyline)
                {
                    rst.Add(dbobject as Polyline);
                }
            }
            var hydrantpaths = new List<Polyline>();
            foreach (Line line in lines)
            {
                
                var tmpPts = StartOneStep(pt, rst, line);
                foreach(var tmpPt in tmpPts)
                {
                    var tmpPath = CreateStartLines(frame, line, tmpPt, rst);
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
            if (hydrantpaths.Count == 0)
            {
                return null;
            }
            hydrantpaths = hydrantpaths.OrderBy(o => PathCost(o)).ToList();
            hydrantPath = hydrantpaths.First();
            return hydrantPath;
        }

        private Polyline CreateStartLines(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            //创建延伸线
            //----简单的一条延伸线且不穿洞
            var polyLine = CreateSymbolLine(frame, closetLane, startPt, holes);
            if (polyLine != null)
            {
                return polyLine;
            }
            else
            {
                //----用a*算法计算路径躲洞
                return GetPathByAStar(frame, closetLane, startPt, holes);
            }
        }
        /// <summary>
        /// 计算简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private Polyline CreateSymbolLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)))
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, closetPt.ToPoint2D(), 0, 0, 0);
                if (!ThHydrantConnectPipeUtils.LineIntersctBySelect(holes, line, 200) && !ThHydrantConnectPipeUtils.CheckIntersectWithFrame(line, frame))
                {
                    return line;
                }
            }

            return null;
        }
        private Polyline GetPathByAStar(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(frame, dir, closetLane, 400, 300, 50);

            //----设置障碍物
            aStarRoute.SetObstacle(holes);

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
