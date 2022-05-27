using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ChamferService
    {
        double length = 150;
        List<RouteModel> routes;
        public ChamferService(List<RouteModel> _routes)
        {
            routes = _routes;
        }

        /// <summary>
        /// 倒角
        /// </summary>
        public List<RouteModel> Chamfer()
        {
            var lstTree = GetRouteLstTree();
            foreach (var lTree in lstTree)
            {
                HandleLstTreeChamfer(lTree, null);
            }
            return GetRoutePath(lstTree);
        }

        /// <summary>
        /// 拿到树中的所有路径
        /// </summary>
        /// <param name="routeLst"></param>
        /// <returns></returns>
        private List<RouteModel> GetRoutePath(List<RouteList> routeLst)
        {
            List<RouteModel> routes = new List<RouteModel>();
            foreach (var rLst in routeLst)
            {
                routes.Add(rLst.route);
                if (rLst.Childs != null && rLst.Childs.Count > 0)
                {
                    routes.AddRange(GetRoutePath(rLst.Childs));
                }
            }
            
            return routes;
        }

        /// <summary>
        /// 计算管线树
        /// </summary>
        /// <returns></returns>
        private List<RouteList> GetRouteLstTree()
        {
            var rootRoutes = routes.Where(x => x.connecLine != null).ToList();
            var otherRoutes = routes.Except(rootRoutes).ToList();
            List<RouteList> routeTree = new List<RouteList>();
            foreach (var route in rootRoutes)
            {
                RouteList routeList = new RouteList();
                routeList.route = route;
                routeList.Childs = GetRouteList(route.route, ref otherRoutes);
                routeList.oringinRoutePoly = route.route;
                routeTree.Add(routeList);
            }
            return routeTree;
        }

        /// <summary>
        /// 计算树
        /// </summary>
        /// <param name="polyRoute"></param>
        /// <param name="routeModels"></param>
        /// <returns></returns>
        private List<RouteList> GetRouteList(Polyline polyRoute, ref List<RouteModel> routeModels)
        {
            var childRoutes = routeModels.Where(x => polyRoute.GetClosestPointTo(x.route.EndPoint, false).DistanceTo(x.route.EndPoint) < 0.01).ToList();
            routeModels = routeModels.Except(childRoutes).ToList();
            var routeLst = new List<RouteList>();
            foreach (var cRoute in childRoutes)
            {
                RouteList cRouteList = new RouteList();
                cRouteList.route = cRoute;
                cRouteList.Childs = GetRouteList(cRoute.route, ref routeModels);
                cRouteList.oringinRoutePoly = cRoute.route;
                routeLst.Add(cRouteList);
            }
            return routeLst;
        }

        /// <summary>
        /// 按树结构处理转角
        /// </summary>
        /// <param name="routeLst"></param>
        /// <param name="parentRoute"></param>
        private void HandleLstTreeChamfer(RouteList routeLst, RouteList parentRoute)
        {
            if (parentRoute != null)
            {
                routeLst.route.route = ConnectChamfer(routeLst.route.route, parentRoute.route.route, parentRoute.oringinRoutePoly);
            }
            routeLst.route.route = HandleChamfer(routeLst.route.route);

            foreach (var cRoute in routeLst.Childs)
            {
                HandleLstTreeChamfer(cRoute, routeLst);
            }
        } 

        /// <summary>
        /// 处理倒角
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Polyline HandleChamfer(Polyline polyline)
        {
            var resLines = new List<Line>();
            Line line = null;
            for (int i = 1; i < polyline.NumberOfVertices; i++)
            {
                if (line == null)
                {
                    line = new Line(polyline.GetPoint3dAt(i - 1), polyline.GetPoint3dAt(i));
                    resLines.Add(line);
                    continue;
                }
                var thisLine = new Line(polyline.GetPoint3dAt(i - 1), polyline.GetPoint3dAt(i));
                if (CheckAngle(line, thisLine))
                {
                    var resLine = ChamferByLine(line, thisLine, out Line resLine1, out Line resLine2);
                    if (resLine1 != null)
                    {
                        resLines.Remove(line);
                        resLines.Add(resLine1);
                    }
                    resLines.Add(resLine);
                    if (resLine2 != null)
                    {
                        resLines.Add(resLine2);
                        thisLine = resLine2;
                    }
                    else
                    {
                        line = null;
                        continue;
                    }
                }
                else
                {
                    resLines.Add(thisLine);
                }
                line = thisLine;
            }
            return CreateChamforByLine(resLines);
        }

        /// <summary>
        /// 处理转角
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="parentPoly"></param>
        /// <param name="parentOringinPoly"></param>
        /// <returns></returns>
        private Polyline ConnectChamfer(Polyline polyline, Polyline parentPoly, Polyline parentOringinPoly)
        {
            Polyline resPoly = new Polyline();
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                resPoly.AddVertexAt(i, polyline.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
            }

            var clonePoly = parentOringinPoly.Clone() as Polyline;
            clonePoly.ReverseCurve();
            var allLines = clonePoly.GetAllLineByPolyline();
            var closetLine = GeometryUtils.GetClosetLine(allLines, polyline.EndPoint).Key;
            if (parentPoly.GetClosestPointTo(polyline.EndPoint, false).DistanceTo(polyline.EndPoint) < 0.01)
            {
                var dis = polyline.EndPoint.DistanceTo(resPoly.EndPoint);
                if (dis > length)
                {
                    var lineDir = (polyline.EndPoint - polyline.StartPoint).GetNormal();
                    var lineEndP = polyline.EndPoint - lineDir * length;
                    resPoly.AddVertexAt(resPoly.NumberOfVertices, lineEndP.ToPoint2D(), 0, 0, 0);
                }
            }
            else
            {
                resPoly.AddVertexAt(resPoly.NumberOfVertices, polyline.EndPoint.ToPoint2D(), 0, 0, 0);
            }
            var dir = (closetLine.StartPoint - closetLine.EndPoint).GetNormal();
            var lastPt = polyline.EndPoint + dir * length;
            resPoly.AddVertexAt(resPoly.NumberOfVertices, lastPt.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 重新构建倒角之后的polyline
        /// </summary>
        /// <param name="resLines"></param>
        /// <returns></returns>
        private Polyline CreateChamforByLine(List<Line> resLines)
        {
            Polyline resPoly = new Polyline();
            foreach (var rLine in resLines)
            {
                resPoly.AddVertexAt(resPoly.NumberOfVertices, rLine.StartPoint.ToPoint2D(), 0, 0, 0);
            }
            resPoly.AddVertexAt(resPoly.NumberOfVertices, resLines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 进行倒角处理
        /// </summary>
        /// <param name="line"></param>
        /// <param name="thisLine"></param>
        /// <param name="resLine1"></param>
        /// <param name="resLine2"></param>
        /// <returns></returns>
        private Line ChamferByLine(Line line, Line thisLine, out Line resLine1, out Line resLine2)
        {
            resLine1 = null;
            resLine2 = null;
            var sp = line.StartPoint;
            var ep = thisLine.EndPoint;
            if (line.Length > length)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var lineEndP = line.EndPoint - lineDir * length;
                resLine1 = new Line(sp, lineEndP);
                sp = lineEndP;
            }
            if (thisLine.Length > length)
            {
                var lineDir = (thisLine.EndPoint - thisLine.StartPoint).GetNormal();
                var lineSP = thisLine.StartPoint + lineDir * length;
                resLine2 = new Line(lineSP, ep);
                ep = lineSP;
            }

            return new Line(sp, ep);
        }

        /// <summary>
        /// 检验角度是否需要做倒角
        /// </summary>
        /// <param name="line"></param>
        /// <param name="secLine"></param>
        /// <returns></returns>
        private bool CheckAngle(Line line, Line secLine)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var secDir = (secLine.EndPoint - secLine.StartPoint).GetNormal();
            if (dir.DotProduct(secDir) < 0.01)
            {
                return true;
            }

            return false;
        }
    }

    public class RouteList
    {
        public RouteModel route { get; set; }

        public List<RouteList> Childs = new List<RouteList>();

        public Polyline oringinRoutePoly { get; set; }
    }
}
