using System;
using System.Collections.Generic;
using System.Linq;
using Linq2Acad;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerConnect.Model;
using NFox.Cad;
using Dreambuild.AutoCAD;
using DotNetARX;

namespace ThMEPWSS.SprinklerConnect.Engine
{
    public class ThSprinklerConnectEngine
    {
        private static double DTTol { get; set; }
        private static ThSprinklerParameter SprinklerParameter { get; set; }
        private static List<Point3d> SprinklerSearched { get; set; } = new List<Point3d>();


        public ThSprinklerConnectEngine(ThSprinklerParameter sprinklerParameter)
        {
            SprinklerParameter = sprinklerParameter;
        }

        public void SprinklerConnectEngine(List<Polyline> doubleStall, List<Polyline> geometry)
        {
            var netList = ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(SprinklerParameter, geometry, out double dtTol);
            DTTol = dtTol;
            var laneLine = LaneLine(doubleStall);

            using (var acadDatabase = AcadDatabase.Active())
            {
                var rowSeparation = new List<List<List<Point3d>>>();
                var rowConnection = new List<ThSprinklerRowConnect>();
                SprinklerSearched = new List<Point3d>();
                // < netList.Count
                for (int i = 0; i < netList.Count; i++)
                {
                    // < netList[i].ptsGraph.Count
                    for (int j = 0; j < netList[i].ptsGraph.Count; j++)
                    {
                        rowConnection.AddRange(GraphPtsConnect(netList[i], j, laneLine));
                    }
                }

                // 散点处理
                HandleScatter(rowConnection);
                // 列分割
                rowSeparation.AddRange(RowSeparation(rowConnection, laneLine));

                rowSeparation.ForEach(row =>
                {
                    var lines = new DBObjectCollection();
                    for (int i = 1; i < row.Count; i++)
                    {
                        lines.Add(new Line(row[i - 1][0], row[i][0]));
                    }
                    for (int i = 1; i < row.Count; i++)
                    {
                        for (int j = 1; j < row[i].Count; j++)
                        {
                            var closePt = (lines[0] as Line).GetClosestPointTo(row[i][j], true);
                            lines.Add(new Line(closePt, row[i][0]));
                            lines.Add(new Line(closePt, row[i][j]));
                        }
                    }
                    lines.OfType<Line>().ForEach(l => acadDatabase.ModelSpace.Add(l));
                });
            }
        }

