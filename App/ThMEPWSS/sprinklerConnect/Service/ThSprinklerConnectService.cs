using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.SprinklerConnect.Service
{
    public class ThSprinklerConnectService
    {
        public double DTTol { get; set; }
        public ThSprinklerParameter SprinklerParameter { get; set; }
        public List<Point3d> SprinklerSearched { get; set; } = new List<Point3d>();
        //private static List<Point3d> PipeScatters { get; set; } = new List<Point3d>();
        public List<Line> LaneLine { get; set; } = new List<Line>();
        public List<Polyline> Geometry { get; set; } = new List<Polyline>();

        public ThSprinklerConnectService(ThSprinklerParameter sprinklerParameter, List<Polyline> geometry, double dtTol)
        {
            SprinklerParameter = sprinklerParameter;
            Geometry = geometry;
            DTTol = dtTol;
        }

        public List<ThSprinklerRowConnect> GraphPtsConnect(ThSprinklerNetGroup net, int graphIdx, List<Point3d> pipeScatters, bool isVertical = true)
        {
            // 给定索引所对应的图
            var graph = net.PtsGraph[graphIdx];
            // 虚拟点
            var virtualPts = net.GetVirtualPts(graphIdx).OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            // 虚拟点在pts中的索引集
            var virtualPtsIndex = net.GetVirtualPtsIndex(graphIdx);
            // 图中的所有点位，包括虚拟点
            var graphPts = net.GetGraphPts(graphIdx);
            // 图中的所有喷淋点位
            var realPts = graphPts.Where(pt => !virtualPts.Contains(pt)).ToList();

            // 返回图中的连接关系
            var connection = new List<Tuple<List<ThSprinklerRowConnect>, List<Point3d>, List<Point3d>>>();

            // 计算主方向（与支管垂直的方向）
            if (graphPts.Count < 3)
            {
                return new List<ThSprinklerRowConnect>();
            }
            Vector3d mainDirction;
            if (LaneLine.Count > 0)
            {
                mainDirction = graphPts.GetConvexHull().MainDirction(SprinklerParameter.SubMainPipe, LaneLine, 2 * DTTol);
                if (!isVertical)
                {
                    mainDirction = mainDirction.GetVerticalDirction();
                }
            }
            else
            {
                mainDirction = graphPts.GetConvexHull().MainDirction(SprinklerParameter.SubMainPipe, LaneLine, 10.0);
            }

            for (int time = 0; time < 2; time++)
            {
                var sprinklerSearched = new List<Point3d>();
                var connectionTemp = new List<ThSprinklerRowConnect>();
                var pipeScattersTemp = new List<Point3d>();
                var overCount = false;

                // 已检索的虚拟点位
                var virtualPtsSearched = new List<int>();
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
                            var firstPt = net.Pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex];
                            if (SprinklerSearched.Contains(firstPt)
                                || sprinklerSearched.Contains(firstPt)
                                || pipeScatters.Contains(firstPt)
                                || pipeScattersTemp.Contains(firstPt))
                            {
                                edgeNode = edgeNode.Next;
                                continue;
                            }

                            // 图中虚拟点所在的线段
                            var edge = new Line(net.Pts[virtualNode.NodeIndex], firstPt);
                            if (!edge.IsOrthogonal(SprinklerParameter.SubMainPipe))
                            {
                                edgeNode = edgeNode.Next;
                                continue;
                            }

                            var dirction = edge.LineDirection();
                            // 判断是否需要往该方向延伸
                            if (mainDirction.ContinueConnect(dirction))
                            {
                                // 单次循环内已检索的喷淋点位
                                var realPtsSearchedTemp = new List<Point3d>();

                                // 点位顺序
                                realPtsSearchedTemp.Add(firstPt);
                                // 记录点位及其对应的顺序
                                var order = 0;
                                var virtualPt = net.Pts[virtualNode.NodeIndex];
                                var rowConnect = new ThSprinklerRowConnect();
                                rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                rowConnect.OrderDict.Add(order++, new List<Point3d> { firstPt });
                                rowConnect.Count++;
                                rowConnect.StartPoint = virtualPt;
                                rowConnect.EndPoint = firstPt;
                                if (LaneLine.Count > 0 && edge.GetCloseLaneLine(LaneLine).Item1 < 5000.0)
                                {
                                    rowConnect.IsStallArea = true;
                                }
                                else
                                {
                                    rowConnect.IsStallArea = false;
                                }

                                var edgeIndex = edgeNode.EdgeIndex;
                                while (KeepSearching1(graph, net, edgeIndex, SprinklerParameter.SprinklerPt, realPts, out var newIdx,
                                    realPtsSearchedTemp, sprinklerSearched, dirction, virtualPts, virtualPt, rowConnect, order))
                                {
                                    order++;
                                    edgeIndex = newIdx;
                                }

                                if ((!rowConnect.OrderDict.ContainsKey(-1) && rowConnect.Count > 8)
                                    || rowConnect.Count > 16)
                                {
                                    for (int m = 9; m <= rowConnect.Count; m++)
                                    {
                                        realPtsSearchedTemp.Remove(rowConnect.OrderDict[m][0]);
                                        rowConnect.OrderDict.Remove(m);
                                    }
                                    rowConnect.Count = 8;
                                    rowConnect.EndPoint = rowConnect.OrderDict[8][0];
                                    overCount = true;
                                    if (rowConnect.OrderDict.ContainsKey(-1))
                                    {
                                        rowConnect.OrderDict.Remove(-1);
                                    }
                                }

                                virtualPtsSearched.Add(i);
                                if (rowConnect.Count == 1)
                                {
                                    pipeScattersTemp.AddRange(realPtsSearchedTemp);
                                }
                                else
                                {
                                    sprinklerSearched.AddRange(realPtsSearchedTemp);
                                }
                                connectionTemp.Add(rowConnect);
                            }

                            edgeNode = edgeNode.Next;
                        }
                    }
                }

                if ((time == 0 || connectionTemp.Count == 0) && realPts.Count > sprinklerSearched.Count + pipeScattersTemp.Count)
                {
                    // 对剩余点位进行检索
                    for (int i = 0; i < realPts.Count; i++)
                    {
                        if (SprinklerSearched.Contains(realPts[i]) || sprinklerSearched.Contains(realPts[i])
                            || pipeScatters.Contains(realPts[i]) || pipeScattersTemp.Contains(realPts[i]))
                        {
                            continue;
                        }

                        // 找出距离点位较近的支干管虚拟点
                        var virtualPtList = realPts[i].SearchVirtualPt(SprinklerParameter.SubMainPipe, 2.0 * DTTol);
                        if (virtualPtList.Count == 0)
                        {
                            continue;
                        }

                        virtualPtList.ForEach(virtualPt =>
                        {
                            var edge = new Line(virtualPt, realPts[i]);
                            if (edge.VaildLine(SprinklerParameter.SprinklerPt, SprinklerParameter.AllPipe, Geometry))
                            {
                                return;
                            }

                            var dirction = edge.LineDirection();
                            if (mainDirction.ContinueConnect(dirction))
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
                                rowConnect.EndPoint = realPts[i];
                                rowConnect.Count++;
                                if (LaneLine.Count > 0 && edge.GetCloseLaneLine(LaneLine).Item1 < 5000.0)
                                {
                                    rowConnect.IsStallArea = true;
                                }
                                else
                                {
                                    rowConnect.IsStallArea = false;
                                }

                                var edgeIndex = graph.SearchNodeIndex(net.Pts.IndexOf(realPts[i]));
                                while (KeepSearching1(graph, net, edgeIndex, SprinklerParameter.SprinklerPt, graphPts, out var newIdx,
                                    realPtsSearchedTemp, sprinklerSearched, dirction, virtualPts, virtualPt, rowConnect, order))
                                {
                                    order++;
                                    edgeIndex = newIdx;
                                }

                                if ((!rowConnect.OrderDict.ContainsKey(-1) && rowConnect.Count > 8)
                                    || rowConnect.Count > 16)
                                {
                                    for (int m = 9; m <= rowConnect.Count; m++)
                                    {
                                        realPtsSearchedTemp.Remove(rowConnect.OrderDict[m][0]);
                                        rowConnect.OrderDict.Remove(m);
                                    }
                                    rowConnect.Count = 8;
                                    rowConnect.EndPoint = rowConnect.OrderDict[8][0];
                                    overCount = true;
                                    if (rowConnect.OrderDict.ContainsKey(-1))
                                    {
                                        rowConnect.OrderDict.Remove(-1);
                                    }
                                }
                                if (rowConnect.Count > 1)
                                {
                                    sprinklerSearched.AddRange(realPtsSearchedTemp);
                                    connectionTemp.Add(rowConnect);
                                }
                            }
                        });
                    }
                }

                var secChecked = false;
                // 沿次方向检索
                if ((sprinklerSearched.Count + pipeScattersTemp.Count) / (realPts.Count * 1.0) < 0.95
                    || realPts.Count - sprinklerSearched.Count - pipeScattersTemp.Count > 3)
                {
                    var connectionTempClone = connectionTemp.Select(row => row.Clone() as ThSprinklerRowConnect).ToList();
                    var overCountClone = false;
                    var closeToStall = false;
                    var sprinklerSearchedClone = new List<Point3d>();
                    var everScater = new List<Point3d>();
                    sprinklerSearched.ForEach(pt => sprinklerSearchedClone.Add(pt));
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
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
                                    var firstPt = net.Pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex];
                                    // 图中虚拟点所在的线段
                                    var edge = new Line(net.Pts[virtualNode.NodeIndex], firstPt);
                                    if (!edge.IsOrthogonal(SprinklerParameter.SubMainPipe))
                                    {
                                        edgeNode = edgeNode.Next;
                                        continue;
                                    }

                                    var hasScatter = false;
                                    firstPt.IsNoisePoint(SprinklerSearched, sprinklerSearchedClone, realPts, everScater, ref hasScatter);

                                    var rowConnect = new ThSprinklerRowConnect();
                                    if (LaneLine.Count > 0 && edge.GetCloseLaneLine(LaneLine).Item1 < 5000.0)
                                    {
                                        rowConnect.IsStallArea = true;
                                    }
                                    else
                                    {
                                        rowConnect.IsStallArea = false;
                                    }

                                    var dirction = edge.LineDirection();
                                    // 单次循环内已检索的喷淋点位
                                    var realPtsSearchedTemp = new List<Point3d>();
                                    // 点位顺序
                                    realPtsSearchedTemp.Add(firstPt);
                                    // 记录点位及其对应的顺序
                                    var order = 0;
                                    var virtualPt = net.Pts[virtualNode.NodeIndex];
                                    rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                    rowConnect.OrderDict.Add(order++, new List<Point3d> { firstPt });
                                    rowConnect.Count++;
                                    rowConnect.StartPoint = net.Pts[virtualNode.NodeIndex];
                                    rowConnect.EndPoint = firstPt;

                                    var hasScatterDict = new Dictionary<int, bool>
                                    {
                                        { 0, hasScatter },
                                        { -1, false },
                                    };
                                    var edgeIndex = edgeNode.EdgeIndex;
                                    while (KeepSearching2(graph, net, edgeIndex, SprinklerParameter.SprinklerPt, realPts, out var newIdx,
                                        ref hasScatter, realPtsSearchedTemp, sprinklerSearchedClone, SprinklerSearched, everScater,
                                        dirction, virtualPts, virtualPt, rowConnect, order))
                                    {
                                        if (order <= 8)
                                        {
                                            hasScatterDict[0] = hasScatter;
                                        }
                                        else
                                        {
                                            hasScatterDict[-1] = hasScatter;
                                        }
                                        order++;
                                        edgeIndex = newIdx;
                                    }

                                    if (!rowConnect.OrderDict.ContainsKey(-1) && rowConnect.Count > 8
                                        || rowConnect.Count > 16)
                                    {
                                        for (int m = 9; m <= rowConnect.Count; m++)
                                        {
                                            realPtsSearchedTemp.Remove(rowConnect.OrderDict[m][0]);
                                            rowConnect.OrderDict.Remove(m);
                                        }
                                        rowConnect.Count = 8;
                                        rowConnect.EndPoint = rowConnect.OrderDict[8][0];
                                        overCountClone = true;
                                        if (rowConnect.OrderDict.ContainsKey(-1))
                                        {
                                            rowConnect.OrderDict.Remove(-1);
                                        }
                                    }
                                    if (rowConnect.Count > 1
                                        && ((rowConnect.Count <= 8 && hasScatterDict[0])
                                            || (rowConnect.Count <= 16 && hasScatterDict[-1])))
                                    {
                                        virtualPtsSearched.Add(i);
                                        ThSprinklerConnectTools.HandleSecondRow(connectionTempClone, rowConnect, sprinklerSearchedClone, realPtsSearchedTemp);
                                        if (rowConnect.Base.CloseToStall(LaneLine, DTTol))
                                        {
                                            closeToStall = true;
                                        }
                                    }

                                    edgeNode = edgeNode.Next;
                                }
                            }
                        }
                    }

                    if (sprinklerSearchedClone.Count > (sprinklerSearched.Count + pipeScattersTemp.Count) * 1.05
                        || sprinklerSearchedClone.Count > sprinklerSearched.Count + pipeScattersTemp.Count && !closeToStall)
                    {
                        sprinklerSearched = sprinklerSearchedClone;
                        connectionTemp = connectionTempClone;
                        pipeScattersTemp = new List<Point3d>();
                        secChecked = true;
                        overCount = overCountClone;
                    }
                }

                // 再次检索
                if (secChecked
                    && (sprinklerSearched.Count + pipeScattersTemp.Count) / (realPts.Count * 1.0) < 0.95
                    || realPts.Count - sprinklerSearched.Count - pipeScattersTemp.Count > 3)
                {
                    var connectionTempClone = connectionTemp.Select(row => row.Clone() as ThSprinklerRowConnect).ToList();
                    var overCountClone = false;
                    var sprinklerSearchedClone = new List<Point3d>();
                    var everScater = new List<Point3d>();
                    sprinklerSearched.ForEach(pt => sprinklerSearchedClone.Add(pt));
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
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
                                    var firstPt = net.Pts[graph.SprinklerVertexNodeList[edgeNode.EdgeIndex].NodeIndex];
                                    // 图中虚拟点所在的线段
                                    var edge = new Line(net.Pts[virtualNode.NodeIndex], firstPt);
                                    if (!edge.IsOrthogonal(SprinklerParameter.SubMainPipe))
                                    {
                                        edgeNode = edgeNode.Next;
                                        continue;
                                    }

                                    var hasScatter = false;
                                    firstPt.IsNoisePoint(SprinklerSearched, sprinklerSearchedClone, realPts, everScater, ref hasScatter);

                                    var rowConnect = new ThSprinklerRowConnect();
                                    if (LaneLine.Count > 0 && edge.GetCloseLaneLine(LaneLine).Item1 < 5000.0)
                                    {
                                        rowConnect.IsStallArea = true;
                                    }
                                    else
                                    {
                                        rowConnect.IsStallArea = false;
                                    }

                                    var dirction = edge.LineDirection();

                                    if (mainDirction.ContinueConnect(dirction))
                                    {
                                        // 单次循环内已检索的喷淋点位
                                        var realPtsSearchedTemp = new List<Point3d>();
                                        // 点位顺序
                                        realPtsSearchedTemp.Add(firstPt);
                                        // 记录点位及其对应的顺序
                                        var order = 0;
                                        var virtualPt = net.Pts[virtualNode.NodeIndex];
                                        rowConnect.OrderDict.Add(order++, new List<Point3d> { virtualPt });
                                        rowConnect.OrderDict.Add(order++, new List<Point3d> { firstPt });
                                        rowConnect.Count++;
                                        rowConnect.StartPoint = net.Pts[virtualNode.NodeIndex];
                                        rowConnect.EndPoint = firstPt;

                                        var hasScatterDict = new Dictionary<int, bool>
                                        {
                                            { 0, hasScatter },
                                            { -1, false },
                                        };
                                        var edgeIndex = edgeNode.EdgeIndex;
                                        while (KeepSearching2(graph, net, edgeIndex, SprinklerParameter.SprinklerPt, realPts, out var newIdx,
                                            ref hasScatter, realPtsSearchedTemp, sprinklerSearchedClone, SprinklerSearched, everScater,
                                            dirction, virtualPts, virtualPt, rowConnect, order))
                                        {
                                            if (order <= 8)
                                            {
                                                hasScatterDict[0] = hasScatter;
                                            }
                                            else
                                            {
                                                hasScatterDict[-1] = hasScatter;
                                            }
                                            order++;
                                            edgeIndex = newIdx;
                                        }

                                        if (!rowConnect.OrderDict.ContainsKey(-1) && rowConnect.Count > 8
                                            || rowConnect.Count > 16)
                                        {
                                            for (int m = 9; m <= rowConnect.Count; m++)
                                            {
                                                realPtsSearchedTemp.Remove(rowConnect.OrderDict[m][0]);
                                                rowConnect.OrderDict.Remove(m);
                                            }
                                            rowConnect.Count = 8;
                                            rowConnect.EndPoint = rowConnect.OrderDict[8][0];
                                            overCountClone = true;
                                            if (rowConnect.OrderDict.ContainsKey(-1))
                                            {
                                                rowConnect.OrderDict.Remove(-1);
                                            }
                                        }
                                        if (hasScatter && rowConnect.Count > 1)
                                        {
                                            virtualPtsSearched.Add(i);
                                            ThSprinklerConnectTools.HandleSecondRow(connectionTempClone, rowConnect, sprinklerSearchedClone, realPtsSearchedTemp);
                                        }

                                        if (rowConnect.Count > 1
                                        && ((rowConnect.Count <= 8 && hasScatterDict[0])
                                            || (rowConnect.Count <= 16 && hasScatterDict[-1])))
                                        {
                                            virtualPtsSearched.Add(i);
                                            ThSprinklerConnectTools.HandleSecondRow(connectionTempClone, rowConnect, sprinklerSearchedClone, realPtsSearchedTemp);
                                        }
                                    }

                                    edgeNode = edgeNode.Next;
                                }
                            }
                        }
                    }

                    if (sprinklerSearchedClone.Count >= sprinklerSearched.Count)
                    {
                        sprinklerSearched = sprinklerSearchedClone;
                        connectionTemp = connectionTempClone;
                        overCount = overCountClone;
                    }
                }

                if (time == 0 && LaneLine.Count > 0 && !overCount
                    && (sprinklerSearched.Count + pipeScattersTemp.Count) / (realPts.Count * 1.0) > 0.8
                    && realPts.Count - sprinklerSearched.Count - pipeScattersTemp.Count < 10)
                {
                    SprinklerSearched.AddRange(sprinklerSearched);
                    pipeScatters.AddRange(pipeScattersTemp);
                    return connectionTemp;
                }
                else
                {
                    mainDirction = mainDirction.GetVerticalDirction();
                    var tuple = new Tuple<List<ThSprinklerRowConnect>, List<Point3d>, List<Point3d>>(connectionTemp, sprinklerSearched, pipeScattersTemp);
                    connection.Add(tuple);
                }
            }

            var firstRowCount = 0;
            if (LaneLine.Count > 0)
            {
                connection[0].Item1.ForEach(item =>
                {
                    if (item.OrderDict.ContainsKey(-1))
                    {
                        firstRowCount++;
                    }
                });
            }
            var secondRowCount = 0;
            if (LaneLine.Count > 0)
            {
                connection[1].Item1.ForEach(item =>
                {
                    if (item.OrderDict.ContainsKey(-1))
                    {
                        secondRowCount++;
                    }
                });
            }

            if (connection[0].Item1.Count == 0)
            {
                if (connection[1].Item1.Count == 0)
                {
                    return new List<ThSprinklerRowConnect>();
                }
                else
                {
                    SprinklerSearched.AddRange(connection[1].Item2);
                    pipeScatters.AddRange(connection[1].Item3);
                    return connection[1].Item1;
                }
            }
            if (connection[1].Item1.Count == 0)
            {
                if (connection[0].Item1.Count == 0)
                {
                    return new List<ThSprinklerRowConnect>();
                }
                else
                {
                    SprinklerSearched.AddRange(connection[0].Item2);
                    pipeScatters.AddRange(connection[0].Item3);
                    return connection[0].Item1;
                }
            }

            var firstRowCountList = connection[0].Item1.Select(row => row.Count).ToList();
            var firstAvg = firstRowCountList.Average();
            var fitstVar = firstRowCountList.Sum(x => Math.Pow(x - firstAvg, 2)) / firstRowCountList.Count();

            var secondRowCountList = connection[1].Item1.Select(row => row.Count).ToList();
            var secondAvg = secondRowCountList.Average();
            var secondVar = secondRowCountList.Sum(x => Math.Pow(x - secondAvg, 2)) / secondRowCountList.Count();
            // 比较两次已检索点的数量，多的优先；当已检索点个数相同时，再比较生成支管数量，少的优先
            if (connection[0].Item2.Count > connection[1].Item2.Count
                || (connection[0].Item2.Count == connection[1].Item2.Count && fitstVar < secondVar))
            {
                SprinklerSearched.AddRange(connection[0].Item2);
                pipeScatters.AddRange(connection[0].Item3);
                return connection[0].Item1;
            }
            else
            {
                SprinklerSearched.AddRange(connection[1].Item2);
                pipeScatters.AddRange(connection[1].Item3);
                return connection[1].Item1;
            }
        }

        /// <summary>
        /// 处理环
        /// </summary>
        /// <param name="rowConnection"></param>
        /// <param name="secRowConnection"></param>
        public void HandleLoopRow(List<ThSprinklerRowConnect> rowConnection)
        {
            var lines = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lines);

            rowConnection.ForEach(row =>
            {
                var filter = spatialIndex.SelectCrossingPolygon(row.Base.ExtendLine(-10.0).Buffer(1.0));
                if (filter.Count > 2)
                {
                    // 更新索引
                    spatialIndex.Update(new DBObjectCollection(), new DBObjectCollection { row.Base });
                    row.OrderDict.Values.ForEach(o => o.ForEach(pt => SprinklerSearched.Remove(pt)));
                    rowConnection.Remove(row);
                }
            });
        }

        public void HandleScatter(List<ThSprinklerRowConnect> rowConnection, List<Point3d> pipeScatters, List<Line> subMainPipe)
        {
            var ptList = SprinklerParameter.SprinklerPt.OrderBy(pt => pt.X).ToList();
            for (int time = 0; time < 1; time++)
            {
                var objs = rowConnection.Select(row => row.Base).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var piptIndex = new ThCADCoreNTSSpatialIndex(subMainPipe.ToCollection());
                for (int i = 0; i < ptList.Count; i++)
                {
                    if (SprinklerSearched.Contains(ptList[i]) || pipeScatters.Contains(ptList[i]))
                    {
                        continue;
                    }

                    if (rowConnection.Count > 0)
                    {
                        var frame = ptList[i].CreateSquare(2 * DTTol);
                        var pipeFilter = piptIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
                        var closeDistToPipe = ptList[i].CloseDistToPipe(pipeFilter);

                        var filter = spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
                        if (filter.Count == 0)
                        {
                            continue;
                        }

                        var closeDistToRow = 2 * filter[0].DistanceTo(ptList[i], true) + filter[0].DistanceTo(ptList[i], false);
                        var closeIndex = -1;
                        var startPoints = filter.Select(l => l.StartPoint).ToList();
                        var endPoints = filter.Select(l => l.EndPoint).ToList();
                        for (int n = 0; n < rowConnection.Count; n++)
                        {
                            if (!startPoints.Contains(rowConnection[n].Base.StartPoint) || !endPoints.Contains(rowConnection[n].Base.EndPoint))
                            {
                                continue;
                            }

                            var realClosePt = rowConnection[n].Base.GetClosestPointTo(ptList[i], false);
                            var extendClosePt = rowConnection[n].Base.GetClosestPointTo(ptList[i], true);
                            var realLine = new Line(realClosePt, ptList[i]);
                            var extendLine = new Line(realClosePt, extendClosePt);

                            // 判断线是否与管线相交
                            if (realLine.Length < 1.0 || realLine.IsIntersectsWithPipe(SprinklerParameter.AllPipe)
                                || (extendLine.Length > 1.0 && extendLine.IsIntersectsWithPipe(SprinklerParameter.AllPipe)))
                            {
                                continue;
                            }

                            // 判断线是否与墙线相交
                            if (ThSprinklerConnectTools.IsLineInWall(realClosePt, extendClosePt, ptList[i], Geometry))
                            {
                                continue;
                            }

                            // 避免多余线添加至列
                            if (time == 1)
                            {
                                if (ThSprinklerNetworkService.SearchClosePt(ptList[i], SprinklerParameter.SubMainPipe, 1.2 * DTTol, out var virtualPtList))
                                {
                                    continue;
                                }
                            }

                            var distance = 2 * rowConnection[n].Base.DistanceTo(ptList[i], true) + 0.8 * rowConnection[n].Base.DistanceTo(ptList[i], false);
                            if (closeDistToRow + 1.0 > distance)
                            {
                                closeDistToRow = distance;
                                closeIndex = n;
                            }
                        }

                        if (closeIndex == -1)
                        {
                            continue;
                        }

                        if (closeDistToPipe * 1.5 < closeDistToRow)
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

                                rowPts[ptIndex].Add(ptList[i]);
                                SprinklerSearched.Add(ptList[i]);
                                rowConnection[closeIndex].Count++;

                                var newLine = ptList[i].GetLongLine(rowConnection[closeIndex].Base);
                                spatialIndex.Update(new DBObjectCollection { newLine }, new DBObjectCollection { rowConnection[closeIndex].Base });
                                rowConnection[closeIndex].StartPoint = newLine.StartPoint;
                                rowConnection[closeIndex].EndPoint = newLine.EndPoint;
                            }
                        }
                    }
                }
            }
        }

        public List<ThSprinklerRowConnect> RowSeparation(List<ThSprinklerRowConnect> connection, bool isVertical = true)
        {
            var results = new List<ThSprinklerRowConnect>();
            connection.ForEach(o =>
            {
                if (!o.OrderDict.ContainsKey(-1))
                {
                    var row = new Dictionary<int, List<Point3d>>();
                    var num = 0;
                    for (int i = 0; i < o.OrderDict.Count; i++)
                    {
                        var ptList = new List<Point3d>();
                        ptList.Add(o.OrderDict[i][0]);
                        num++;
                        if (num <= 9)
                        {
                            row.Add(num - 1, ptList);
                        }
                        else
                        {
                            SprinklerSearched.Remove(o.OrderDict[i][0]);
                        }
                    }

                    for (int i = o.OrderDict.Count - 1; i >= 0; i--)
                    {
                        for (int n = 1; n < o.OrderDict[i].Count; n++)
                        {
                            if (num >= 9)
                            {
                                SprinklerSearched.Remove(o.OrderDict[i][n]);
                            }
                            else
                            {
                                row[i].Add(o.OrderDict[i][n]);
                                num++;
                            }
                        }
                    }

                    var rowConn = new ThSprinklerRowConnect
                    {
                        OrderDict = row,
                        Count = num - 1,
                        IsStallArea = o.IsStallArea,
                        StartPoint = o.OrderDict[0][0],
                        EndPoint = o.OrderDict[o.OrderDict.Count - 1][0],
                    };
                    results.Add(rowConn);
                }
                else
                {
                    var index = 1;
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
                    //if (o.Count <= 2 || LaneLine.Count == 0)
                    //{
                    //    o.OrderDict = OrderChange(o.OrderDict, true);
                    //    index = 1;
                    //}
                    if (o.IsStallArea && isVertical)
                    {
                        var newLine = new Line(o.OrderDict[-1][0], o.OrderDict[o.OrderDict.Count - 2][0]);
                        if (o.Count <= 8 && newLine.IsIntersection(LaneLine) && newLine.Length > DTTol)
                        {
                            o.OrderDict = o.OrderDict.OrderChange(true);
                            index = 1;
                        }
                        else
                        {
                            var newNum = 0;
                            var minDelta = o.Count;
                            for (int i = 1; i < o.OrderDict.Count - 1; i++)
                            {
                                var edge = new Line(o.OrderDict[i - 1][0], o.OrderDict[i][0]);
                                if (edge.IsIntersection(LaneLine))
                                {
                                    var delta = Math.Abs(o.Count - 2 * newNum);
                                    if (minDelta > delta && newNum <= 8 && o.Count - newNum <= 8)
                                    {
                                        index = i;
                                        minDelta = delta;
                                    }
                                }
                                newNum += o.OrderDict[i].Count;
                            }
                        }
                    }

                    var first = new Dictionary<int, List<Point3d>>
                    {
                        { 0, o.OrderDict[0] },
                    };
                    var firstNum = 1;
                    var second = new Dictionary<int, List<Point3d>>
                    {
                        {0, o.OrderDict[-1] },
                    };
                    var secondNum = 1;

                    for (int j = 1; j < index; j++)
                    {
                        var ptList = new List<Point3d>();
                        ptList.Add(o.OrderDict[j][0]);
                        firstNum++;
                        if (firstNum <= 9)
                        {
                            first.Add(firstNum - 1, ptList);
                        }
                        else
                        {
                            SprinklerSearched.Remove(o.OrderDict[j][0]);
                        }
                    }

                    for (int j = index - 1; j >= 1; j--)
                    {
                        for (int n = 1; n < o.OrderDict[j].Count; n++)
                        {
                            if (firstNum >= 9)
                            {
                                SprinklerSearched.Remove(o.OrderDict[j][n]);
                            }
                            else
                            {
                                first[j].Add(o.OrderDict[j][n]);
                                firstNum++;
                            }
                        }
                    }

                    for (int j = o.OrderDict.Count - 2; j >= index; j--)
                    {
                        var ptList = new List<Point3d>();
                        ptList.Add(o.OrderDict[j][0]);
                        secondNum++;
                        if (secondNum <= 9)
                        {
                            second.Add(secondNum - 1, ptList);
                        }
                        else
                        {
                            SprinklerSearched.Remove(o.OrderDict[j][0]);
                        }
                    }

                    for (int j = index; j < o.OrderDict.Count - 1; j++)
                    {
                        for (int n = 1; n < o.OrderDict[j].Count; n++)
                        {
                            if (secondNum >= 9)
                            {
                                SprinklerSearched.Remove(o.OrderDict[j][n]);
                            }
                            else
                            {
                                second[o.OrderDict.Count - 1 - j].Add(o.OrderDict[j][n]);
                                secondNum++;
                            }
                        }
                    }

                    if (first.Count > 1)
                    {
                        var rowConn = new ThSprinklerRowConnect
                        {
                            OrderDict = first,
                            Count = firstNum - 1,
                            IsStallArea = o.IsStallArea,
                            StartPoint = first[0][0],
                            EndPoint = first[first.Count - 1][0],
                        };
                        results.Add(rowConn);
                    }
                    if (second.Count > 1)
                    {
                        var rowConn = new ThSprinklerRowConnect
                        {
                            OrderDict = second,
                            Count = secondNum - 1,
                            IsStallArea = o.IsStallArea,
                            StartPoint = second[0][0],
                            EndPoint = second[second.Count - 1][0],
                        };

                        results.Add(rowConn);
                    }
                }
            });

            return results;
        }

        public void ConnScatterToPipe(List<ThSprinklerRowConnect> rowSeparation, List<Point3d> pipeScatters)
        {
            var ptList = SprinklerParameter.SprinklerPt.OrderBy(pt => pt.X).ToList();
            var ptIndex = new ThCADCoreNTSSpatialIndex(ptList.Select(pt => new DBPoint(pt)).ToCollection());
            var rowIndex = new ThCADCoreNTSSpatialIndex(rowSeparation.Select(row => row.Base).ToCollection());
            for (int i = 0; i < ptList.Count; i++)
            {
                if (SprinklerSearched.Contains(ptList[i]) || pipeScatters.Contains(ptList[i]))
                {
                    continue;
                }

                if (ThSprinklerNetworkService.SearchClosePt(ptList[i], SprinklerParameter.SubMainPipe, 1.2 * DTTol, out var virtualPtList, true))
                {
                    foreach (var closePt in virtualPtList)
                    {
                        var newLine = new Line(closePt, ptList[i]).ExtendLine(-10.0);
                        if (newLine.Length < 10.0)
                        {
                            continue;
                        }
                        var ptFilter = ptIndex.SelectCrossingPolygon(newLine.Buffer(10.0));
                        var rowFilter = rowIndex.SelectCrossingPolygon(newLine.Buffer(10.0));
                        if (ptFilter.Count == 0 && rowFilter.Count == 0 && !newLine.IsLineInWall(Geometry))
                        {
                            var row = new Dictionary<int, List<Point3d>>
                            {
                                {0, new List<Point3d> { closePt } },
                                {1, new List<Point3d>{ ptList[i]} },
                            };

                            var rowConn = new ThSprinklerRowConnect
                            {
                                StartPoint = closePt,
                                EndPoint = ptList[i],
                                OrderDict = row
                            };
                            rowConn.Count++;
                            rowSeparation.Add(rowConn);
                            pipeScatters.Add(ptList[i]);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理连续散点情形
        /// </summary>
        public void HandleConsequentScatter(ThSprinklerNetGroup net, int graphIdx,
            List<ThSprinklerRowConnect> rowConnection, List<Point3d> pipeScatters, List<Polyline> smallRooms, List<Polyline> obstacle)
        {
            // 给定索引所对应的图
            var graph = net.PtsGraph[graphIdx];
            // 图中的所有点位，包括虚拟点
            var graphPts = net.GetGraphPts(graphIdx);
            // 虚拟点
            var virtualPts = net.GetVirtualPts(graphIdx).OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            // 图中的所有喷淋点位
            var realPts = graphPts.Where(pt => !virtualPts.Contains(pt)).ToList();

            if (realPts.Count == 0)
            {
                return;
            }
            // 判断喷头是否位于小房间中
            var isSprinklerInSmallRoom = realPts[0].IsSprinklerInSmallRoom(smallRooms);
            var sprinklerTol = 0;
            if (isSprinklerInSmallRoom.Item1 == true)
            {
                sprinklerTol = SprinklerParameter.SprinklerPt.Where(pt => isSprinklerInSmallRoom.Item2.Contains(pt)).Count();
            }

            var rowLines = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(rowLines);
            var pipeIndex = new ThCADCoreNTSSpatialIndex(SprinklerParameter.SubMainPipe.ToCollection());

            // 已搜索点位
            var seachedPts = new List<Point3d>();

            for (int time = 0; time < 2; time++)
            {
                for (int i = 0; i < realPts.Count; i++)
                {
                    if (SprinklerSearched.Contains(realPts[i]) || seachedPts.Contains(realPts[i]) || pipeScatters.Contains(realPts[i]))
                    {
                        continue;
                    }
                    var edgeIndex = graph.SearchNodeIndex(net.Pts.IndexOf(realPts[i]));
                    var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
                    var ptNext = net.Pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];

                    var node = vertexNode.FirstEdge;

                    var vaildNode = 0;
                    if (!net.PtsVirtual.Contains(ptNext) && !seachedPts.Contains(ptNext) && !SprinklerSearched.Contains(ptNext))
                    {
                        vaildNode++;
                    }

                    while (node.Next != null)
                    {
                        node = node.Next;
                        var ptNextTemp = net.Pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                        if (!net.PtsVirtual.Contains(ptNextTemp) && !seachedPts.Contains(ptNextTemp) && !SprinklerSearched.Contains(ptNextTemp))
                        {
                            vaildNode++;
                            ptNext = ptNextTemp;
                        }
                    }

                    if (vaildNode != 1)
                    {
                        continue;
                    }

                    var scatterCount = 0;
                    var pipeScattersTemp = new List<Point3d>();
                    if (pipeScatters.Contains(ptNext))
                    {
                        pipeScattersTemp.Add(ptNext);
                        scatterCount++;
                    }

                    var ptList = new List<Point3d>();
                    ptList.Add(realPts[i]);
                    ptList.Add(ptNext);
                    seachedPts.Add(realPts[i]);
                    seachedPts.Add(ptNext);

                    var edge = new Line(realPts[i], ptNext);
                    var startDirection = edge.LineDirection();
                    var ptNextIndex = node.EdgeIndex;
                    while (KeepSearching3(graph, net, startDirection, seachedPts, ptList, ref ptNextIndex,
                        pipeScatters, pipeScattersTemp, virtualPts, ref scatterCount))
                    {
                        edge = new Line(realPts[i], net.Pts[graph.SprinklerVertexNodeList[ptNextIndex].NodeIndex]);
                    }

                    if (scatterCount > 1)
                    {
                        continue;
                    }



                    var center = new Point3d((realPts[i].X + ptNext.X) / 2, (realPts[i].Y + ptNext.Y) / 2, 0);
                    var filter = spatialIndex.SelectCrossingPolygon(center.CreateSquare(3 * DTTol));
                    var closeRowLines = new List<Line>();
                    if (filter.Count > 0)
                    {
                        filter.OfType<Line>().ForEach(line =>
                        {
                            closeRowLines.Add(line);
                        });
                        closeRowLines = closeRowLines.OrderBy(line => line.Distance(edge)
                            + edge.GetDistToPoint(line.GetClosestPointTo(edge.StartPoint, false), true)).ToList();
                    }

                    var pipefilter = pipeIndex.SelectCrossingPolygon(center.CreateSquare(3 * DTTol));
                    var closePipeLines = new List<Line>();
                    if (pipefilter.Count > 0)
                    {
                        pipefilter.OfType<Line>().ForEach(line =>
                        {
                            closePipeLines.Add(line);
                        });
                        closePipeLines = closePipeLines.OrderBy(line => line.Distance(edge)
                            + edge.GetDistToPoint(line.GetClosestPointTo(edge.StartPoint, false), true)).ToList();
                    }

                    if (closeRowLines.Count == 0 || isSprinklerInSmallRoom.IsPipeInSmallRoom(closePipeLines))
                    {
                        // 连接到支干管
                        if (closePipeLines.Count == 0 || !isSprinklerInSmallRoom.Item1)
                        {
                            continue;
                        }
                        else
                        {
                            for (int pipeCount = 0; pipeCount < closePipeLines.Count; pipeCount++)
                            {
                                if (edge.ConnectToPipe(closePipeLines[pipeCount], Geometry, SprinklerParameter.AllPipe, rowConnection,
                                    ptList, SprinklerSearched))
                                {
                                    pipeScattersTemp.ForEach(pt => rowConnection.RemoveAll(row => row.Base.EndPoint == pt));
                                    pipeScattersTemp.ForEach(pt => pipeScatters.RemoveAll(o => o == pt));
                                    if (isSprinklerInSmallRoom.Item1)
                                    {
                                        smallRooms.Remove(isSprinklerInSmallRoom.Item2);
                                    }
                                    break;
                                }
                            }
                            continue;
                        }
                    }

                    if (closePipeLines.Count == 0)
                    {
                        // 连接到支管
                        if (closeRowLines.Count == 0)
                        {
                            continue;
                        }
                        else
                        {
                            for (int rowCount = 0; rowCount < closeRowLines.Count; rowCount++)
                            {
                                if (sprinklerTol <= 8
                                    && edge.ConnectToRow(isSprinklerInSmallRoom.Item1, closeRowLines[rowCount], Geometry, obstacle,
                                        SprinklerParameter.AllPipe, rowConnection, ptList, SprinklerSearched, sprinklerTol))
                                {
                                    pipeScattersTemp.ForEach(pt => rowConnection.RemoveAll(row => row.Base.EndPoint == pt));
                                    pipeScattersTemp.ForEach(pt => pipeScatters.RemoveAll(o => o == pt));
                                    if (isSprinklerInSmallRoom.Item1)
                                    {
                                        smallRooms.Remove(isSprinklerInSmallRoom.Item2);
                                    }
                                    break;
                                }
                            }
                            continue;
                        }
                    }

                    if (closeRowLines.Count > 0 && closePipeLines.Count > 0)
                    {
                        var closeLines = new List<Line>();
                        closeLines.AddRange(closeRowLines);
                        closeLines.AddRange(closePipeLines);
                        closeLines = closeLines.OrderBy(line => line.Distance(edge)
                            + edge.GetDistToPoint(line.GetClosestPointTo(edge.StartPoint, false), true)).ToList();

                        for (int count = 0; count < closeLines.Count; count++)
                        {
                            if (closeRowLines.Contains(closeLines[count]))
                            {
                                if (sprinklerTol <= 8
                                    && edge.ConnectToRow(isSprinklerInSmallRoom.Item1, closeLines[count], Geometry, obstacle,
                                        SprinklerParameter.AllPipe, rowConnection, ptList, SprinklerSearched, sprinklerTol))
                                {
                                    pipeScattersTemp.ForEach(pt => rowConnection.RemoveAll(row => row.Base.EndPoint == pt));
                                    pipeScattersTemp.ForEach(pt => pipeScatters.RemoveAll(o => o == pt));
                                    if (isSprinklerInSmallRoom.Item1)
                                    {
                                        smallRooms.Remove(isSprinklerInSmallRoom.Item2);
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                if (edge.ConnectToPipe(closeLines[count], Geometry, SprinklerParameter.AllPipe, rowConnection,
                                    ptList, SprinklerSearched))
                                {
                                    pipeScattersTemp.ForEach(pt => rowConnection.RemoveAll(row => row.Base.EndPoint == pt));
                                    pipeScattersTemp.ForEach(pt => pipeScatters.RemoveAll(o => o == pt));
                                    if (isSprinklerInSmallRoom.Item1)
                                    {
                                        smallRooms.Remove(isSprinklerInSmallRoom.Item2);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool KeepSearching1(ThSprinklerGraph graph, ThSprinklerNetGroup net, int edgeIndex, List<Point3d> sprinklers,
            List<Point3d> realPts, out int newIdx, List<Point3d> realPtsSearchedTemp, List<Point3d> sprinklerSearched, Vector3d dirction,
            List<Point3d> virtualPts, Point3d virtualPt, ThSprinklerRowConnect rowConnect, int order)
        {
            if (edgeIndex == -1)
            {
                newIdx = -1;
                return false;
            }

            // 返回是否退出当前索引的循环
            // 搜索下一个点位
            var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
            var originalPt = net.Pts[vertexNode.NodeIndex];
            var ptNext = net.Pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edgeNext = new Line(originalPt, ptNext);
            var node = vertexNode.FirstEdge;
            newIdx = vertexNode.FirstEdge.EdgeIndex;
            // 如果点位已被检索，或之间连线角度偏大，或节点为虚拟点，则进入循环
            while (SprinklerSearched.Contains(ptNext)
                || sprinklerSearched.Contains(ptNext)
                || realPtsSearchedTemp.Contains(ptNext)
                || virtualPt.IsEqualTo(ptNext)
                || edgeNext.LineDirection().DotProduct(dirction) < 0.998)
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.Pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edgeNext = new Line(originalPt, ptNext);
                    newIdx = node.EdgeIndex;
                }
                else
                {
                    // 继续沿该方向进行搜索
                    var extendLine = new Line(originalPt - dirction, originalPt + 2.5 * dirction * DTTol);
                    var ptSearched = extendLine.SearchPointByDirction(sprinklers, realPts, originalPt, Geometry, out var firstPt);
                    var virtualPtSearched = extendLine.SearchVirtualPt(originalPt, Geometry, SprinklerParameter.SubMainPipe, out var firstVirtualPt);
                    virtualPtSearched = false;
                    if (ptSearched && !virtualPtSearched)
                    {
                        ptNext = firstPt;
                        newIdx = graph.SearchNodeIndex(net.Pts.IndexOf(ptNext));
                        if (newIdx == -1
                            || SprinklerSearched.Contains(ptNext)
                            || sprinklerSearched.Contains(ptNext)
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
                            newIdx = graph.SearchNodeIndex(net.Pts.IndexOf(ptNext));
                            if (SprinklerSearched.Contains(ptNext) || sprinklerSearched.Contains(ptNext) || realPtsSearchedTemp.Contains(ptNext))
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
                rowConnect.OrderDict = rowConnect.OrderDict.OrderChange();
                rowConnect.EndPoint = ptNext;
                return false;
            }
            else
            {
                //if (LaneLine.Count == 0 && order >= 8)
                //{
                //    rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                //    rowConnect.EndPoint = ptNext;
                //    rowConnect.Count++;
                //    realPtsSearchedTemp.Add(ptNext);
                //    return false;
                //}
                rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                rowConnect.EndPoint = ptNext;
                rowConnect.Count++;
                realPtsSearchedTemp.Add(ptNext);
                return true;
            }
        }

        /// <summary>
        /// 判断次方向连线
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="net"></param>
        /// <param name="edgeIndex"></param>
        /// <param name="graphPts"></param>
        /// <param name="newIdx"></param>
        /// <param name="realPtsSearchedTemp"></param>
        /// <param name="sprinklerSearched"></param>
        /// <param name="dirction"></param>
        /// <param name="virtualPts"></param>
        /// <param name="virtualPt"></param>
        /// <param name="rowConnect"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool KeepSearching2(ThSprinklerGraph graph, ThSprinklerNetGroup net, int edgeIndex, List<Point3d> sprinklers,
            List<Point3d> realPts, out int newIdx, ref bool hasScatter, List<Point3d> realPtsSearchedTemp, List<Point3d> sprinklerSearched,
            List<Point3d> allSprinklerSearched, List<Point3d> everScatter, Vector3d dirction, List<Point3d> virtualPts,
            Point3d virtualPt,
            ThSprinklerRowConnect rowConnect, int order)
        {
            if (edgeIndex == -1)
            {
                newIdx = -1;
                return false;
            }

            // 返回是否退出当前索引的循环
            // 搜索下一个点位
            var vertexNode = graph.SprinklerVertexNodeList[edgeIndex];
            var originalPt = net.Pts[vertexNode.NodeIndex];
            var ptNext = net.Pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edgeNext = new Line(originalPt, ptNext);
            var node = vertexNode.FirstEdge;
            newIdx = vertexNode.FirstEdge.EdgeIndex;

            // 如果点位已被检索，或之间连线角度偏大，或节点为虚拟点，则进入循环
            while (realPtsSearchedTemp.Contains(ptNext)
                || allSprinklerSearched.Contains(ptNext)
                || virtualPt.IsEqualTo(ptNext)
                || edgeNext.LineDirection().DotProduct(dirction) < 0.998)
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.Pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edgeNext = new Line(originalPt, ptNext);
                    newIdx = node.EdgeIndex;
                }
                else
                {
                    // 继续沿该方向进行搜索
                    var extendLine = new Line(originalPt - dirction, originalPt + 2.5 * dirction * DTTol);
                    var ptSearched = extendLine.SearchPointByDirction(sprinklers, realPts, originalPt, Geometry, out var firstPt);
                    var virtualPtSearched = extendLine.SearchVirtualPt(originalPt, Geometry, SprinklerParameter.SubMainPipe, out var firstVirtualPt);
                    if (ptSearched && !virtualPtSearched)
                    {
                        ptNext = firstPt;
                        newIdx = graph.SearchNodeIndex(net.Pts.IndexOf(ptNext));
                        if (newIdx == -1
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
                            newIdx = graph.SearchNodeIndex(net.Pts.IndexOf(ptNext));
                            if (realPtsSearchedTemp.Contains(ptNext))
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
                rowConnect.OrderDict = rowConnect.OrderDict.OrderChange();
                rowConnect.EndPoint = ptNext;
                return false;
            }
            else
            {
                ptNext.IsNoisePoint(allSprinklerSearched, sprinklerSearched, realPts, everScatter, ref hasScatter);

                //if (LaneLine.Count == 0 && order >= 8)
                //{
                //    rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                //    rowConnect.EndPoint = ptNext;
                //    rowConnect.Count++;
                //    realPtsSearchedTemp.Add(ptNext);
                //    return false;
                //}
                rowConnect.OrderDict.Add(order, new List<Point3d> { ptNext });
                rowConnect.EndPoint = ptNext;
                rowConnect.Count++;
                realPtsSearchedTemp.Add(ptNext);
                return true;
            }
        }

        /// <summary>
        /// 对剩余点沿所给方向继续搜索
        /// </summary>
        /// <returns></returns>
        private bool KeepSearching3(ThSprinklerGraph graph, ThSprinklerNetGroup net, Vector3d dirction, List<Point3d> seachedPts,
            List<Point3d> ptList, ref int ptNextIndex, List<Point3d> pipeScatters, List<Point3d> pipeScattersTemp, List<Point3d> virtualPts,
            ref int scatterCount)
        {
            // tag
            var vertexNode = graph.SprinklerVertexNodeList[ptNextIndex];
            var startPoint = net.Pts[vertexNode.NodeIndex];
            var ptNext = net.Pts[graph.SprinklerVertexNodeList[vertexNode.FirstEdge.EdgeIndex].NodeIndex];
            var edge = new Line(startPoint, ptNext);
            var node = vertexNode.FirstEdge;

            while (edge.LineDirection().DotProduct(dirction) < 0.998
                || virtualPts.Contains(ptNext)
                || SprinklerSearched.Contains(ptNext)
                || seachedPts.Contains(ptNext))
            {
                if (node.Next != null)
                {
                    node = node.Next;
                    ptNext = net.Pts[graph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                    edge = new Line(startPoint, ptNext);
                }
                else
                {
                    return false;
                }
            }

            if (pipeScatters.Contains(ptNext))
            {
                pipeScattersTemp.Add(ptNext);
                scatterCount++;
                if (scatterCount > 1)
                {
                    return false;
                }
            }

            ptNextIndex = node.EdgeIndex;
            ptList.Add(ptNext);
            seachedPts.Add(ptNext);
            return true;
        }

        public void SprinklerConnect(List<ThSprinklerRowConnect> rowConnection, List<Polyline> geometry, List<Polyline> obstacle,
            double connTolerance)
        {
            var wallIndex = new ThCADCoreNTSSpatialIndex(geometry.ToCollection());
            var obstacleIndex = new ThCADCoreNTSSpatialIndex(obstacle.ToCollection());

            rowConnection.ForEach(row =>
            {
                var count = row.OrderDict.Keys.Where(key => key >= 0).Count();
                var ptsTemp = new List<Tuple<Point3d, bool>>();
                ptsTemp.Add(Tuple.Create(row.OrderDict[0][0], false));
                for (int i = 1; i < count; i++)
                {
                    // true表示为喷头点
                    ptsTemp.Add(Tuple.Create(row.OrderDict[i][0], true));
                }
                if (count > 1)
                {
                    var baseLine = new Line(ptsTemp[0].Item1, ptsTemp[1].Item1);
                    for (int i = 1; i < count; i++)
                    {
                        for (int j = 1; j < row.OrderDict[i].Count; j++)
                        {
                            var closePt = baseLine.GetClosestPointTo(row.OrderDict[i][j], true);
                            int num = 1;
                            for (; num < ptsTemp.Count; num++)
                            {
                                var newLine = new Line(ptsTemp[num - 1].Item1, ptsTemp[num].Item1);
                                if (newLine.DistanceTo(closePt, false) < 10.0)
                                {
                                    if (ptsTemp[num - 1].Item2 && closePt.DistanceTo(ptsTemp[num - 1].Item1) < connTolerance / 2)
                                    {
                                        var closePtTemp = ptsTemp[num - 1].Item1 + connTolerance * baseLine.LineDirection();
                                        var extendPt = closePtTemp + (row.OrderDict[i][j] - closePt);
                                        var firstLine = new Line(closePtTemp, extendPt);
                                        var secondLine = new Line(extendPt, row.OrderDict[i][j]);
                                        row.ConnectLines.Add(firstLine);
                                        row.ConnectLines.Add(secondLine);
                                        ptsTemp.Insert(num, Tuple.Create(closePtTemp, false));
                                    }
                                    else if (ptsTemp[num].Item2 && closePt.DistanceTo(ptsTemp[num].Item1) < connTolerance / 2)
                                    {
                                        var closePtTemp = ptsTemp[num].Item1 - connTolerance * baseLine.LineDirection();
                                        var extendPt = closePtTemp + (row.OrderDict[i][j] - closePt);
                                        var firstLine = new Line(closePtTemp, extendPt);
                                        var secondLine = new Line(extendPt, row.OrderDict[i][j]);
                                        row.ConnectLines.Add(firstLine);
                                        row.ConnectLines.Add(secondLine);
                                        ptsTemp.Insert(num, Tuple.Create(closePtTemp, false));
                                    }
                                    else
                                    {
                                        var firstLine = new Line(closePt, row.OrderDict[i][j]);
                                        row.ConnectLines.Add(firstLine);
                                        ptsTemp.Insert(num, Tuple.Create(closePt, false));
                                    }
                                    break;
                                }
                            }
                            if (num == ptsTemp.Count)
                            {
                                if (ptsTemp[num - 1].Item2 && closePt.DistanceTo(ptsTemp[num - 1].Item1) < connTolerance)
                                {
                                    var closePtTemp = ptsTemp[num - 1].Item1 + connTolerance * baseLine.LineDirection();
                                    var firstLine = new Line(closePtTemp, closePtTemp + (row.OrderDict[i][j] - closePt));
                                    var secondLine = new Line(closePtTemp + (row.OrderDict[i][j] - closePt), row.OrderDict[i][j]);
                                    row.ConnectLines.Add(firstLine);
                                    row.ConnectLines.Add(secondLine);
                                    ptsTemp.Insert(num, Tuple.Create(closePtTemp, false));
                                }
                                else
                                {
                                    var firstLine = new Line(closePt, row.OrderDict[i][j]);
                                    row.ConnectLines.Add(firstLine);
                                    ptsTemp.Insert(num, Tuple.Create(closePt, false));
                                }
                            }
                        }
                    }
                    for (int i = 1; i < ptsTemp.Count; i++)
                    {
                        row.ConnectLines.Add(new Line(ptsTemp[i - 1].Item1, ptsTemp[i].Item1));
                    }
                }

                if (row.OrderDict.ContainsKey(-2))
                {
                    for (int j = 1; j < row.OrderDict[-2].Count; j++)
                    {
                        var line = new Line(row.OrderDict[-2][j - 1], row.OrderDict[-2][j]);
                        if (j > 1)
                        {
                            var removeLine = row.ConnectLines.Where(l => l.EndPoint == line.StartPoint).FirstOrDefault();
                            if (removeLine != null)
                            {
                                if (line.DistanceTo(removeLine.StartPoint, false) < 10.0)
                                {
                                    row.ConnectLines.Remove(removeLine);
                                    row.ConnectLines.Add(new Line(removeLine.StartPoint, line.EndPoint));
                                }
                            }
                            row.ConnectLines.Add(line);
                        }
                        else
                        {
                            // 直线上距离点位最近的点
                            var closePt = line.GetClosestPointTo(ptsTemp[ptsTemp.Count - 1].Item1, true);
                            if (ptsTemp.Count == 1)
                            {

                                if (closePt.DistanceTo(line.StartPoint) > connTolerance && closePt.DistanceTo(line.EndPoint) > connTolerance)
                                {
                                    var closePtTemp = line.GetClosestPointTo(ptsTemp[ptsTemp.Count - 1].Item1, false);
                                    row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                    row.StartPoint = ptsTemp[ptsTemp.Count - 1].Item1;
                                    row.EndPoint = closePt;
                                    if (closePt.DistanceTo(closePtTemp) > 10.0)
                                    {
                                        row.ConnectLines.Add(line);
                                        if (row.OrderDict[-2][j - 1].DistanceTo(closePt) + 1.0 < row.OrderDict[-2][j].DistanceTo(closePt))
                                        {
                                            row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                        }
                                        else
                                        {
                                            row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j]));
                                        }
                                    }
                                    else
                                    {
                                        row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                        row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j]));
                                    }
                                }
                                else
                                {
                                    var closePtTemp = line.GetCenterPoint();
                                    var extendPt = ptsTemp[ptsTemp.Count - 1].Item1 + (closePtTemp - closePt);
                                    row.StartPoint = ptsTemp[ptsTemp.Count - 1].Item1;
                                    row.EndPoint = extendPt;
                                    row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, extendPt));
                                    row.ConnectLines.Add(new Line(extendPt, closePtTemp));
                                    row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                    row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                }
                                continue;
                            }
                            var scrLine = new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptsTemp[ptsTemp.Count - 1].Item1);
                            // 标准L字型连线
                            if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.02
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 1].Item1) < connTolerance)
                            {
                                var closePtTemp = ptsTemp[ptsTemp.Count - 1].Item1
                                    + (ptsTemp[ptsTemp.Count - 2].Item1 - ptsTemp[ptsTemp.Count - 1].Item1).GetNormal() * connTolerance;
                                row.ConnectLines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, closePtTemp));
                                row.ConnectLines.Add(new Line(closePtTemp, ptsTemp[ptsTemp.Count - 1].Item1));
                                row.ConnectLines.Add(new Line(closePtTemp, closePtTemp + (line.GetCenterPoint() - closePt)));
                                row.ConnectLines.Add(new Line(closePtTemp + (line.GetCenterPoint() - closePt), line.GetCenterPoint()));
                                row.ConnectLines.Add(new Line(line.GetCenterPoint(), row.OrderDict[-2][j - 1]));
                                row.ConnectLines.Add(new Line(line.GetCenterPoint(), row.OrderDict[-2][j]));
                            }
                            else if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.02
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 2].Item1) > scrLine.Length
                                && line.GetDistToPoint(closePt, false) > connTolerance / 2)
                            {
                                row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                row.ConnectLines.Add(line);
                                if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                {
                                    row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                }
                                else
                                {
                                    row.ConnectLines.Add(new Line(row.OrderDict[-2][j], closePt));
                                }
                            }
                            else if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.02
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 2].Item1) > scrLine.Length
                                && closePt.DistanceTo(row.OrderDict[-2][j - 1]) > connTolerance / 2
                                && closePt.DistanceTo(row.OrderDict[-2][j]) > connTolerance / 2)
                            {
                                row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j]));
                            }
                            else if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.02
                                && closePt.DistanceTo(ptsTemp[ptsTemp.Count - 2].Item1) < connTolerance / 2)
                            {
                                var scrCenter = scrLine.GetCenterPoint();
                                var moveVector = scrCenter - ptsTemp[ptsTemp.Count - 2].Item1;
                                var startPointTidal = line.StartPoint + moveVector;
                                var endPointTidal = line.EndPoint + moveVector;

                                row.ConnectLines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, scrCenter));
                                row.ConnectLines.Add(new Line(scrCenter, ptsTemp[ptsTemp.Count - 1].Item1));
                                if (scrCenter.DistanceTo(startPointTidal) < scrCenter.DistanceTo(endPointTidal))
                                {
                                    row.ConnectLines.Add(new Line(scrCenter, startPointTidal));
                                    row.ConnectLines.Add(new Line(startPointTidal, endPointTidal));
                                }
                                else
                                {
                                    row.ConnectLines.Add(new Line(scrCenter, endPointTidal));
                                    row.ConnectLines.Add(new Line(endPointTidal, startPointTidal));
                                }
                                row.ConnectLines.Add(new Line(startPointTidal, line.StartPoint));
                                row.ConnectLines.Add(new Line(endPointTidal, line.EndPoint));
                            }
                            else if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.02)
                            {
                                row.ConnectLines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, closePt));
                                row.ConnectLines.Add(new Line(closePt, ptsTemp[ptsTemp.Count - 1].Item1));
                                row.ConnectLines.Add(line);
                                if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                {
                                    row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                }
                                else
                                {
                                    row.ConnectLines.Add(new Line(row.OrderDict[-2][j], closePt));
                                }
                            }
                            else if (ptsTemp[ptsTemp.Count - 1].Item2
                                && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) > 0.998)
                            {
                                var goingOn = true;
                                var closePtTemp = line.GetClosestPointTo(closePt, false);
                                var extendLine = new Line(closePt, closePtTemp);

                                // 两线平行且中心连线
                                if (extendLine.Length < connTolerance)
                                {
                                    var scrCenter = scrLine.GetCenter();
                                    var lineCenter = line.GetClosestPointTo(scrCenter, false);
                                    var crossLine = new Line(scrCenter, lineCenter);
                                    if (!row.IsSmallRoom)
                                    {
                                        var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                        if (filter.Count == 0)
                                        {
                                            goingOn = false;
                                        }
                                    }
                                    else
                                    {
                                        var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                        if (filter.Count == 0)
                                        {
                                            goingOn = false;
                                        }
                                    }

                                    if (!goingOn)
                                    {
                                        if (lineCenter.DistanceTo(row.OrderDict[-2][j - 1]) > connTolerance
                                            && lineCenter.DistanceTo(row.OrderDict[-2][j]) > connTolerance)
                                        {
                                            row.ConnectLines.Add(crossLine);
                                            row.ConnectLines.Add(new Line(lineCenter, row.OrderDict[-2][j - 1]));
                                            row.ConnectLines.Add(new Line(lineCenter, row.OrderDict[-2][j]));
                                        }
                                        else
                                        {
                                            goingOn = true;
                                        }
                                    }
                                    if (goingOn)
                                    {
                                        var ptOnScrLine = new Point3d();
                                        for (int exp = 1; exp < 3 && goingOn; exp++)
                                        {
                                            for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                            {
                                                closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                                ptOnScrLine = scrLine.GetClosestPointTo(closePtTemp, false);
                                                crossLine = new Line(closePtTemp, ptOnScrLine);
                                                if (!row.IsSmallRoom)
                                                {
                                                    var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                                else
                                                {
                                                    var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                            }
                                        }
                                        row.ConnectLines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                        row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                        row.ConnectLines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                        row.ConnectLines.Add(new Line(ptOnScrLine, closePtTemp));
                                        row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                    }
                                }
                                else
                                {
                                    var centerPoint = line.GetCenterPoint();
                                    var ptOnScrLine = scrLine.GetClosestPointTo(centerPoint, true);
                                    var lineList = new List<Line>();
                                    extendLine = new Line(scrLine.EndPoint, ptOnScrLine);
                                    var crossLine = new Line(ptOnScrLine, centerPoint);
                                    lineList.Add(extendLine);
                                    lineList.Add(crossLine);
                                    lineList.RemoveAll(line => line.Length == 0);

                                    if (!row.IsSmallRoom)
                                    {
                                        for (int countTemp = 0; countTemp < lineList.Count; countTemp++)
                                        {
                                            var filter = wallIndex.SelectCrossingPolygon(lineList[countTemp].Buffer(1.0));
                                            if (filter.Count == 0)
                                            {
                                                goingOn = false;
                                            }
                                            else
                                            {
                                                goingOn = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int countTemp = 0; countTemp < lineList.Count; countTemp++)
                                        {
                                            var filter = obstacleIndex.SelectCrossingPolygon(lineList[countTemp].Buffer(1.0));
                                            if (filter.Count == 0)
                                            {
                                                goingOn = false;
                                            }
                                            else
                                            {
                                                goingOn = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!goingOn)
                                    {
                                        row.ConnectLines.Add(extendLine);
                                        row.ConnectLines.Add(crossLine);
                                        row.ConnectLines.Add(new Line(centerPoint, row.OrderDict[-2][j - 1]));
                                        row.ConnectLines.Add(new Line(centerPoint, row.OrderDict[-2][j]));
                                    }
                                    if (goingOn)
                                    {
                                        ptOnScrLine = new Point3d();
                                        var scrPtTemp = ptsTemp[ptsTemp.Count - 1].Item1 + scrLine.LineDirection() * connTolerance;
                                        for (int exp = 1; exp < 3 && goingOn; exp++)
                                        {
                                            for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                            {
                                                closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                                ptOnScrLine = closePtTemp + (scrPtTemp - closePt);
                                                crossLine = new Line(closePtTemp, ptOnScrLine);
                                                if (!row.IsSmallRoom)
                                                {
                                                    var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                                else
                                                {
                                                    var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                            }
                                        }
                                        row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, scrPtTemp));
                                        row.ConnectLines.Add(new Line(scrPtTemp, ptOnScrLine));
                                        row.ConnectLines.Add(new Line(ptOnScrLine, closePtTemp));
                                        row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                    }
                                }
                            }
                            else
                            {
                                if (line.GetDistToPoint(closePt, false) < 1.0
                                    && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) > 0.998)
                                {
                                    var closePtTemp = new Point3d();
                                    var ptOnScrLine = new Point3d();
                                    var goingOn = true;
                                    for (int exp = 1; exp < 3 && goingOn; exp++)
                                    {
                                        for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                        {
                                            closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                            ptOnScrLine = closePtTemp + (ptsTemp[ptsTemp.Count - 1].Item1 - closePt);
                                            if (ptsTemp[ptsTemp.Count - 1].Item2)
                                            {
                                                if (ptOnScrLine.DistanceTo(ptsTemp[ptsTemp.Count - 1].Item1) < connTolerance)
                                                {
                                                    continue;
                                                }
                                            }
                                            var crossLine = new Line(closePtTemp, ptOnScrLine);
                                            if (!row.IsSmallRoom)
                                            {
                                                var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                            else
                                            {
                                                var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                        }
                                    }
                                    row.ConnectLines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                    row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                    row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                    row.ConnectLines.Add(new Line(closePtTemp, ptOnScrLine));
                                    row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                    row.ConnectLines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                }
                                else if (line.GetDistToPoint(closePt, false) < 1.0
                                            && Math.Abs(line.LineDirection().DotProduct(scrLine.LineDirection())) < 0.02)
                                {
                                    var closePtTemp = new Point3d();
                                    var ptOnScrLine = new Point3d();
                                    var connectLine = new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt);
                                    var goingOn = true;
                                    if (!row.IsSmallRoom)
                                    {
                                        var filter = wallIndex.SelectCrossingPolygon(connectLine.Buffer(1.0));
                                        if (filter.Count == 0)
                                        {
                                            goingOn = false;
                                        }
                                    }
                                    else
                                    {
                                        var filter = obstacleIndex.SelectCrossingPolygon(connectLine.Buffer(1.0));
                                        if (filter.Count == 0)
                                        {
                                            goingOn = false;
                                        }
                                    }
                                    if (closePt.DistanceTo(line.StartPoint) < connTolerance
                                        || closePt.DistanceTo(line.EndPoint) < connTolerance
                                        || goingOn)
                                    {
                                        for (int exp = 1; exp < 3 && goingOn; exp++)
                                        {
                                            for (int num = 1; num < Math.Pow(2, exp) && goingOn; num++)
                                            {
                                                closePtTemp = row.OrderDict[-2][j - 1] + num / Math.Pow(2, exp) * line.Length * line.LineDirection();
                                                ptOnScrLine = closePtTemp + (ptsTemp[ptsTemp.Count - 1].Item1 - closePt);
                                                if (ptsTemp[ptsTemp.Count - 1].Item2)
                                                {
                                                    if (ptOnScrLine.DistanceTo(ptsTemp[ptsTemp.Count - 1].Item1) < connTolerance)
                                                    {
                                                        continue;
                                                    }
                                                }
                                                var crossLine = new Line(closePtTemp, ptOnScrLine);
                                                if (!row.IsSmallRoom)
                                                {
                                                    var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                                else
                                                {
                                                    var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                    if (filter.Count == 0)
                                                    {
                                                        goingOn = false;
                                                    }
                                                }
                                            }
                                        }
                                        row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                        row.ConnectLines.Add(new Line(ptOnScrLine, closePtTemp));
                                        row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, ptOnScrLine));
                                    }
                                    else
                                    {
                                        row.ConnectLines.Add(connectLine);
                                        row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j - 1]));
                                        row.ConnectLines.Add(new Line(closePt, row.OrderDict[-2][j]));
                                    }
                                }
                                else
                                {
                                    if (line.GetDistToPoint(closePt, false) > connTolerance && !ptsTemp[ptsTemp.Count - 1].Item2)
                                    {
                                        row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 1].Item1, closePt));
                                        row.ConnectLines.Add(line);
                                        if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                        {
                                            row.ConnectLines.Add(new Line(row.OrderDict[-2][j - 1], closePt));
                                        }
                                        else
                                        {
                                            row.ConnectLines.Add(new Line(row.OrderDict[-2][j], closePt));
                                        }
                                    }
                                    else
                                    {
                                        var extendDirection = true;
                                        if (closePt.DistanceTo(row.OrderDict[-2][j - 1]) < closePt.DistanceTo(row.OrderDict[-2][j]))
                                        {
                                            extendDirection = false;
                                        }
                                        var closePtTemp = new Point3d();
                                        var ptOnScrLine = new Point3d();
                                        var crossLine = new Line();
                                        var extendLine = new Vector3d();
                                        var goingOn = true;
                                        for (int coefficient = 1; coefficient < 5 && goingOn; coefficient++)
                                        {
                                            if (extendDirection)
                                            {
                                                extendLine = line.LineDirection() * connTolerance * coefficient;
                                            }
                                            else
                                            {
                                                extendLine = -line.LineDirection() * connTolerance * coefficient;
                                            }
                                            closePtTemp = closePt + extendLine;
                                            ptOnScrLine = ptsTemp[ptsTemp.Count - 1].Item1 + extendLine;
                                            crossLine = new Line(closePtTemp, ptOnScrLine);
                                            if (!row.IsSmallRoom)
                                            {
                                                var filter = wallIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                            else
                                            {
                                                var filter = obstacleIndex.SelectCrossingPolygon(crossLine.Buffer(1.0));
                                                if (filter.Count == 0)
                                                {
                                                    goingOn = false;
                                                }
                                            }
                                        }
                                        if (goingOn)
                                        {
                                            continue;
                                        }
                                        row.ConnectLines.RemoveAll(l => l.StartPoint == ptsTemp[ptsTemp.Count - 2].Item1 && l.EndPoint == ptsTemp[ptsTemp.Count - 1].Item1);
                                        row.ConnectLines.Add(new Line(ptsTemp[ptsTemp.Count - 2].Item1, ptOnScrLine));
                                        row.ConnectLines.Add(new Line(ptOnScrLine, ptsTemp[ptsTemp.Count - 1].Item1));
                                        row.ConnectLines.Add(crossLine);
                                        row.ConnectLines.Add(line);

                                        if (extendDirection)
                                        {
                                            row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j]));
                                        }
                                        else
                                        {
                                            row.ConnectLines.Add(new Line(closePtTemp, row.OrderDict[-2][j - 1]));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            });
        }

        public void HandleSingleScatter(List<ThSprinklerRowConnect> rowConnection, List<Point3d> pipeScatters, double connTolerance)
        {
            var ptList = SprinklerParameter.SprinklerPt.OrderBy(pt => pt.X).ToList();
            var continueConn = true;
            var objs = rowConnection.Select(row => row.Base).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            int coefficient = 2;
            while (continueConn && coefficient <= 8)
            {
                continueConn = false;
                for (int i = 0; i < ptList.Count; i++)
                {
                    if(i == 121)
                    {

                    }
                    if (SprinklerSearched.Contains(ptList[i]) || pipeScatters.Contains(ptList[i]))
                    {
                        continue;
                    }

                    if (rowConnection.Count > 0)
                    {
                        var filter = spatialIndex.SelectCrossingPolygon(ptList[i].CreateSquare(coefficient * DTTol)).OfType<Line>().ToList();
                        if (filter.Count == 0)
                        {
                            continue;
                        }

                        var startPoints = filter.Select(line => line.StartPoint).ToList();
                        var endPoints = filter.Select(line => line.EndPoint).ToList();

                        var filterRow = rowConnection
                            .Where(row => startPoints.Contains(row.StartPoint) && endPoints.Contains(row.EndPoint)).ToList();
                        var obstacle = new DBObjectCollection();
                        var wallObstacle = new DBObjectCollection();
                        filterRow.ForEach(row => row.ConnectLines.ForEach(line => obstacle.Add(line)));
                        SprinklerParameter.SprinklerPt.ForEach(pt => obstacle.Add(new DBPoint(pt)));
                        Geometry.ForEach(geometry => wallObstacle.Add(geometry));
                        var obstacleIndex = new ThCADCoreNTSSpatialIndex(obstacle);
                        var wallIndex = new ThCADCoreNTSSpatialIndex(wallObstacle);

                        var validRow = filterRow.Where(row => row.Count < 8).ToList();
                        var map = new List<Tuple<double, Line, int>>();
                        for (int rowNum = 0; rowNum < validRow.Count; rowNum++)
                        {
                            if (validRow[rowNum].ConnectLines.Count > 0)
                            {
                                var tuple = ptList[i].GetCloseDistToLines(validRow[rowNum].ConnectLines);
                                map.Add(Tuple.Create(tuple.Item1, tuple.Item2, rowNum));
                            }
                        }
                        map = map.OrderBy(tuple => tuple.Item1).ToList();
                        for (int mapNum = 0; mapNum < map.Count; mapNum++)
                        {
                            var closePt = map[mapNum].Item2.GetClosestPointTo(ptList[i], false);
                            var closePtTemp = map[mapNum].Item2.GetClosestPointTo(ptList[i], true);
                            var lines = new List<Line>();
                            var needRemoved = false;
                            if (closePt.DistanceTo(closePtTemp) > connTolerance / 2)
                            {
                                var extendLine = new Line(closePt, closePtTemp);
                                var crossLine = new Line(closePtTemp, ptList[i]);
                                lines.Add(extendLine);
                                lines.Add(crossLine);
                            }
                            else if (map[mapNum].Item2.EndPoint.DistanceTo(closePtTemp) < connTolerance / 2)
                            {
                                var square = map[mapNum].Item2.EndPoint.CreateSquare(10.0);
                                var isSprinklerPt = false;
                                for (int count = 1; count < 9; count++)
                                {
                                    if (validRow[map[mapNum].Item3].OrderDict.ContainsKey(count))
                                    {
                                        if (square.Contains(validRow[map[mapNum].Item3].OrderDict[count][0]))
                                        {
                                            isSprinklerPt = true;
                                        }
                                    }
                                }
                                if (isSprinklerPt)
                                {
                                    needRemoved = true;
                                    closePt = map[mapNum].Item2.EndPoint - map[mapNum].Item2.LineDirection() * connTolerance;
                                    var realPtTidal = ptList[i] - map[mapNum].Item2.LineDirection() * connTolerance;
                                    lines.Add(new Line(closePt, realPtTidal));
                                    lines.Add(new Line(realPtTidal, ptList[i]));
                                }
                                else
                                {
                                    lines.Add(new Line(closePt, ptList[i]));
                                }
                            }
                            else
                            {
                                needRemoved = true;
                                lines.Add(new Line(closePt, ptList[i]));
                            }

                            lines.RemoveAll(line => line.Length < 10.0);
                            var obstacleCount = 0;
                            for (int k = 0; k < lines.Count; k++)
                            {
                                var reducedFrame = lines[k].ExtendLine(-10.0).Buffer(20.0);
                                var intersection = obstacleIndex.SelectCrossingPolygon(reducedFrame);
                                var wallIntersection = wallIndex.SelectFence(reducedFrame);
                                obstacleCount += (intersection.Count + wallIntersection.Count);
                            }
                            if (obstacleCount == 0)
                            {
                                if (needRemoved)
                                {
                                    validRow[map[mapNum].Item3].ConnectLines.Remove(map[mapNum].Item2);
                                    lines.Add(new Line(map[mapNum].Item2.StartPoint, closePt));
                                    lines.Add(new Line(closePt, map[mapNum].Item2.EndPoint));
                                }
                                lines.ForEach(line => validRow[map[mapNum].Item3].ConnectLines.Add(line));
                                validRow[map[mapNum].Item3].Count++;
                                if (validRow[map[mapNum].Item3].OrderDict.ContainsKey(10))
                                {
                                    validRow[map[mapNum].Item3].OrderDict[10].Add(ptList[i]);
                                }
                                else
                                {
                                    validRow[map[mapNum].Item3].OrderDict.Add(10, new List<Point3d> { ptList[i] });
                                }
                                pipeScatters.Add(ptList[i]);
                                continueConn = true;
                                break;
                            }
                        }
                    }
                }
                coefficient++;
            }
        }

        // 处理小房间内未处理的喷头
        public void HandleSprinklerInSmallRoom(ThSprinklerNetGroup net, int graphIdx,
            List<ThSprinklerRowConnect> rowConnection, List<Polyline> smallRooms, List<Point3d> pipeScatters, List<Polyline> obstacle)
        {
            // 给定索引所对应的图
            var graph = net.PtsGraph[graphIdx];
            // 图中的所有点位，包括虚拟点
            var graphPts = net.GetGraphPts(graphIdx);
            // 虚拟点
            var virtualPts = net.GetVirtualPts(graphIdx).OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToList();
            // 图中的所有喷淋点位
            var realPts = graphPts.Where(pt => !virtualPts.Contains(pt)).ToList();

            if (realPts.Count == 0)
            {
                return;
            }
            // 判断喷头是否位于小房间中
            var sprinklerTol = 0;
            var isSprinklerInSmallRoom = realPts[0].IsSprinklerInSmallRoom(smallRooms);
            if (isSprinklerInSmallRoom.Item1 == true)
            {
                sprinklerTol = SprinklerParameter.SprinklerPt.Where(pt => isSprinklerInSmallRoom.Item2.Contains(pt)).Count();
            }
            else
            {
                return;
            }

            var rowLines = rowConnection.Select(row => row.Base).ToCollection();
            var rowIndex = new ThCADCoreNTSSpatialIndex(rowLines);
            var pipeIndex = new ThCADCoreNTSSpatialIndex(SprinklerParameter.SubMainPipe.ToCollection());

            // 已搜索点位
            var roomCenter = realPts.GetSprinklersCenter();
            var rowFilter = rowIndex.SelectCrossingPolygon(roomCenter.CreateSquare(3 * DTTol));
            var closeRowLines = new List<Line>();
            if (rowFilter.Count > 0)
            {
                rowFilter.OfType<Line>().ForEach(line =>
                {
                    closeRowLines.Add(line);
                });
                closeRowLines = closeRowLines.OrderBy(line => line.DistanceTo(roomCenter, false)).ToList();
            }

            var pipeFilter = pipeIndex.SelectCrossingPolygon(roomCenter.CreateSquare(3 * DTTol));
            var closePipeLines = new List<Line>();
            if (pipeFilter.Count > 0)
            {
                pipeFilter.OfType<Line>().ForEach(line =>
                {
                    closePipeLines.Add(line);
                });
                closePipeLines = closePipeLines.OrderBy(line => line.DistanceTo(roomCenter, false)).ToList();
            }

            if (closeRowLines.Count + closePipeLines.Count > 0)
            {
                var startPoints = closeRowLines.Select(line => line.StartPoint).ToList();
                var endPoints = closeRowLines.Select(line => line.EndPoint).ToList();

                var filterRow = rowConnection
                    .Where(row => startPoints.Contains(row.StartPoint) && endPoints.Contains(row.EndPoint)).ToList();
                var obstacleTemp = new DBObjectCollection();
                filterRow.ForEach(row => row.ConnectLines.ForEach(line => obstacleTemp.Add(line)));
                closePipeLines.ForEach(row => obstacleTemp.Add(row));
                Geometry.ForEach(geometry => obstacle.Add(geometry));
                var obstacleIndex = new ThCADCoreNTSSpatialIndex(obstacleTemp);
                var roomIndex = new ThCADCoreNTSSpatialIndex(Geometry.ToCollection());

                var closeLines = new List<Line>();
                closeLines.AddRange(closeRowLines);
                closeLines.AddRange(closePipeLines);
                closeLines = closeLines.OrderBy(line => line.DistanceTo(roomCenter, false)).ToList();

                for (int count = 0; count < closeLines.Count; count++)
                {
                    if (closeRowLines.Contains(closeLines[count]))
                    {
                        var rowConn = rowConnection
                            .Where(row => row.Base.StartPoint == closeLines[count].StartPoint
                            && row.Base.EndPoint == closeLines[count].EndPoint).FirstOrDefault();
                        if (rowConn != null)
                        {
                            if (sprinklerTol + rowConn.Count <= 8)
                            {
                                var closePtOnRowExtend = closeLines[count].GetClosestPointTo(roomCenter, true);
                                var closePtOnRow = closeLines[count].GetClosestPointTo(closePtOnRowExtend, false);
                                var crossLine = new Line(closePtOnRow, closePtOnRowExtend);
                                // tag
                                if (crossLine.Length < 10.0)
                                {
                                    continue;
                                }
                                if (!isSprinklerInSmallRoom.Item2.Contains(closePtOnRowExtend))
                                {
                                    continue;
                                }
                                // 判断是否撞障
                                var lineBuffer = crossLine.ExtendLine(-10.0).Buffer(1.0);
                                var crossRoom = roomIndex.SelectFence(lineBuffer);
                                if (crossRoom.Count > 2)
                                {
                                    continue;
                                }
                                var intersectObstacle = obstacleIndex.SelectFence(lineBuffer);
                                if (intersectObstacle.Count > 0)
                                {
                                    continue;
                                }

                                var extendPts = new List<Point3d>();
                                var linesConn = new List<Line>();
                                for (int i = 0; i < realPts.Count; i++)
                                {
                                    if (SprinklerSearched.Contains(realPts[i])
                                        || pipeScatters.Contains(realPts[i]))
                                    {
                                        continue;
                                    }
                                    var closePt = crossLine.GetClosestPointTo(realPts[i], true);
                                    extendPts.Add(closePt);
                                    linesConn.Add(new Line(closePt, realPts[i]));
                                    SprinklerSearched.Add(realPts[i]);
                                }
                                extendPts = extendPts.DistinctPoints().OrderBy(pt => pt.DistanceTo(closePtOnRow)).ToList();
                                if (extendPts.Count > 0)
                                {
                                    linesConn.Add(new Line(closePtOnRow, extendPts[0]));
                                    for (int num = 1; num < extendPts.Count; num++)
                                    {
                                        linesConn.Add(new Line(extendPts[num], extendPts[num - 1]));
                                    }
                                    rowConn.Count += sprinklerTol;
                                    rowConn.IsSmallRoom = true;
                                    rowConn.ConnectLines.AddRange(linesConn);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (sprinklerTol <= 8)
                        {
                            var closePtOnPipe = closeLines[count].GetClosestPointTo(roomCenter, false);
                            var crossLine = new Line(closePtOnPipe, roomCenter);
                            if (!isSprinklerInSmallRoom.Item2.Contains(closePtOnPipe))
                            {
                                continue;
                            }
                            // 判断是否撞障
                            var lineBuffer = crossLine.ExtendLine(-10.0).Buffer(1.0);
                            var crossRoom = roomIndex.SelectFence(lineBuffer);
                            if (crossRoom.Count > 2)
                            {
                                continue;
                            }
                            var intersectObstacle = obstacleIndex.SelectFence(lineBuffer);
                            if (intersectObstacle.Count > 0)
                            {
                                continue;
                            }

                            var rowConn = new ThSprinklerRowConnect();
                            var extendPts = new List<Point3d>();
                            var linesConn = new List<Line>();
                            for (int i = 0; i < realPts.Count; i++)
                            {
                                if (SprinklerSearched.Contains(realPts[i])
                                    || pipeScatters.Contains(realPts[i]))
                                {
                                    continue;
                                }
                                var closePt = crossLine.GetClosestPointTo(realPts[i], true);
                                extendPts.Add(closePt);
                                linesConn.Add(new Line(closePt, realPts[i]));
                                SprinklerSearched.Add(realPts[i]);
                            }
                            extendPts = extendPts.DistinctPoints().OrderBy(pt => pt.DistanceTo(closePtOnPipe)).ToList();
                            if (extendPts.Count > 0)
                            {
                                linesConn.Add(new Line(closePtOnPipe, extendPts[0]));
                                for (int num = 1; num < extendPts.Count; num++)
                                {
                                    linesConn.Add(new Line(extendPts[num], extendPts[num - 1]));
                                }
                                rowConn.Count += sprinklerTol;
                                rowConn.IsSmallRoom = true;
                                rowConn.ConnectLines.AddRange(linesConn);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void BreakMainLine(List<Line> results)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(results.ToCollection());
            var pipes = SprinklerParameter.AllPipe;
            pipes.ForEach(p =>
            {
                var breakPts = new List<Point3d>
                {
                    p.StartPoint
                };
                var frame = p.ExtendLine(-15.0).Buffer(1.0);
                var filter = spatialIndex.SelectCrossingPolygon(frame);
                filter.OfType<Line>().ForEach(line =>
                {
                    breakPts.Add(line.GetClosestPointTo(p.StartPoint, false));
                });
                breakPts = ThSprinklerConnectTools.DistinctPoints(breakPts);
                breakPts = breakPts.OrderBy(pt => pt.DistanceTo(p.StartPoint)).ToList();
                breakPts.Add(p.EndPoint);

                for (int i = 1; i < breakPts.Count; i++)
                {
                    results.Add(new Line(breakPts[i - 1], breakPts[i]));
                }
            });
        }

        //private bool Intersection(Line line, Line other, out Point3d intersectPt)
        //{
        //    var geometry = line.ToNTSLineString().Intersection(other.ToNTSLineString());
        //    if (geometry is Point point)
        //    {
        //        intersectPt = point.ToAcGePoint3d();
        //        return true;
        //    }
        //    else
        //    {
        //        intersectPt = new Point3d();
        //        return false;
        //    }
        //}
    }
}
