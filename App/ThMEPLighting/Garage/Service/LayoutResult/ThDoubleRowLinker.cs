using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThDoubleRowLinker
    {
        public static List<ThLightNodeLink> DoubleRowMode(List<ThLightEdge> edges, double doubleRowOffsetDis)
        {
            var results = new List<ThLightNodeLink>();
            var firstEdges = edges.Where(e => e.EdgePattern == EdgePattern.First).OrderByDescending(e => e.Edge.Length).ToList();
            var secondEdges = edges.Where(e => e.EdgePattern == EdgePattern.Second).OrderByDescending(e => e.Edge.Length).ToList();
            var firstEdge = firstEdges.First();
            ThLightEdge secondEdge = null;
            foreach (var edge in secondEdges)
            {
                if (edge.Edge.Distance(firstEdge.Edge) < doubleRowOffsetDis + 10.0
                    && Math.Abs(edge.Direction.DotProduct(firstEdge.Direction)) > Math.Cos(1 / 180.0 * Math.PI))
                {
                    secondEdge = edge;
                    break;
                }
            }
            if (secondEdge == null)
            {
                return results;
            }

            var firstNode1 = firstEdge.LightNodes.FirstOrDefault();
            if (firstNode1 == null)
            {
                return results;
            }
            var firstNode2 = secondEdge.LightNodes.Where(node => node.Number.Equals("WL04"))
                .OrderBy(node => node.Position.DistanceTo(firstNode1.Position)).FirstOrDefault();
            if (firstNode2 == null)
            {
                return results;
            }
            var nodeLink1 = new ThLightNodeLink
            {
                First = firstNode2,
                Second = firstNode1,
                Edges = new List<Line> { secondEdge.Edge, firstEdge.Edge },
            };
            results.Add(nodeLink1);

            var secondNode1 = firstEdge.LightNodes.Where(node => !node.Number.Equals(firstNode1.Number)).FirstOrDefault();
            if (secondNode1 == null)
            {
                return results;
            }
            var secondNode2 = secondEdge.LightNodes.Where(node => node.Number.Equals("WL02"))
                .OrderBy(node => node.Position.DistanceTo(secondNode1.Position) + node.Position.DistanceTo(firstNode1.Position)).FirstOrDefault();
            if (secondNode2 == null)
            {
                return results;
            }
            var nodeLink2 = new ThLightNodeLink
            {
                First = secondNode1,
                Second = secondNode2,
                Edges = new List<Line> { firstEdge.Edge, secondEdge.Edge },
            };
            results.Add(nodeLink2);

            return results;
        }
    }
}
