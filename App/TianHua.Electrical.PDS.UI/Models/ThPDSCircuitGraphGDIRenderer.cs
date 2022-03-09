using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSCircuitGraphGDIRenderer : ThPDSCircuitGraphRenderer
    {
        public override void Render(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph, ThPDSProjectGraphNode node, ThPDSCircuitGraphRenderContext context)
        {
            //可以支持，暂不支持
        }
    }
}
