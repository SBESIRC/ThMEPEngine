using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel
{
    /// <summary>
    /// AStarNode 用于保存规划到当前节点时的各个Cost值以及父节点。
    /// tyj 2021.3.11
    /// </summary>
    public class GlobleNode : IComparable<GlobleNode>
    {
        public GlobleNode(GloblePoint loc, GlobleNode parent, double _costG, double _costH)
        {
            this.location = loc;
            this.parentNode = parent;
            this.costG = _costG;
            this.costH = _costH;
        }

        /// <summary>
        /// 父节点
        /// </summary>
        private GlobleNode parentNode = null;
        public GlobleNode ParentNode
        {
            get { return parentNode; }
        }

        /// <summary>
        /// 节点所在的位置
        /// </summary>
        private GloblePoint location = new GloblePoint(0, 0);
        public GloblePoint Location
        {
            get { return location; }
        }

        /// <summary>
        /// 从起点到本节点的代价。G值
        /// </summary>
        private double costG = 0;
        public double CostG
        {
            get { return costG; }
        }

        /// <summary>
        /// 使用启发式方法估算的从本节点到目的节点的代价。H值
        /// </summary>
        private double costH = 0;
        public double CostH
        {
            get { return costH; }
        }

        /// <summary>
        /// F值
        /// </summary>
        public double CostF
        {
            get
            {
                return this.CostG + this.CostH;
            }
        }

        /// <summary>
        /// 节点走向
        /// </summary>
        public CompassDirections Directions { get; set; }

        /// <summary>
        /// 当从起点到达本节点有更优的路径时，调用该方法采用更优的路径(且弯头最少)。
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="_costG"></param>
        public void ResetParentNode(GlobleNode previous, double _costG, CompassDirections _directions)
        {
            if (this.CostG > _costG)
            {
                this.parentNode = previous;
                this.costG = _costG;
                this.Directions = _directions;
            }
            else if (this.CostG == _costG && (previous.Directions == this.Directions || _directions == previous.Directions))
            {
                this.parentNode = previous;
                this.costG = _costG - 0.5;
                this.Directions = _directions;
            }
        }

        /// <summary>
        /// 判断是否节点相等(0相等；-1小于；1大于)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int CompareTo(GlobleNode node)
        {
            if (this.location.X == node.location.X &&
                this.location.Y == node.location.Y)
            {
                return 0;
            }

            double fValue = this.CostF - node.CostF;
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
