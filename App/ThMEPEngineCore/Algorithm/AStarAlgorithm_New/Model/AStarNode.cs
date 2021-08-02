using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New.Model
{
    public class AStarNode
    {
        /// <summary>
        /// 节点所在的位置
        /// </summary>
        public Point3d Location { get; set; }

        /// <summary>
        /// 父节点
        /// </summary>
        public AStarNode ParentNode { get; set; }

        /// <summary>
        /// 从起点到本节点的代价。G值
        /// </summary>
        public double CostG { get; set; }

        /// <summary>
        /// 使用启发式方法估算的从本节点到目的节点的代价。H值
        /// </summary>
        public double CostH { get; set; }

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

        /// <summary>
        /// 判断当前点是否是拐点
        /// </summary>
        public bool IsInflectionPoint { get; set; }

        /// <summary>
        /// 构造节点
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="parent"></param>
        /// <param name="_costG"></param>
        /// <param name="_costH"></param>
        public AStarNode(Point3d loc, AStarNode parent, double _costG, double _costH)
        {
            this.Location = loc;
            this.ParentNode = parent;
            this.CostG = _costG;
            this.CostH = _costH;
        }

        /// <summary>
        /// 当从起点到达本节点有更优的路径时，调用该方法采用更优的路径。
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="_costG"></param>
        public void ResetParentNode(AStarNode previous, double _costG)
        {
            this.ParentNode = previous;
            this.CostG = _costG;
        }
    }
}
