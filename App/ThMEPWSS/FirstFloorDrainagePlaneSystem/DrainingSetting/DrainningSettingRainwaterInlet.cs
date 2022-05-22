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
    public class DrainningSettingRainwaterInlet : DraningSettingService
    {
        double inletWidth = 100;
        double moveLength = 500;
        public DrainningSettingRainwaterInlet(List<RouteModel> _pipes, ThMEPOriginTransformer _originTransformer)
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
            var line = pipes.First().connecLine;
            foreach (var pipe in pipes)
            {
                var allPts = GeometryUtils.GetAllPolylinePts(pipe.route).OrderBy(x => line.GetClosestPointTo(x, true).DistanceTo(x)).ToList();
                var lastPt = allPts.First();
                if (pipe.route.StartPoint.DistanceTo(lastPt) < pipe.route.EndPoint.DistanceTo(lastPt))
                {
                    pipe.route.ReverseCurve();
                }
            }
            GetRainwaterInlet(pipes);
        }

        /// <summary>
        /// 计算雨水口
        /// </summary>
        /// <param name="pipes"></param>
        private void GetRainwaterInlet(List<RouteModel> pipes)
        {
            var inletPts = new List<KeyValuePair<Point3d, Vector3d>>();
            var routes = new List<Polyline>();
            foreach (var pipe in pipes)
            {
                var routePoly = pipe.route;
                originTransformer.Reset(routePoly);
                GeometryUtils.CutPolylineByLength(routePoly, moveLength, out Polyline sPoly, out Polyline ePoly, out Vector3d layoutDir, out Point3d cutPt);
                routes.Add(sPoly);
                if (ePoly.Length > 0)
                {
                    ePoly = GeometryUtils.ShortenPolyline(ePoly, inletWidth * 2, true);
                    routes.Add(ePoly);
                    inletPts.Add(new KeyValuePair<Point3d, Vector3d>(cutPt, -layoutDir));
                }
            }

            Print(inletPts, routes);
        }

        /// <summary>
        /// 打印结果
        /// </summary>
        /// <param name="pipes"></param>
        private void Print(List<KeyValuePair<Point3d, Vector3d>> layoutInfo, List<Polyline> routes)
        {
            InsertBlockService.InsertConnectPipe(routes, ThWSSCommon.DraiLayerName, null);
            InsertBlockService.scaleNum = scale;
            InsertBlockService.InsertBlock(layoutInfo, ThWSSCommon.RainwaterInletLayerName, ThWSSCommon.RainwaterInletBlockName);
        }
    }
}
