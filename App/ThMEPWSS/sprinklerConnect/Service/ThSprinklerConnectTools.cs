using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using NetTopologySuite.Operation.Buffer;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.SprinklerConnect.Service
{
    public static class ThSprinklerConnectTools
    {
        public static Polyline CreateSquare(this Point3d center, double length)
        {
            var pline = new Polyline
            {
                Closed = true
            };
            var pts = new Point3dCollection
            {
                center + length * Vector3d.XAxis + length * Vector3d.YAxis,
                center - length * Vector3d.XAxis + length * Vector3d.YAxis,
                center - length * Vector3d.XAxis - length * Vector3d.YAxis,
                center + length * Vector3d.XAxis - length * Vector3d.YAxis,
            };
            pline.CreatePolyline(pts);
            return pline;
        }

        /// <summary>
        /// 判断线是否与线组相交，若相交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        public static bool IsIntersectsWithPipe(this Line line, List<Line> lineList)
        {
            var lineExtendFrame = line.ExtendLine(1.0).Buffer(10.0);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lineList.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(lineExtendFrame);
            if(filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断线是否和墙线相交，若相交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public static bool IsLineInWall(Point3d first, Point3d second, Point3d third, List<Polyline> geometry)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var pline = new Polyline();
            var pts = new Point3dCollection
            {
                first,
                second,
                third,
            };
            pline.CreatePolyline(pts);
            var filter = spatialIndex.SelectFence(pline);

            if (filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsLineInWall(this Line line, List<Polyline> geometry)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var filter = spatialIndex.SelectFence(line.Buffer(1.0));

            if (filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Vector3d MainDirction(this Polyline convexHull, List<Line> subMainPipe, List<Line> laneLine, double tolerance)
        {
            var temp = convexHull.DPSimplify(10.0);
            Polyline frame;
            if (temp.Area > 1.0)
            {
                frame = temp.Buffer(tolerance).OfType<Polyline>().OrderByDescending(o => o.Area).First();
            }
            else
            {
                var objs = new DBObjectCollection();
                temp.Explode(objs);
                var maxLine = objs.OfType<Line>().OrderByDescending(o => o.Length).First();
                frame = maxLine.ToNTSLineString().Buffer(tolerance, EndCapStyle.Square).ToDbObjects()[0] as Polyline;
            }

            var filter = new List<Line>();
            if (laneLine.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(laneLine.ToCollection());
                filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
                if (filter.Count == 0)
                {
                    frame = frame.Buffer(tolerance).OfType<Polyline>().OrderByDescending(o => o.Area).First();
                    filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
                }
            }
            if (filter.Count == 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(subMainPipe.ToCollection());
                filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
            }
            var trim = new List<Line>();
            for (int j = 0; j < filter.Count; j++)
            {
                var objs = new DBObjectCollection();
                var pline = frame.Trim(filter[j]).OfType<Polyline>().FirstOrDefault();
                // Exception
                if (pline == null)
                {
                    continue;
                }
                pline.Explode(objs);
                trim.Add(objs.OfType<Line>().OrderByDescending(l => l.Length).First());
            }

            if (trim.Count == 0)
            {
                return new Vector3d();
            }
            var orderList = new List<Tuple<double, double,int, Vector3d>>();
            for (int i = 0; i < trim.Count; i++)
            {
                var angle = trim[i].Angle > Math.PI ? trim[i].Angle - Math.PI : trim[i].Angle;
                var length = trim[i].Length;

                int j = 0;
                for (; j < orderList.Count; j++)
                {
                    if (Math.Abs(angle - orderList[j].Item1) < Math.PI / 180.0)
                    {
                        var count = orderList[j].Item3 + 1;
                        var lengthTotal = orderList[j].Item2 + length / count;
                        var tuple = Tuple.Create(orderList[j].Item1, lengthTotal, count, orderList[j].Item4);
                        orderList[j] = tuple;
                        break;
                    }
                }
                if (j == orderList.Count)
                {
                    var dirction = trim[i].LineDirection();
                    var tuple = Tuple.Create(angle, length,1, dirction);
                    orderList.Add(tuple);
                }
            }
            orderList = orderList.OrderByDescending(o => o.Item2).ToList();
            return orderList.First().Item4;
        }

        public static Polyline GetConvexHull(this List<Point3d> pts)
        {
            var convexPl = new Polyline();
            var netI2d = pts.Select(x => x.ToPoint2d()).ToList();

            if (netI2d.Select(o => o.X).Distinct().Count() > 1 && netI2d.Select(o => o.Y).Distinct().Count() > 1)
            {
                var convex = netI2d.GetConvexHull();
                for (int j = 0; j < convex.Count; j++)
                {
                    convexPl.AddVertexAt(convexPl.NumberOfVertices, convex.ElementAt(j), 0, 0, 0);
                }
                convexPl.Closed = true;

                if (convexPl.Area > 1.0)
                {
                    return convexPl;
                }
            }

            var newPts = pts.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            var longLine = new Line(newPts.First(), newPts[newPts.Count - 1]);
            return longLine.Buffer(1.0);
        }

        /// <summary>
        /// 获得垂线
        /// </summary>
        /// <param name="point"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        public static Vector3d GetVerticalDirction(this Vector3d dirction)
        {
            if (dirction.X != 0)
            {
                return new Vector3d(-dirction.Y / dirction.X, 1, 0).GetNormal();
            }
            else
            {
                return new Vector3d(1, -dirction.X / dirction.Y, 0).GetNormal();
            }
        }

        /// <summary>
        /// 判断直线与支干管是否正交，若正交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool IsOrthogonal(this Line line, List<Line> subMainPipe)
        {
            var lineExtend = line.ExtendLine(1.0);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(subMainPipe.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(lineExtend.Buffer(1.0)).OfType<Line>().First();
            return IsOrthogonal(lineExtend, filter);
        }

        private static bool IsOrthogonal(Line first, Line second)
        {
            return Math.Abs(first.LineDirection().DotProduct(second.LineDirection())) < 0.02;
        }

        public static bool ContinueConnect(this Vector3d laneLine, Vector3d dirction)
        {
            if (Math.Abs(dirction.DotProduct(laneLine)) < 0.02)
            {
                return true;
            }
            return false;
        }

        public static Tuple<double, Line> GetCloseLaneLine(this Line line, List<Line> laneLine)
        {
            var newLine = line.ExtendLine(3000.0);
            var closeDistance = laneLine[0].ExtendLine(5000.0).Distance(newLine);
            var closeLine = laneLine[0];
            for (int i = 1; i < laneLine.Count; i++)
            {
                var distance = laneLine[i].ExtendLine(5000.0).Distance(newLine);
                if (closeDistance > distance)
                {
                    closeDistance = distance;
                    closeLine = laneLine[i];
                }
            }
            return new Tuple<double, Line>(closeDistance, closeLine);
        }

        /// <summary>
        /// 对各个支干管，搜索其离起点最近的点位，并返回阈值范围内的点
        /// </summary>
        /// <param name="originalPt"></param>
        /// <param name="virtualPts"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Point3d> SearchVirtualPt(this Point3d originalPt, List<Line> subMainPipe, double tol)
        {
            var virtualPts = new List<Point3d>();
            subMainPipe.ForEach(pipe =>
            {
                var closePt = pipe.GetClosestPointTo(originalPt, false);
                if (Math.Abs((closePt - originalPt).GetNormal().DotProduct(pipe.LineDirection())) > 0.02)
                {
                    return;
                }
                var dist = closePt.DistanceTo(originalPt);
                if (dist < tol)
                {
                    virtualPts.Add(closePt);
                }
            });

            return virtualPts;
        }

        /// <summary>
        /// 在支干管和线的交点中，搜索距起点最近的点
        /// </summary>
        /// <param name="extendLine"></param>
        /// <param name="originalPt"></param>
        /// <param name="firstVirtualPt"></param>
        /// <returns></returns>
        public static bool SearchVirtualPt(this Line extendLine, Point3d originalPt, List<Polyline> geometry, List<Line> subMainPipe, out Point3d firstVirtualPt)
        {
            firstVirtualPt = new Point3d();
            var pts = new List<Point3d>();
            subMainPipe.ForEach(pipe =>
            {
                if (!IsOrthogonal(pipe, extendLine))
                {
                    return;
                }
                var breakPt = new Point3dCollection();
                extendLine.IntersectWith(pipe, Intersect.OnBothOperands, breakPt, (IntPtr)0, (IntPtr)0);
                if (breakPt.Count > 0)
                {
                    pts.AddRange(breakPt.OfType<Point3d>().ToList());
                }
            });

            if (pts.Count > 0)
            {
                firstVirtualPt = pts.OrderBy(pt => pt.DistanceTo(originalPt)).FirstOrDefault();
                var newLine = new Line(originalPt, firstVirtualPt);
                // Exception
                if (newLine.Length < 1.0)
                {
                    return false;
                }
                var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
                var wallFilter = wallIndex.SelectFence(newLine.Buffer(1.0));
                if (wallFilter.Count > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断线上是否存在已检索点或墙线
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="originalPt"></param>
        /// <param name="line"></param>
        /// <param name="firstPt"></param>
        /// <returns></returns>
        public static bool VaildLine(this Line line, List<Point3d> pts, List<Line> pipes, List<Polyline> geometry)
        {
            var pline = line.ExtendLine(-15.0).Buffer(1.0);
            // 检测线上是否存在点
            var dbPoints = pts.Select(o => new DBPoint(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var filter = spatialIndex.SelectCrossingPolygon(pline);
            // 判断是否与管线相交
            var pipeIndex = new ThCADCoreNTSSpatialIndex(pipes.ToCollection());
            var pipeFilter = pipeIndex.SelectCrossingPolygon(pline);
            // 检测是否与墙线相交
            var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var wallFilter = wallIndex.SelectCrossingPolygon(pline);

            if (filter.Count + pipeFilter.Count + wallFilter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断散点是否为噪音
        /// </summary>
        public static void IsNoisePoint(this Point3d point, List<Point3d> sprinklerSearched, List<Point3d> sprinklerSearchedClone,
            List<Point3d> realPts, List<Point3d> everScatter, ref bool hasScatter)
        {
            var scatterList = realPts
                .Where(pt => !sprinklerSearchedClone.Contains(pt) && !sprinklerSearched.Contains(pt)).ToList();
            scatterList.AddRange(everScatter);
            if (!sprinklerSearchedClone.Contains(point))
            {
                var square = CreateSquare(point, 2400.0 * 1.5);
                var scatterCount = 0;
                scatterList.ForEach(pt =>
                {
                    if (square.Contains(pt))
                    {
                        scatterCount++;
                    }
                });
                if (scatterCount > 1)
                {
                    hasScatter = true;
                    everScatter.Add(point);
                }
            }
        }

        /// <summary>
        /// 次方向散点处理
        /// </summary>
        /// <param name="rowConnection"></param>
        /// <param name="secRowConnection"></param>
        public static void HandleSecondRow(List<ThSprinklerRowConnect> connectionTempClone, ThSprinklerRowConnect rowConnect,
            List<Point3d> sprinklerSearchedClone, List<Point3d> realPtsSearchedTemp)
        {
            var lines = connectionTempClone.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var frame = rowConnect.Base.ExtendLine(10.0).Buffer(50.0);
            var filter = spatialIndex.SelectCrossingPolygon(frame);

            filter.OfType<Line>().ForEach(line =>
            {
                var filterRow = connectionTempClone.Where(row => row.StartPoint == line.StartPoint && row.EndPoint == line.EndPoint).First();
                if (filterRow.OrderDict.ContainsKey(-1))
                {
                    var i = 1;
                    for (; i < filterRow.OrderDict.Count - 1; i++)
                    {
                        if (frame.Contains(filterRow.OrderDict[i][0]))
                        {
                            break;
                        }
                    }
                    var k = 1;
                    var orderDict = filterRow.OrderDict.OrderChange(true);
                    for (; k < orderDict.Count - 1; k++)
                    {
                        if (frame.Contains(orderDict[k][0]))
                        {
                            break;
                        }
                    }
                    if (k > i)
                    {
                        i = k;
                        filterRow.OrderDict = orderDict;
                        var ptTemp = filterRow.StartPoint;
                        filterRow.StartPoint = filterRow.EndPoint;
                        filterRow.EndPoint = ptTemp;
                    }
                    if (i == 1)
                    {
                        spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { line });
                        filterRow.OrderDict.Values.ForEach(values => values.ForEach(pt => sprinklerSearchedClone.Remove(pt)));
                        connectionTempClone.Remove(filterRow);
                    }
                    else
                    {
                        for (int j = filterRow.OrderDict.Count - 2; j >= i; j--)
                        {
                            sprinklerSearchedClone.Remove(filterRow.OrderDict[j][0]);
                            filterRow.OrderDict.Remove(j);
                        }
                        filterRow.OrderDict.Remove(-1);
                        filterRow.Count = i - 1;
                        filterRow.EndPoint = filterRow.OrderDict[i - 1][0];
                        spatialIndex.Update(new DBObjectCollection { filterRow.Base }, new DBObjectCollection { line });
                    }
                }
                else
                {
                    var i = 1;
                    for (; i < filterRow.OrderDict.Count; i++)
                    {
                        if (frame.Contains(filterRow.OrderDict[i][0]))
                        {
                            break;
                        }
                    }
                    if (i == 1)
                    {
                        spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { line });
                        filterRow.OrderDict.Values.ForEach(values => values.ForEach(pt => sprinklerSearchedClone.Remove(pt)));
                        connectionTempClone.Remove(filterRow);
                    }
                    else
                    {
                        for (int j = filterRow.OrderDict.Count - 1; j >= i; j--)
                        {
                            sprinklerSearchedClone.Remove(filterRow.OrderDict[j][0]);
                            filterRow.OrderDict.Remove(j);
                        }
                        filterRow.Count = i - 1;
                        filterRow.EndPoint = filterRow.OrderDict[i - 1][0];
                        spatialIndex.Update(new DBObjectCollection { filterRow.Base }, new DBObjectCollection { line });
                    }
                }
            });

            var lastFilter = spatialIndex.SelectCrossingPolygon(frame);
            var goingOn = true;
            while (lastFilter.Count > 0 && goingOn)
            {
                if (rowConnect.OrderDict.ContainsKey(-1))
                {
                    rowConnect.EndPoint = rowConnect.OrderDict[rowConnect.Count][0];
                    rowConnect.OrderDict.Remove(-1);
                }
                else if (rowConnect.Count > 1)
                {
                    realPtsSearchedTemp.Remove(rowConnect.OrderDict[rowConnect.Count][0]);
                    rowConnect.EndPoint = rowConnect.OrderDict[rowConnect.Count - 1][0];
                    rowConnect.OrderDict.Remove(rowConnect.Count);
                    rowConnect.Count--;
                }
                else
                {
                    goingOn = false;
                }
                frame = rowConnect.Base.ExtendLine(10.0).Buffer(1.0);
                lastFilter = spatialIndex.SelectCrossingPolygon(frame);
            }
            if (goingOn)
            {
                connectionTempClone.Add(rowConnect);
                sprinklerSearchedClone.AddRange(realPtsSearchedTemp);
            }
        }

        /// <summary>
        /// 根据新增点，获得最长的线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static Line GetLongLine(this Point3d pt, Line line)
        {
            var closePtOnLine = line.GetClosestPointTo(pt, true);
            var first = new Line(line.StartPoint, closePtOnLine);
            var second = new Line(closePtOnLine, line.EndPoint);
            var list = new List<Line>
            {
                first,
                second,
                line
            };
            return list.OrderByDescending(l => l.Length).First();
        }

        /// <summary>
        /// 判断线与车道线是否正交，正交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        public static bool IsIntersection(this Line line, List<Line> laneLine)
        {
            var closeDistance = laneLine[0].ExtendLine(5000.0).Distance(line);
            var isOrthogonal = IsOrthogonal(line, laneLine[0]);
            for (int i = 1; i < laneLine.Count; i++)
            {
                if (!IsOrthogonal(line, laneLine[i]))
                {
                    continue;
                }
                var distance = laneLine[i].ExtendLine(5000.0).Distance(line);
                isOrthogonal = true;
                if (closeDistance > distance)
                {
                    closeDistance = distance;
                }
            }
            return isOrthogonal && closeDistance < 10.0;
        }

        /// <summary>
        /// 调整点位顺序，使起点离支干管最近
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static Dictionary<int, List<Point3d>> OrderChange(this Dictionary<int, List<Point3d>> dict, bool force = false)
        {
            var startDist = dict[0][0].DistanceTo(dict[1][0]);
            var endDist = dict[dict.Count - 2][0].DistanceTo(dict[-1][0]);
            if (startDist > endDist || force)
            {
                var newDict = new Dictionary<int, List<Point3d>>();
                var order = 0;
                newDict.Add(order++, dict[-1]);
                for (int i = dict.Count - 2; i > 0; i--)
                {
                    newDict.Add(order++, dict[i]);
                }
                newDict.Add(-1, dict[0]);
                return newDict;
            }
            else
            {
                return dict;
            }
        }

        /// <summary>
        /// 判断喷头是否在小房间内，若在则返回true
        /// </summary>
        /// <param name="sprinkler"></param>
        /// <param name="smallRooms"></param>
        /// <returns></returns>
        public static Tuple<bool, Polyline> IsSprinklerInSmallRoom(this Point3d sprinkler, List<Polyline> smallRooms)
        {
            var isSprinklerInSmallRoom = false;
            var smallroom = new Polyline();
            smallRooms.ForEach(r =>
            {
                if (r.Contains(sprinkler))
                {
                    smallroom = r;
                    isSprinklerInSmallRoom = true;
                }
            });
            return Tuple.Create(isSprinklerInSmallRoom, smallroom);
        }

        /// <summary>
        /// 判断小房间内是否存在管线
        /// </summary>
        /// <returns></returns>
        public static bool IsPipeInSmallRoom(this Tuple<bool, Polyline> isSprinklerInSmallRoom, List<Line> closePipeLines)
        {
            if (!isSprinklerInSmallRoom.Item1)
            {
                return false;
            }
            var spatialIndex = new ThCADCoreNTSSpatialIndex(closePipeLines.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(isSprinklerInSmallRoom.Item2);

            if (filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ConnectToPipe(this Line edge, Line targetLine, List<Polyline> geometry, List<Line> allPipe,
            List<ThSprinklerRowConnect> rowConnection, List<Point3d> ptList, List<Point3d> sprinklerSearched)
        {
            // 判断两线是否正交或平行
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.02
                && Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.998)
            {
                return false;
            }

            var canConnect = edge.CanConnect(targetLine, geometry, allPipe);
            if (canConnect)
            {
                return false;
            }

            var rowConn = new ThSprinklerRowConnect();
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.998)
            {
                var startDist = edge.DistanceTo(targetLine.StartPoint, false);
                var endDist = edge.DistanceTo(targetLine.EndPoint, false);
                var centerPoint = GetCenterPoint(edge);
                var actualDistPt = targetLine.GetClosestPointTo(centerPoint, true);
                var actualDist = actualDistPt.DistanceTo(centerPoint);
                if (startDist < endDist)
                {
                    if (startDist - actualDist < 10.0)
                    {
                        rowConn.OrderDict.Add(0, new List<Point3d> { targetLine.StartPoint });
                    }
                    else
                    {
                        rowConn.OrderDict.Add(0, new List<Point3d> { actualDistPt });
                    }
                }
                else
                {
                    if (endDist - actualDist < 10.0)
                    {
                        rowConn.OrderDict.Add(0, new List<Point3d> { targetLine.EndPoint });
                    }
                    else
                    {
                        rowConn.OrderDict.Add(0, new List<Point3d> { actualDistPt });
                    }
                }
            }
            else if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.02)
            {
                var closePt = targetLine.GetClosestPointTo(edge.StartPoint, false);
                rowConn.OrderDict.Add(0, new List<Point3d> { closePt });
            }
            rowConn.OrderDict.Add(-2, ptList);
            rowConn.StartPoint = ptList[0];
            rowConn.EndPoint = ptList[ptList.Count - 1];
            rowConn.Count = ptList.Count;
            sprinklerSearched.AddRange(ptList);
            rowConnection.Add(rowConn);
            return true;
        }

        /// <summary>
        /// 判断两线之间是否完全被墙隔绝，若隔绝则返回true
        /// </summary>
        /// <returns></returns>
        private static bool CanConnect(this Line edge, Line targetLine, List<Polyline> geometry, List<Polyline> obstacle, List<Line> allPipe)
        {
            var canConnect = true;
            if (obstacle.Count == 0)
            {
                return canConnect;
            }

            var linesCollection = new List<Line>();
            var collection = new DBObjectCollection();
            geometry.ForEach(o => collection.Add(o));
            var wallIndex = new ThCADCoreNTSSpatialIndex(collection);

            var obstacleCollection = new DBObjectCollection();
            obstacle.ForEach(o => obstacleCollection.Add(o));
            allPipe.ForEach(o => obstacleCollection.Add(o));
            var obstacleIndex = new ThCADCoreNTSSpatialIndex(obstacleCollection);

            // 垂直情形
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.02)
            {
                var ptOnScrLine = targetLine.GetClosestPointTo(edge.StartPoint, false);
                var closePt = edge.GetClosestPointTo(ptOnScrLine, false);
                var crossLine = new Line(ptOnScrLine, closePt);
                var first = new Line(closePt, edge.StartPoint);
                var second = new Line(closePt, edge.EndPoint);
                linesCollection.Add(crossLine);
                if (first.Length > second.Length && second.Length > 0)
                {
                    linesCollection.Add(second);
                }
                else if (first.Length > 0)
                {
                    linesCollection.Add(first);
                }

                var filterCount = 0;
                for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
                {
                    var wallFilter = wallIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                    filterCount += wallFilter.Count;
                }
                var obstacleCount = 0;
                for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
                {
                    var obstacleFilter = obstacleIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                    obstacleCount += obstacleFilter.Count;
                }

                if (filterCount <= 2 && obstacleCount == 0)
                {
                    canConnect = false;
                }
            }
            else if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.998)
            {
                // 中心线
                var centerPoint = GetCenterPoint(edge);
                var actualDistPt = targetLine.GetClosestPointTo(centerPoint, false);
                var centerLine = new Line(centerPoint, actualDistPt);

                // 起始线
                var startDistPt = targetLine.GetClosestPointTo(edge.StartPoint, false);
                var startLine = new Line(edge.StartPoint, startDistPt);

                // 结尾线
                var endDistPt = targetLine.GetClosestPointTo(edge.EndPoint, false);
                var endLine = new Line(edge.EndPoint, endDistPt);

                linesCollection.Add(centerLine);
                linesCollection.Add(startLine);
                linesCollection.Add(endLine);
                for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
                {
                    var obstacleFilter = obstacleIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                    if (obstacleFilter.Count == 0)
                    {
                        canConnect = false;
                        break;
                    }
                }
            }

            return canConnect;
        }

        /// <summary>
        /// 判断两线之间是否完全被墙隔绝，若隔绝则返回true
        /// </summary>
        /// <returns></returns>
        private static bool CanConnect(this Line edge, Line targetLine, List<Polyline> geometry, List<Line> allPipe)
        {
            var canConnect = true;
            var linesCollection = new List<Line>();

            var collection = new DBObjectCollection();
            geometry.ForEach(o => collection.Add(o));
            allPipe.ForEach(o => collection.Add(o));
            var wallIndex = new ThCADCoreNTSSpatialIndex(collection);

            // 垂直情形
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.02)
            {
                var ptOnScrLine = targetLine.GetClosestPointTo(edge.StartPoint, false);
                var closePt = edge.GetClosestPointTo(ptOnScrLine, false);
                var crossLine = new Line(ptOnScrLine, closePt);
                var first = new Line(closePt, edge.StartPoint);
                var second = new Line(closePt, edge.EndPoint);
                linesCollection.Add(crossLine);
                if (first.Length > second.Length)
                {
                    linesCollection.Add(second);
                }
                else
                {
                    linesCollection.Add(first);
                }

                linesCollection.RemoveAll(line => line.Length == 0);
                var filterCount = 0;
                for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
                {
                    var wallFilter = wallIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                    filterCount += wallFilter.Count;
                }
                if (filterCount == 0)
                {
                    canConnect = false;
                }
            }
            else if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.998)
            {
                // 中心线
                var centerPoint = GetCenterPoint(edge);
                var actualDistPt = targetLine.GetClosestPointTo(centerPoint, false);
                var centerLine = new Line(centerPoint, actualDistPt);

                // 起始线
                var startDistPt = targetLine.GetClosestPointTo(edge.StartPoint, false);
                var startLine = new Line(edge.StartPoint, startDistPt);

                // 结尾线
                var endDistPt = targetLine.GetClosestPointTo(edge.EndPoint, false);
                var endLine = new Line(edge.EndPoint, endDistPt);

                linesCollection.Add(centerLine);
                linesCollection.Add(startLine);
                linesCollection.Add(endLine);
                linesCollection.RemoveAll(line => line.Length == 0);
                for (int lineCount = 0; lineCount < linesCollection.Count; lineCount++)
                {
                    var wallFilter = wallIndex.SelectFence(linesCollection[lineCount].Buffer(1.0));
                    if (wallFilter.Count == 0)
                    {
                        canConnect = false;
                        break;
                    }
                }
            }

            return canConnect;
        }

        /// <summary>
        /// 计算线段中点
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Point3d GetCenterPoint(this Line line)
        {
            return new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);
        }

        public static bool ConnectToRow(this Line edge, bool isSprinklerInSmallRoom, Line targetLine, List<Polyline> geometry,
            List<Polyline> obstacle, List<Line> allPipe, List<ThSprinklerRowConnect> rowConnection, List<Point3d> ptList,
            List<Point3d> sprinklerSearched, int sprinklerTol)
        {
            // 判断两线是否正交或平行
            if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) > 0.02
                && Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.998)
            {
                return false;
            }

            if (isSprinklerInSmallRoom)
            {
                var canConnect = edge.CanConnect(targetLine, geometry, obstacle, allPipe);
                if (canConnect)
                {
                    return false;
                }
            }
            else
            {
                var canConnect = edge.CanConnect(targetLine, geometry, allPipe);
                if (canConnect)
                {
                    return false;
                }
            }

            //var extendPt = edge.GetClosestPointTo(targetLine.EndPoint, true);
            //if (Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.02
            //    && (extendPt.DistanceTo(edge.StartPoint) < 150.0
            //        || extendPt.DistanceTo(edge.EndPoint) < 150.0))
            //{
            //    return false;
            //}

            var rowConn = rowConnection
                .Where(row => row.Base.StartPoint == targetLine.StartPoint
                && row.Base.EndPoint == targetLine.EndPoint).FirstOrDefault();
            if (rowConn == null)
            {
                if (edge.Distance(targetLine) < 10.0 && Math.Abs(edge.LineDirection().DotProduct(targetLine.LineDirection())) < 0.02)
                {
                    var rowConnTemp = new ThSprinklerRowConnect();
                    rowConnTemp.OrderDict.Add(0, new List<Point3d> { targetLine.StartPoint });
                    rowConnTemp.Count++;
                    for (int i = 1; i <= ptList.Count; i++)
                    {
                        rowConnTemp.OrderDict.Add(i, new List<Point3d> { ptList[ptList.Count - i] });
                        rowConnTemp.Count++;
                        sprinklerSearched.Add(ptList[ptList.Count - i]);
                    }
                    rowConnection.Add(rowConnTemp);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (rowConn.Count + sprinklerTol > 8 || rowConn.Count + ptList.Count > 8)
            {
                return false;
            }

            ptList.ChangeListOrder(rowConn.EndPoint);

            for (int num = 2; ; num++)
            {
                if (!rowConn.OrderDict.ContainsKey(-num))
                {
                    rowConn.OrderDict.Add(-num, ptList);
                    rowConn.Count += ptList.Count;
                    rowConn.IsSmallRoom = isSprinklerInSmallRoom;
                    sprinklerSearched.AddRange(ptList);
                    break;
                }
            }
            return true;
        }

        /// <summary>
        /// 搜索沿某一方向的最近点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="originalPt"></param>
        /// <param name="extendLine"></param>
        /// <param name="firstPt"></param>
        /// <returns></returns>
        public static bool SearchPointByDirction(this Line extendLine, List<Point3d> sprinklers, List<Point3d> pts, Point3d originalPt,
            List<Polyline> geometry, out Point3d firstPt)
        {
            firstPt = new Point3d();
            if(extendLine.Length < 10.0)
            {
                return false;
            }

            var pline = extendLine.Buffer(1.0);
            var dbPoints = sprinklers.Select(o => new DBPoint(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var filter = spatialIndex.SelectCrossingPolygon(pline);
            if (filter.Count > 1)
            {
                firstPt = filter.OfType<DBPoint>().Select(pt => pt.Position).OrderBy(pt => pt.DistanceTo(originalPt)).ToList()[1];
                if (pts.Contains(firstPt))
                {
                    var newLine = new Line(originalPt, firstPt);
                    var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
                    var wallFilter = wallIndex.SelectFence(newLine.Buffer(1.0));
                    if (wallFilter.Count > 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获得散点到列的最短距离
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static Tuple<double, Line> GetCloseDistToLines(this Point3d point, List<Line> lines)
        {
            var closeDistance = lines[0].DistanceTo(point, false);
            var closeLine = lines[0];
            for (int i = 1; i < lines.Count; i++)
            {
                var distance = lines[i].DistanceTo(point, false);
                if (distance < closeDistance + 1)
                {
                    closeDistance = distance;
                    closeLine = lines[i];
                }
            }
            return Tuple.Create(closeDistance, closeLine);
        }

        /// <summary>
        /// 计算喷头集的中心
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Point3d GetSprinklersCenter(this List<Point3d> pts)
        {
            var x = pts.Select(pt => pt.X).Sum() / pts.Count;
            var y = pts.Select(pt => pt.Y).Sum() / pts.Count;
            return new Point3d(x, y, 0);
        }

        /// <summary>
        /// 过滤重复点
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<Point3d> DistinctPoints(this List<Point3d> list)
        {
            var kdTree = new ThCADCoreNTSKdTree(10.0);
            list.ForEach(o => kdTree.InsertPoint(o));
            return kdTree.SelectAll().OfType<Point3d>().ToList();
        }

        private static void ChangeListOrder(this List<Point3d> list, Point3d targetPt)
        {
            if (list.Count > 2 && list[0].DistanceTo(targetPt) > list[list.Count - 1].DistanceTo(targetPt))
            {
                list.Reverse();
            }
        }

        public static bool CloseToStall(this Line line, List<Line> laneLine, double dtTol)
        {
            if (line.Length > 10.0)
            {
                var frame = line.Buffer(dtTol);
                var spatialIndex = new ThCADCoreNTSSpatialIndex(laneLine.ToCollection());
                var filter = spatialIndex.SelectCrossingPolygon(frame);
                if (filter.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static double CloseDistToPipe(this Point3d point, List<Line> subMainPipe)
        {
            if(subMainPipe.Count == 0)
            {
                return 10000.0;
            }
            var closeDist = subMainPipe[0].DistanceTo(point, false);
            for(int i =1;i<subMainPipe.Count;i++)
            {
                var distance = subMainPipe[i].DistanceTo(point, false);
                if(distance < closeDist + 1)
                {
                    closeDist = distance;
                }
            }
            return closeDist;
        }
    }
}
