using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimNetworkService
    {
        public static List<ThSprinklerNetGroup> SeparateGraph(List<ThSprinklerNetGroup> netList)
        {
            var newNetList = new List<ThSprinklerNetGroup>();
            foreach (var net in netList)
            {
                if (net.PtsGraph.Count() == 1)
                {
                    newNetList.Add(net);
                }
                else
                {
                    for (int i = 0; i < net.PtsGraph.Count(); i++)
                    {
                        var pts = net.GetGraphPts(i);
                        var dict = new Dictionary<int, int>();
                        for (int ptI = 0; ptI < pts.Count(); ptI++)
                        {
                            dict.Add(net.Pts.IndexOf(pts[ptI]), ptI);
                        }

                        var newNet = new ThSprinklerNetGroup();
                        var newGraph = new ThSprinklerGraph();
                        newNet.Angle = net.Angle;
                        newNet.Transformer = net.Transformer;
                        newNet.Pts.AddRange(pts);
                        newNet.PtsGraph.Add(newGraph);

                        var oriGraph = net.PtsGraph[i];
                        for (int j = 0; j < oriGraph.SprinklerVertexNodeList.Count(); j++)
                        {
                            var idxPt = dict[oriGraph.SprinklerVertexNodeList[j].NodeIndex];
                            newGraph.AddVertex(idxPt);

                            var node = oriGraph.SprinklerVertexNodeList[j].FirstEdge;
                            while (node != null)
                            {
                                var idxPtO = dict[oriGraph.SprinklerVertexNodeList[node.EdgeIndex].NodeIndex];
                                newGraph.AddVertex(idxPtO);
                                newGraph.AddEdge(idxPt, idxPtO);
                                newGraph.AddEdge(idxPtO, idxPt);

                                node = node.Next;
                            }
                        }
                        newNetList.Add(newNet);
                    }
                }
            }

            return newNetList;
        }

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

        public static void GenerateCollineationGroup(ref List<ThSprinklerNetGroup> transNetList)
        {
            foreach(ThSprinklerNetGroup netGroup in transNetList)
            {
                List<Point3d> pts = netGroup.Pts;

                foreach(ThSprinklerGraph graph in netGroup.PtsGraph)
                {
                    netGroup.XCollineationGroup.Add(GetCollineation(pts, graph, true));
                    netGroup.YCollineationGroup.Add(GetCollineation(pts, graph, false));
                }

            }
        }

        private static List<List<int>> GetCollineation(List<Point3d> pts, ThSprinklerGraph graph, bool isXAxis)
        {
            List<List<int>> collineationList = new List<List<int>>();
            bool[] isContained = Enumerable.Repeat(false, pts.Count).ToArray();

            for (int i = 0; i < pts.Count; i++)
            {
                if (!isContained[i])
                {
                    isContained[i] = true;
                    List<int> collineation = GetCollineation(ref isContained, i, pts, graph, isXAxis);
                    if(collineation!= null && collineation.Count > 0)
                    {
                        collineationList.Add(collineation);
                    }
                    
                }
            }

            return collineationList;

        }

        private static List<int> GetCollineation(ref bool[] isContained, int ptIndex, List<Point3d> pts, ThSprinklerGraph graph, bool isXAxis, double tolerance = 45.0)
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

            return collineation;
        }


    }
}
