using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Print;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.DrainingSetting
{
    public class DrainningSettingTaggingService : DraningSettingService
    {
        double moveLength = 1500;
        public DrainningSettingTaggingService(List<RouteModel> _pipes, ThMEPOriginTransformer _originTransformer)
        {
            pipes = _pipes;
            originTransformer = _originTransformer;
        }

        public override void CreateDraningSetting()
        {
            if (pipes.Count <= 0)
            {
                return;
            }
            var resPipe = CalTaggingPt();
            Print(resPipe);
        }

        /// <summary>
        /// 调整连接线
        /// </summary>
        /// <returns></returns>
        private List<RouteModel> CalTaggingPt()
        {
            foreach (var pipe in pipes)
            {
                var line = pipes.First().connecLine;
                var allPts = GeometryUtils.GetAllPolylinePts(pipe.route).OrderBy(x => line.GetClosestPointTo(x, true).DistanceTo(x)).ToList();
                var lastPt = allPts.First();
                if (pipe.route.StartPoint.DistanceTo(pipe.startPosition) > pipe.route.EndPoint.DistanceTo(pipe.startPosition))
                {
                    pipe.route.ReverseCurve();
                }
                pipe.route = GeometryUtils.ShortenPolyline(pipe.route, moveLength);
            }

            return pipes;
        }

        /// <summary>
        /// 打印结果
        /// </summary>
        /// <param name="pipes"></param>
        private void Print(List<RouteModel> pipes)
        {
            var layoutInfos = pipes.Select(x =>
            {
                var route = x.route;
                var pt = route.GetPoint3dAt(route.NumberOfVertices - 1);
                var secPt = route.GetPoint3dAt(route.NumberOfVertices - 2);
                var transPr = originTransformer.Reset(pt);
                var dir = (pt - secPt).GetNormal();
                originTransformer.Reset(x.route);
                return new KeyValuePair<Point3d, Vector3d>(transPr, dir);
            }).ToList();
            InsertBlockService.InsertConnectPipe(pipes.Select(x => x.route).ToList(), ThWSSCommon.DraiLayerName, null);
            InsertBlockService.scaleNum = scale;
            InsertBlockService.InsertBlock(layoutInfos, ThWSSCommon.DisconnectionLayerName, ThWSSCommon.DisconnectionBlockName);
            PrintTaggingMarks(layoutInfos);
        }

        /// <summary>
        /// 打印标注（“接至雨水口”）
        /// </summary>
        /// <param name="layoutInfos"></param>
        private void PrintTaggingMarks(List<KeyValuePair<Point3d,Vector3d>> layoutInfos)
        {
            foreach (var lInfo in layoutInfos)
            {
                var dir = -lInfo.Value;
                var routeDir = dir.RotateBy(Math.PI * (45.0 / 180.0), Vector3d.ZAxis);
                var movePt = lInfo.Key + routeDir * 1000;
                var lastPt = movePt + Vector3d.XAxis * 1100;
                Polyline notePoly = new Polyline();
                notePoly.AddVertexAt(0, lInfo.Key.ToPoint2D(), 0, 0, 0);
                notePoly.AddVertexAt(1, movePt.ToPoint2D(), 0, 0, 0);
                notePoly.AddVertexAt(2, lastPt.ToPoint2D(), 0, 0, 0);
                var txtPt = movePt + Vector3d.XAxis * 500 + Vector3d.YAxis * 200;
                var markLines = new List<Polyline>() { notePoly };
                var dimtext = new DBText() { Height = 350, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = "接至雨水口", Position = txtPt, AlignmentPoint = txtPt };
                var dbTxts = new List<DBText>() { dimtext };
                PrintMarks.PrintNoteLines(markLines, ThWSSCommon.RainDimsLayerName, scale);
                PrintMarks.PrintText(dbTxts, ThWSSCommon.RainDimsLayerName, scale);
            }
        }
    }
}
