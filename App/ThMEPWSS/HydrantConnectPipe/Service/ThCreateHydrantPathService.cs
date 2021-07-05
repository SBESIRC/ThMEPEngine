using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThCreateHydrantPathService
    {
        private double HydrantAngle;
        private Point3d StartPoint;
        private List<Line> Terminationlines;
        private List<Polyline> ObstaclePolylines;
        
        public ThCreateHydrantPathService()
        {
            HydrantAngle = 0.0;
            StartPoint = new Point3d(-300,600,0);
            Terminationlines = new List<Line>();
            ObstaclePolylines = new List<Polyline>();
        }
        public void SetObstacle(Polyline polyline)
        {
            ObstaclePolylines.Add(polyline);
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

            if(flag)//有消火栓
            {
                //计算支管开始方向，沿着该方向走500
                Vector3d vec = new Vector3d(Math.Sin(HydrantAngle),Math.Cos(HydrantAngle),0);
                vec *= 500;
                var tmpPoint = StartPoint + vec;
                //找最近的4条线
                var lines = ThHydrantConnectPipeUtils.GetNearbyLine4(StartPoint, Terminationlines);
                List<Point3d> pts = new List<Point3d>();
                pts.Add(StartPoint);
                foreach (var line in lines)
                {
                    pts.Add(line.StartPoint);
                    pts.Add(line.EndPoint);
                }
                //构造frame
                var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pts, 200);

                var hydrantpaths = new List<Polyline>();
                foreach (Line line in lines)
                {
                    var tmpPath = CreateStartLines(frame, line, StartPoint, ObstaclePolylines);
                    tmpPath.ColorIndex = 4;
                    hydrantpaths.Add(tmpPath);
                }
                //hydrantpaths = hydrantpaths.OrderBy(o => o.PolyLineLength()).ToList();
                //hydrantPath = hydrantpaths.First();
                //hydrantPath.AddVertexAt(0, StartPoint.ToPoint2d(), 0, 0, 0);
                //Draw.AddToCurrentSpace(hydrantPath);
            }
            else
            {
                //找最近的4条线
                var lines = ThHydrantConnectPipeUtils.GetNearbyLine4(StartPoint, Terminationlines);
                List<Point3d> pts = new List<Point3d>();
                pts.Add(StartPoint);
                foreach (var line in lines)
                {
                    pts.Add(line.StartPoint);
                    pts.Add(line.EndPoint);
                }
                //构造frame
                var frame = ThHydrantConnectPipeUtils.CreateMapFrame(pts, 800);
                var hydrantpaths = new List<Polyline>();
                foreach (Line line in lines)
                {
                    var tmpPath = CreateStartLines(frame, line, StartPoint, ObstaclePolylines);
                    if(null == tmpPath)
                    {
                        continue;
                    }
                    tmpPath.ColorIndex = 4;
                    hydrantpaths.Add(tmpPath);
                }
                if(hydrantpaths.Count == 0)
                {
                    return null;
                }
                hydrantpaths = hydrantpaths.OrderBy(o => o.PolyLineLength()).ToList();
                hydrantPath = hydrantpaths.First();
                Draw.AddToCurrentSpace(hydrantPath);
            }

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
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(frame, dir, closetLane, 400, 250, 0);

            //----设置障碍物
            aStarRoute.SetObstacle(holes);

            //----计算路径
            var path = aStarRoute.Plan(startPt);

            return path;
        }
    }
}
