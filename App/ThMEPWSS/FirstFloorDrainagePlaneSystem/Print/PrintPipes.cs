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
                    case VerticalPipeType.RainwaterInlet13Pipe:
                    case VerticalPipeType.RainPipe:
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
                    else if (x.verticalPipeType == VerticalPipeType.RainwaterInlet13Pipe)
                    {
                        var recangle = CreateRecangle(x.route.StartPoint, x.block.Rotation, x.block.ScaleFactors.Y);
                        x.route = GeometryUtils.ShortenPolylineByRecangle(x.route, recangle, true);
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

        private static Polyline CreateRecangle(Point3d pt, double angle, double scale)
        {
            var length = 100 * scale;
            var vecY = Vector3d.YAxis.RotateBy(angle, Vector3d.ZAxis);
            var vecX = Vector3d.ZAxis.CrossProduct(vecY);
            Polyline poly = new Polyline() { Closed = true };
            poly.AddVertexAt(0, (pt + vecY * length + vecX * length).ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, (pt - vecY * length + vecX * length).ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(2, (pt - vecY * length - vecX * length).ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(3, (pt + vecY * length - vecX * length).ToPoint2D(), 0, 0, 0);
            return poly;
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
