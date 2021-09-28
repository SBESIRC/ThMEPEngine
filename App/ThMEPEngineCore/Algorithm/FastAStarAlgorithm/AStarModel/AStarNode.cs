using System;

namespace ThMEPEngineCore.Algorithm.FastAStarAlgorithm.AStarModel
{
    /// <summary>
    /// AStarNode 用于保存规划到当前节点时的各个Cost值以及父节点。
    /// tyj 2021.3.11
    /// </summary>
    public class AStarNode : IComparable<AStarNode>
    {
        public AStarNode(Point loc, AStarNode parent, CompassDirections parentdirection, int _costG, int _costH)
        {
            this.location = loc;
            this.parentNode = parent;
            this.direction = parentdirection;
            this.costG = _costG;
            this.costH = _costH;
        }

        /// <summary>
        /// 父节点
        /// </summary>
        private AStarNode parentNode = null;
        public AStarNode ParentNode
        {
            get { return parentNode; }
        }

        /// <summary>
        /// 节点所在的位置
        /// </summary>
        private Point location = new Point(0, 0);
        public Point Location
        {
            get { return location; }
        }

        /// <summary>
        /// 节点所在的位置
        /// </summary>
        private CompassDirections direction =CompassDirections.NotSet;
        public CompassDirections Direction
        {
            get { return direction; }
        }

        /// <summary>
        /// 从起点到本节点的代价。G值
        /// </summary>
        private int costG = 0;
        public int CostG
        {
            get { return costG; }
        }

        /// <summary>
        /// 使用启发式方法估算的从本节点到目的节点的代价。H值
        /// </summary>
        private int costH = 0;
        public int CostH
        {
            get { return costH; }
        }

        /// <summary>
        /// F值
        /// </summary>
        public int CostF
        {
            get
            {
                if (this.CostH < 10)
                    return this.CostG + this.CostH;
                else
                    return this.CostG + 2 * this.CostH;
            }
        }

        /// <summary>
        /// 当从起点到达本节点有更优的路径时，调用该方法采用更优的路径。
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="_costG"></param>
        public void ResetParentNode(AStarNode previous, int _costG, CompassDirections parentdirection)
        {
            this.parentNode = previous;
            this.costG = _costG;
            this.direction = parentdirection;
        }

        /// <summary>
        /// 判断是否节点相等
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int CompareTo(AStarNode node)
        {
            if (this.location.X == node.location.X &&
                this.location.Y == node.location.Y)
            {
                return 0;
            }

            int fValue = this.CostF - node.CostF;
            if (fValue <= 0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
}
