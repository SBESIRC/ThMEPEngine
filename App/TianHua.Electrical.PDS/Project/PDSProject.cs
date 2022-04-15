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
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using TianHua.Electrical.PDS.Service;
using DwgGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode, TianHua.Electrical.PDS.Model.ThPDSCircuitGraphEdge<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode>>;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

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

        public ProjectGlobalConfiguration projectGlobalConfiguration;

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
                this.graphData = new ProjectGraph().CreatPDSProjectGraph();
                this.projectGlobalConfiguration = new ProjectGlobalConfiguration();
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
        public void PushGraphData(DwgGraph graph)
        {
            var ProjectGraph = new ProjectGraph();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            this.graphData = ProjectGraph.CreatPDSProjectGraph();
            if (!instance.DataChanged.IsNull())
            {
                instance.DataChanged();//推送消息告知VM刷新
            }
        }

        /// <summary>
        /// 二次推送Data数据
        /// </summary>
        public void SecondaryPushGraphData(DwgGraph graph)
        {
            var ProjectGraph = new ProjectGraph();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            if (!this.graphData.IsNull() && this.graphData.Graph.Vertices.Count() > 0)
            {
                this.graphData.Graph.Vertices.ForEach(node =>
                {
                    node.Load.InstalledCapacity.IsDualPower = node.Details.IsDualPower;
                    node.Load.InstalledCapacity.LowPower = node.Load.InstalledCapacity.LowPower > 0 ? node.Details.LowPower : 0;
                    node.Load.InstalledCapacity.HighPower = node.Load.InstalledCapacity.HighPower > 0 ? node.Details.HighPower : 0;
                });
                ThPDSGraphCompareService compareService = new ThPDSGraphCompareService();
                compareService.Diff(this.graphData.Graph, ProjectGraph);
                if (!instance.DataChanged.IsNull())
                {
                    instance.DataChanged();//推送消息告知VM刷新
                }
            }
            else
            {
                //Project未加载，此时不应该二次抓取数据
                //暂时不报错，跳过处理
            }
        }

        /// <summary>
        /// 项目更新至DWG
        /// </summary>
        public ProjectGraph ProjectUpdateToDwg(DwgGraph graph)
        {
            var ProjectGraph = new ProjectGraph();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
                ));
            if (!this.graphData.IsNull() && this.graphData.Graph.Vertices.Count() > 0)
            {
                this.graphData.Graph.Vertices.ForEach(node =>
                {
                    node.Load.InstalledCapacity.IsDualPower = node.Details.IsDualPower;
                    node.Load.InstalledCapacity.LowPower = node.Load.InstalledCapacity.LowPower > 0 ? node.Details.LowPower : 0;
                    node.Load.InstalledCapacity.HighPower = node.Load.InstalledCapacity.HighPower > 0 ? node.Details.HighPower : 0;
                });
                ThPDSGraphCompareService compareService = new ThPDSGraphCompareService();
                compareService.Diff(ProjectGraph , this.graphData.Graph);
                return ProjectGraph;
            }
            else
            {
                //Project未加载，此时不应该更新至DWG
                throw new NotSupportedException();
            }
        }

        public ThPDSProjectGraphNode CreatProjectNode(ThPDSCircuitGraphNode node)
        {
            var newNode = new ThPDSProjectGraphNode();
            newNode.Type = node.NodeType;
            newNode.IsStartVertexOfGraph = node.IsStartVertexOfGraph;
            newNode.Load = node.Loads.Count == 0 ? new ThPDSLoad() : node.Loads[0];
            if(node.Loads.Count > 1)
            {
                //多负载必定单功率
                newNode.Load.InstalledCapacity.HighPower = node.Loads.Sum(o => o.InstalledCapacity.IsNull() ? 0 : o.InstalledCapacity.HighPower);
                newNode.Details.HighPower = newNode.Load.InstalledCapacity.HighPower;
                newNode.Load.InstalledCapacity.IsDualPower = false;
                newNode.Details.IsDualPower = false;
            }
            else
            {
                var load = node.Loads[0];
                newNode.Load.InstalledCapacity = load.InstalledCapacity;
                newNode.Details.LowPower = load.InstalledCapacity.LowPower;
                newNode.Details.HighPower = load.InstalledCapacity.HighPower;
                newNode.Details.IsDualPower = load.InstalledCapacity.IsDualPower;
            }
            newNode.Details.IsOnlyLoad = node.Loads.Count == 1;
            return newNode;
        }
    }
}
