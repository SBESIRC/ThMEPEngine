using QuikGraph;
using QuikGraph.MSAGL;
using System.Windows.Controls;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// ThPDSProjectGraphDiffPanel.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSProjectGraphDiffPanel : UserControl
    {
        private Grid MainGrid
        {
            get
            {
                return this.Content as Grid;
            }
        }
        private GraphViewer Viewer { get; set; }
        private DockPanel GraphViewerPanel { get; set; }

        private Graph _msaglGraph;
        private Graph MsaglGraph
        {
            get
            {
                if (_msaglGraph == null)
                {
                    _msaglGraph = Graph.ToMsaglGraph();
                }
                return _msaglGraph;
            }
        }

        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; private set; }

        public ThPDSProjectGraphDiffPanel(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            Graph = graph;
            Viewer = new GraphViewer();
            GraphViewerPanel = new DockPanel();
            InitializeComponent();
            InitializeGraphViewer();
        }

        private void InitializeGraphViewer()
        {
            GraphViewerPanel.ClipToBounds = true;
            MainGrid.Children.Add(GraphViewerPanel);
            Viewer.BindToPanel(GraphViewerPanel);
            Viewer.Graph = MsaglGraph;
        }
    }
}
