using System;
using System.Linq;
using QuickGraph;
using System.Collections.Generic;

namespace ThMEPHAVC.Duct
{
    class ThDuctGraphAnalysisEngine
    {
        private AdjacencyGraph<ThDuctVertex, Edge<ThDuctVertex>> Graph { get; set; }
        private List<List<Edge<ThDuctVertex>>> EndLevelEdges { get; set; }

        public ThDuctGraphAnalysisEngine(AdjacencyGraph<ThDuctVertex, Edge<ThDuctVertex>> graph)
        {
            Graph = graph;

            //找到图中所有出度为0的顶点
            var endvertexs = Graph.Vertices.Where(v => Graph.OutDegree(v) == 0);
            EndLevelEdges = new List<List<Edge<ThDuctVertex>>>();
            //从出度为0的点(最后一级的最末端顶点)往回遍历
            foreach (var endvertex in endvertexs)
            {
                List<Edge<ThDuctVertex>> singleedges = new List<Edge<ThDuctVertex>>();
                FindSinleEdgeFromVertex(endvertex, singleedges);
                EndLevelEdges.Add(singleedges);
            }
        }

        private void FindSinleEdgeFromVertex(ThDuctVertex searchtarget, List<Edge<ThDuctVertex>> singleedges)
        {
            var lastedge = Graph.Edges.Where(e => e.Target.Equals(searchtarget));
            if (lastedge.Count()==0)
            {
                return;
            }
            singleedges.Add(lastedge.First());
            if (Graph.OutDegree(lastedge.First().Source) > 1)
            {
                return;
            }
            else
            {
                FindSinleEdgeFromVertex(lastedge.First().Source, singleedges);
            }
        }
    }
}
