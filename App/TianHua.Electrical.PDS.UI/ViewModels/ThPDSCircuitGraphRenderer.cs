using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public abstract class ThPDSCircuitGraphRenderer
    {
        public abstract void Render(
            AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph,
            ThPDSProjectGraphNode node, ThPDSCircuitGraphRenderContext context);
    }
}
