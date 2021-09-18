using System.Collections.Generic;

namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    public abstract class GraphNodeBase : IGraphNode
    {
        public object GraphNode { get; }
        public double NodeWeight { get; }
        public bool IsEnd { get; set; }
        public object Tag { get; set; }
        public object NodeType { get; set; }
        public abstract bool NodeIsEqual(IGraphNode node, object precision, object parameter);
        public abstract double NodeDistanceToNode(IGraphNode node);

        public virtual IGraphNode CenterGraphNode(List<IGraphNode> graphNodes)
        {
            return null;
        }

        public GraphNodeBase(object node, bool isEnd, double nodeWeight,object tag,object nodeType) 
        {
            this.GraphNode = node;
            this.IsEnd = isEnd;
            this.NodeWeight = nodeWeight;
            this.Tag = tag;
            this.NodeType = nodeType;
        }
    }
}
