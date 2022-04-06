﻿using Dreambuild.AutoCAD;
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
                this.graphData = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>().CreatPDSProjectGraph();
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
        public void PushGraphData(AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph)
        {
            var ProjectGraph = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge>();
            var VertexDir = graph.Vertices.ToDictionary(key => key, value => CreatProjectNode(value));
            graph.Vertices.ForEach(o => ProjectGraph.AddVertex(VertexDir[o]));
            graph.Edges.ForEach(o => ProjectGraph.AddEdge(
                new ThPDSProjectGraphEdge(VertexDir[o.Source], VertexDir[o.Target]) { Circuit = o.Circuit }
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
            if(node.Loads.Count > 1)
            {
                //多负载必定单功率
                newNode.Details.LowPower = node.Loads.Sum(o => o.InstalledCapacity.IsNull() ? 0 : o.InstalledCapacity.UsualPower.Sum());
                newNode.Details.IsDualPower = false;
            }
            else
            {
                var load = node.Loads[0];
                var power = load.InstalledCapacity.UsualPower.Union(load.InstalledCapacity.FirePower).ToList();
                if(power.Count == 0)
                {
                    newNode.Details.LowPower = 0;
                    newNode.Details.IsDualPower = false;
                }
                else if(power.Count == 1)
                {
                    newNode.Details.LowPower = power.First();
                    newNode.Details.IsDualPower = false;
                }
                else
                {
                    newNode.Details.LowPower = power.Min();
                    newNode.Details.HighPower = power.Max();
                    newNode.Details.IsDualPower = true;
                }
            }
            newNode.Details.IsOnlyLoad = node.Loads.Count == 1;
            //newNode.nodeDetails = new NodeDetails();
            return newNode;
        }
    }
}