        private static List<List<List<Point3d>>> RowSeparation(List<ThSprinklerRowConnect> connection, List<Line> laneLine)
        {
            var results = new List<List<List<Point3d>>>();
            connection.ForEach(o =>
            {
                var row = new List<List<Point3d>>();
                if (!o.OrderDict.ContainsKey(-1))
                {
                    var num = 0;
                    for (int i = 0; i < o.OrderDict.Count; i++)
                    {
                        var ptList = new List<Point3d>();
                        for (int n = 0; n < o.OrderDict[i].Count; n++)
                        {
                            ptList.Add(o.OrderDict[i][n]);
                            num++;
                            if (num >= 9)
                            {
                                break;
                            }
                        }
                        row.Add(ptList);
                        if (num >= 9)
                        {
                            break;
                        }
                    }
                    results.Add(row);
                }
                else
                {
                    var index = 1;
                    if (o.Count < 7)
                    {
                        var first = new List<List<Point3d>>();
                        for (int j = 0; j < o.OrderDict.Count - 1; j++)
                        {
                            first.Add(o.OrderDict[j]);
                        }
                        results.Add(first);
                    }
                    else
                    {
                        var num = 0;
                        for (int i = 1; i < o.OrderDict.Count - 1; i++)
                        {
                            num += o.OrderDict[i].Count;
                            if (num > o.Count / 2)
                            {
                                index = i;
                                break;
                            }
                        }
                        if (o.IsStallArea)
                        {
                            var newNum = 0;
                            var minDelta = o.Count;
                            for (int i = 1; i < o.OrderDict.Count - 1; i++)
                            {
                                var edge = new Line(o.OrderDict[i - 1][0], o.OrderDict[i][0]);
                                if (IsIntersection(edge, laneLine))
                                {
                                    var delta = Math.Abs(o.Count - 2 * newNum);
                                    if (minDelta > delta)
                                    {
                                        index = i;
                                        minDelta = delta;
                                    }
                                }
                                newNum += o.OrderDict[i].Count;
                            }
                        }

                        var first = new List<List<Point3d>>
                        {
                            o.OrderDict[0],
                        };
                        var firstNum = 0;
                        var second = new List<List<Point3d>>
                        {
                            o.OrderDict[-1],
                        };
                        var secondNum = 0;

                        for (int j = 1; j < index; j++)
                        {
                            var ptList = new List<Point3d>();
                            for (int n = 0; n < o.OrderDict[j].Count; n++)
                            {
                                ptList.Add(o.OrderDict[j][n]);
                                firstNum++;
                                if (firstNum >= 9)
                                {
                                    break;
                                }
                            }
                            first.Add(o.OrderDict[j]);
                            if (firstNum >= 9)
                            {
                                break;
                            }
                        }
                        for (int j = o.OrderDict.Count - 2; j >= index; j--)
                        {
                            var ptList = new List<Point3d>();
                            for (int n = 0; n < o.OrderDict[j].Count; n++)
                            {
                                ptList.Add(o.OrderDict[j][n]);
                                secondNum++;
                                if (secondNum > 9)
                                {
                                    break;
                                }
                            }
                            second.Add(o.OrderDict[j]);
                            if (secondNum > 9)
                            {
                                break;
                            }
                        }

                        if (first.Count > 1)
                        {
                            results.Add(first);
                        }
                        if (second.Count > 1)
                        {
                            results.Add(second);
                        }
                    }
                }
            });

            return results;
        }

