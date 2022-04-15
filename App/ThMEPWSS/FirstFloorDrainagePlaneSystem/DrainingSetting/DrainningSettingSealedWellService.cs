using Autodesk.AutoCAD.DatabaseServices;
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
    public class DrainningSettingSealedWellService : DraningSettingService
    {
        double radius = 400;
        double scale = 0.5;
        double moveLength = 500;
        public DrainningSettingSealedWellService(List<RouteModel> _pipes)
        {
            pipes = _pipes;
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
        /// 计算水封井
        /// </summary>
        /// <param name="pipes"></param>
        private void GetRainwaterInlet(List<RouteModel> pipes)
        {
            double allLength = moveLength + radius * 2 * scale;
            var inletPts = new List<KeyValuePair<Point3d, Vector3d>>();
            var routes = new List<Polyline>();
            foreach (var pipe in pipes)
            {
                var sp = pipe.route.GetPoint3dAt(0);
                var secP = pipe.route.GetPoint3dAt(1);
                var dir = (secP - sp).GetNormal();
                var firRoute = new Polyline();
                firRoute.AddVertexAt(0, sp.ToPoint2D(), 0, 0, 0);
                firRoute.AddVertexAt(1, (sp + dir * moveLength).ToPoint2D(), 0, 0, 0);
                routes.Add(firRoute);
                inletPts.Add(new KeyValuePair<Point3d, Vector3d>(sp + dir * (moveLength + radius * scale), Vector3d.YAxis));
                pipe.route = GeometryUtils.ShortenPolyline(pipe.route, allLength, true);
                routes.Add(pipe.route); 
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
            InsertBlockService.InsertBlock(layoutInfo, ThWSSCommon.SealedWellLayerName, ThWSSCommon.SealedWellBlockName);
        }
    }
}
