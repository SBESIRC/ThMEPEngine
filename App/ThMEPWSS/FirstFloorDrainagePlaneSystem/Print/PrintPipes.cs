using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Print
{
    public static class PrintPipes
    {
        public static void Print(List<RouteModel> routes, double scale, ThMEPOriginTransformer originTransformer)
        {
            var routeGroup = routes.GroupBy(x => x.verticalPipeType).ToList();
            string layer = ThWSSCommon.DraiLayerName;
            foreach (var group in routeGroup)
            {
                switch (group.Key)
                {
                    case VerticalPipeType.SewagePipe:
                    case VerticalPipeType.ConfluencePipe:
                        layer = ThWSSCommon.DraiSewageLayerName;
                        break;
                    case VerticalPipeType.WasteWaterPipe:
                        layer = ThWSSCommon.DraiWasteLayerName;
                        break;
                    case VerticalPipeType.CondensatePipe:
                    case VerticalPipeType.rainPipe:
                        layer = ThWSSCommon.DraiLayerName;
                        break;
                    default:
                        break;
                }
                var pipeLst = group.ToList();
                pipeLst.ForEach(x =>
                {
                    if (x.IsFloorDrainPipe)
                    {
                        PrintPipeFloorDraining(x.route.StartPoint, originTransformer);
                        x.route = GeometryUtils.ShortenPolylineByCircle(x.route, new Circle(x.route.StartPoint, Vector3d.ZAxis, 75 * scale), true);
                    }
                    else
                    {
                        if (x.printCircle != null && !x.HasReservedPlug)
                        {
                            PrintPipeCircle(x.printCircle, originTransformer);
                            x.route = GeometryUtils.ShortenPolyline(x.route, 50, true);
                        }
                        else if (x.originCircle != null)
                        {
                            x.route = GeometryUtils.ShortenPolyline(x.route, x.originCircle.Radius, true);
                        }
                    }
                });
                var pipes = pipeLst.Select(x => { originTransformer.Reset(x.route); return x.route; }).ToList();
                InsertBlockService.scaleNum = scale;
                InsertBlockService.InsertConnectPipe(pipes, layer, null);
            }
        }

        private static void PrintPipeFloorDraining(Point3d pt, ThMEPOriginTransformer originTransformer)
        {
            var transPt = originTransformer.Reset(pt);
            var layoutInfos = new List<KeyValuePair<Point3d, Vector3d>>() { new KeyValuePair<Point3d, Vector3d>(transPt, Vector3d.YAxis) };
            InsertBlockService.InsertBlock(layoutInfos, ThWSSCommon.FloorDrainingLayerName, ThWSSCommon.FloorDrainingBlockName);
        }

        private static void PrintPipeCircle(Circle circle, ThMEPOriginTransformer originTransformer)
        {
            originTransformer.Reset(circle);
            InsertBlockService.InsertPipeCircle(circle, ThWSSCommon.OutdoorWasteWellLayerName);
        }
    }
}
