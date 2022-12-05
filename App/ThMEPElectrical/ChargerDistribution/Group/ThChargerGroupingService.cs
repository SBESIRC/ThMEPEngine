using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using QuikGraph;
using Dreambuild.AutoCAD;
using QuikGraph.Algorithms;
using Autodesk.AutoCAD.Geometry;
using GraphNode = ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode;
using GraphEdge = ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphEdge<ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode>;
using UndirectedGraph = QuikGraph.UndirectedGraph<
    ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode,
    ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphEdge<ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode>>;
using BidirectionalGraph = QuikGraph.BidirectionalGraph<
    ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode,
    ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphEdge<ThMEPElectrical.ChargerDistribution.Group.ThChargerGraphNode>>;
using ThMEPElectrical.ChargerDistribution.Model;

namespace ThMEPElectrical.ChargerDistribution.Group
{
    public class ThChargerGroupingService
    {
        public List<List<Point3d>> Grouping(List<Point3d> chargers, Point3d start, int maxCount)
        {
            var results = new List<List<Point3d>>();
            var graphs = MinimumSpanningTree(chargers, 10000.0, maxCount);
            graphs.ForEach(graph =>
            {
                var search = new List<GraphNode>();
                var startNodes = graph.Vertices.Where(o => graph.OutDegree(o) + graph.InDegree(o) == 1).ToList();
                startNodes.ForEach(node =>
                {
                    if (search.Contains(node))
                    {
                        return;
                    }
                    var result = new List<Point3d>();
                    Recycle(graph, search, result, node);
                    results.Add(result);
                });
                var isolate = graph.Vertices.Where(o => graph.OutDegree(o) + graph.InDegree(o) == 0).ToList();
                isolate.ForEach(o => results.Add(new List<Point3d> { o.Point })); 
            });

            return results;
        }

        private void Recycle(BidirectionalGraph graph, List<GraphNode> search, List<Point3d> result, GraphNode node)
        {
            if (search.Contains(node))
            {
                return;
            }
            search.Add(node);
            result.Add(node.Point);
            var edges = new List<GraphEdge>();
            edges.AddRange(graph.OutEdges(node));
            edges.AddRange(graph.InEdges(node));
            edges.ForEach(edge =>
            {
                Recycle(graph, search, result, edge.Target);
                Recycle(graph, search, result, edge.Source);
            });
        }

