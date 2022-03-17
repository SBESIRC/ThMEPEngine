using Autodesk.AutoCAD.DatabaseServices;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.SystemDiagram.Engine;

namespace ThMEPElectrical.SystemDiagram.Extension
{
    public static class ThAFASGraphExtension
    {
        public static void AddEdgeAndVertex(this AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>> graph, Entity startEntity, Entity secondEntity, List<Curve> edge)
        {
            ThAFASVertex Source = graph.Vertices.Where(o => o.VertexElement == startEntity).First();
            var Target = new ThAFASVertex() { VertexElement = secondEntity, IsStartVertexOfGraph = false };
            graph.AddVertex(Target);
            graph.AddEdge(new ThAFASEdge<ThAFASVertex>(Source, Target) { Edge = edge });
        }
    }
}
