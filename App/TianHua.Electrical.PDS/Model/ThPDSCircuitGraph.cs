using System;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using System.ComponentModel;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Electrical.PDS.Model
{
    public enum PDSNodeType
    {
        [Description("None")]
        None = 0,
        [Description("空负载")]
        Empty = 1,
        [Description("未知负载")]
        Unkown = 2,
        [Description("配电箱")]
        DistributionBox = 3,
        [Description("负载")]
        Load = 4,
        [Description("桥架")]
        CableCarrier = 5,
        [Description("变压器")]
        PowerTransformer = 6,
        [Description("馈线母排")]
        FeederBusbar = 7,
        [Description("虚拟负载")]
        VirtualLoad = 8,
    }

    public class ThPDSCircuitGraphNode : IEquatable<ThPDSCircuitGraphNode>
    {
        public ThPDSCircuitGraphNode()
        {
            Loads = new List<ThPDSLoad>();
            LightingCableTray = new ThPDSLightingCableTray();
            HasWarning = false;
        }

        public PDSNodeType NodeType { get; set; }
        public List<ThPDSLoad> Loads { get; set; }
        public ThPDSLightingCableTray LightingCableTray { get; set; }
        public bool HasWarning { get; set; }

        public bool Equals(ThPDSCircuitGraphNode other)
        {
            return this.NodeType == other.NodeType && this.Loads.SequenceEqual(other.Loads);
        }

        public void SetOnLightingCableTray(bool onLlightingCableTray, Curve cableTray)
        {
            if (!onLlightingCableTray)
            {
                return;
            }

            LightingCableTray = new ThPDSLightingCableTray
            {
                OnLightingCableTray = onLlightingCableTray,
                CableTray = cableTray,
            };
        }
    }

    public class ThPDSCircuitGraphEdge<T> : EquatableEdge<T> where T : ThPDSCircuitGraphNode
    {
        public ThPDSCircuit Circuit { get; set; }
        public ThPDSCircuitGraphEdge(T source, T target) : base(source, target)
        {
            Circuit = new ThPDSCircuit();
        }
    }

    public class ThPDSCircuitGraph
    {
        private BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph { get; set; }
        public BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> Graph
        {
            get
            {
                return graph;
            }
            set
            {
                graph = value;
            }
        }
    }
}
