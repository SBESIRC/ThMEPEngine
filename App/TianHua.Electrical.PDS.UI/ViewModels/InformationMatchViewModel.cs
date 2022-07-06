using QuikGraph;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class InformationMatchViewModel : NotifyPropertyChangedBase
    {
        public BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph
        {
            get
            {
                return PDSProject.Instance.graphData.Graph;
            }
        }
    }
}
