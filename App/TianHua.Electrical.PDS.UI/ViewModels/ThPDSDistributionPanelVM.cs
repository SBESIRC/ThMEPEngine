using QuikGraph;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSDistributionPanelVM : ObservableObject
    {
        /// <summary>
        /// 项目数据
        /// </summary>
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }

        /// <summary>
        /// 项目数据图的节点
        /// </summary>
        private ThPDSCircuitGraphTreeModel _graphTreeModel;
        public ThPDSCircuitGraphTreeModel GraphTreeModel { 
            get
            {
                if (_graphTreeModel == null)
                {
                    var builder = new ThPDSCircuitGraphTreeBuilder();
                    _graphTreeModel = builder.Build(Graph);
                }
                return _graphTreeModel;
            }
        }
    }
}
