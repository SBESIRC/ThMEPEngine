using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateTubeWellService
    {
        double routeDis = 1500;
        ParamSettingViewModel paramSetting;
        List<RouteModel> routes;
        public CreateTubeWellService(List<RouteModel> _routes, ParamSettingViewModel _paramSetting)
        {
            routes = _routes;
            paramSetting = _paramSetting;
        }

        public void Layout()
        {
            var usefulRoutes = routes.Where(x => x.connecLine != null).ToList();
            var routeDic = GroupRoutes(usefulRoutes);
            var resLst = ClassifyRoutes(routeDic);
            var tubeWellPts = new List<Point3d>();
            foreach (var routeLst in resLst)
            {
                tubeWellPts.Add(CalTubeWellPt(routes));
            }
        }

        /// <summary>
        /// 计算管井放置点位
        /// </summary>
        /// <param name="routes"></param>
        /// <returns></returns>
        private Point3d CalTubeWellPt(List<RouteModel> routes)
        {
            var routeEndPts = routes.Select(x => x.route.StartPoint.DistanceTo(x.startPosition) > x.route.EndPoint.DistanceTo(x.startPosition) ? x.route.StartPoint : x.route.EndPoint)
                .ToList(); ;
            if (routeEndPts.Count == 1)
            {
                return routeEndPts.First();
            }
            else
            {
                var firPt = routeEndPts.First();
                var secPt = routeEndPts.Last();
                return new Point3d((firPt.X + secPt.X) / 2, (firPt.Y + secPt.Y) / 2, 0);
            }
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
    }
}