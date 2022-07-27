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



            return netList;
        }

        public static void CorrectGraphConnection(ref List<ThSprinklerNetGroup> netList, double tolerance = 45.0)
        {
            foreach(ThSprinklerNetGroup net in netList)
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



    }
}
