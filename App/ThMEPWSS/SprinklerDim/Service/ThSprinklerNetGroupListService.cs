using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerNetGroupListService
    {
        /// <summary>
        /// 房间重新分隔出net group
        /// </summary>
        /// <param name="netList"></param>
        /// <param name="roomsIn"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> ReGroupByRoom(List<ThSprinklerNetGroup> netList, List<MPolygon> roomsIn,out List<MPolygon> roomsOut, string printTag)
        {
            List<ThSprinklerNetGroup> newNetList = new List<ThSprinklerNetGroup>();
            roomsOut = new List<MPolygon>();
            if (roomsIn.Count > 0)
            {
                foreach (ThSprinklerNetGroup net in netList)
                {
                    // 获取所有线 与 散点
                    ThCADCoreNTSSpatialIndex linesSI = ThDataTransformService.GenerateSpatialIndex(net.GetAllLines(net.Pts));
                    List<Point3d> singlePoints = net.GetSinglePoints(net.Pts);

                    // 房间框线框住的 线与散点 重新生成net group
                    for (int i = 0; i < roomsIn.Count; i++)
                    {
                        MPolygon room = roomsIn[i];
                        if (room == null)
                            continue;

                        bool tag = false;
                        ThSprinklerNetGroup newNet = new ThSprinklerNetGroup(net.Angle);

                        // 获取window line 和 fence line
                        List<Line> selectWindowLines = ThDataTransformService.Change(ThGeometryOperationService.SelectWindowPolygon(linesSI, room));
                        List<Line> selectFenceLines = ThDataTransformService.Change(ThGeometryOperationService.SelectFence(linesSI, room));


                        // 加入满足首尾两点被框进房间的fence line到window line
                        if (selectFenceLines.Count > 0)
                        {
                            foreach (Line l in selectFenceLines)
                            {
                                if (ThGeometryOperationService.IsContained(room, l.StartPoint) && ThGeometryOperationService.IsContained(room, l.EndPoint))
                                {
                                    selectWindowLines.Add(l);
                                }
                            }

                        }

                        if (selectWindowLines.Count > 0)
                        {
                            tag = true;
                            newNet = ThSprinklerNetGraphService.CreateNetwork(net.Angle, selectWindowLines);
                        }

                        // 断房间框线也可能产生散点
                        foreach (Line l in selectFenceLines)
                        {
                            if (ThGeometryOperationService.IsContained(room, l.StartPoint))
                            {
                                tag = true;
                                newNet.AddPt(l.StartPoint);

                                // test
                                //DrawUtils.ShowGeometry(l.StartPoint, string.Format("SSS-{0}-0SinglePointProd", printTag), 11, 50, 1000);
                            }

                            if (ThGeometryOperationService.IsContained(room, l.EndPoint))
                            {
                                tag = true;
                                newNet.AddPt(l.EndPoint);

                                // test
                                //DrawUtils.ShowGeometry(l.EndPoint, string.Format("SSS-{0}-0SinglePointProd", printTag), 11, 50, 1000);
                            }

                        }

                        foreach (Point3d p in singlePoints)
                        {
                            // test
                            //DrawUtils.ShowGeometry(p, string.Format("SSS-{0}-0SinglePoint", printTag), 4, 50, 800);

                            if (ThGeometryOperationService.IsContained(room, p))
                            {
                                tag = true;
                                newNet.AddPt(p);
                            }
                        }

                        if (tag)
                        {
                            newNetList.Add(newNet);
                            roomsOut.Add(room);
                        }

                    }

                }

            }

            //test
            //for (int i = 0; i < newNetList.Count; i++)
            //{
            //    var net = newNetList[i];
            //    List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-1Room-{0}-{1}", i, j, printTag), i % 7);
            //    }
            //}


            return newNetList;
        }

        private static List<MPolygon> PreprocessRooms(List<Polyline> rooms)
        {
            // 按面积从大到小排房间
            List<MPolygon> roomList = new List<MPolygon>();
            foreach (Polyline room in rooms)
            {
                MPolygon mPolygon = ThMPolygonTool.CreateMPolygon(room);
                roomList.Add(mPolygon);
            }
            roomList.Sort((x, y) => 0 - x.Area.CompareTo(y.Area));

            for (int i = 0; i < roomList.Count; i++)
            {
                for (int j = i + 1; j < roomList.Count; j++)
                {
                    DBObjectCollection dboc = new DBObjectCollection() { roomList[j] };
                    DBObjectCollection dbocd = roomList[i].DifferenceMP(dboc);

                    if (dbocd.Count > 0)
                    {
                        if (dbocd.OfType<MPolygon>().Count() > 0)
                        {
                            roomList[i] = dbocd.OfType<MPolygon>().OrderByDescending(x => x.Area).FirstOrDefault();
                        }
                        else
                        {
                            roomList[i] = ThMPolygonTool.CreateMPolygon(dbocd.OfType<Polyline>().FirstOrDefault());
                        }
                    }
                    else
                    {
                        roomList[i] = null;
                        break;
                    }

                }
            }

            return roomList;
        }



        /// <summary>
        /// 把喷淋区转换到正交坐标系
        /// </summary>
        /// <param name="netList"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> ChangeToOrthogonalCoordinates(List<ThSprinklerNetGroup> netList)
        {
            List<ThSprinklerNetGroup> transNetList = new List<ThSprinklerNetGroup>();

            foreach (ThSprinklerNetGroup net in netList)
            {
                List<Point3d> pts = net.Pts;
                Matrix3d transformer = ThCoordinateService.GetCoordinateTransformer(new Point3d(0, 0, 0), pts[0], net.Angle);
                List<Point3d> transPts = ThCoordinateService.MakeTransformation(pts, transformer);

                transNetList.Add(new ThSprinklerNetGroup(transPts, net.PtsGraph, transformer));
            }

            return transNetList;
        }



        /// <summary>
        /// 正交坐标系下容差45mm以上视为不共线，需要断开
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="tolerance"></param>
        public static void CorrectGraphConnection(ref List<ThSprinklerNetGroup> transNetList, double tolerance = 45.0)
        {
            foreach(ThSprinklerNetGroup net in transNetList)
            {
                List<Point3d> pts = net.Pts;
                foreach(ThSprinklerGraph graph in net.PtsGraph)
                {
                    List<ThSprinklerVertexNode> nodeList = graph.SprinklerVertexNodeList;
                    foreach(ThSprinklerVertexNode node in nodeList)
                    {
                        Point3d currentPt = pts[node.PtIndex];
                        var edge = node.FirstEdge;
                        while (edge != null)
                        {
                            Point3d connectPt = pts[nodeList[edge.NodeIndex].PtIndex];
                            if(Math.Abs(currentPt.X-connectPt.X) > tolerance && Math.Abs(currentPt.Y - connectPt.Y) > tolerance)
                            {
                                graph.DeleteEdge(node.PtIndex, nodeList[edge.NodeIndex].PtIndex);
                                graph.DeleteEdge(nodeList[edge.NodeIndex].PtIndex, node.PtIndex);
                            }

                            edge = edge.Next;
                        }

                    }

                }
                
            }

        }



        /// <summary>
        /// 生成共线且相连的组，共线若不相连形成多组
        /// </summary>
        /// <param name="transNetList"></param>
        public static void GenerateCollineationGroup(ref List<ThSprinklerNetGroup> transNetList)
        {
            foreach(ThSprinklerNetGroup netGroup in transNetList)
            {
                List<Point3d> pts = netGroup.Pts;

                netGroup.XCollineationGroup.Clear();
                netGroup.YCollineationGroup.Clear();

                foreach (ThSprinklerGraph graph in netGroup.PtsGraph)
                {
                    netGroup.XCollineationGroup.Add(GetCollineationGroup(pts, graph, true));
                    netGroup.YCollineationGroup.Add(GetCollineationGroup(pts, graph, false));
                }

            }
        }

        private static List<List<int>> GetCollineationGroup(List<Point3d> pts, ThSprinklerGraph graph, bool isXAxis)
        {
            List<List<int>> collineationList = new List<List<int>>();
            bool[] isContained = Enumerable.Repeat(false, pts.Count).ToArray();

            for (int i = 0; i < pts.Count; i++)
            {
                if (!isContained[i])
                {
                    isContained[i] = true;
                    List<int> collineation = GetCollineationGroup(ref isContained, i, pts, graph, isXAxis);
                    if(collineation!= null && collineation.Count > 0)
                    {
                        collineationList.Add(collineation);
                    }
                    
                }
            }

            collineationList.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x[0]], isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y[0]], isXAxis)));
            return collineationList;

        }

        private static List<int> GetCollineationGroup(ref bool[] isContained, int ptIndex, List<Point3d> pts, ThSprinklerGraph graph, bool isXAxis, double tolerance = 45.0)
        {
            if (graph.SearchNodeIndex(ptIndex) == -1)
                return null;

            List<int> collineation = new List<int> { ptIndex};
            List<int> nodeIndexs = new List<int> { graph.SearchNodeIndex(ptIndex) };
            while(nodeIndexs.Count > 0)
            {
                List<int> tmp = new List<int>();

                foreach(int nodeIndex in nodeIndexs)
                {
                    if(nodeIndex != -1)
                    {
                        int iPtIndex = graph.SprinklerVertexNodeList[nodeIndex].PtIndex;
                        var edge = graph.SprinklerVertexNodeList[nodeIndex].FirstEdge;
                        while (edge != null)
                        {
                            int jPtIndex = graph.SprinklerVertexNodeList[edge.NodeIndex].PtIndex;
                            double det = ThCoordinateService.GetOriginalValue(pts[iPtIndex], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[jPtIndex], !isXAxis);
                            if (!isContained[jPtIndex] && Math.Abs(det) > tolerance)
                            {
                                isContained[jPtIndex] = true;
                                collineation.Add(jPtIndex);
                                tmp.Add(graph.SearchNodeIndex(jPtIndex));
                            }
                            edge = edge.Next;
                        }
                    }
                    
                }

                nodeIndexs = tmp;
            }

            collineation.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], !isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], !isXAxis)));
            return collineation;
        }



        /// <summary>
        /// 生成共线且在误差内的组，共线不相连的两组，若最近距离在误差内，合成一组
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="step"></param>
        public static void GenerateCollineation(ref List<ThSprinklerNetGroup> transNetList, double step, string printTag)
        {

            foreach (ThSprinklerNetGroup netGroup in transNetList)
            {
                List<Point3d> pts = netGroup.Pts;

                netGroup.XCollineationGroup.Clear();
                netGroup.YCollineationGroup.Clear();

                foreach (ThSprinklerGraph graph in netGroup.PtsGraph)
                {
                    List<List<int>> xCollineationGroup = GetCollineationGroup(pts, graph, true);
                    List<List<int>> xCollineation = GetCollineation(pts, netGroup.LinesCuttedOffByRoomWall, xCollineationGroup, true, step);
                    netGroup.XCollineationGroup.Add(xCollineation);

                    List<List<int>> yCollineationGroup = GetCollineationGroup(pts, graph, false);
                    List<List<int>> yCollineation = GetCollineation(pts, netGroup.LinesCuttedOffByRoomWall, yCollineationGroup, false, step);
                    netGroup.YCollineationGroup.Add(yCollineation);
                }

            }

            // test
            List<Line> allLines = new List<Line>();
            foreach (ThSprinklerNetGroup netGroup in transNetList)
            {
                List<Point3d> pts = ThCoordinateService.MakeTransformation(netGroup.Pts, netGroup.Transformer.Inverse());

                foreach (List<List<int>> collineation in netGroup.XCollineationGroup)
                {
                    foreach (List<int> line in collineation)
                    {
                        for (int i = 0; i < line.Count - 1; i++)
                        {
                            allLines.Add(new Line(pts[line[i]], pts[line[i + 1]]));
                        }
                    }
                }

                foreach (List<List<int>> collineation in netGroup.YCollineationGroup)
                {
                    foreach (List<int> line in collineation)
                    {
                        for (int i = 0; i < line.Count - 1; i++)
                        {
                            allLines.Add(new Line(pts[line[i]], pts[line[i + 1]]));
                        }
                    }
                }

            }
            DrawUtils.ShowGeometry(allLines, string.Format("SSS-{0}-5Line", printTag), 4);

        }

        private static List<List<int>> GetCollineation(List<Point3d> pts, HashSet<Tuple<int, int>> LinesCuttedOffByWall, List<List<int>> collineationGroup, bool isXAxis, double step, double tolerance=45.0)
        {
            List<List<int>> collineation = new List<List<int>>();
            bool[] isVisited = Enumerable.Repeat(false, collineationGroup.Count).ToArray();

            for (int i = 0; i < collineationGroup.Count; i++)
            {
                if (!isVisited[i])
                {
                    isVisited[i] = true;
                    bool tag = true;
                    List<int> group1 = collineationGroup[i];
                    while (tag)
                    {
                        tag = false;
                        for (int j = i + 1; j < collineationGroup.Count; j++)
                        {
                            if (!isVisited[j])
                            {
                                List<int> group2 = collineationGroup[j];
                                double collineTol1 = ThCoordinateService.GetOriginalValue(pts[group1[0]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[group2[group2.Count - 1]], isXAxis);
                                double collineTol2 = ThCoordinateService.GetOriginalValue(pts[group1[group1.Count - 1]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[group2[0]], isXAxis);

                                if (Math.Min(Math.Abs(collineTol1), Math.Abs(collineTol2)) > tolerance)// 检查是否有可能共线
                                {
                                    break;
                                }
                                else
                                {
                                    double connectTol1 = ThCoordinateService.GetOriginalValue(pts[group1[0]], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[group2[group2.Count - 1]], !isXAxis);
                                    double connectTol2 = ThCoordinateService.GetOriginalValue(pts[group1[group1.Count - 1]], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[group2[0]], !isXAxis);

                                    if (Math.Min(Math.Abs(connectTol1), Math.Abs(connectTol2)) < 1.5 * step)// 检查是否有可能合并
                                    {
                                        List<int> combinedGroup = new List<int>();
                                        combinedGroup.AddRange(group1);
                                        combinedGroup.AddRange(group2);

                                        if (IsOneLine(pts, LinesCuttedOffByWall, combinedGroup, isXAxis, step))
                                        {
                                            group1 = combinedGroup;
                                            isVisited[j] = true;
                                            tag = true;
                                        }

                                    }

                                }

                            }

                        }

                    }

                    collineation.Add(group1);
                }
            }

            return collineation;
        }

        private static bool IsOneLine(List<Point3d> pts, HashSet<Tuple<int, int>> LinesCuttedOffByWall, List<int> line, bool isXAxis, double step, double tolerance=45.0)
        {
            line.Sort((x, y) => ThCoordinateService.GetOriginalValue(pts[x], !isXAxis).CompareTo(ThCoordinateService.GetOriginalValue(pts[y], !isXAxis)));

            for (int i = 0; i < line.Count-1; i++)
            {
                if(LinesCuttedOffByWall.Contains(new Tuple<int, int>(line[i], line[i + 1])) || LinesCuttedOffByWall.Contains(new Tuple<int, int>(line[i + 1], line[i])))
                    return false;

                double collineTol = ThCoordinateService.GetOriginalValue(pts[line[i+1]], isXAxis) - ThCoordinateService.GetOriginalValue(pts[line[i]], isXAxis);
                double connectTol = ThCoordinateService.GetOriginalValue(pts[line[i+1]], !isXAxis) - ThCoordinateService.GetOriginalValue(pts[line[i]], !isXAxis);

                if (Math.Abs(collineTol) > tolerance || connectTol > 1.5 * step)
                    return false;

            }

            return true;
        }



        /// <summary>
        /// 根据房间、墙断开不能连接的线，并记录下来
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="mixRoomWall"></param>
        /// <param name="printTag"></param>
        public static void CutOffLinesCrossRoomOrWall(List<ThSprinklerNetGroup> transNetList, List<Polyline> mixRoomWall, ThCADCoreNTSSpatialIndex mixRoomWallSI, string printTag)
        {

            for (int idx = 0; idx < transNetList.Count; idx++)
            {
                ThSprinklerNetGroup net = transNetList[idx];
                net.LinesCuttedOffByRoomWall.Clear();
                List<Point3d> pts = ThCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());

                //找出图与墙相交的线
                ThCADCoreNTSSpatialIndex graphLinesSI = ThDataTransformService.GenerateSpatialIndex(net.GetAllLines(pts));
                List<Line> crossWallLines = ThDataTransformService.Change(ThGeometryOperationService.SelectFence(graphLinesSI, mixRoomWall));

                //判断相交线是否需要断开
                foreach (Line line in crossWallLines)
                {
                    if (ThSprinklerDimConflictService.NeedToCutOff(line, mixRoomWallSI))
                    {
                        int i = pts.IndexOf(line.StartPoint);
                        int j = pts.IndexOf(line.EndPoint);
                        net.LinesCuttedOffByRoomWall.Add(new Tuple<int, int>(i, j));
                        net.LinesCuttedOffByRoomWall.Add(new Tuple<int, int>(j, i));

                        foreach (ThSprinklerGraph graph in net.PtsGraph)
                        {
                            graph.DeleteEdge(i, j);
                            graph.DeleteEdge(j, i);
                        }
                    }

                }

            }

            //test
            //for (int i = 0; i < transNetList.Count; i++)
            //{
            //    var net = transNetList[i];
            //    List<Point3d> pts = ThCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-4Wall-{0}-{1}", i, j, printTag), i % 7);
            //    }
            //}
        }



        /// <summary>
        /// 断开连接较少的线
        /// </summary>
        /// <param name="transNetList"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> CutOffLinesWithFewerConnections(List<ThSprinklerNetGroup> transNetList)
        {
            List<ThSprinklerNetGroup> opNetList = new List<ThSprinklerNetGroup>();
            foreach (ThSprinklerNetGroup netGroup in transNetList)
            {
                var pts = netGroup.Pts;

                List<Line> remainingLines = new List<Line>();
                for (int i = 0; i < netGroup.PtsGraph.Count; i++)
                {
                    ThSprinklerGraph graph = netGroup.PtsGraph[i];
                    CutoffLines(pts, ref graph, netGroup.XCollineationGroup[i], true);
                    CutoffLines(pts, ref graph, netGroup.YCollineationGroup[i], false);
                    remainingLines.AddRange(graph.GetAllLines(pts));
                }
                ThSprinklerNetGroup newNetGroup = ThSprinklerNetGraphService.CreateNetwork(netGroup.Angle, remainingLines);
                newNetGroup.Transformer = netGroup.Transformer;

                // 加入散点
                List<Point3d> singlePoints = netGroup.GetSinglePoints(pts);
                foreach (Point3d point in singlePoints)
                {
                    int ptIndex = newNetGroup.AddPt(point);
                    ThSprinklerGraph g = new ThSprinklerGraph();
                    g.AddVertex(ptIndex);
                    newNetGroup.PtsGraph.Add(g);
                }

                opNetList.Add(newNetGroup);
            }

            return opNetList;
        }

        private static void CutoffLines(List<Point3d> pts, ref ThSprinklerGraph graph, List<List<int>> collineationList, bool isXAxis)
        {
            // 共线中把相距较近的形成一组
            foreach (List<int> group in collineationList)
            {
                // 找出与组相连的组  connectListDict（key存collineationList中的index  value存相连的PtIndex，value[0]存前者 value[1]存后者）
                Dictionary<int, List<List<int>>> connectListDict = GetConnectListDict(group, pts, graph, collineationList, isXAxis);


                // 计算当前组与后面各组连接的百分比，并反向求百分比，计算二者均值
                foreach (KeyValuePair<int, List<List<int>>> kv in connectListDict)
                {
                    List<List<int>> connectList = kv.Value;
                    List<int> backwardGroup = collineationList[kv.Key];
                    IEnumerable<int> connectBackPtIndexList = backwardGroup.Intersect(connectList[1]);

                    double forwardConnectionPercentage = 1.0 * connectBackPtIndexList.Count() / group.Count;
                    double backwardConnectionPercentage = 1.0 * connectBackPtIndexList.Count() / backwardGroup.Count;
                    if ((forwardConnectionPercentage + backwardConnectionPercentage) / 2.0 <= 1.0 / 3)
                    {
                        // 断开均值小于等于1/3的两组
                        foreach (int connectBackPtIndex in connectBackPtIndexList)
                        {
                            int connectPrePtIndex = connectList[0][connectList[1].IndexOf(connectBackPtIndex)];
                            graph.DeleteEdge(connectPrePtIndex, connectBackPtIndex);
                            graph.DeleteEdge(connectBackPtIndex, connectPrePtIndex);
                        }

                    }

                }

            }

        }

        private static Dictionary<int, List<List<int>>> GetConnectListDict(List<int> group, List<Point3d> pts, ThSprinklerGraph graph, List<List<int>> collineationList, bool isXAxis, double tolerance = 45.0)
        {
            Dictionary<int, List<List<int>>> connectListDict = new Dictionary<int, List<List<int>>>();
            foreach (int iPtIndex in group)
            {
                var edge = graph.SprinklerVertexNodeList[graph.SearchNodeIndex(iPtIndex)].FirstEdge;
                while (edge != null)
                {
                    int jPtIndex = graph.SprinklerVertexNodeList[edge.NodeIndex].PtIndex;
                    double det = ThCoordinateService.GetOriginalValue(pts[jPtIndex], isXAxis) - ThCoordinateService.GetOriginalValue(pts[iPtIndex], isXAxis);
                    if (det > tolerance)
                    {
                        break;
                    }

                    edge = edge.Next;
                }

                if (edge != null)
                {
                    int jPtIndex = graph.SprinklerVertexNodeList[edge.NodeIndex].PtIndex;
                    int collineationListIndex = GetCollineationListIndex(collineationList, jPtIndex);
                    if (collineationListIndex != -1)
                    {
                        if (connectListDict.ContainsKey(collineationListIndex))
                        {
                            List<List<int>> listDict = connectListDict[collineationListIndex];
                            listDict[0].Add(iPtIndex);
                            listDict[1].Add(jPtIndex);
                        }
                        else
                        {
                            connectListDict.Add(collineationListIndex, new List<List<int>> { new List<int> { iPtIndex }, new List<int> { jPtIndex } });
                        }
                    }

                }
            }
            return connectListDict;
        }

        private static int GetCollineationListIndex(List<List<int>> collineationList, int ptIndex)
        {
            for (int i = 0; i < collineationList.Count; i++)
            {
                if (collineationList[i].Contains(ptIndex))
                    return i;
            }

            return -1;
        }


    }
}
