using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPLighting.Common
{
    public class ThCdzmLightGraphService : ThLightGraphService
    {
        public ThCdzmLightGraphService(List<ThLightEdge> edges, Point3d start) : base(edges, start)
        {
        }

        protected override ThLightEdge FindNeighbourEdge(ThLightEdge currentEdge, List<ThLightEdge> linkEdges)
        {
            var collinearEdges = linkEdges.Where(o => ThGeometryTool.IsCollinearEx(
                  currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint,
                  o.Edge.StartPoint, o.Edge.EndPoint)).ToList();
            if (collinearEdges.Count > 0)
            {
                return collinearEdges.OrderByDescending(o => o.Edge.Length).First();
            }
            var unCollinearEdges = linkEdges.Where(o => !ThGeometryTool.IsCollinearEx(
                  currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint,
                  o.Edge.StartPoint, o.Edge.EndPoint)).ToList();
            if (unCollinearEdges.Count == 1)
            {
                return unCollinearEdges[0];
            }
            else
            {
                // 对于多个分支，优先选择夹角最小的通道
                var minAngle = double.MaxValue;
                ThLightEdge neighbourEdge = null;
                foreach (var edge in unCollinearEdges)
                {
                    if(ThGarageUtils.IsLessThan45Degree(currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint,
                                                        edge.Edge.StartPoint, edge.Edge.EndPoint))
                    {
                        var angle = currentEdge.Direction.GetAngleTo(edge.Direction);
                        if (angle < minAngle)
                        {
                            minAngle = angle;
                            neighbourEdge = edge;
                        }
                    }
                }
                return neighbourEdge;
            }
        }
        protected override Point3d UpdateEdge(ThLightEdge lightEdge, Point3d startPt)
        {
            //找出第一根边上的分支
            BuildMultiBranch(lightEdge, startPt); //获取一条边下一端的支路
            Point3d nextPt = GetNextLinkPt(lightEdge, startPt);
            BuildMultiBranch(lightEdge, nextPt); //获取一条边下一端的支路
            lightEdge.Update(startPt);
            return nextPt;
        }
    }
}
