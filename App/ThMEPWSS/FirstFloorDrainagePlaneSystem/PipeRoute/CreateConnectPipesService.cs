using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateConnectPipesService
    {
        double distance = 300;
        public CreateConnectPipesService(double step)
        {
            distance = step;
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="blockPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public List<Polyline> CreatePipes(Polyline polyline, Line closetLane, Point3d blockPt, List<Polyline> holes)
        {
            List<Polyline> resLines = new List<Polyline>();
            //寻找起点
            var startPt = CreateDistancePoint(polyline, holes, blockPt);

            //创建延伸线
            //----简单的一条延伸线且不穿洞
            var extendLine = CreateSymbolExtendLine(polyline, closetLane, startPt, holes);
            if (extendLine != null)
            {
                resLines.Add(extendLine);
            }
            else
            {
                //----用a*算法计算路径躲洞
                resLines.AddRange(GetPathByAStar(polyline, closetLane, startPt, holes));
            }

            return resLines;
        }

        /// <summary>
        /// 计算起始点离外框线一定距离
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="blockPt"></param>
        /// <returns></returns>
        private Point3d CreateDistancePoint(Polyline frame, List<Polyline> holes, Point3d blockPt)
        {
            Point3d resPt = blockPt;
            List<Polyline> avoidPolys = new List<Polyline>(holes);
            avoidPolys.Add(frame);
            foreach (var poly in avoidPolys)
            {
                int i = 0;
                while (i <= 4)
                {
                    i++;
                    var closetPt = poly.GetClosestPointTo(resPt, false);
                    var ptDistance = resPt.DistanceTo(closetPt);
                    if (ptDistance >= distance)
                    {
                        break;
                    }

                    var moveDir = (resPt - closetPt).GetNormal();
                    resPt = resPt + moveDir * (distance - ptDistance);
                }
            }

            return resPt;
        }

        /// <summary>
        /// 计算简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private Polyline CreateSymbolExtendLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)) || closetPt.DistanceTo(startPt) < 1)
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, closetPt.ToPoint2D(), 0, 0, 0);
                if (!CheckService.LineIntersctBySelect(holes, line, 200) && !CheckService.CheckIntersectWithFrame(line, frame))
                {
                    return line;
                }

            }

            return null;
        }

        /// <summary>
        /// 使用a*算法寻路
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private List<Polyline> GetPathByAStar(Polyline polyline, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            List<Polyline> resLines = new List<Polyline>();
            //计算逃生路径(用A*算法)
            //----构建寻路地图框线
            var mapFrame = CreateMapFrame(closetLane, startPt, holes, 2500);
            mapFrame = mapFrame.Intersection(new DBObjectCollection() { polyline }).Cast<Polyline>().OrderByDescending(x => x.Area).First();

            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(mapFrame, dir, closetLane, distance, distance, distance);

            //----设置障碍物
            var resHoles = SelelctCrossing(holes, mapFrame);
            aStarRoute.SetObstacle(resHoles);

            //----计算路径
            var path = aStarRoute.Plan(startPt);
            if (path != null)
            {
                resLines.Add(path);
            }

            return resLines;
        }

        /// <summary>
        /// 创建地图
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <param name="expandLength"></param>
        /// <returns></returns>
        private Polyline CreateMapFrame(Line lane, Point3d startPt, List<Polyline> holes, double expandLength)
        {
            Vector3d xDir = (lane.EndPoint - lane.StartPoint).GetNormal();
            List<Point3d> pts = new List<Point3d>() { startPt, lane.StartPoint, lane.EndPoint };
            var polyline = GeometryUtils.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;
            var intersectHoles = SelelctCrossing(holes, polyline);
            foreach (var iHoles in intersectHoles)
            {
                pts.AddRange(GeometryUtils.GetAllPolylinePts(iHoles));
            }
            var resPolyline = GeometryUtils.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;

            return resPolyline;
        }

        /// <summary>
        /// 筛选元素
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
        {
            var objs = polylines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            return resHoles;
        }
    }
}
