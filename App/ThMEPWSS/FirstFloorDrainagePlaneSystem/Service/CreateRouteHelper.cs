using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class CreateRouteHelper
    {
        /// <summary>
        /// 获取最近的线信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lines, Point3d startPt, Polyline polyline, List<Polyline> wallPolys, double step)
        {
            var closeInfo = GeometryUtils.GetClosetLine(lines, startPt);
            Line checkLine = new Line(startPt, closeInfo.Value);
            if (!CheckService.CheckIntersectWithFrame(checkLine, polyline) && !CheckService.CheckIntersectWithHoles(checkLine, wallPolys))
            {
                var checkDir = (closeInfo.Value - startPt).GetNormal();
                var lineDir = Vector3d.ZAxis.CrossProduct((closeInfo.Key.EndPoint - closeInfo.Key.StartPoint).GetNormal());
                if (checkDir.IsParallelTo(lineDir, new Tolerance(0.001, 0.001)))
                {
                    return closeInfo;
                }
            }

            BFSPathPlaner pathPlaner = new BFSPathPlaner(step, wallPolys);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lines, polyline);
            var closetPt = closetLine.GetClosestPointTo(startPt, false);

            return new KeyValuePair<Line, Point3d>(closetLine, closetPt);
        }

        /// <summary>
        /// 创建连接线加权区域
        /// </summary>
        /// <param name="polylines"></param>
        /// <returns></returns>
        public static List<Polyline> CreateConnectLineHoles(List<Polyline> polylines, double lineDis)
        {
            var resLines = new List<Polyline>();
            foreach (var polyline in polylines)
            {
                resLines.AddRange(polyline.BufferFlatPL(lineDis).Cast<Polyline>().ToList());
            }
            return resLines;
        }

        /// <summary>
        /// 将其他点创建成洞口（不允许通过其他点）
        /// </summary>
        /// <param name="pipes"></param>
        /// <param name="thisPipe"></param>
        /// <param name="closeLine"></param>
        /// <returns></returns>
        public static List<Polyline> CreateOtherPipeHoles(List<VerticalPipeModel> pipes, VerticalPipeModel thisPipe, Line closeLine, double step)
        {
            var dir = (closeLine.EndPoint - closeLine.StartPoint).GetNormal();
            var otherPipes = pipes.Except(new List<VerticalPipeModel>() { thisPipe }).ToList();
            var pipeHoles = new List<Polyline>();
            foreach (var pipe in otherPipes)
            {
                pipeHoles.Add(pipe.Position.CreatePolylineByPt(step / 2, dir));
            }

            return pipeHoles;
        }

        /// <summary>
        /// 将房间内的管线和房间外的连接线合并成一根连接线
        /// </summary>
        /// <param name="inRoute"></param>
        /// <param name="outRoute"></param>
        /// <returns></returns>
        public static Polyline MergeRouteLine(Polyline inRoute, Polyline outRoute)
        {
            Polyline resRoute = new Polyline();
            for (int i = 0; i < inRoute.NumberOfVertices; i++)
            {
                resRoute.AddVertexAt(resRoute.NumberOfVertices, inRoute.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
            }
            if (resRoute.EndPoint.DistanceTo(outRoute.EndPoint) < resRoute.EndPoint.DistanceTo(outRoute.StartPoint))
            {
                outRoute.ReverseCurve();
            }
            for (int i = 0; i < outRoute.NumberOfVertices; i++)
            {
                resRoute.AddVertexAt(resRoute.NumberOfVertices, outRoute.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
            }
            return resRoute.DPSimplify(1);
        }
    }
}
