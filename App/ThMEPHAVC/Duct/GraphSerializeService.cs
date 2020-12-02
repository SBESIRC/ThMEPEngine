using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuickGraph;

namespace ThMEPHVAC.Duct
{
    class GraphSerializeService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly GraphSerializeService instance = new GraphSerializeService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static GraphSerializeService() { }
        internal GraphSerializeService() { }
        public static GraphSerializeService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public string GetJsonStringFromGraph(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> graph)
        {
            var graphedges = graph.Edges.ToList();
            var graphvertexes = graph.Vertices.ToList();
            return JsonConvert.SerializeObject(graphedges, Newtonsoft.Json.Formatting.Indented);
        }

        public List<ThDuctEdge<ThDuctVertex>> GetEdgesListFromJsonString(string graphjson)
        {
            return JsonConvert.DeserializeObject<List<ThDuctEdge<ThDuctVertex>>>(graphjson);
        }

        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> GetGraphFromJsonString(string graphjson)
        {
            var edges = GetEdgesListFromJsonString(graphjson);
            var targetvertexs = edges.Select(e => e.Target).ToList();
            var sourcevertexs = edges.Select(e => e.Source).ToList();
            sourcevertexs.RemoveAll(s => targetvertexs.Any(t => t.XPosition == s.XPosition && t.YPosition == s.YPosition));
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> graphrebuild = new AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>();
            
            RebuildGraph(sourcevertexs.First(), edges,graphrebuild,true);
            return graphrebuild;
        }

        private void RebuildGraph(ThDuctVertex startvertex, List<ThDuctEdge<ThDuctVertex>> edges, AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> graph, bool iffirstsearch)
        {
            var searchedges = edges.Where(e=> startvertex.IsSameVertexTo(e.Source)).ToList();
            if (searchedges.Count == 0)
            {
                return;
            }
            foreach (var edge in searchedges)
            {
                var target = edge.Target;
                var source = new ThDuctVertex();
                if (iffirstsearch)
                {
                    source = edge.Source;
                }
                else
                {
                    source = graph.Vertices.Where(v => v.IsSameVertexTo(edge.Source)).First();
                }

                var addedge = new ThDuctEdge<ThDuctVertex>(source, target)
                {
                    DraughtInfomation = edge.DraughtInfomation,
                    AirVolume = edge.AirVolume,
                    TotalVolumeInEdgeChain = edge.TotalVolumeInEdgeChain,
                    EdgeLength = edge.EdgeLength,
                    DraughtCount = edge.DraughtCount
                };
                graph.AddVertex(source);
                graph.AddVertex(target);
                graph.AddEdge(addedge);
                RebuildGraph(edge.Target, edges,graph,false);
            }
        }

    }
}
