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
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateTubeWellService
    {
        public double scale = 1;
        double routeDis = 1500;
        double shortedDis = 400;
        List<RouteModel> routes;
        ThMEPOriginTransformer originTransformer;
        public CreateTubeWellService(List<RouteModel> _routes, ThMEPOriginTransformer _originTransformer)
        {
            routes = _routes;
            originTransformer = _originTransformer;
        }

        public List<RouteModel> Layout()
        {
            var usefulRoutes = routes.Where(x => x.connecLine != null).ToList();
            var otherRoutes = routes.Except(usefulRoutes).ToList();
            var routeDic = GroupRoutes(usefulRoutes);
            var resLst = ClassifyRoutes(routeDic);
            var tubeWellPts = new List<KeyValuePair<VerticalPipeType, KeyValuePair<Point3d, Vector3d>>>();
            foreach (var routeLst in resLst)
            {
                tubeWellPts.Add(new KeyValuePair<VerticalPipeType, KeyValuePair<Point3d, Vector3d>>(routeLst.First().verticalPipeType, CalTubeWellPt(routeLst)));
            }
            Print(tubeWellPts);
            usefulRoutes.ForEach(x => x.route = GeometryUtils.ShortenPolyline(x.route, shortedDis));
            usefulRoutes.AddRange(otherRoutes);

            return usefulRoutes;
        }

        /// <summary>
        /// 计算管井放置点位
        /// </summary>
        /// <param name="routes"></param>
        /// <returns></returns>
        private KeyValuePair<Point3d, Vector3d> CalTubeWellPt(List<RouteModel> routes)
        {
            routes.ForEach(x =>
            {
                if (x.route.EndPoint.DistanceTo(x.startPosition) < x.route.StartPoint.DistanceTo(x.startPosition))
                {
                    x.route.ReverseCurve();
                }
            });
            var routeEndPts = routes.Select(x => x.route.EndPoint).ToList();
            var pt = routeEndPts.First();
            if (routeEndPts.Count >= 1)
            {
                var firPt = routeEndPts.First();
                var secPt = routeEndPts.Last();
                pt = new Point3d((firPt.X + secPt.X) / 2, (firPt.Y + secPt.Y) / 2, 0);
            }
            var polyline = routes.First().route;
            var dir = (polyline.GetPoint3dAt(polyline.NumberOfVertices - 2) - polyline.GetPoint3dAt(polyline.NumberOfVertices - 1)).GetNormal();
            pt = pt - Vector3d.YAxis * shortedDis;
            return new KeyValuePair<Point3d, Vector3d>(pt, Vector3d.YAxis);
        }

        /// <summary>
        /// 分类管线
        /// </summary>
        /// <param name="routeDic"></param>
        /// <returns></returns>
        private List<List<RouteModel>> ClassifyRoutes(Dictionary<Line, List<RouteModel>> routeDic)
        {
            List<List<RouteModel>> resLst = new List<List<RouteModel>>();
            foreach (var dic in routeDic)
            {
                var dir = (dic.Key.EndPoint - dic.Key.StartPoint).GetNormal();
                resLst.AddRange(CalTubeWellRoute(dic.Value, dir));
            }
            return resLst;
        }

        /// <summary>
        /// 计算管井合并连线
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private List<List<RouteModel>> CalTubeWellRoute(List<RouteModel> routes, Vector3d dir)
        {
            var matrix = GeometryUtils.GetMatrix(dir);
            var routeTransDic = routes.ToDictionary(x => x, y =>
            {
                var pt = y.route.StartPoint.DistanceTo(y.startPosition) > y.route.EndPoint.DistanceTo(y.startPosition) ? y.route.StartPoint : y.route.EndPoint;
                return pt.TransformBy(matrix);
            }).OrderBy(x => x.Value.X).ToDictionary(x => x.Key, y => y.Value);

            var routeLst = new List<List<RouteModel>>();
            var resLst = new List<RouteModel>();
            RouteModel thisRoute = null;
            Point3d? point = null;
            foreach (var dic in routeTransDic)
            {
                if (thisRoute == null)
                {
                    thisRoute = dic.Key;
                    point = dic.Value;
                    resLst.Add(thisRoute);
                }
                else
                {
                    if (CheckService.CheckVerticalType(thisRoute.verticalPipeType, dic.Key.verticalPipeType) && point.Value.DistanceTo(dic.Value) < routeDis && resLst.Count <= 4)
                    {
                        resLst.Add(dic.Key);
                    }
                    else
                    {
                        routeLst.Add(new List<RouteModel>(resLst));
                        resLst = new List<RouteModel>();
                        thisRoute = dic.Key;
                        point = dic.Value;
                        resLst.Add(thisRoute);
                    }
                }
            }

            routeLst.Add(new List<RouteModel>(resLst));
            return routeLst;
        }

        /// <summary>
        /// 根据连接线的不同将连接线分组
        /// </summary>
        /// <param name="usefulRoutes"></param>
        /// <returns></returns>
        private Dictionary<Line, List<RouteModel>> GroupRoutes(List<RouteModel> usefulRoutes)
        {
            var routeDic = new Dictionary<Line, List<RouteModel>>();
            foreach (var route in usefulRoutes)
            {
                var keyLine = routeDic.Keys.Where(x => x.IsEqualLine(route.connecLine)).FirstOrDefault();
                if (keyLine != null)
                {
                    routeDic[keyLine].Add(route);
                }
                else
                {
                    routeDic.Add(route.connecLine, new List<RouteModel>() { route });
                }
            }

            return routeDic;
        }

        /// <summary>
        /// 打印管井
        /// </summary>
        /// <param name="layoutPts"></param>
        private void Print(List<KeyValuePair<VerticalPipeType, KeyValuePair<Point3d, Vector3d>>> layoutPts)
        {
            foreach (var pt in layoutPts)
            {
                var transPt = originTransformer.Reset(pt.Value.Key);
                var layoutInfos = new List<KeyValuePair<Point3d, Vector3d>>() { new KeyValuePair<Point3d, Vector3d>(transPt, pt.Value.Value) };
                InsertBlockService.scaleNum = scale;
                string layer = "";
                string blockName = "";
                if (pt.Key == VerticalPipeType.CondensatePipe || pt.Key == VerticalPipeType.rainPipe)
                {
                    layer = ThWSSCommon.OutdoorRainWellLayerName;
                    blockName = ThWSSCommon.OutdoorRainWellBlockName;
                }
                else if (pt.Key == VerticalPipeType.SewagePipe || pt.Key == VerticalPipeType.ConfluencePipe || pt.Key == VerticalPipeType.WasteWaterPipe)
                {
                    layer = ThWSSCommon.OutdoorWasteWellLayerName;
                    blockName = ThWSSCommon.OutdoorWasteWellBlockName;
                }
                InsertBlockService.InsertBlock(layoutInfos, layer, blockName);
            }
        }
    }
}