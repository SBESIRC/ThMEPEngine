using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using ThMEPLighting.FEI.Service;
using ThMEPLighting.FEI.Model;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class CreateExtendLineWithAStarService
    {
        double distance = 400;

        public List<ExtendLineModel> CreateStartLines(Polyline polyline, Line closetLane, Point3d blockPt, List<Polyline> holes)
        {
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();

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
        /// 计算简单的延伸线
        /// </summary>
        /// <param name="closetLane"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private ExtendLineModel CreateSymbolExtendLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)) || closetPt.DistanceTo(startPt) < 1)
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, closetPt.ToPoint2D(), 0, 0, 0); 
                if (!SelectService.LineIntersctBySelect(holes, line, 200) && !CheckService.CheckIntersectWithFrame(line, frame))
                {
                    ExtendLineModel extendLine = new ExtendLineModel();
                    extendLine.line = line;
                    extendLine.priority = Priority.startExtendLine;
                    return extendLine;
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
        private List<ExtendLineModel> GetPathByAStar(Polyline polyline, Line closetLane, Point3d startPt, List<Polyline> holes)
        {
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            //计算逃生路径(用A*算法)
            //----构建寻路地图框线
            var mapFrame = OptimizeStartExtendLineService.CreateMapFrame(closetLane, startPt, holes, 2500);
            mapFrame = mapFrame.Intersection(new DBObjectCollection() { polyline }).Cast<Polyline>().OrderByDescending(x => x.Area).First();
            
            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(mapFrame, dir, closetLane, 400, 250, 250);

            //----设置障碍物
            var resHoles = SelectService.SelelctCrossing(holes, mapFrame);
            aStarRoute.SetObstacle(resHoles);

            //----计算路径
            var path = aStarRoute.Plan(startPt);
            if (path != null)
            {
                ExtendLineModel extendLine = new ExtendLineModel();
                extendLine.line = path;
                extendLine.priority = Priority.startExtendLine;
                resLines.Add(extendLine);
            }

            return resLines;
        }

        /// <summary>
        /// 计算起始点离外框线大400距离
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
    }
}
