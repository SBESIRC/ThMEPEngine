﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ReprocessingPipe
    {
        double frameSpace = 300;
        double frameDis = 150;
        List<RouteModel> routes;
        List<Polyline> outFrame;
        public ReprocessingPipe(List<RouteModel> routeModels, List<Polyline> _outFrame)
        {
            routes = routeModels;
            outFrame = _outFrame;
        }

        /// <summary>
        /// 后处理
        /// </summary>
        public List<RouteModel> Reprocessing()
        {
            var polys = routes.Select(x => x.route).ToList();
            var frame = FindOutFrame(polys);
            var line = FindRouteIntersectLine(polys.First(), frame);
            var dir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());

            var routeDic = routes.ToDictionary(x => x, y => FindRouteIntersectLine(y.route, frame));
            return AdjustRoute(routeDic, dir);
        }

        /// <summary>
        /// 调整路由线保证间距
        /// </summary>
        /// <param name="routeDic"></param>
        /// <param name="dir"></param>
        private List<RouteModel> AdjustRoute(Dictionary<RouteModel, Line> routeDic, Vector3d dir)
        {
            var firRoute = routeDic.OrderBy(x => x.Key.route.NumberOfVertices).First();
            var leftDics = new Dictionary<RouteModel, Line>();
            var rightDics = new Dictionary<RouteModel, Line>();
            foreach (var dic in routeDic)
            {
                if (dic.Key != firRoute.Key)
                {
                    var checkDir = (dic.Value.StartPoint - firRoute.Value.StartPoint).GetNormal();
                    if (checkDir.DotProduct(dir) >= 0)
                    {
                        leftDics.Add(dic.Key, dic.Value);
                    }
                    else
                    {
                        rightDics.Add(dic.Key, dic.Value);
                    }
                }
            }

            var resRoutes = MoveRouteBySpace(firRoute, leftDics, dir);
            resRoutes.Add(firRoute.Key);
            resRoutes.AddRange(MoveRouteBySpace(firRoute, rightDics, dir));

            return resRoutes;
        }

        /// <summary>
        /// 根据间距移动出户路由
        /// </summary>
        /// <param name="firRoute"></param>
        /// <param name="routeDic"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private List<RouteModel> MoveRouteBySpace(KeyValuePair<RouteModel, Line> firRoute, Dictionary<RouteModel, Line> routeDic, Vector3d dir)
        {
            List<RouteModel> resRoute = new List<RouteModel>();
            if (routeDic.Count <= 0)
            {
                return resRoute;
            }

            var checkDir = (routeDic.First().Value.StartPoint - firRoute.Value.StartPoint).GetNormal();
            if (checkDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }

            double space = frameSpace;
            routeDic = routeDic.OrderBy(x => x.Value.Distance(firRoute.Value)).ToDictionary(x => x.Key, y => y.Value);
            while (routeDic.Count > 0)
            {
                var dic = routeDic.First();
                var dis = dic.Value.Distance(firRoute.Value);
                var routeModel = dic.Key;
                if (dis != space)
                {
                    var moveDis = (space - dis);
                    routeModel.route = MoveRouteLineSegment(dir, moveDis, dic);
                }
                resRoute.Add(routeModel);
                space += frameSpace;
                routeDic.Remove(dic.Key);
            }

            return resRoute;
        }

        /// <summary>
        /// 移动路由线
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="moveDis"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        private Polyline MoveRouteLineSegment(Vector3d dir, double moveDis, KeyValuePair<RouteModel, Line> route)
        {
            var allLines = route.Key.route.GetAllLineByPolyline();
            var resLineDic = new Dictionary<Line, bool>();
            foreach (var line in allLines)
            {
                if (route.Value.StartPoint.IsEqualTo(line.StartPoint, new Tolerance(0.01, 0.01)) ||
                    route.Value.EndPoint.IsEqualTo(line.EndPoint, new Tolerance(0.01, 0.01)))
                {
                    var moveLine = new Line(line.StartPoint + dir * moveDis, line.EndPoint + dir * moveDis);
                    resLineDic.Add(moveLine, true);
                    route = new KeyValuePair<RouteModel, Line>(route.Key, moveLine);
                }
                else
                {
                    resLineDic.Add(line, false);
                }
            }
            return CreateMovePolyline(resLineDic);
        }

        /// <summary>
        /// 将移动线创建成polyline
        /// </summary>
        /// <param name="lineDic"></param>
        private Polyline CreateMovePolyline(Dictionary<Line, bool> lineDic)
        {
            var pts = new List<Point3d>();
            var allKeys = lineDic.Keys.ToList();
            var preMove = false;
            for (int i = 0; i < lineDic.Count; i++)
            {
                var line = allKeys[i];
                if (lineDic[line])
                {
                    pts.Add(line.StartPoint);
                    pts.Add(line.EndPoint);
                    preMove = true;
                    continue;
                }
                else
                {
                    if (!preMove)
                    {
                        pts.Add(line.StartPoint);
                    }
                    if (i == lineDic.Count - 1)
                    {
                        pts.Add(line.EndPoint);
                    }
                }
                preMove = false;
            }
            Polyline resPoly = new Polyline();
            foreach (var pt in pts)
            {
                resPoly.AddVertexAt(resPoly.NumberOfVertices, pt.ToPoint2D(), 0, 0, 0);
            }
            return resPoly;
        }

        /// <summary>
        /// 找到相交段的线
        /// </summary>
        /// <param name="routePoly"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        private Line FindRouteIntersectLine(Polyline routePoly, Polyline frame)
        {
            var allLines = routePoly.GetAllLineByPolyline();
            return allLines.FirstOrDefault(x => x.IsIntersects(frame));
        }

        /// <summary>
        /// 寻找出户的框线
        /// </summary>
        /// <returns></returns>
        private Polyline FindOutFrame(List<Polyline> routePolys)
        {
            var frames = outFrame.Where(x => routePolys.Any(y=>y.IsIntersects(x))).ToList();
            var ep = routePolys.First().EndPoint;
            var needFrame = frames.OrderByDescending(x => x.DistanceTo(ep, false)).First();
            return needFrame;
        }
    }
}