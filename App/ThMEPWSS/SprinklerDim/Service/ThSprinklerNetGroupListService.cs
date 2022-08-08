﻿using System;
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
        /// <param name="rooms"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> ReGroupByRoom(List<ThSprinklerNetGroup> netList, List<Polyline> rooms, string printTag)
        {
            List<MPolygon> roomList = PreprocessRooms(rooms);

            List<ThSprinklerNetGroup> newNetList = new List<ThSprinklerNetGroup>();
            if (roomList.Count > 0)
            {
                foreach (ThSprinklerNetGroup net in netList)
                {
                    // 获取所有线
                    DBObjectCollection lines = new DBObjectCollection();
                    foreach (ThSprinklerGraph graph in net.PtsGraph)
                    {
                        foreach (Line l in graph.Print(net.Pts))
                            lines.Add(l);
                    }
                    ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(lines);

                    // 房间框线框住的线重新生成net group
                    for (int i = 0; i < roomList.Count; i++)
                    {
                        MPolygon room = roomList[i];
                        if (room == null)
                            continue;

                        // 获取window line
                        List<Line> selectWindowLines = new List<Line>();
                        DBObjectCollection dbSelect = linesSI.SelectWindowPolygon(room);
                        foreach (DBObject dbo in dbSelect)
                        {
                            selectWindowLines.Add((Line)dbo);
                        }

                        if (selectWindowLines.Count > 0)
                        {
                            // 获取fence line
                            List<Line> selectFenceLines = new List<Line>();
                            DBObjectCollection tdbSelect = linesSI.SelectFence(room);
                            foreach (DBObject dbo in tdbSelect)
                            {
                                selectFenceLines.Add((Line)dbo);
                            }

                            // 加入满足首尾两点被框进房间的fence line到window line
                            if (selectFenceLines.Count > 0)
                            {
                                foreach (Line l in selectFenceLines)
                                {
                                    if (IsContained(room, l.StartPoint) && IsContained(room, l.EndPoint))
                                    {
                                        selectWindowLines.Add(l);
                                    }
                                }

                            }

                            newNetList.Add(ThSprinklerNetGraphService.CreateNetwork(net.Angle, selectWindowLines));
                        }

                    }

                }

            }
            else
                newNetList = netList;


            //test
            for (int i = 0; i < newNetList.Count; i++)
            {
                var net = newNetList[i];
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    var lines = net.PtsGraph[j].Print(pts);
                    DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-1Room-{0}-{1}", i, j, printTag), i % 7);
                }
            }


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
                    // DrawUtils.ShowGeometry(roomList[i], "SSS-1ROOM-" + i.ToString()+"-"+j.ToString());

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

        private static bool IsContained(MPolygon room, Point3d pt)
        {
            if (!ThCADCoreNTSPolygonExtension.Contains(room.Shell(), pt))
            {
                return false;
            }

            List<Polyline> holes = room.Holes();
            foreach (Polyline hole in holes)
            {
                if (ThCADCoreNTSPolygonExtension.Contains(hole, pt))
                    return false;
            }

            return true;
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
                Matrix3d transformer = ThChangeCoordinateService.GetCoordinateTransformer(new Point3d(0, 0, 0), pts[0], net.Angle);

                List<Point3d> transPts = ThChangeCoordinateService.MakeTransformation(pts, transformer);
                ThSprinklerNetGroup transGroup = new ThSprinklerNetGroup(transPts, net.PtsGraph, transformer);
                transNetList.Add(transGroup);
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
                        Point3d currentPt = pts[node.NodeIndex];
                        var edge = node.FirstEdge;
                        while (edge != null)
                        {
                            Point3d connectPt = pts[nodeList[edge.EdgeIndex].NodeIndex];
                            if(Math.Abs(currentPt.X-connectPt.X) > tolerance && Math.Abs(currentPt.Y - connectPt.Y) > tolerance)
                            {
                                graph.DeleteEdge(node.NodeIndex, nodeList[edge.EdgeIndex].NodeIndex);
                                graph.DeleteEdge(nodeList[edge.EdgeIndex].NodeIndex, node.NodeIndex);
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

            collineationList.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x[0]], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y[0]], isXAxis)));
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
                        int iPtIndex = graph.SprinklerVertexNodeList[nodeIndex].NodeIndex;
                        var edge = graph.SprinklerVertexNodeList[nodeIndex].FirstEdge;
                        while (edge != null)
                        {
                            int jPtIndex = graph.SprinklerVertexNodeList[edge.EdgeIndex].NodeIndex;
                            double det = ThChangeCoordinateService.GetOriginalValue(pts[iPtIndex], !isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[jPtIndex], !isXAxis);
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

            collineation.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], !isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], !isXAxis)));
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
                    List<List<int>> xCollineation = GetCollineation(pts, netGroup.LinesCuttedOffByWall, xCollineationGroup, true, step);
                    netGroup.XCollineationGroup.Add(xCollineation);

                    List<List<int>> yCollineationGroup = GetCollineationGroup(pts, graph, false);
                    List<List<int>> yCollineation = GetCollineation(pts, netGroup.LinesCuttedOffByWall, yCollineationGroup, false, step);
                    netGroup.YCollineationGroup.Add(yCollineation);
                }

            }

            // test
            List<Line> allLines = new List<Line>();
            foreach (ThSprinklerNetGroup netGroup in transNetList)
            {
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(netGroup.Pts, netGroup.Transformer.Inverse());

                foreach(List<List<int>> collineation in netGroup.XCollineationGroup)
                {
                    foreach(List<int> line in collineation)
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
                                double collineTol1 = ThChangeCoordinateService.GetOriginalValue(pts[group1[0]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[group2[group2.Count - 1]], isXAxis);
                                double collineTol2 = ThChangeCoordinateService.GetOriginalValue(pts[group1[group1.Count - 1]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[group2[0]], isXAxis);

                                if (Math.Min(Math.Abs(collineTol1), Math.Abs(collineTol2)) > tolerance)// 检查是否有可能共线
                                {
                                    break;
                                }
                                else
                                {
                                    double connectTol1 = ThChangeCoordinateService.GetOriginalValue(pts[group1[0]], !isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[group2[group2.Count - 1]], !isXAxis);
                                    double connectTol2 = ThChangeCoordinateService.GetOriginalValue(pts[group1[group1.Count - 1]], !isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[group2[0]], !isXAxis);

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
            line.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], !isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], !isXAxis)));

            for (int i = 0; i < line.Count-1; i++)
            {
                if(LinesCuttedOffByWall.Contains(new Tuple<int, int>(line[i], line[i + 1])) || LinesCuttedOffByWall.Contains(new Tuple<int, int>(line[i + 1], line[i])))
                    return false;

                double collineTol = ThChangeCoordinateService.GetOriginalValue(pts[line[i+1]], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[line[i]], isXAxis);
                double connectTol = ThChangeCoordinateService.GetOriginalValue(pts[line[i+1]], !isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[line[i]], !isXAxis);

                if (Math.Abs(collineTol) > tolerance || connectTol > 1.5 * step)
                    return false;

            }

            return true;
        }


        /// <summary>
        /// 根据墙断开不能连接的线，并记录下来
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="walls"></param>
        /// <param name="printTag"></param>
        public static void CutOffLinesCrossWall(List<ThSprinklerNetGroup> transNetList, List<Polyline> walls, string printTag)
        {
            for (int idx = 0; idx < transNetList.Count; idx++)
            {
                ThSprinklerNetGroup net = transNetList[idx];
                net.LinesCuttedOffByWall.Clear();
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());

                //生成所有线（图）
                DBObjectCollection graphLines = new DBObjectCollection();
                foreach (ThSprinklerGraph graph in net.PtsGraph)
                {
                    foreach (Line l in graph.Print(pts))
                        graphLines.Add(l);
                }
                ThCADCoreNTSSpatialIndex graphLinesSI = new ThCADCoreNTSSpatialIndex(graphLines);

                //找出图与墙相交线
                List<Line> crossWallLines = new List<Line>();
                foreach (Polyline wall in walls)
                {
                    DBObjectCollection dbSelect = graphLinesSI.SelectFence(wall);
                    foreach (DBObject dbo in dbSelect)
                    {
                        crossWallLines.Add((Line)dbo);
                    }

                }

                //判断相交线是否需要断开
                foreach (Line line in crossWallLines)
                {
                    if (ThSprinklerDimConflictService.IsConflicted(line, walls))
                    {
                        int i = SearchIndex(pts, line.StartPoint);
                        int j = SearchIndex(pts, line.EndPoint);
                        net.LinesCuttedOffByWall.Add(new Tuple<int, int>(i, j));
                        net.LinesCuttedOffByWall.Add(new Tuple<int, int>(j, i));

                        foreach (ThSprinklerGraph graph in net.PtsGraph)
                        {
                            graph.DeleteEdge(i, j);
                            graph.DeleteEdge(j, i);
                        }
                    }

                }

            }

            //test
            for (int i = 0; i < transNetList.Count; i++)
            {
                var net = transNetList[i];
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    var lines = net.PtsGraph[j].Print(pts);
                    DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-4Wall-{0}-{1}", i, j, printTag), i % 7);
                }
            }
        }

        private static int SearchIndex(List<Point3d> pts, Point3d pt)
        {
            return pts.IndexOf(pt);
        }


    }
}
