using QuikGraph;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSDistributionPanel : UserControl
    {
        public WpfServices.ThPDSDistributionPanelService Service = new();
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph { get; set; }
        public ThPDSDistributionPanel()
        {
            InitializeComponent();
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
            this.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType);
            if (Service == null)
            {
                this.Loaded += ThPDSDistributionPanel_Loaded;
                this.tv.SelectedItemChanged += Tv_SelectedItemChanged;
            }
            else
            {
                Service.TreeView = tv;
                Service.Canvas = canvas;
                Service.propertyGrid = propertyGrid;
                Service.Graph = Graph;
                Service.Init();
            }
        }

        private void Tv_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdatePropertyGrid(this.tv.SelectedItem);
            UpdateCanvas();
        }

        private void ThPDSDistributionPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var builder = new ThPDSCircuitGraphTreeBuilder();
            this.tv.DataContext = builder.Build(Graph);
            if (new Services.ThPDSCircuitGraphComponentGenerator().IN(null) is null)
            {
                UpdateCanvas();
            }
        }

        private void UpdateCanvas()
        {
            if (this.tv.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
            var left = ThCADExtension.ThEnumExtension.GetDescription(Graph.Vertices.ToList()[sel.Id].Details.CircuitFormType.CircuitFormType) ?? "1路进线";
            var v = Graph.Vertices.ToList()[sel.Id];
            var rights = Graph.Edges.Where(eg => eg.Source == Graph.Vertices.ToList()[sel.Id]).Select(eg => ThCADExtension.ThEnumExtension.GetDescription(eg.Details.CircuitForm.CircuitFormType) ?? "常规").Select(x => x.Replace("(", "（").Replace(")", "）")).ToList();
            var rd = new ThPDSCircuitGraphHighDpiRenderer() { Left = left, Rights = rights, PDSBlockInfos = Services.ThPDSCircuitGraphComponentGenerator.PDSBlockInfos };
            rd.Render(canvas, Graph);
        }

        private void UpdatePropertyGrid(object vm)
        {
            propertyGrid.SelectedObject = vm;
        }
    }
}
