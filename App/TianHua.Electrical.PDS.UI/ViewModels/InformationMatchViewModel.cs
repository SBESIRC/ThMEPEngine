using Dreambuild.AutoCAD;
using QuickGraph;
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
        //该ViewModel为测试的，后期删除
        public InformationMatchViewModel(ThPDSProjectVMGraphInfo GraphInfo)
        {
            this.graphInfo = GraphInfo;
            this.Graph = GraphInfo.graphData;
            //this.Circuit = new ObservableCollection<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>();
        }
        private ThPDSProjectVMGraphInfo graphInfo { get; set; }

        private AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph;
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph
        {
            get { return graph; }
            set { graph = value; }
        }

        //private ThPDSProjectGraphNode currentNode;
        //public ThPDSProjectGraphNode CurrentNode
        //{
        //    get { return currentNode; }
        //    set
        //    {
        //        currentNode = value;
        //        Circuit = new ObservableCollection<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>();
        //        graph.Edges.Where(o => o.Source.Equals(value)).ForEach(o => Circuit.Add(o));
        //    }
        //}

        //private ObservableCollection<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> circuits;
        //public ObservableCollection<ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Circuit
        //{
        //    get { return circuits; } 
        //    set { circuits = value; }
        //}
    }
}
