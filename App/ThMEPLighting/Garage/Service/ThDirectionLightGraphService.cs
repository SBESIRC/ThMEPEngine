using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 根据方向来遍历,不支持支路
    /// 用于车道线双排布置
    /// </summary>
    public class ThDirectionLightGraphService : ThLightGraphService
    {
        public List<ThLightGraphService> Graphs { get; private set; } // 当Model使用
        public ThDirectionLightGraphService(List<ThLightEdge> edges) 
            : base(edges,Point3d.Origin)
        {
            Graphs = new List<ThLightGraphService>();
        }

        public override void Build()
        {
            while(Edges.Count>0)
            {
                var first = Edges.First();
                var links = new List<ThLightEdge> { first };
                Find(links, first.EndPoint);
                Find(links, first.StartPoint);
                Update(links);

                // 获取起点
                var poly = ThGarageUtils.ToPolyline(links.Select(o => o.Edge).ToList(),
                    ThGarageLightCommon.RepeatedPointDistance);

                // 创建Model
                var linkPath = new ThLinkPath
                {
                    Edges = links,
                    IsMain = true,
                    Start = poly.StartPoint,
                    PreEdge = new ThLightEdge(),
                };
                var graph = new ThLightGraphService(links, poly.StartPoint);
                graph.Links.Add(linkPath);
                Graphs.Add(graph);

                // 更新Edges 
                Edges = Edges.Where(o => !o.IsTraversed).ToList();
            }
        }

        /// <summary>
        /// 查找相连的线
        /// </summary>
        /// <param name="links"></param>
        /// <param name="start"></param>
        private void FindPrev(List<ThLightEdge> links, Point3d portPt)
        {
            var portEdges = GetPortEdges(links, portPt);
            if (portEdges.Count == 0)
            {
                return;
            }
            var neighbourEdge = FindNeighbourEdge(links[links.Count - 1], portEdges);
            if (neighbourEdge == null)
            {
                return;
            }
            links.Insert(0,neighbourEdge);
            FindPrev(links, neighbourEdge.StartPoint);
        }

        /// <summary>
        /// 查找相连的线
        /// </summary>
        /// <param name="links"></param>
        /// <param name="start"></param>
        private void FindNext(List<ThLightEdge> links, Point3d portPt)
        {
            //当Degree为零，或碰到已遍历的边结束
            var portEdges = GetPortEdges(links,portPt);
            if (portEdges.Count == 0)
            {
                return;
            }
            var neighbourEdge = FindNeighbourEdge(links[links.Count - 1], portEdges);
            if (neighbourEdge == null)
            {
                return;
            }
            links.Add(neighbourEdge);
            FindNext(links, neighbourEdge.EndPoint);
        }

        private List<ThLightEdge> GetPortEdges(List<ThLightEdge> links, Point3d portPt)
        {
            //当Degree为零，或碰到已遍历的边结束
            var portEdges = SearchEdges(portPt, ThGarageLightCommon.RepeatedPointDistance);
            return portEdges
                .Where(o => !links.Select(l => l.Id).Contains(o.Id))
                .Where(o => !o.IsTraversed)
                .ToList();
        }

        protected override ThLightEdge FindNeighbourEdge(ThLightEdge currentEdge, List<ThLightEdge> linkEdges)
        {
            // 先选择方向一致性的边
            var dirConsistentEdges = linkEdges.Where(o => IsDirectionConsistent(currentEdge, o)).ToList();

            // 筛选共线的边
            var collinearEdges = dirConsistentEdges.Where(o => IsCollinear(currentEdge, o)).ToList();
            if (collinearEdges.Count > 0)
            {
                return collinearEdges.OrderByDescending(o => o.Edge.Length).First();
            }

            // 获取不共线的边
            var unCollinearEdges = dirConsistentEdges.Where(o => !IsCollinear(currentEdge, o)).ToList();
            if (unCollinearEdges.Count == 1)
            {
                return unCollinearEdges[0];
            }
            else
            {
                // 对于多个分支，优先选择外角小于45度且夹角最小的通道
                var angleRangedEdges = dirConsistentEdges
                    .Where(o => IsLessThan45Degree(currentEdge, o))
                    .OrderBy(o=> currentEdge.Direction.GetAngleTo(o.Direction))
                    .ToList();
                return angleRangedEdges.Count()>0? angleRangedEdges.First():null;
            }
        }

        private void Update(List<ThLightEdge> edges)
        {
            edges.ForEach(o => o.IsTraversed = true);
        }
        private bool IsDirectionConsistent(ThLightEdge first, ThLightEdge second)
        {
            var firstSp = first.StartPoint;
            var firstEp = first.EndPoint;

            var secondSp = second.StartPoint;
            var secondEp = second.EndPoint;

            if (firstEp.DistanceTo(secondSp) <= ThGarageLightCommon.RepeatedPointDistance)
            {
                return true;
            }
            if (firstSp.DistanceTo(secondEp) <= ThGarageLightCommon.RepeatedPointDistance)
            {
                return true;
            }
            return false;
        }

        private bool IsCollinear(ThLightEdge first, ThLightEdge second)
        {
            return ThGeometryTool.IsCollinearEx(
                  first.Edge.StartPoint, first.Edge.EndPoint,
                  second.Edge.StartPoint, second.Edge.EndPoint);
        }

        private bool IsLessThan45Degree(ThLightEdge first, ThLightEdge second)
        {
            return ThGarageUtils.IsLessThan45Degree(
                first.Edge.StartPoint, first.Edge.EndPoint,
                second.Edge.StartPoint, second.Edge.EndPoint);
        }
    }
}