        private static List<ThSprinklerRowConnect> GraphPtsConnect(ThSprinklerNetGroup net, int graphIdx, List<Line> laneLine)
        {
            // 给定索引所对应的图
            var graph = net.ptsGraph[graphIdx];
            // 虚拟点
            var virtualPts = net.GetVirtualPts(graphIdx).OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            // 虚拟点在pts中的索引集
            var virtualPtsIndex = net.GetVirtualPtsIndex(graphIdx);
            // 图中的所有点位，包括虚拟点
            var graphPts = net.GetGraphPts(graphIdx);
            // 图中的所有喷淋点位
            var realPts = graphPts.Where(pt => !virtualPts.Contains(pt)).ToList();
            // 已检索的喷淋点位
            var realPtsSearched = new List<Point3d>();
            // 已检索的虚拟点位
            var virtualPtsSearched = new List<int>();
            // 返回图中的连接关系
            var connection = new List<ThSprinklerRowConnect>();

            // 计算主方向
            if (graphPts.Count < 3)
            {
                return connection;
            }
            var mainDirction = MainDirction(GetConvexHull(graphPts), laneLine);

            // 沿主方向检索
            for (int i = 0; i < virtualPtsIndex.Count; i++)
            {
                // 找出图中的虚拟点对应的节点索引
                var idx = graph.SearchNodeIndex(virtualPtsIndex[i]);
                if (idx != -1)
                {
                    // 虚拟点对应的节点
                    var virtualNode = graph.SprinklerVertexNodeList[idx];
                    var edgeNode = virtualNode.FirstEdge;
                    while (edgeNode != null)
                    {
                        if (SprinklerSearched.Contains(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]))
                        {
                            edgeNode = edgeNode.Next;
                            continue;
                        }

                        // 图中虚拟点所在的线段
                        var edge = new Line(net.pts[virtualNode.NodeIndex],
                                            net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                        if (!IsOrthogonal(edge))
                        {
                            edgeNode = edgeNode.Next;
                            continue;
                        }

                        var dirction = edge.Delta.GetNormal();
                        // 判断是否需要往该方向延伸
                        if (ContinueConnect(mainDirction, dirction))
                        {
                            // 单次循环内已检索的喷淋点位
                            var realPtsSearchedTemp = new List<Point3d>();

                            // 点位顺序
                            realPtsSearchedTemp.Add(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                            // 记录点位及其对应的顺序
                            var order = 0;
                            var virtualPt = net.pts[virtualNode.NodeIndex];
                            var rowConnect = new ThSprinklerRowConnect();
                            rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                            rowConnect.OrderDict.Add(order++, new List<Point3d> { net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex] });
                            rowConnect.Count++;
                            rowConnect.StartPoint = net.pts[virtualNode.NodeIndex];
                            if (GetCloseLaneLine(edge, laneLine).Item1 < 5000.0)
                            {
                                rowConnect.IsStallArea = true;
                            }
                            else
                            {
                                rowConnect.IsStallArea = false;
                            }

                            var edgeIndex = edgeNode.EdgeIndex;
                            while (KeepSearching(graph, net, edgeIndex, out var newIdx, realPtsSearchedTemp,
                                dirction, virtualPts, virtualPt, rowConnect, order))
                            {
                                order++;
                                edgeIndex = newIdx;
                            }

                            if (order > 2)
                            {
                                virtualPtsSearched.Add(i);
                                SprinklerSearched.AddRange(realPtsSearchedTemp);
                                connection.Add(rowConnect);
                            }
                        }

                        edgeNode = edgeNode.Next;
                    }
                }
            }

            // 对剩余点位进行检索
            for (int i = 0; i < realPts.Count; i++)
            {
                if (SprinklerSearched.Contains(realPts[i]))
                {
                    continue;
                }

                // 找出距离点位较近的支干管虚拟点
                var virtualPtList = SearchVirtualPt(realPts[i], 2.0 * DTTol);
                if (virtualPtList.Count == 0)
                {
                    continue;
                }

                virtualPtList.ForEach(virtualPt =>
                {
                    var edge = new Line(virtualPt, realPts[i]);
                    if (PointOnLine(SprinklerSearched, edge))
                    {
                        return;
                    }

                    var dirction = edge.Delta.GetNormal();
                    if (ContinueConnect(mainDirction, dirction))
                    {
                        // 单次循环内已检索的喷淋点位
                        var realPtsSearchedTemp = new List<Point3d>();
                        // 点位顺序
                        realPtsSearchedTemp.Add(realPts[i]);
                        // 记录点位及其对应的顺序
                        var order = 0;
                        var rowConnect = new ThSprinklerRowConnect();
                        rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                        rowConnect.OrderDict.Add(order++, new List<Point3d> { realPts[i] });
                        rowConnect.StartPoint = virtualPt;
                        rowConnect.Count++;
                        if (GetCloseLaneLine(edge, laneLine).Item1 < 5000.0)
                        {
                            rowConnect.IsStallArea = true;
                        }
                        else
                        {
                            rowConnect.IsStallArea = false;
                        }

                        var edgeIndex = graph.SearchNodeIndex(net.pts.IndexOf(realPts[i]));
                        while (KeepSearching(graph, net, edgeIndex, out var newIdx, realPtsSearchedTemp,
                                dirction, virtualPts, virtualPt, rowConnect, order))
                        {
                            order++;
                            edgeIndex = newIdx;
                        }

                        if (order > 2)
                        {
                            virtualPtsSearched.Add(i);
                            SprinklerSearched.AddRange(realPtsSearchedTemp);
                            connection.Add(rowConnect);
                        }
                    }
                });
            }

            // 沿次方向检索
            for (int i = 0; i < virtualPtsIndex.Count; i++)
            {
                if (virtualPtsSearched.Contains(i))
                {
                    continue;
                }

                // 找出图中的虚拟点对应的节点索引
                var idx = graph.SearchNodeIndex(virtualPtsIndex[i]);
                if (idx != -1)
                {
                    // 虚拟点对应的节点
                    var virtualNode = graph.SprinklerVertexNodeList[idx];

                    var edgeNode = virtualNode.FirstEdge;
                    while (edgeNode != null)
                    {
                        if (SprinklerSearched.Contains(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]))
                        {
                            edgeNode = edgeNode.Next;
                            continue;
                        }

                        // 图中虚拟点所在的线段
                        var edge = new Line(net.pts[virtualNode.NodeIndex],
                                            net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                        if (!IsOrthogonal(edge))
                        {
                            edgeNode = edgeNode.Next;
                            continue;
                        }

                        var dirction = edge.Delta.GetNormal();

                        // 单次循环内已检索的喷淋点位
                        var realPtsSearchedTemp = new List<Point3d>();
                        // 点位顺序
                        realPtsSearchedTemp.Add(net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex]);
                        // 记录点位及其对应的顺序
                        var order = 0;
                        var virtualPt = net.pts[virtualNode.NodeIndex];
                        var rowConnect = new ThSprinklerRowConnect();
                        rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                        rowConnect.OrderDict.Add(order++, new List<Point3d> { net.pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex] });
                        rowConnect.Count++;
                        rowConnect.StartPoint = net.pts[virtualNode.NodeIndex];
                        if (GetCloseLaneLine(edge, laneLine).Item1 < 5000.0)
                        {
                            rowConnect.IsStallArea = true;
                        }
                        else
                        {
                            rowConnect.IsStallArea = false;
                        }

                        var edgeIndex = edgeNode.EdgeIndex;
                        while (KeepSearching(graph, net, edgeIndex, out var newIdx, realPtsSearchedTemp,
                            dirction, virtualPts, virtualPt, rowConnect, order))
                        {
                            order++;
                            edgeIndex = newIdx;
                        }

                        if (order > 2)
                        {
                            virtualPtsSearched.Add(i);
                            SprinklerSearched.AddRange(realPtsSearchedTemp);
                            connection.Add(rowConnect);
                        }

                        edgeNode = edgeNode.Next;
                    }
                }
            }

