using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.Utils;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.RoutePlannerService;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service
{
    public class PipePathService
    {
        double distance = 250;
        /// <summary>
        /// 创建点到点的路径
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="sPt"></param>
        /// <param name="blockPt"></param>
        /// <param name="dir"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public Polyline CreatePipePath(Polyline polyline, Point3d sPt, Point3d blockPt, Vector3d dir, List<Polyline> holes)
        {
            Polyline resLine = null;
            //寻找起点
            //var startPt = CreateDistancePoint(polyline, sPt);
            //var endPt = CreateDistancePoint(polyline, blockPt);

            //创建延伸线
            //----简单的一条延伸线且不穿洞
            var extendLine = CreateSymbolExtendLine(polyline, sPt, blockPt, dir, holes);
            if (extendLine != null)
            {
                resLine = extendLine;
            }
            else
            {
                //----用a*算法计算路径躲洞
                resLine = GetPathByAStar(polyline, sPt, blockPt, dir, holes);
            }

            return resLine;
        }

        /// <summary>
        /// 创建点到线的路径
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="blockPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public Polyline CreatePipePath(Polyline polyline, Line closetLane, Point3d blockPt, List<Polyline> holes)
        {
            Polyline resPoly = null;

            //寻找起点
            var startPt = CreateDistancePoint(polyline, blockPt);

            //创建延伸线
            //----简单的一条延伸线且不穿洞
            var extendLine = CreateSymbolExtendLine(polyline, closetLane, startPt, holes);
            if (extendLine != null)
            {
                resPoly = extendLine;
            }
            else
            {
                //----用a*算法计算路径躲洞
                resPoly = GetPathByAStar(polyline, closetLane, startPt, holes);
            }

            return resPoly;
        }

        /// <summary>
        /// 计算到线的简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private Polyline CreateSymbolExtendLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)))
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
        /// 计算到点的简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private Polyline CreateSymbolExtendLine(Polyline frame, Point3d startPt, Point3d blockPt, Vector3d dir, List<Polyline> holes)
        {
            var checkDir = Vector3d.ZAxis.CrossProduct(dir);
            var lineDir = (startPt - blockPt).GetNormal();
            if (lineDir.IsParallelTo(dir, new Tolerance(0.001, 0.001)) || lineDir.IsParallelTo(checkDir, new Tolerance(0.001, 0.001)))
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, blockPt.ToPoint2D(), 0, 0, 0);
                if (!CheckService.LineIntersctBySelect(holes, line, 200) && !CheckService.CheckIntersectWithFrame(line, frame))
                {
                    return line;
                }

            }

            return null;
        }

        /// <summary>
        /// 使用a*算法寻找点到线的路径
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private Polyline GetPathByAStar(Polyline polyline, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            Polyline resLine = null;
            //计算逃生路径(用A*算法)
            //----构建寻路地图框线
            var mapFrame = CreateMapFrame(closetLane, startPt, holes, 2500);
            mapFrame = mapFrame.Intersection(new DBObjectCollection() { polyline }).Cast<Polyline>().OrderByDescending(x => x.Area).First();

            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(mapFrame, dir, closetLane, 400, 250, 150);

            //----设置障碍物
            var objs = holes.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(mapFrame).Cast<Polyline>().ToList();
            aStarRoute.SetObstacle(resHoles);

            //----计算路径
            var path = aStarRoute.Plan(startPt);
            if (path != null)
            {
                resLine = path;
            }

            return resLine;
        }

        /// <summary>
        /// 使用a*算法寻找点到点的路径
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private Polyline GetPathByAStar(Polyline polyline, Point3d startPt, Point3d blockPt, Vector3d dir, List<Polyline> holes)
        {
            Polyline resLines = null;
            //计算逃生路径(用A*算法)
            //----构建寻路地图框线
            var mapFrame = CreateMapFrame(dir, startPt, blockPt, holes, 5000);
            mapFrame = mapFrame.Intersection(new DBObjectCollection() { polyline }).Cast<Polyline>().OrderBy(x => x.Area).First();

            //----初始化寻路类
            AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(mapFrame, dir, blockPt, 50, 50);

            //----设置障碍物
            var objs = holes.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(mapFrame).Cast<Polyline>().ToList();
            aStarRoute.SetObstacle(resHoles);

            //----计算路径
            var path = aStarRoute.Plan(startPt);
            if (path != null)
            {
                resLines = path;
            }

            return resLines;
        }

        /// <summary>
        /// 计算起始点离外框线大于800距离
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="blockPt"></param>
        /// <returns></returns>
        private Point3d CreateDistancePoint(Polyline frame, Point3d blockPt)
        {
            Point3d resPt = blockPt;
            int i = 0;
            while (i <= 4)
            {
                i++;
                var closetPt = frame.GetClosestPointTo(resPt, false);
                var ptDistance = resPt.DistanceTo(closetPt);
                if (ptDistance >= distance)
                {
                    break;
                }

                var moveDir = (resPt - closetPt).GetNormal();
                resPt = resPt + moveDir * (distance - ptDistance);
            }

            return resPt;
        }

        /// <summary>
        /// 构建点连点的地图
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <param name="expandLength"></param>
        /// <returns></returns>
        private Polyline CreateMapFrame(Vector3d xDir, Point3d startPt, Point3d endPt, List<Polyline> holes, double expandLength)
        {
            var addPt1 = startPt + xDir * 100;
            var addPt2 = endPt + xDir * 100;
            List<Point3d> pts = new List<Point3d>() { startPt, endPt, addPt1, addPt2 };
            var polyline = UtilService.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;
            //GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            //var intersectHoles = getLayoutStructureService.GetNeedHoles(holes, polyline);
            //foreach (var iHoles in intersectHoles)
            //{
            //    pts.AddRange(iHoles.Vertices().Cast<Point3d>());
            //}
            //var resPolyline = UtilService.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;

            return polyline;
        }

        /// <summary>
        /// 构建点连线的地图
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="startPt"></param>
        /// <param name="holes"></param>
        /// <param name="expandLength"></param>
        /// <returns></returns>
        public static Polyline CreateMapFrame(Line lane, Point3d startPt, List<Polyline> holes, double expandLength)
        {
            Vector3d xDir = (lane.EndPoint - lane.StartPoint).GetNormal();
            List<Point3d> pts = new List<Point3d>() { startPt, lane.StartPoint, lane.EndPoint };
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var polyline = UtilService.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;
            var intersectHoles = getLayoutStructureService.GetNeedHoles(holes, polyline);
            foreach (var iHoles in intersectHoles)
            {
                pts.AddRange(iHoles.Vertices().Cast<Point3d>());
            }
            var resPolyline = UtilService.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;

            return resPolyline;
        }
    }
}
