using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    /// <summary>
    /// 评价模型
    /// </summary>
    public class EvaluationModel
    {
        public static int Evaluation(int[] bits, List<ThBeamTopologyNode> nodes, List<List<ThBeamTopologyNode>> space)
        {
            if (bits.Length != nodes.Count)
                return 0;
            var newSpace = new List<List<ThBeamTopologyNode>>();
            for (int i = 0; i < space.Count; i++)
            {
                var NewNodes = space[i].ToArray().ToList();
                for (int j = 0; j < bits.Length; j++)
                {
                    if (bits[j]==i+1)
                    {
                        NewNodes.Add(nodes[j]);
                    }
                }
                var neighbor = newSpace.FirstOrDefault(o => o.IsNeighbor(NewNodes, true));
                if (neighbor != null)
                {
                    neighbor.AddRange(NewNodes);
                }
                else
                {
                    newSpace.Add(NewNodes);
                }
            }
            int BaseScore = 500;
            var AreaSum = space.Sum(o => o.Sum(x => x.Boundary.Area));
            var AverageArea = AreaSum / space.Count;
            newSpace.ForEach(o =>
            {
                var UnionPolygon = o.UnionPolygon();
                var ConvexPolyline = UnionPolygon.ConvexHullPL();
                var weights = UnionPolygon.Area > AverageArea ? UnionPolygon.Area / AverageArea : 1;//为大面积附加权重，使其'脱颖而出'
                var score = (int)Math.Ceiling(UnionPolygon.Area / ConvexPolyline.Area * 100 * weights);
                BaseScore += score;
            });
            BaseScore -= newSpace.Count * 100;
            return BaseScore;
        }

        public static int Evaluation(List<List<ThBeamTopologyNode>> space)
        {
            int result = 0;
            return result;
        }
    }
}
