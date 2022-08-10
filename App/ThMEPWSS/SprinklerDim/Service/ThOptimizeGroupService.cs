using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThOptimizeGroupService
    {
        /// <summary>
        /// 断环算法的优化，断开连接较少的两根线之间的所有连线，形成多个图
        /// </summary>
        /// <param name="netList"></param>
        /// <param name="printTag"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> GetSprinklerPtOptimizedNet(List<ThSprinklerNetGroup> netList, double step, string printTag)
        {
            List<ThSprinklerNetGroup> transNetList = ThSprinklerNetGroupListService.ChangeToOrthogonalCoordinates(netList);
            ThSprinklerNetGroupListService.CorrectGraphConnection(ref transNetList, 45.0);

            // test
            //for (int i = 0; i < transNetList.Count; i++)
            //{
            //    var net = transNetList[i];
            //    List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-245mm-{0}-{1}", i, j, printTag), i % 7);
            //    }
            //}


            ThSprinklerNetGroupListService.GenerateCollineationGroup(ref transNetList);
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
                    remainingLines.AddRange(graph.Print(pts));
                }
                ThSprinklerNetGroup newNetGroup = ThSprinklerNetGraphService.CreateNetwork(netGroup.Angle, remainingLines);
                newNetGroup.Transformer = netGroup.Transformer;

                // 加入散点
                List<Point3d> singlePoints = GetSinglePoints(pts, netGroup.PtsGraph);
                foreach(Point3d point in singlePoints)
                {
                    int ptIndex = newNetGroup.AddPt(point);
                    ThSprinklerGraph g = new ThSprinklerGraph();
                    g.AddVertex(ptIndex);
                    newNetGroup.PtsGraph.Add(g);
                }

                opNetList.Add(newNetGroup);
            }

            // test
            //for (int i = 0; i < opNetList.Count; i++)
            //{
            //    var net = opNetList[i];
            //    List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
            //    for (int j = 0; j < net.PtsGraph.Count; j++)
            //    {
            //        var lines = net.PtsGraph[j].Print(pts);
            //        DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-3OpNet-{0}-{1}", i, j, printTag), i % 7);
            //    }
            //}

            return opNetList;
        }

        public static List<Point3d> GetSinglePoints(List<Point3d> pts, List<ThSprinklerGraph> graphs)
        {
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < pts.Count; i++)
            {
                bool tag = true;
                foreach (ThSprinklerGraph graph in graphs)
                {
                    int nodeIndex = graph.SearchNodeIndex(i);
                    if (nodeIndex != -1 && graph.SprinklerVertexNodeList[nodeIndex].FirstEdge != null)
                    {
                        tag = false;
                        break;
                    }
                }

                if (tag)
                {
                    points.Add(pts[i]);
                }
            }

            return points;
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

        private static Dictionary<int, List<List<int>>> GetConnectListDict(List<int> group, List<Point3d> pts, ThSprinklerGraph graph, List<List<int>> collineationList, bool isXAxis, double tolerance=45.0)
        {
            Dictionary<int, List<List<int>>> connectListDict = new Dictionary<int, List<List<int>>>();
            foreach (int iPtIndex in group)
            {
                var edge = graph.SprinklerVertexNodeList[graph.SearchNodeIndex(iPtIndex)].FirstEdge;
                while (edge != null)
                {
                    int jPtIndex = graph.SprinklerVertexNodeList[edge.EdgeIndex].NodeIndex;
                    double det = ThChangeCoordinateService.GetOriginalValue(pts[jPtIndex], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[iPtIndex], isXAxis);
                    if (det > tolerance)
                    {
                        break;
                    }

                    edge = edge.Next;
                }

                if (edge != null)
                {
                    int jPtIndex = graph.SprinklerVertexNodeList[edge.EdgeIndex].NodeIndex;
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



        //public static void CutoffLines(List<Point3d> pts, ref ThSprinklerGraph graph, bool isXAxis, double DTTol)
        //{
        //    // 共线的形成一组
        //    Dictionary<long, List<int>> collineationDic = GetCollineationDic(pts, isXAxis);

        //    // Dictionary<long, List<int>> collineationDicSortedDic = from objDic in collineationDic orderby objDic.Key select objDic;
        //    foreach (KeyValuePair<long, List<int>> kvp in collineationDic)
        //    {
        //        long xCurrent = kvp.Key;
        //        List<int> collineationList = kvp.Value;

        //        // 从共线中把相距较近的形成组中组
        //        List<List<int>> collineationGroupList = GetGroupsFromCollineation(pts, graph, collineationList, !isXAxis);
        //        foreach (List<int> group in collineationGroupList)
        //        {
        //            // 找出组中组 相连的 组(connectListDict.value中存相连的PtIndex，value[0]存组中组 value[1]存组)
        //            Dictionary<long, List<List<int>>> connectListDict = GetConnectListDict(group, pts, graph, isXAxis);


        //            // 计算（共线且相距较近的）组中组与后面各组中组连接的百分比，并反向求百分比，计算二者均值
        //            foreach (KeyValuePair<long, List<List<int>>> kv in connectListDict)
        //            {
        //                List<List<int>> connectList = kv.Value;

        //                List<int> backwardCollineationList = collineationDic[kv.Key];
        //                List<List<int>> backwardCollineationGroupList = GetGroupsFromCollineation(pts, graph, backwardCollineationList, !isXAxis);

        //                foreach (List<int> backwardGroup in backwardCollineationGroupList)
        //                {
        //                    IEnumerable<int> connectBackPtIndexList = backwardGroup.Intersect(connectList[1]);

        //                    double forwardConnectionPercentage = 1.0 * connectBackPtIndexList.Count() / group.Count;
        //                    double backwardConnectionPercentage = 1.0 * connectBackPtIndexList.Count() / backwardGroup.Count;
        //                    if (forwardConnectionPercentage > 0 && backwardConnectionPercentage > 0 && (forwardConnectionPercentage + backwardConnectionPercentage) / 2.0 <= 1.0 / 3)
        //                    {
        //                        // 断开均值小于等于1/3的两组
        //                        foreach (int connectBackPtIndex in connectBackPtIndexList)
        //                        {
        //                            int connectPrePtIndex = connectList[0][connectList[1].IndexOf(connectBackPtIndex)];
        //                            graph.DeleteEdge(connectPrePtIndex, connectBackPtIndex);
        //                            graph.DeleteEdge(connectBackPtIndex, connectPrePtIndex);
        //                        }

        //                    }


        //                }


        //            }


        //        }

        //    }
        //}

        //public static Dictionary<long, List<int>> GetCollineationDic(List<Point3d> pts, bool isXAxis)
        //{
        //    Dictionary<long, List<int>> collineationDic = new Dictionary<long, List<int>>();
        //    for (int i = 0; i < pts.Count; i++)
        //    {
        //        var pt = pts[i];

        //        if (collineationDic.ContainsKey(ThChangeCoordinateService.GetValue(pt, isXAxis)))
        //        {
        //            var collineationList = collineationDic[ThChangeCoordinateService.GetValue(pt, isXAxis)];
        //            collineationList.Add(i);
        //        }
        //        else
        //        {
        //            collineationDic[ThChangeCoordinateService.GetValue(pt, isXAxis)] = new List<int> { i };
        //        }
        //    }

        //    return collineationDic;
        //}

        //public static List<List<int>> GetGroupsFromCollineation(List<Point3d> pts, ThSprinklerGraph graph, List<int> collineationList, bool isXAxis)
        //{
        //    collineationList.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));

        //    List<List<int>> groups = new List<List<int>>();
        //    List<int> one = new List<int> { collineationList[0] };

        //    for (int i = 1; i < collineationList.Count; i++)
        //    {
        //        int iPtIndex = one[one.Count - 1];
        //        int jPtIndex = collineationList[i];

        //        if (graph.IsConnect(iPtIndex, jPtIndex))
        //        {
        //            one.Add(jPtIndex);
        //        }
        //        else
        //        {
        //            groups.Add(one);
        //            one = new List<int> { jPtIndex };
        //        }
        //    }
        //    groups.Add(one);

        //    return groups;
        //}

        //public static Dictionary<long, List<List<int>>> GetConnectListDict(List<int> group, List<Point3d> pts, ThSprinklerGraph graph, bool isXAxis)
        //{
        //    Dictionary<long, List<List<int>>> connectListDict = new Dictionary<long, List<List<int>>>();
        //    foreach (int iPtIndex in group)
        //    {
        //        var edge = graph.SprinklerVertexNodeList[graph.SearchNodeIndex(iPtIndex)].FirstEdge;
        //        while (edge != null)
        //        {
        //            var jPtIndex = graph.SprinklerVertexNodeList[edge.EdgeIndex].NodeIndex;
        //            if (ThChangeCoordinateService.GetOriginalValue(pts[jPtIndex], isXAxis) > ThChangeCoordinateService.GetOriginalValue(pts[iPtIndex], isXAxis))
        //            {
        //                break;
        //            }

        //            edge = edge.Next;
        //        }

        //        if (edge != null)
        //        {
        //            var jPtIndex = graph.SprinklerVertexNodeList[edge.EdgeIndex].NodeIndex;
        //            if (connectListDict.ContainsKey(ThChangeCoordinateService.GetValue(pts[jPtIndex], isXAxis)))
        //            {
        //                var listDict = connectListDict[ThChangeCoordinateService.GetValue(pts[jPtIndex], isXAxis)];
        //                listDict[0].Add(iPtIndex);
        //                listDict[1].Add(jPtIndex);
        //            }
        //            else
        //            {
        //                connectListDict.Add(ThChangeCoordinateService.GetValue(pts[jPtIndex], isXAxis), new List<List<int>> { new List<int> { iPtIndex }, new List<int> { jPtIndex } });
        //            }
        //        }
        //    }
        //    return connectListDict;
        //}


    }
}
