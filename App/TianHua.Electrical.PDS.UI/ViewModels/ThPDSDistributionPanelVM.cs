using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSDistributionPanelVM
    {
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }

        public ThPDSCircuitGraphTreeModel GraphTreeModel { get; private set; }
    }
}
