using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.Point_PathFinding.Model
{
    public class NodeModel : IComparable<NodeModel>
    {
        public NodeModel(Point3d loc, NodeModel parent, double _costG, double _costH)
        {
            Location = loc;
            this.parentNode = parent;
            this.costG = _costG;
            this.costH = _costH;
        }

        /// <summary>
        /// 父节点
        /// </summary>
        private NodeModel parentNode = null;
        public NodeModel ParentNode
        {
            get { return parentNode; }
        }

        /// <summary>
        /// 节点所在的位置
        /// </summary>
        public Point3d Location { get; set; }

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

        /// <summary>
        /// 当从起点到达本节点有更优的路径时，调用该方法采用更优的路径。
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="_costG"></param>
        public void ResetParentNode(NodeModel previous, double _costG)
        {
            this.parentNode = previous;
            this.costG = _costG;
        }


        /// <summary>
        /// 判断是否节点相等
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int CompareTo(NodeModel node)
        {
            if (node.Location.IsEqualTo(this.Location))
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
