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
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New;

namespace ThMEPElectrical.SecurityPlaneSystem.ConnectPipe.Service
{
    public class PipePathService
    {
        double distance = 250;
        public Polyline CreatePipePath(Polyline polyline, Point3d sPt, Point3d blockPt, Vector3d dir, List<Polyline> holes)
        {
            Polyline resLine = new Polyline();

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
        /// 计算简单的延伸线
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
        /// 使用a*算法寻路
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
            var mapFrame = polyline;

            //----初始化寻路类
            AStarOptimizeRoutePlanner aStarRoute = new AStarOptimizeRoutePlanner(mapFrame, dir, 50, 50);

            //----设置障碍物
            var objs = holes.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(mapFrame).Cast<Polyline>().ToList();
            aStarRoute.SetObstacle(resHoles);

            //----计算路径
            var path = aStarRoute.Plan(startPt, blockPt);
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
        /// 构建地图
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
            var polyline = UtilService.GetBoungdingBox(pts, xDir).Buffer(expandLength)[0] as Polyline;
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
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
