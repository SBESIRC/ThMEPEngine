using Dreambuild.AutoCAD;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Configure;

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

        public ThPDSProjectGraph graphData;

        //public BreakerComponentConfiguration breakerConfig;

        private bool InitializedState;

        public Action DataChanged;

        /// <summary>
        /// 加载项目
        /// </summary>
        /// <param name="url"></param>
        public void Load(string url)
        {
            if(!InitializedState)
            {
                this.LoadGlobalConfig();
                InitializedState = true;
            }
            if(string.IsNullOrEmpty(url))
            {
                //Creat New Project
                this.graphData = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>().CreatPDSProjectGraph();
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

        /// <summary>
        /// 推送Data数据
        /// </summary>
        public void PushGraphData(AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph)
        {
            var ProjectGraph = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge<ThPDSProjectGraphNode>(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            if(!this.graphData.IsNull() && this.graphData.Graph.Vertices.Count() > 0)
            {
                this.graphData = ProjectGraph.CreatPDSProjectGraph();
                //this.graphData.Compatible(ProjectGraph);暂未支持校验功能
            }
            else
            {
                this.graphData = ProjectGraph.CreatPDSProjectGraph();
            }
            if (!instance.DataChanged.IsNull())
            {
                instance.DataChanged();//推送消息告知VM刷新
            }
        }

        public ThPDSProjectGraphNode CreatProjectNode(ThPDSCircuitGraphNode node)
        {
            var newNode = new ThPDSProjectGraphNode();
            newNode.Type = node.NodeType;
            newNode.IsStartVertexOfGraph = node.IsStartVertexOfGraph;
            newNode.Load = node.Loads.Count == 0 ? new ThPDSLoad() : node.Loads[0];
            //newNode.Load.InstalledCapacity = node.Loads.Sum(O => O.InstalledCapacity);
            //newNode.nodeDetails.IsDualPower = node.Loads[0];
            newNode.Details.LowPower = node.Loads.Sum(o => o.InstalledCapacity.IsNull() ? 0 : o.InstalledCapacity.UsualPower.Sum());
            newNode.Details.IsOnlyLoad = node.Loads.Count == 1;
            //newNode.nodeDetails = new NodeDetails();
            return newNode;
        }
    }
}