        private List<BidirectionalGraph> MinimumSpanningTree(List<Point3d> chargers, double distance, int maxCount)
        {
            var graph = new UndirectedGraph();
            graph.AddVertex(new ThChargerGraphNode(chargers.First()));
            for (var i = 1; i < chargers.Count; i++)
            {
                var node = new ThChargerGraphNode(chargers[i]);
                graph.AddVertex(node);
                graph.Vertices.ForEach(vertice =>
                {
                    var edge = new GraphEdge(vertice, node);
                    graph.AddEdge(edge);
                });
            }

            double edgeWeights(GraphEdge edge)
            {
                if (edge.Source != null && edge.Target != null)
                {
                    return edge.Source.Point.DistanceTo(edge.Target.Point);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            var tree = graph.MinimumSpanningTreeKruskal(edgeWeights).ToList();

            var index = tree.Count;
            for (var i = tree.Count - 1; i >= 0; i--)
            {
                if (tree[i].Source.Point.DistanceTo(tree[i].Target.Point) < distance)
                {
                    index = i;
                    break;
                }
            }
            while (index + 1 < tree.Count)
            {
                tree.RemoveAt(index + 1);
            }

            var graphs = Divide(tree);
            graphs.ForEach(o =>
            {
                var graphEdgeData = MinimumSpanningTree(o, maxCount);
                graphEdgeData.AddEdges.ForEach(edge =>
                {
                    tree.Add(edge);
                });
                graphEdgeData.RemoveEdges.ForEach(edge =>
                {
                    tree.RemoveAll(e => e.Equals(edge) || e.Equals(edge.Inverse()));
                });
                o.RemoveEdgeIf(edge => !tree.Contains(edge));
            });

            return graphs;
        }

        private List<BidirectionalGraph> Divide(List<GraphEdge> tree)
        {
            var search = new List<GraphEdge>();
            var graphs = new List<BidirectionalGraph>();
            while (tree.Count > search.Count)
            {
                var graph = new BidirectionalGraph();
                graphs.Add(graph);
                var first = tree.Except<GraphEdge>(search).First();
                AddEdge(graph, first);
                search.Add(first);
                var todo = false;
                do
                {
                    todo = false;
                    tree.Except<GraphEdge>(search).ForEach(edge =>
                    {
                        var contains = graph.Vertices.Contains(edge.Source) || graph.Vertices.Contains(edge.Target);
                        if (contains)
                        {
                            AddEdge(graph, edge);
                            search.Add(edge);
                            todo = true;
                        }
                    });
                }
                while (todo);
            }

            return graphs;
        }

        private ThChargerGraphData MinimumSpanningTree(BidirectionalGraph graph, int maxCount)
        {
            // 理想组数
            var groupCount = Math.Ceiling(graph.Vertices.Count() / Convert.ToDouble(maxCount));
            var residue = graph.Vertices.Count() - (groupCount - 1) * maxCount;
            var results = new List<ThChargerGraphData>();
            var outsideNodes = graph.Vertices.Where(v => graph.OutDegree(v) == 1).OrderBy(v => v.Point.DistanceTo(Point3d.Origin)).ToList();
            outsideNodes.ForEach(node =>
            {
                var result = new ThChargerGraphData();
                results.Add(result);
                var search = new List<GraphNode> { node };
                var count = 1;
                var nextNode = Recycle(graph, search, maxCount, node, result, ref count);
                var edges = graph.OutEdges(nextNode).Where(o => !search.Contains(o.Target)).ToList();
                result.RemoveEdges.AddRange(edges);
                count = 0;
                Recycle(graph, search, maxCount, edges, result, ref count);
            });

            return results.OrderBy(o => o.RemoveEdges.Count).ThenByDescending(o => o.Evaluation).FirstOrDefault();
        }

        private GraphNode Recycle(BidirectionalGraph graph, List<GraphNode> search, int maxCount, GraphNode node, ThChargerGraphData result, ref int count)
        {
            while (count < maxCount)
            {
                var edges = graph.OutEdges(node).Where(o => !search.Contains(o.Target)).OrderBy(o => o.Source.Point.DistanceTo(o.Target.Point)).ToList();
                if (edges.Count() == 0)
                {
                    break;
                }
                else if (edges.Count() > 1)
                {

                }
                for (var i = 0; i < edges.Count(); i++)
                {
                    if (count < maxCount)
                    {
                        search.Add(edges[i].Target);
                        count++;
                        node = Recycle(graph, search, maxCount, edges[i].Target, result, ref count);
                    }
                    else
                    {
                        result.RemoveEdges.Add(edges[i]);
                        for (var j = i + 1; j < edges.Count(); j++)
                        {
                            var newEdge = new GraphEdge(edges[i].Target, edges[j].Target);
                            graph.AddEdge(newEdge);
                            result.AddEdges.Add(newEdge);
                        }
                        var newCount = 0;
                        Recycle(graph, search, maxCount, edges[i], result, ref newCount);
                    }
                }
            }
            return node;
        }

        private void Recycle(BidirectionalGraph graph, List<GraphNode> search, int maxCount, List<GraphEdge> edges, ThChargerGraphData result, ref int count)
        {
            for (var i = 0; i < edges.Count; i++)
            {
                for (var j = i + 1; j < edges.Count; j++)
                {
                    var newEdge = new GraphEdge(edges[i].Target, edges[j].Target);
                    graph.AddEdge(newEdge);
                    result.AddEdges.Add(newEdge);
                }
                Recycle(graph, search, maxCount, edges[i], result, ref count);
            }
        }

        private void Recycle(BidirectionalGraph graph, List<GraphNode> search, int maxCount, GraphEdge edge, ThChargerGraphData result, ref int count)
        {
            search.Add(edge.Target);
            count++;
            var nextNode = Recycle(graph, search, maxCount, edge.Target, result, ref count);
            if (count >= maxCount)
            {
                count = 0;
            }
            var nextEdges = graph.OutEdges(nextNode).Where(o => !search.Contains(o.Target)).ToList();
            result.RemoveEdges.AddRange(nextEdges);
            Recycle(graph, search, maxCount, nextEdges, result, ref count);
        }

        private void AddEdge(BidirectionalGraph graph, GraphEdge edge)
        {
            graph.AddVertex(edge.Source);
            graph.AddVertex(edge.Target);
            graph.AddEdge(edge);
            graph.AddEdge(edge.Inverse());
        }

    }
}
