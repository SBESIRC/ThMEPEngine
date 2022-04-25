using Dreambuild.AutoCAD;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Models;
using TianHua.Electrical.PDS.UI.Project;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class InformationMatchViewModel : NotifyPropertyChangedBase
    {
        public InformationMatchViewModel(ThPDSProjectVMGraphInfo GraphInfo)
        {
            this.graphInfo = GraphInfo;
            this.Graph = GraphInfo.graphData;
            //this.Circuit = new ObservableCollection<ThPDSProjectGraphEdge>();
        }
        private ThPDSProjectVMGraphInfo graphInfo { get; set; }

        private BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph;
        public BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph
        {
            get { return graph; }
            set { graph = value; OnPropertyChanged(); }
        }

        //private ThPDSProjectGraphNode currentNode;
        //public ThPDSProjectGraphNode CurrentNode
        //{
        //    get { return currentNode; }
        //    set
        //    {
        //        currentNode = value;
        //        Circuit = new ObservableCollection<ThPDSProjectGraphEdge>();
        //        graph.Edges.Where(o => o.Source.Equals(value)).ForEach(o => Circuit.Add(o));
        //    }
        //}

        //private ObservableCollection<ThPDSProjectGraphEdge> circuits;
        //public ObservableCollection<ThPDSProjectGraphEdge> Circuit
        //{
        //    get { return circuits; } 
        //    set { circuits = value; }
        //}
    }
}
