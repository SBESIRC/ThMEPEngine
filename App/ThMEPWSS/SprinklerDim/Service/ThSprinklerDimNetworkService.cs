using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;


namespace ThMEPWSS.ThSprinklerDim.Service
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
                        var lines = net.GetGraphLines(i);
                        var dict = new Dictionary<int, int>();
                        for (int ptI = 0; ptI < pts.Count(); ptI++)
                        {
                            dict.Add(net.Pts.IndexOf(pts[ptI]), ptI);
                        }

                        var newNet = new ThSprinklerNetGroup();
                        var newGraph = new ThSprinklerGraph();
                        newNet.Angle = net.Angle;
                        newNet.Lines.AddRange(lines);
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
    }
}