            return connection;
        }

        private static void HandleScatter(List<ThSprinklerRowConnect> rowConnection)
        {
            var objs = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var ptList = SprinklerParameter.SprinklerPt.OrderBy(pt => pt.X).ToList();
            var temp = SprinklerSearched.OrderBy(pt => pt.X).ToList();
            for (int i = 0; i < ptList.Count; i++)
            {
                if(i == 243)
                {
                    var tag = true;
                }
                if (SprinklerSearched.Contains(ptList[i]))
                {
                    continue;
                }

                if (rowConnection.Count > 0)
                {
                    var filter = spatialIndex.SelectCrossingPolygon(CreateSquare(ptList[i], 2 * DTTol)).OfType<Line>().ToList();
                    if(filter.Count ==0)
                    {
                        continue;
                    }

                    var closeDistToRow = 2 * filter[0].DistanceTo(ptList[i], true) + filter[0].DistanceTo(ptList[i], false);
                    var closeIndex = -1;
                    var startPoints = filter.Select(l => l.StartPoint).ToList();
                    var endPoints = filter.Select(l => l.EndPoint).ToList();
                    for (int n = 0; n < rowConnection.Count; n++)
                    {
                        if(!startPoints.Contains(rowConnection[n].Base.StartPoint) || !endPoints.Contains(rowConnection[n].Base.EndPoint))
                        {
                            continue;
                        }

                        var realClosePt = rowConnection[n].Base.GetClosestPointTo(ptList[i], false);
                        var realLine = new Line(realClosePt, ptList[i]);
                        if(IsIntersects(realLine, SprinklerParameter.SubMainPipe))
                        {
                            continue;
                        }

                        var distance = 2 * rowConnection[n].Base.DistanceTo(ptList[i], true) + rowConnection[n].Base.DistanceTo(ptList[i], false);
                        if (closeDistToRow + 1.0 > distance)
                        {
                            closeDistToRow = distance;
                            closeIndex = n;
                        }
                    }

                    if(closeIndex == -1)
                    {
                        continue;
                    }

                    if (closeDistToRow < 1.8 * DTTol)
                    {
                        var rowPts = rowConnection[closeIndex].OrderDict;
                        if (rowPts.TryGetValue(1, out var first))
                        {
                            var closeDistToPoint = ptList[i].DistanceTo(first[0]);
                            var ptIndex = 1;
                            for (int m = 2; m < rowPts.Count - 1; m++)
                            {
                                var distance = ptList[i].DistanceTo(rowPts[m][0]);
                                if (closeDistToPoint > distance)
                                {
                                    closeDistToPoint = distance;
                                    ptIndex = m;
                                }
                            }
                            if (rowPts.TryGetValue(rowPts.Count - 1, out var end))
                            {
                                var distance = ptList[i].DistanceTo(end[0]);
                                if (closeDistToPoint > distance)
                                {
                                    closeDistToPoint = distance;
                                    ptIndex = rowPts.Count - 1;
                                }
                            }

                            if (closeDistToPoint < 2.0 * DTTol)
                            {
                                rowPts[ptIndex].Add(ptList[i]);
                                rowConnection[closeIndex].Count++;
                                SprinklerSearched.Add(ptList[i]);
                            }
                        }
                    }
                }
            }
        }

        private static List<Line> LaneLine(List<Polyline> doubleStall)
        {
            var laneLine = new List<Line>();
            doubleStall.ForEach(o =>
            {
                var pts = o.Vertices();
                laneLine.Add(new Line(pts[0], pts[1]));
                laneLine.Add(new Line(pts[2], pts[3]));
            });
            return laneLine;
        }

        private static bool ContinueConnect(Vector3d laneLine, Vector3d dirction)
        {
            if (Math.Abs(dirction.DotProduct(laneLine)) < 0.005)
            {
                return true;
            }
            return false;
        }

        private static Tuple<double, Line> GetCloseLaneLine(Line line, List<Line> laneLine)
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
        /// 判断线与车道线是否正交，正交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        private static bool IsIntersection(Line line, List<Line> laneLine)
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

        //private static Vector3d GetVerticalDirction(Point3d point, List<Line> laneLine)
        //{
        //    var closeDirction = GetCloseDirction(point, laneLine);
        //    if(closeDirction.X != 0)
        //    {
        //        return new Vector3d(-closeDirction.Y / closeDirction.X, 1, 0).GetNormal();
        //    }
        //    else
        //    {
        //        return new Vector3d(1, -closeDirction.X / closeDirction.Y, 0).GetNormal();
        //    }
        //}

        private static bool KeepSearching(ThSprinklerGraph graph, ThSprinklerNetGroup net, int edgeIndex, out int newIdx,
            List<Point3d> realPtsSearchedTemp, Vector3d dirction, List<Point3d> virtualPts,
            Point3d virtualPt, ThSprinklerRowConnect rowConnect, int order)
        {
            if (edgeIndex == -1)
            {
                newIdx = -1;
                return false;
            }

            // 返回是否退出当前索引的循环
            // 搜索下一个点位
            var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
            var originalPt = net.pts[vertexNode.NodeIndex];
            var ptNext = net.pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edgeNext = new Line(originalPt, ptNext);
            var node = vertexNode.FirstEdge;
            newIdx = vertexNode.FirstEdge.EdgeIndex;
            // 如果点位已被检索，或之间连线角度偏大，或节点为虚拟点，则进入循环
            while (SprinklerSearched.Contains(ptNext)
                || realPtsSearchedTemp.Contains(ptNext)
                || virtualPt.IsEqualTo(ptNext)
                || edgeNext.Delta.GetNormal().DotProduct(dirction) < 0.995)
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edgeNext = new Line(originalPt, ptNext);
                    newIdx = node.EdgeIndex;
                }
                else
                {
                    // 继续沿该方向进行搜索
                    var extendLine = new Line(originalPt - dirction, originalPt + 2.5 * dirction * DTTol);
                    var ptSearched = SearchPointByDirction(net.pts, originalPt, extendLine, out var firstPt);
                    var virtualPtSearched = SearchVirtualPt(extendLine, originalPt, out var firstVirtualPt);
                    if (ptSearched && !virtualPtSearched)
                    {
                        ptNext = firstPt;
                        newIdx = graph.SearchNodeIndex(net.pts.IndexOf(ptNext));
                        if (newIdx == -1
                            || SprinklerSearched.Contains(ptNext)
                            || realPtsSearchedTemp.Contains(ptNext))
                        {
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (!ptSearched && virtualPtSearched)
                    {
                        virtualPts.Add(firstVirtualPt);
                        ptNext = firstVirtualPt;
                        break;
                    }
                    else if (ptSearched && virtualPtSearched)
                    {
                        if (firstPt.DistanceTo(originalPt) < firstVirtualPt.DistanceTo(originalPt))
                        {
                            ptNext = firstPt;
                            newIdx = graph.SearchNodeIndex(net.pts.IndexOf(ptNext));
                            if (SprinklerSearched.Contains(ptNext) || realPtsSearchedTemp.Contains(ptNext))
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            virtualPts.Add(firstVirtualPt);
                            ptNext = firstVirtualPt;
                            break;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (virtualPts.Contains(ptNext))
            {
                rowConnect.OrderDict.Add(-1, new List<Point3d> { ptNext });
                rowConnect.OrderDict = OrderChange(rowConnect.OrderDict);
                rowConnect.EndPoint = ptNext;
                return false;
            }
            else
            {
                rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                rowConnect.EndPoint = ptNext;
                rowConnect.Count++;
                realPtsSearchedTemp.Add(ptNext);
                return true;
            }
        }

        /// <summary>
        /// 搜索沿某一方向的最近点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="originalPt"></param>
        /// <param name="extendLine"></param>
        /// <param name="firstPt"></param>
        /// <returns></returns>
        private static bool SearchPointByDirction(List<Point3d> pts, Point3d originalPt, Line extendLine, out Point3d firstPt)
        {
            firstPt = new Point3d();
            var pline = extendLine.Buffer(1.0);
            var dbPoints = pts.Select(o => new DBPoint(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var filter = spatialIndex.SelectCrossingPolygon(pline);

            if (filter.Count > 1)
            {
                firstPt = filter.OfType<DBPoint>().Select(pt => pt.Position).OrderBy(pt => pt.DistanceTo(originalPt)).ToList()[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 在支干管和线的交点中，搜索距起点最近的点
        /// </summary>
        /// <param name="extendLine"></param>
        /// <param name="originalPt"></param>
        /// <param name="firstVirtualPt"></param>
        /// <returns></returns>
        private static bool SearchVirtualPt(Line extendLine, Point3d originalPt, out Point3d firstVirtualPt)
        {
            firstVirtualPt = new Point3d();
            var pts = new List<Point3d>();
            SprinklerParameter.SubMainPipe.ForEach(pipe =>
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
                return true;
            }
            return false;
        }

        /// <summary>
        /// 对各个支干管，搜索其离起点最近的点位，并返回阈值范围内的点
        /// </summary>
        /// <param name="originalPt"></param>
        /// <param name="virtualPts"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static List<Point3d> SearchVirtualPt(Point3d originalPt, double tol)
        {
            var virtualPts = new List<Point3d>();
            SprinklerParameter.SubMainPipe.ForEach(pipe =>
            {
                var closePt = pipe.GetClosestPointTo(originalPt, false);
                if (Math.Abs((closePt - originalPt).GetNormal().DotProduct(pipe.Delta.GetNormal())) > 0.005)
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
        /// 判断线上是否存在已检索点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="originalPt"></param>
        /// <param name="line"></param>
        /// <param name="firstPt"></param>
        /// <returns></returns>
        private static bool PointOnLine(List<Point3d> pts, Line line)
        {
            var pline = line.Buffer(1.0);
            var dbPoints = pts.Select(o => new DBPoint(o)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var filter = spatialIndex.SelectCrossingPolygon(pline);

            if (filter.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 调整点位顺序，使起点离支干管最近
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private static Dictionary<int, List<Point3d>> OrderChange(Dictionary<int, List<Point3d>> dict)
        {
            var startDist = dict[0][0].DistanceTo(dict[1][0]);
            var endDist = dict[dict.Count - 2][0].DistanceTo(dict[-1][0]);
            if (startDist > endDist)
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
        /// 判断直线与支干管是否正交，若正交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static bool IsOrthogonal(Line line)
        {
            var lineExtend = line.ExtendLine(1.0);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(SprinklerParameter.SubMainPipe.ToCollection());
            var filter = spatialIndex.SelectCrossingPolygon(lineExtend.Buffer(1.0)).OfType<Line>().First();
            return IsOrthogonal(lineExtend, filter);
        }

        private static bool IsOrthogonal(Line first, Line second)
        {
            return Math.Abs(first.Delta.GetNormal().DotProduct(second.Delta.GetNormal())) < 0.005;
        }

        private static Polyline GetConvexHull(List<Point3d> pts)
        {
            var convexPl = new Polyline();
            var netI2d = pts.Select(x => x.ToPoint2d()).ToList();

            var convex = netI2d.GetConvexHull();

            for (int j = 0; j < convex.Count; j++)
            {
                convexPl.AddVertexAt(convexPl.NumberOfVertices, convex.ElementAt(j), 0, 0, 0);
            }
            convexPl.Closed = true;

            if (convexPl.Area < 1.0)
            {
                var newPts = pts.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
                var maxLine = new Line(pts.First(), pts[pts.Count - 1]);
                return maxLine.Buffer(1.0);
            }

            return convexPl;
        }

        private static Vector3d MainDirction(Polyline convexHull, List<Line> laneLine)
        {
            var lines = new DBObjectCollection();
            laneLine.ForEach(l => lines.Add(l));
            SprinklerParameter.SubMainPipe.ForEach(l => lines.Add(l));

            var frame = convexHull.Buffer(DTTol).OfType<Polyline>().OrderByDescending(o => o.Area).First();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();

            var trim = new List<Line>();
            for (int j = 0; j < filter.Count; j++)
            {
                var objs = new DBObjectCollection();
                var pline = frame.Trim(filter[j]).OfType<Polyline>().First();
                pline.Explode(objs);
                trim.Add(objs.OfType<Line>().OrderByDescending(l => l.Length).First());
            }

            if (trim.Count == 0)
            {
                return new Vector3d();
            }
            var orderList = new List<Tuple<double, double, Vector3d>>();
            for (int i = 0; i < trim.Count; i++)
            {
                var angle = trim[i].Angle > Math.PI ? trim[i].Angle - Math.PI : trim[i].Angle;
                var length = trim[i].Length;

                int j = 0;
                for (; j < orderList.Count; j++)
                {
                    if (Math.Abs(angle - orderList[j].Item1) < 1.0 * 180.0 * Math.PI)
                    {
                        var lengthTotal = orderList[j].Item2 + length;
                        var tuple = new Tuple<double, double, Vector3d>(orderList[j].Item1, lengthTotal, orderList[j].Item3);
                        orderList[j] = tuple;
                        break;
                    }
                }
                if (j == orderList.Count)
                {
                    var dirction = trim[i].Delta.GetNormal();
                    var tuple = new Tuple<double, double, Vector3d>(angle, length, dirction);
                    orderList.Add(tuple);
                }
            }
            orderList = orderList.OrderByDescending(o => o.Item2).ToList();
            return orderList.First().Item3;
        }

        /// <summary>
        /// 判断线是否与线组相交，若相交则返回true
        /// </summary>
        /// <param name="line"></param>
        /// <param name="laneLine"></param>
        /// <returns></returns>
        private static bool IsIntersects(Line line, List<Line> lineList)
        {
            var lineExtend = line.ExtendLine(1.0);
            for (int i = 0; i < lineList.Count; i++)
            {
                if (lineList[i].LineIsIntersection(lineExtend))
                {
                    return true;
                }
            }
            return false;
        }

        private static Polyline CreateSquare(Point3d center, double length)
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
    }
}
