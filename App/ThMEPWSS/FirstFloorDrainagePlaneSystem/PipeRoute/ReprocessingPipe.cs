using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ReprocessingPipe
    {
        double frameSpace = 300;
        double moveStep = 100;
        double frameDis = 150;
        List<RouteModel> routes;
        List<Polyline> outFrame;
        List<Polyline> holes;
        public ReprocessingPipe(List<RouteModel> routeModels, List<Polyline> _outFrame, List<Polyline> _holes)
        {
            routes = routeModels;
            outFrame = _outFrame;
            holes = _holes;
        }

        /// <summary>
        /// 后处理
        /// </summary>
        public List<RouteModel> Reprocessing()
        {
            foreach (var Frame in outFrame)
            {
                var interRoutes = GetIntersectRoute(Frame, routes);
                HandleSpace(Frame, interRoutes);
            }
            return routes;
        }

        /// <summary>
        /// 后处理间距
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="routes"></param>
        private void HandleSpace(Polyline frame, List<RouteModel> routes)
        {
            if (routes.Count <= 0)
            {
                return;
            }
            var dir = GetOrderDir(frame, routes.First().route);
            var matrix = CalOrderMatrix(dir);
            var orderRoutes = OrderRoutes(matrix, routes);
            var frameLength = GetFrameLength(matrix, frame);
            MoveRouteBySpace(orderRoutes, frame, dir, holes, frameLength);
        }

        /// <summary>
        /// 调整出户框线路由间距为300
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="frame"></param>
        /// <param name="dir"></param>
        /// <param name="holes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private bool MoveRouteBySpace(List<RouteModel> routes, Polyline frame, Vector3d dir, List<Polyline> holes, double length)
        {
            length = length - frameDis;
            var spaceDis = 0.0;
            var allPts = frame.Vertices().Cast<Point3d>().ToList();
            for (int i = 0; i < routes.Count; i++)
            {
                if (i == 0)
                {
                    spaceDis = spaceDis + frameDis;
                }
                else
                {
                    spaceDis = spaceDis + frameSpace;
                }
                var route = routes[i];
                var routePoly = route.route;
                var ptDis = allPts.Select(x => routePoly.DistanceTo(x, false)).OrderBy(x => x).First();
                do
                {
                    var moveDis = spaceDis - ptDis; 
                    if (MoveRouteLineSegment(frame, dir, moveDis, holes, ref route))
                    {
                        break;
                    }
                    else
                    {
                        spaceDis = spaceDis + moveStep;
                    }
                } while (spaceDis < length);
            }

            return spaceDis < length;
        }

        /// <summary>
        /// 移动出乎框线处的路由线
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="routeModel"></param>
        /// <param name="dir"></param>
        /// <param name="moveDis"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private bool MoveRouteLineSegment(Polyline frame, Vector3d dir, double moveDis, List<Polyline> holes, ref RouteModel routeModel)
        {
            var allLines = routeModel.route.GetAllLineByPolyline();
            var resLineDic = new Dictionary<Line, bool>();
            foreach (var line in allLines)
            {
                if (frame.IsIntersects(line))
                {
                    var lineDir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
                    if (lineDir.DotProduct(dir) < 0)
                    {
                        lineDir = -lineDir;
                    }
                    var moveLine = new Line(line.StartPoint + lineDir * moveDis, line.EndPoint + lineDir * moveDis);
                    if (CheckService.CheckIntersectWithHoles(moveLine, holes))
                    {
                        return false;
                    }
                    resLineDic.Add(moveLine, true);
                }
                else
                {
                    resLineDic.Add(line, false);
                }
            }
            routeModel.route = CreateMovePolyline(resLineDic);
            return true;
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
        /// 求框线长度
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        private double GetFrameLength(Matrix3d matrix, Polyline frame)
        {
            var allPts = frame.Vertices().Cast<Point3d>().ToList();
            allPts = allPts.Select(x => x.TransformBy(matrix.Inverse())).OrderBy(x => x.X).ToList();
            return Math.Abs(allPts.First().X - allPts.Last().X);
        }

        /// <summary>
        /// 排序路由
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="routes"></param>
        /// <returns></returns>
        private List<RouteModel> OrderRoutes(Matrix3d matrix, List<RouteModel> routes)
        {
            return routes.OrderBy(x =>
            {
                var transPt = x.startPosition.TransformBy(matrix.Inverse());
                return transPt.X;
            }).ToList();
        }

        /// <summary>
        /// 获取排序矩阵
        /// </summary>
        /// <param name="xDir"></param>
        /// <returns></returns>
        private Matrix3d CalOrderMatrix(Vector3d xDir)
        {
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            return matrix;
        }

        /// <summary>
        /// 计算获取和调整方向
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        private Vector3d GetOrderDir(Polyline frame, Polyline route)
        {
            var allLines = StructGeoService.GetAllLineByPolyline(route);
            var line = allLines.Where(x => x.IsIntersects(frame)).First();
            var dir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
            var allPts = frame.Vertices().Cast<Point3d>().ToList();
            var pt = allPts.OrderBy(x => line.DistanceTo(x, false)).First();
            var closePt = line.GetClosestPointTo(pt, false);
            if (dir.DotProduct((closePt - pt).GetNormal()) < 0)
            {
                dir = -dir;
            }
            return dir;
        }

        /// <summary>
        /// 获取相交的路由
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="routes"></param>
        /// <returns></returns>
        private List<RouteModel> GetIntersectRoute(Polyline frame, List<RouteModel> routes)
        {
            return routes.Where(x => frame.IsIntersects(x.route)).ToList();
        }
    }
}