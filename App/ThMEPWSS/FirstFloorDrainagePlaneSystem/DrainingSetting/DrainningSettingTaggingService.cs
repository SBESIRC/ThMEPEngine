using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Print;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.DrainingSetting
{
    public class DrainningSettingTaggingService : DraningSettingService
    {
        double moveLength = 1000;
        public DrainningSettingTaggingService(List<RouteModel> _pipes)
        {
            pipes = _pipes;
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
            var line = pipes.First().connecLine;
            foreach (var pipe in pipes)
            {
                var allPts = GeometryUtils.GetAllPolylinePts(pipe.route).OrderBy(x => line.GetClosestPointTo(x, true).DistanceTo(x)).ToList();
                var lastPt = allPts.First();
                if (pipe.route.StartPoint.DistanceTo(lastPt) < pipe.route.EndPoint.DistanceTo(lastPt))
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
                var dir = (pt - secPt).GetNormal();
                return new KeyValuePair<Point3d, Vector3d>(pt, dir);
            }).ToList();
            InsertBlockService.InsertConnectPipe(pipes.Select(x => x.route).ToList(), ThWSSCommon.DraiLayerName, null);
            InsertBlockService.scaleNum = scale;
            InsertBlockService.InsertBlock(layoutInfos, ThWSSCommon.DisconnectionLayerName, ThWSSCommon.DisconnectionBlockName);
        }
    }
}
