using QuikGraph;
using System.Collections.Generic;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class LoadCalculationViewModel : NotifyPropertyChangedBase
    {
        public BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph => PDSProject.Instance.graphData.Graph;
        public List<THPDSProjectSubstation> Substations => PDSProject.Instance.substations;
        public THPDSSubstationMap SubstationMap => PDSProject.Instance.substationMap;
    }
}
