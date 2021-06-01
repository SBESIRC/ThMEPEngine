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
        public static Polyline LineToPolyline(Line line, Vector3d sideDir, double sideDis,double expansion=0,bool isTwoSide=false)
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
            if (isTwoSide)
            { 
                sp = sp - sideDir.MultiplyBy(sideDis);
                ep = ep - sideDir.MultiplyBy(sideDis);
            }
            Point3d spNext = sp + sideDir.MultiplyBy(isTwoSide ? sideDis * 2 : sideDis);
            Point3d epNext = ep + sideDir.MultiplyBy(isTwoSide ? sideDis * 2 : sideDis);

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
            if (dis2 > precisionOut)
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
            var isColl = LineIsCollinear(firstSp, firstEp, secondSp, secondEp, out List<Point3d> collPoints, tolerance, precisionOut, precisionAngle);
            return isColl;
        }
        public static bool LineIsCollinear(Point3d firstSp, Point3d firstEp, List<Line> targetLines, double tolerance = 1.0, double precisionOut = 5, double precisionAngle = 5)
        {
            var isColl = false;
            foreach (var line in targetLines) 
            {
                isColl = LineIsCollinear(firstSp, firstEp, line.StartPoint, line.EndPoint, out List<Point3d> collPoints, tolerance, precisionOut, precisionAngle);
                if (isColl)
                    return true;
            }
            return isColl;
        }
        public static bool LineIsCollinear(Point3d firstSp, Point3d firstEp, Point3d secondSp, Point3d secondEp,out List<Point3d> collPoints, double tolerance = 1.0, double precisionOut = 5, double precisionAngle = 5)
        {
            collPoints = new List<Point3d>();
            var firstLength = firstSp.DistanceTo(firstEp);
            var secondLength = secondEp.DistanceTo(secondSp);
            if (Math.Abs(firstLength - secondLength) > 1000 && firstLength < secondLength)
                return LineIsCollinear(secondSp, secondEp, firstSp, firstEp, out collPoints, tolerance, precisionOut, precisionAngle);
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
                List<Point3d> pts = new List<Point3d>() { firstSp, firstEp, tempSp, tempEp };
                pts = PointOrderByLine(pts, firstSp, firstDir);
                if (firstSp.DistanceTo(tempSp) < tolerance)
                {
                    if (pts.Count % 2 == 0)
                    {
                        //取中间两个点
                        int c = pts.Count / 2;
                        collPoints.Add(pts[c - 1]);
                        collPoints.Add(pts[c]);
                    }
                    else 
                    {
                        collPoints.Add(pts[0]);
                        collPoints.Add(pts[1]);
                    }
                    return true;
                }
                Line line = new Line(tempSp, tempEp);
                Line firstLine = new Line(firstSp, firstEp);
                if (PointInLine(firstSp, line) || PointInLine(firstEp, line) || PointInLine(tempEp, firstLine))
                {
                    if (pts.Count % 2 == 0)
                    {
                        //取中间两个点
                        int c = pts.Count / 2;
                        collPoints.Add(pts[c - 1]);
                        collPoints.Add(pts[c]);
                    }
                    else
                    {
                        collPoints.Add(pts[1]);
                        collPoints.Add(pts[2]);
                    }
                    return true;
                }
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
        /// 闭合区域外侧
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="isOut"></param>
        /// <returns></returns>
        public static Dictionary<Line, Vector3d> PolylineOutDir(Polyline pline, bool isOut = true)
        {
            
            Dictionary<Line, Vector3d> valuePairs = new Dictionary<Line, Vector3d>();
            var polyline = pline.DPSimplify(10);
            //要注意顺时针，逆时针问题,这个默认时顺时针
            var pNormal = polyline.Normal;
            var objs = new DBObjectCollection();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var sp = polyline.GetPoint3dAt(i);
                var ep = polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices);
                if (sp.DistanceTo(ep) < 0.0001)
                    continue;
                var line = new Line(sp, ep);
                var lineDir = (ep - sp).GetNormal();
                var outDir = lineDir.CrossProduct(pNormal).GetNormal();
                if (isOut)
                    outDir = outDir.Negate();
                valuePairs.Add(line, outDir);
                objs.Add(line);
            }
            
            List<Line> newLines = new List<Line>();
            if (objs.Count < 40)
            {
                //这里主要时处理柱墙的，一般不会有太多线，线太多时合并太影响效率，这里只在线不是特别多时进行合成操作
                newLines = ThMEPEngineCore.Algorithm.ThMEPLineExtension.LineSimplifier(objs, 5, 2.0, 2.0, Math.PI / 180.0).Cast<Line>().ToList();
            }
            else 
            {
                return valuePairs;
            }
            Dictionary<Line, Vector3d> retValuePairs = new Dictionary<Line, Vector3d>();
            foreach (var line in newLines) 
            {
                Vector3d outDir = new Vector3d();
                foreach (var item in valuePairs) 
                {
                    if (LineIsCollinear(line.StartPoint, line.EndPoint, item.Key.StartPoint, item.Key.EndPoint))
                    {
                        outDir = item.Value;
                        break;
                    }
                }
                retValuePairs.Add(line, outDir);
            }
            return retValuePairs;
        }
        public static Point3d LineCloseNearPoint(Line line, Point3d point,bool pointInLine=true)
        {
            var prjPoint = PointToLine(point, line);
            if (pointInLine)
            {
                if (PointInLine(prjPoint, line))
                    return prjPoint;
                if (point.DistanceTo(line.StartPoint) < point.DistanceTo(line.EndPoint))
                    return line.StartPoint;
                return line.EndPoint;
            }
            return prjPoint;
        }
        public static List<Point3d> PointOrderByLine(List<Point3d> orderPoints,Point3d lineSp,Vector3d lineDirection) 
        {
            Dictionary<Point3d, double> pointDis = PointOrderDistanceByLine(orderPoints, lineSp, lineDirection);
            return pointDis.OrderBy(c => c.Value).Select(c => c.Key).ToList();
        }
        public static Dictionary<Point3d,double> PointOrderDistanceByLine(List<Point3d> orderPoints, Point3d lineSp, Vector3d lineDirection)
        {
            Dictionary<Point3d, double> pointDis = new Dictionary<Point3d, double>();
            foreach (var item in orderPoints)
            {
                var vector = item - lineSp;
                if (pointDis.Any(c => c.Key.IsEqualTo(item)))
                    continue;
                pointDis.Add(item, vector.DotProduct(lineDirection));
            }
            return pointDis;
        }
    }
}
