using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    class EmgPilotLampUtil
    {
        /// <summary>
        /// 点投影到线
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Point3d PointToLine(Point3d point, Line line)
        {
            Point3d lineSp = line.StartPoint;
            Vector3d lineDirection = (line.EndPoint - line.StartPoint).GetNormal();
            var vect = point - lineSp;
            var dot = vect.DotProduct(lineDirection);
            return lineSp + lineDirection.MultiplyBy(dot);
        }
        /// <summary>
        /// 点投影到线
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lineSp"></param>
        /// <param name="lineDirection"></param>
        /// <returns></returns>
        public static Point3d PointToLine(Point3d point, Point3d lineSp, Vector3d lineDirection)
        {
            var vect = point - lineSp;
            var dot = vect.DotProduct(lineDirection);
            return lineSp + lineDirection.MultiplyBy(dot);
        }
        /// <summary>
        /// 将线按照一侧方向进行外扩为Polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sideDir"></param>
        /// <param name="sideDis"></param>
        /// <returns></returns>
        public static Polyline LineToPolyline(Line line, Vector3d sideDir, double sideDis,double expansion=0)
        {
            if (null == line)
                return null;
            if (expansion < -0.00001 && line.Length< Math.Abs(expansion*2))
                return null;
            Point3d sp = line.StartPoint;
            Point3d ep = line.EndPoint;
            Vector3d lineDir = (ep - sp).GetNormal();
            double dot = lineDir.DotProduct(sideDir);
            if (Math.Abs(dot) > 0.9)//扩展方向和线的夹角太小
                return null;
            sp = sp - lineDir.MultiplyBy(expansion);
            ep = ep + lineDir.MultiplyBy(expansion);
            Point3d spNext = sp + sideDir.MultiplyBy(sideDis);
            Point3d epNext = ep + sideDir.MultiplyBy(sideDis);

            Point2d sp2d = new Point2d(sp.X, sp.Y);
            Point2d ep2d = new Point2d(ep.X, ep.Y);
            Point2d sp2dNext = new Point2d(spNext.X, spNext.Y);
            Point2d ep2dNext = new Point2d(epNext.X, epNext.Y);
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, sp2d, 0, 0, 0);
            polyline.AddVertexAt(1, ep2d, 0, 0, 0);
            polyline.AddVertexAt(2, ep2dNext, 0, 0, 0);
            polyline.AddVertexAt(3, sp2dNext, 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }

        /// <summary>
        /// 点在线上
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <param name="precision"></param>
        /// <param name="precisionOut"></param>
        /// <returns></returns>
        public static bool PointInLine(Point3d point,Line line,double precision=5,double precisionOut =5) 
        {
            var prjPt = PointToLine(point, line);
            var dis = prjPt.DistanceTo(line.StartPoint) + prjPt.DistanceTo(line.EndPoint);
            var dis2 = prjPt.DistanceTo(point);
            if (dis > (line.Length + precision))
                return false;
            if (dis2 > precision)
                return false;
            return true;
        }
        public static bool PointInLines(Point3d point, List<Line> lines, double precision = 5, double precisionOut = 5) 
        {
            bool inLines = false;
            if (null == lines || lines.Count < 1)
                return false;
            foreach (var item in lines) 
            {
                if (item == null)
                    continue;
                inLines = PointInLine(point, item, precision, precisionOut);
                if (inLines)
                    break;
            }
            return inLines;
        }

        public static bool LineIsCollinear(Point3d firstSp, Point3d firstEp,Point3d secondSp, Point3d secondEp, double tolerance = 1.0, double precisionOut = 5, double precisionAngle =5) 
        {
            //这里不考虑异面问题，这里认为线为XOY平面上的两根线
            var maxAngle = precisionAngle * Math.PI / 180;
            //两根线有一定夹角，距离也可以认为是有共线
            var firstDir = (firstEp - firstSp).GetNormal();
            var secondDir = (secondEp - secondSp).GetNormal();
            var angle = firstDir.GetAngleTo(secondDir, Vector3d.ZAxis);
            angle %= Math.PI;
            if (Math.Abs(angle) > maxAngle && Math.Abs(Math.PI - angle) > maxAngle)
                return false;
            //认为平行，判断是否共线
            var prjSecondSp = PointToLine(secondSp, firstSp, firstDir);
            var prjSecondEp = PointToLine(secondEp, firstSp, firstDir);
            if (prjSecondSp.DistanceTo(secondSp) <= precisionOut || prjSecondEp.DistanceTo(secondEp) <= precisionOut)
            {
                //认为两个线离的比较近，进一步判断是否有共线
                //将线的方向调为一致
                var prjDir = (prjSecondEp - prjSecondSp).GetNormal();
                var dot = prjDir.DotProduct(firstDir);
                var tempSp = prjSecondSp;
                var tempEp = prjSecondEp;
                if (dot < -0.1)
                {
                    tempSp = prjSecondEp;
                    tempEp = prjSecondSp;
                }
                if (firstSp.DistanceTo(tempEp) < tolerance || firstEp.DistanceTo(tempSp) < tolerance)
                    //首尾相接
                    return false;
                if (firstSp.DistanceTo(tempSp) < tolerance)
                    return true;
                Line line = new Line(tempSp, tempEp);
                Line firstLine = new Line(firstSp, firstEp);
                if (PointInLine(firstSp, line) || PointInLine(firstEp, line) || PointInLine(tempSp, firstLine))
                    return true;
            }
            return false;

        }
        /// <summary>
        /// 根据点，根据节点信息构造
        /// </summary>
        /// <param name="allGraphNodes"></param>
        /// <param name="routePts"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        public static GraphRoute InitRouteByPoints(List<GraphNode> allGraphNodes, List<Point3d> routePts)
        {
            if (routePts == null || routePts.Count < 1)
                return null;
            Point3d point = routePts.FirstOrDefault();
            var node = allGraphNodes.Where(c => c.nodePoint.DistanceTo(point) < 2).FirstOrDefault();
            if (node == null)
                return null;
            var currentRoute = new GraphRoute();
            currentRoute.node = node;
            List<Point3d> nextPts = new List<Point3d>();
            for (int i = 1; i < routePts.Count; i++)
                nextPts.Add(routePts[i]);
            currentRoute.nextRoute = InitRouteByPoints(allGraphNodes, nextPts);
            return currentRoute;
        }
        public static GraphRoute InitRouteByNodes(List<GraphNode> graphNodes)
        {
            if (graphNodes == null || graphNodes.Count < 1)
                return null;
            var currentRoute = new GraphRoute();
            currentRoute.node = graphNodes.First();
            List<GraphNode> nextNodes = new List<GraphNode>();
            for (int i = 1; i < graphNodes.Count; i++)
                nextNodes.Add(graphNodes[i]);
            currentRoute.nextRoute = InitRouteByNodes(nextNodes);
            return currentRoute;
        }
        public static bool IsAllHostLine(List<Line> unHostLines, GraphRoute route)
        {
            var tempRoute = route;
            int count = 0;
            while (tempRoute != null)
            {
                if (count > 1)
                    break;
                var node = tempRoute.node;
                if (node != null && !node.isExit)
                {
                    if (PointInLines(node.nodePoint, unHostLines))
                        count += 1;
                }
                tempRoute = tempRoute.nextRoute;
            }
            return count > 1;
        }
        public static List<Line> RouteToLines(GraphRoute route,out List<GraphNode> routeNodes) 
        {
            List<Line> allLines = new List<Line>();
            var tempRoute = route;
            routeNodes = new List<GraphNode>();
            while (tempRoute.nextRoute != null) 
            {
                routeNodes.Add(tempRoute.node);
                var sp = tempRoute.node.nodePoint;
                var nextNode = tempRoute.nextRoute.node;
                var ep = nextNode.nodePoint;
                Line line = new Line(sp, ep);
                allLines.Add(line);
                tempRoute = tempRoute.nextRoute;
                if (tempRoute == null || tempRoute.nextRoute ==null)
                    routeNodes.Add(nextNode);
            }
            return allLines;
        }

        /// <summary>
        /// 判断生生成的灯是否符合要求，如果被边框穿过，不符合要求
        /// （有些框线穿过边框，但需要保留）
        /// </summary>
        /// <param name="createPoint"></param>
        /// <param name="arrowDir"></param>
        /// <param name="outDir"></param>
        /// <returns></returns>
        bool LightIsCorrect(Polyline maxPolyline, Point3d createPoint, Vector3d arrowDir, Vector3d outDir)
        {
            List<Line> lines = new List<Line>();
            var sp = createPoint + arrowDir.MultiplyBy(250);
            var ep = createPoint - arrowDir.MultiplyBy(250);
            var spOut = sp + outDir.MultiplyBy(250);
            var epOut = ep + outDir.MultiplyBy(250);
            lines.Add(new Line(sp, ep));
            lines.Add(new Line(ep, epOut));
            lines.Add(new Line(epOut, spOut));
            lines.Add(new Line(spOut, sp));
            bool isIntersection = false;
            var maxLines = maxPolyline.ExplodeLines();
            foreach (var line in lines)
            {
                if (isIntersection)
                    break;
                var liDir = (line.EndPoint - line.StartPoint).GetNormal();
                foreach (var target in maxLines)
                {
                    if (isIntersection)
                        break;
                    var targetDir = (target.EndPoint - target.StartPoint).GetNormal();
                    double angle = liDir.GetAngleTo(targetDir);
                    angle = angle % Math.PI;
                    if (angle < Math.PI / 18 || angle > (Math.PI - Math.PI / 18))
                        continue;
                    var res = ThCADCoreNTSLineExtension.Intersection(line, target, Intersect.OnBothOperands);
                    if (null != res)
                    {
                        Point3d pt = new Point3d(res.X, res.Y, createPoint.Z);
                        if (pt.DistanceTo(line.StartPoint) < 5 || pt.DistanceTo(line.EndPoint) < 5)
                            continue;
                        isIntersection = true;
                    }
                }
            }
            return isIntersection;
        }
        //GraphRoute InitRouteByPoints(List<Point3d> routePts, GraphRoute pNode)
        //{
        //    if (routePts == null || routePts.Count < 1)
        //        return null;
        //    Point3d point = routePts.FirstOrDefault();
        //    var node = _targetInfo.allNodes.Where(c => c.nodePoint.DistanceTo(point) < 2).FirstOrDefault();
        //    var currentRoute = new GraphRoute();
        //    currentRoute.node = node;
        //    List<Point3d> nextPts = new List<Point3d>();
        //    for (int i = 1; i < routePts.Count; i++)
        //        nextPts.Add(routePts[i]);
        //    currentRoute.nextRoute = InitRouteByPoints(nextPts, currentRoute);
        //    return currentRoute;
        //}
    }
}
