using Autodesk.AutoCAD.DatabaseServices;
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
                    if (x.printCircle != null && !x.HasReservedPlug)
                    {
                        PrintPipeCircle(x.printCircle, originTransformer);
                        x.route = GeometryUtils.ShortenPolyline(x.route, 50, true);
                    }
                });
                var pipes = pipeLst.Select(x => { originTransformer.Reset(x.route); return x.route; }).ToList();
                InsertBlockService.scaleNum = scale;
                InsertBlockService.InsertConnectPipe(pipes, layer, null);
            }
        }

        private static void PrintPipeCircle(Circle circle, ThMEPOriginTransformer originTransformer)
        {
            originTransformer.Reset(circle);
            InsertBlockService.InsertPipeCircle(circle, ThWSSCommon.OutdoorWasteWellLayerName);
        }
    }
}
