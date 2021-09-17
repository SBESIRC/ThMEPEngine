namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    /// <summary>
    /// 图节点的关系
    /// 如果两个节点没有路径，可以没有，也可以是比较大的数字
    /// 如果是有向图请注意开始节点和结束节点的顺序
    /// </summary>
    public class GraphNodeRelation
    {
        /// <summary>
        /// 开始节点
        /// </summary>
        public IGraphNode StartNode { get; }
        /// <summary>
        /// 结束节点
        /// </summary>
        public IGraphNode EndNode { get; }
        /// <summary>
        /// 两个节点间的权重
        /// </summary>
        public double Weight { get; }
        /// <summary>
        /// 是否是单向
        /// </summary>
        public bool IsOneWay { get; }
        /// <summary>
        /// 关系类型（扩展用）
        /// </summary>
        public int RelationType { get; set; }
        /// <summary>
        /// 扩展用
        /// </summary>
        public object Tag { get; set; }

        public GraphNodeRelation(IGraphNode startNode, IGraphNode endNode, double weight) 
            :this(startNode,endNode,weight,false,-1,null)
        { }
        public GraphNodeRelation(IGraphNode startNode,IGraphNode endNode,double weight,bool isOneWay)
            : this(startNode, endNode, weight, isOneWay,-1,null)
        { }
        public GraphNodeRelation(IGraphNode startNode, IGraphNode endNode, double weight, bool isOneWay,int relationType,object tag)
        {
            this.StartNode = startNode;
            this.EndNode = endNode;
            this.Weight = weight;
            this.IsOneWay = isOneWay;
            this.RelationType = relationType;
            this.Tag = tag;
        }
    }
}
