using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;

namespace ThMEPHAVC.Duct
{
    public class ThDuctDrawEngine
    {
        private AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> Graph { get; set; }
        public List<List<ThDuctEdge<ThDuctVertex>>> DraughtEndEdges { get; set; }

        public ThDuctDrawEngine(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> graph, List<List<ThDuctEdge<ThDuctVertex>>> draughtendedges)
        {
            Graph = graph;
            DraughtEndEdges = draughtendedges;
        }

        public void DrawDuctsWithoutDraught()
        {

        }
    }
}
