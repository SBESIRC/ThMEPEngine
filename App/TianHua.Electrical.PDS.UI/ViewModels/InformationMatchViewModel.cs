using QuikGraph;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class InformationMatchViewModel : NotifyPropertyChangedBase
    {
        public InformationMatchViewModel(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
        {
            _graph = graph;
        }

        private readonly BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> _graph;
        public BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph => _graph;
    }
}
