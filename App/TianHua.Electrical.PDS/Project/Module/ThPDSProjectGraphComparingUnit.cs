using System;
using PDSGraph = QuikGraph.AdjacencyGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Project.Module
{
    public abstract class ThPDSProjectGraphComparingUnit
    {
        public abstract void DoCompare(PDSGraph source, PDSGraph target);
    }
}
