using Dreambuild.AutoCAD;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Project
{
    /// <summary>
    /// PDS项目
    /// </summary>
    [Serializable]
    public class PDSProject
    {
        private static PDSProject instance = new PDSProject();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static PDSProject() { }
        internal PDSProject() { }
        public static PDSProject Instance { get { return instance; } }

        public InformationMatchViewerModule InformationMatch;
        public testViewerModule test { get; set; }

        public ThPDSProjectGraph graphData;

        public Action DataChanged;

        /// <summary>
        /// 加载项目
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if(string.IsNullOrEmpty(url))
            {
                //Creat New Project
                instance.graphData = new ThPDSProjectGraph() { Graph = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>() };
                if (!instance.DataChanged.IsNull())
                {
                    instance.DataChanged();//推送消息告知VM刷新
                }
            }
            else
            {
                //Load Choise Project
                if (!instance.DataChanged.IsNull())
                {
                    instance.DataChanged();//推送消息告知VM刷新
                }
            }
        }

        //public testViewerModule ConvertToTest()
        //{
        //    test = new testViewerModule();
        //    ConverterFactory<testViewerModule, testViewerModule> factory = new ConverterFactory<testViewerModule, testViewerModule>();
        //    return factory.ConvertMethod(test);
        //}

        //public InformationMatchViewerModule ConvertToInformationMatch()
        //{
        //    InformationMatch = new InformationMatchViewerModule();
        //    ConverterFactory<InformationMatchViewerModule, InformationMatchViewerModule> factory = new ConverterFactory<InformationMatchViewerModule, InformationMatchViewerModule>();
        //    return factory.ConvertMethod(InformationMatch);
        //}

        /// <summary>
        /// 推送Data数据
        /// </summary>
        public void PushGraphData(AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph)
        {
            this.graphData.Graph = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => this.graphData.Graph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => this.graphData.Graph.AddEdge(
                new ThPDSProjectGraphEdge<ThPDSProjectGraphNode>(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            this.graphData.Graph.Vertices.ForEach(node =>
            {
                if (node.Load.LoadTypeCat_1 ==ThPDSLoadTypeCat_1.DistributionPanel && node.Load.LoadTypeCat_2 ==ThPDSLoadTypeCat_2.FireEmergencyLightingDistributionPanel)
                {
                    node.nodeDetails.CircuitFormType = CircuitFormInType.集中电源;
                }
                else
                {
                    var count = this.graphData.Graph.Edges.Count(o => o.Target.Equals(node));
                    if (count == 1)
                    {
                        node.nodeDetails.CircuitFormType = CircuitFormInType.一路进线;
                    }
                    else if (count == 2)
                    {
                        node.nodeDetails.CircuitFormType = CircuitFormInType.二路进线ATSE;
                    }
                    else if (count == 3)
                    {
                        node.nodeDetails.CircuitFormType = CircuitFormInType.三路进线;
                    }
                }
            });
            this.graphData.Graph.Edges.ForEach(edge =>
            {
                if(edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
                {
                    edge.circuitDetails.CircuitFormType = CircuitFormOutType.配电计量_上海CT;
                }
                else if(edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ACCharger|| edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.DCCharger)
                {
                    edge.circuitDetails.CircuitFormType = CircuitFormOutType.漏电;
                }
                else if(edge.Target.Load.LoadTypeCat_2 == ThPDSLoadTypeCat_2.FireEmergencyLuminaire)
                {
                    edge.circuitDetails.CircuitFormType = CircuitFormOutType.消防应急照明回路WFEL;
                }
                else if (edge.Target.Load.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Socket)
                {
                    edge.circuitDetails.CircuitFormType = CircuitFormOutType.漏电;
                }
                else
                {
                    edge.circuitDetails.CircuitFormType = CircuitFormOutType.常规;
                }
            });
            if (!instance.DataChanged.IsNull())
            {
                instance.DataChanged();//推送消息告知VM刷新
            }
        }

        public ThPDSProjectGraphNode CreatProjectNode(ThPDSCircuitGraphNode node)
        {
            var newNode = new ThPDSProjectGraphNode();
            newNode.NodeType = node.NodeType;
            newNode.IsStartVertexOfGraph = node.IsStartVertexOfGraph;
            newNode.Load = node.Loads.Count == 0 ? new ThPDSLoad() : node.Loads[0];
            //newNode.Load.InstalledCapacity = node.Loads.Sum(O => O.InstalledCapacity);
            //newNode.nodeDetails.IsDualPower = node.Loads[0];
            newNode.nodeDetails = new NodeDetails();
            return newNode;
        }
    }
}
