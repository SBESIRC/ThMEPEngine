using System.Linq;
using System.Collections.Generic;
using GraphEdge = ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphEdge<ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode>;

namespace ThMEPElectrical.ChargerDistribution.Model
{
    public class ThChargerGraphData
    {
        /// <summary>
        /// 待移除的边
        /// </summary>
        public List<GraphEdge> RemoveEdges { get; set; }

        /// <summary>
        /// 可增加的边
        /// </summary>
        public List<GraphEdge> AddEdges { get; set; }

        /// <summary>
        /// 评价值
        /// </summary>
        public double Evaluation
        {
            get
            {
                return RemoveEdges.Sum(edge =>
                {
                    var length = edge.Source.Point.DistanceTo(edge.Target.Point) - 3000.0;
                    return length < 0 ? 0 : length;
                });
            }
        }

        public ThChargerGraphData()
        {
            RemoveEdges = new List<GraphEdge>();
            AddEdges = new List<GraphEdge>();
        }

        public ThChargerGraphData(List<GraphEdge> removeEdges)
        {
            RemoveEdges = removeEdges;
            AddEdges = new List<GraphEdge>();
        }
    }
}
