using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerNetGraphService
    {
        /// <summary>
        /// 组转图
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> ConvertToNet(List<KeyValuePair<double, List<Line>>> groupList)
        {
            var netList = new List<ThSprinklerNetGroup>();
            for (int i = 0; i < groupList.Count; i++)
            {
                var net = ThSprinklerNetGraphService.CreateNetwork(groupList[i].Key, groupList[i].Value);
                netList.Add(net);
            }

            return netList;
        }

        /// <summary>
        /// 一组线转成图组
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="groupLine"></param>
        /// <returns></returns>
        //public static ThSprinklerNetGroup CreateNetwork(double angle, List<Line> groupLine)
        //{
        //    var tol = new Tolerance(10, 10);
        //    var lines = new List<Line>();
        //    var graphListTemp = new List<ThSprinklerGraph>();

        //    var pts = ThSprinklerLineService.LineListToPtList(groupLine);
        //    lines.AddRange(groupLine);

        //    var net = new ThSprinklerNetGroup();
        //    net.Angle = angle;
        //    var alreadyAdded = new List<Line>();
        //    while (pts.Count > 0)
        //    {
        //        var graph = new ThSprinklerGraph();
        //        graphListTemp.Add(graph);

        //        var p = pts[0];
        //        CreateGraph(p, lines, alreadyAdded, net, graph);
        //        pts.RemoveAll(x => IsContains(x, net.Pts));
        //    }

        //    net.PtsGraph.AddRange(graphListTemp.OrderByDescending(x => x.SprinklerVertexNodeList.Count).ToList());
        //    return net;
        //}

        /// <summary>
        /// 成图
        /// </summary>
        /// <param name="p"></param>
        /// <param name="lines"></param>
        /// <param name="alreadyAdded"></param>
        /// <param name="net"></param>
        /// <param name="graph"></param>
        //private static void CreateGraph(Point3d p, List<Line> lines, List<Line> alreadyAdded, ThSprinklerNetGroup net, ThSprinklerGraph graph)
        //{
        //    var tol = new Tolerance(10, 10);

        //    var connL = ThSprinklerLineService.GetConnLine(p, lines, tol);

        //    for (int i = 0; i < connL.Count; i++)
        //    {
        //        if (alreadyAdded.Contains(connL[i]) == false)
        //        {
        //            var ptOther = connL[i].StartPoint;
        //            if (p.IsEqualTo(connL[i].StartPoint, tol))
        //            {
        //                ptOther = connL[i].EndPoint;
        //            }
        //            AddEdge(p, connL[i], net, graph);
        //            alreadyAdded.Add(connL[i]);

        //            lines.RemoveAll(x => x == connL[i]);

        //            CreateGraph(ptOther, lines, alreadyAdded, net, graph);
        //        }
        //    }
        //}


        public static ThSprinklerNetGroup CreateNetwork(double angle, List<Line> groupLine)
        {
            var tol = new Tolerance(10, 10);
            var lines = new List<Line>();
            var graphListTemp = new List<ThSprinklerGraph>();

            var pts = ThSprinklerLineService.LineListToPtList(groupLine);
            lines.AddRange(groupLine);

            var linesObj = lines.ToCollection();
            var linesIdx = new ThCADCoreNTSSpatialIndex(linesObj);

            var net = new ThSprinklerNetGroup();
            net.Angle = angle;
            var alreadyAdded = new List<Line>();
            while (pts.Count > 0)
            {
                var graph = new ThSprinklerGraph();
                graphListTemp.Add(graph);

                var p = pts[0];
                CreateGraph(p, linesIdx, alreadyAdded, net, graph);
                pts.RemoveAll(x => IsContains(x, net.Pts));
            }

            net.PtsGraph.AddRange(graphListTemp.OrderByDescending(x => x.SprinklerVertexNodeList.Count).ToList());
            return net;
        }


        private static void CreateGraph(Point3d p, ThCADCoreNTSSpatialIndex lineListIdx, List<Line> alreadyAdded, ThSprinklerNetGroup net, ThSprinklerGraph graph)
        {
            var tol = new Tolerance(10, 10);

            var connL = ThSprinklerLineService.GetConnLine(p, lineListIdx, 10);

            for (int i = 0; i < connL.Count; i++)
            {
                if (alreadyAdded.Contains(connL[i]) == false)
                {
                    var ptOther = connL[i].StartPoint;
                    if (p.IsEqualTo(connL[i].StartPoint, tol))
                    {
                        ptOther = connL[i].EndPoint;
                    }
                    AddEdge(p, connL[i], net, graph);
                    alreadyAdded.Add(connL[i]);

                    //lines.RemoveAll(x => x == connL[i]);

                    CreateGraph(ptOther, lineListIdx, alreadyAdded, net, graph);
                }
            }
        }



        /// <summary>
        /// 图加边
        /// </summary>
        /// <param name="p"></param>
        /// <param name="l"></param>
        /// <param name="net"></param>
        /// <param name="graph"></param>
        private static void AddEdge(Point3d p, Line l, ThSprinklerNetGroup net, ThSprinklerGraph graph)
        {
            var tol = new Tolerance(10, 10);

            var ptOther = l.StartPoint;
            if (p.IsEqualTo(l.StartPoint, tol))
            {
                ptOther = l.EndPoint;
            }
            var idxPt = net.AddPt(p);
            var idxPtO = net.AddPt(ptOther);
            //net.Lines.Add(l);


            graph.AddVertex(idxPt);
            graph.AddVertex(idxPtO);
            graph.AddEdge(idxPt, idxPtO);
            graph.AddEdge(idxPtO, idxPt);

        }

        //public static ThSprinklerNetGroup CreatePartGroup(ThSprinklerNetGroup netGroup, List<Line> mainPipe, List<Line> subMainPipe)
        //{
        //    var lineList = new List<Line>();
        //    lineList.AddRange(netGroup.Lines);
        //    RemoveLineIntersectWithMain(lineList, mainPipe);

        //    BreakLineIntersectWithSub(lineList, subMainPipe, out var breakLine, out var breakPoint);
        //    var newNetGroup = CreateNetwork(netGroup.Angle, lineList);

        //    AddBreakLineToGraph(newNetGroup, breakLine, breakPoint);
        //    return newNetGroup;
        //}

        ///// <summary>
        ///// 删掉干管相交线
        ///// </summary>
        ///// <param name="lineList"></param>
        ///// <param name="mainPipe"></param>
        ///// <returns></returns>
        //private static List<Line> RemoveLineIntersectWithMain(List<Line> lineList, List<Line> mainPipe)
        //{
        //    var removeList = new List<Line>();

        //    foreach (var l in lineList)
        //    {
        //        foreach (var main in mainPipe)
        //        {
        //            var pts = new Point3dCollection();
        //            l.IntersectWith(main, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
        //            if (pts.Count > 0)
        //            {
        //                removeList.Add(l);
        //                break;
        //            }
        //        }
        //    }

        //    lineList.RemoveAll(x => removeList.Contains(x));
        //    return removeList;
        //}

        ///// <summary>
        ///// breakLine ：原点位 到 打断点
        ///// 打断支干管相交线
        ///// </summary>
        ///// <param name="lineList"></param>
        ///// <param name="subMainPipe"></param>
        ///// <param name="breakLine"></param>
        ///// <param name="breakPoint"></param>
        //private static void BreakLineIntersectWithSub(List<Line> lineList, List<Line> subMainPipe, out List<Line> breakLine, out List<Point3d> breakPoint)
        //{
        //    breakLine = new List<Line>();
        //    breakPoint = new List<Point3d>();
        //    var breakLineRemove = new List<Line>();

        //    foreach (var l in lineList)
        //    {
        //        foreach (var main in subMainPipe)
        //        {
        //            var breakPt = new Point3dCollection();
        //            l.IntersectWith(main, Intersect.OnBothOperands, breakPt, (IntPtr)0, (IntPtr)0);
        //            if (breakPt.Count > 0)
        //            {
        //                breakPoint.Add(breakPt[0]);

        //                var first = new Line(l.StartPoint, breakPt[0]);
        //                if (first.Length > 1.0)
        //                {
        //                    breakLine.Add(first);
        //                }

        //                var second = new Line(l.EndPoint, breakPt[0]);
        //                if (second.Length > 1.0)
        //                {
        //                    breakLine.Add(second);
        //                }

        //                breakLineRemove.Add(l);
        //            }
        //        }
        //    }

        //    lineList.RemoveAll(x => breakLineRemove.Contains(x));
        //}

        ///// <summary>
        ///// 增加打断线到图组
        ///// </summary>
        ///// <param name="net"></param>
        ///// <param name="breakLine"></param>
        ///// <param name="breakPoint"></param>
        //private static void AddBreakLineToGraph(ThSprinklerNetGroup net, List<Line> breakLine, List<Point3d> breakPoint)
        //{
        //    net.PtsVirtual.AddRange(breakPoint);

        //    for (int i = 0; i < net.PtsGraph.Count; i++)
        //    {
        //        var graph = net.PtsGraph[i];

        //        for (int j = breakLine.Count - 1; j >= 0; j--)
        //        {
        //            var bkL = breakLine[j];
        //            var ptIdx = net.Pts.IndexOf(bkL.StartPoint);
        //            var VertexIdx = graph.SearchNodeIndex(ptIdx);
        //            if (VertexIdx != -1)
        //            {
        //                AddEdge(bkL.StartPoint, bkL, net, graph);
        //                breakLine.RemoveAt(j);
        //            }
        //        }
        //    }

        //    for (int i = breakLine.Count - 1; i >= 0; i--)
        //    {
        //        var bkL = breakLine[i];

        //        var graph = new ThSprinklerGraph();
        //        AddEdge(bkL.StartPoint, bkL, net, graph);
        //        net.PtsGraph.Add(graph);

        //        breakLine.RemoveAt(i);
        //    }
        //}

        private static bool IsContains(Point3d pt, List<Point3d> ptList)
        {
            var tol = new Tolerance(10, 10);
            var i = 0;
            for (; i < ptList.Count; i++)
            {
                if (pt.IsEqualTo(ptList[i], tol))
                {
                    break;
                }
            }
            if (i == ptList.Count)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //public static List<ThSprinklerNetGroup> SeparateGraph(List<ThSprinklerNetGroup> netList)
        //{
        //    var newNetList = new List<ThSprinklerNetGroup>();
        //    foreach (var net in netList)
        //    {
        //        if (net.PtsGraph.Count() == 1)
        //        {
        //            newNetList.Add(net);
        //        }
        //        else
        //        {
        //            for (int i = 0; i < net.PtsGraph.Count(); i++)
        //            {
        //                var pts = net.GetGraphPts(i);
        //                var dict = new Dictionary<int, int>();
        //                for (int ptI = 0; ptI < pts.Count(); ptI++)
        //                {
        //                    dict.Add(net.Pts.IndexOf(pts[ptI]), ptI);
        //                }

        //                var newNet = new ThSprinklerNetGroup();
        //                var newGraph = new ThSprinklerGraph();
        //                newNet.Angle = net.Angle;
        //                newNet.Pts.AddRange(pts);
        //                newNet.PtsGraph.Add(newGraph);

        //                var oriGraph = net.PtsGraph[i];
        //                for (int j = 0; j < oriGraph.SprinklerVertexNodeList.Count(); j++)
        //                {
        //                    var idxPt = dict[oriGraph.SprinklerVertexNodeList[j].NodeIndex];
        //                    newGraph.AddVertex(idxPt);

        //                    var node = oriGraph.SprinklerVertexNodeList[j].FirstEdge;
        //                    while (node != null)
        //                    {
        //                        var idxPtO = dict[oriGraph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
        //                        newGraph.AddVertex(idxPtO);
        //                        newGraph.AddEdge(idxPt, idxPtO);
        //                        newGraph.AddEdge(idxPtO, idxPt);

        //                        node = node.Next;
        //                    }
        //                }
        //                newNetList.Add(newNet);
        //            }
        //        }
        //    }

        //    return newNetList;
        //}

    }
}
