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

        public InformationMatchViewerModule InformationMatch { get; set; }
        public testViewerModule test { get; set; }

        public ThPDSProjectGraph graphData;

        /// <summary>
        /// 加载项目
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if(string.IsNullOrEmpty(url))
            {
                //Creat New Project
                instance = new PDSProject()
                {
                    graphData = new ThPDSProjectGraph() { Graph = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>()}
                };
            }
            else
            {
                //Load Choise Project
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
        }

        public ThPDSProjectGraphNode CreatProjectNode(ThPDSCircuitGraphNode node)
        {
            var newNode = new ThPDSProjectGraphNode();
            newNode.NodeType = node.NodeType;
            newNode.IsStartVertexOfGraph = node.IsStartVertexOfGraph;
            newNode.Load = node.Loads[0];
            newNode.Load.InstalledCapacity = node.Loads.Sum(O => O.InstalledCapacity);
            return newNode;
        }
    }
}
