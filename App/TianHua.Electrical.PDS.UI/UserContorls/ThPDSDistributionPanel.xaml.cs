using QuikGraph;
using System.Windows.Controls;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.Project.Module;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSDistributionPanel : UserControl
    {
        private readonly WpfServices.ThPDSDistributionPanelService Service = new();
        private BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph
        {
            get
            {
                return Project.PDSProjectVM.Instance?.InformationMatchViewModel?.Graph;
            }
        }
        public ThPDSDistributionPanel()
        {
            InitializeComponent();
            PDSProject.Instance.ProjectDataChanged += (sender , e) =>
            {
                Service.Init(this, Graph);
                Service.UpdateTreeView(this.tv, Graph);
            };
        }
    }
}
