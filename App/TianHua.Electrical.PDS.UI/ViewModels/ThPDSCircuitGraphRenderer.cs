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
            BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph,
            ThPDSProjectGraphNode node, ThPDSCircuitGraphRenderContext context);
    }
}
