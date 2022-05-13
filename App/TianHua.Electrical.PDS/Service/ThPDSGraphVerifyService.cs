﻿using System;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using QuikGraph.Algorithms;
using TianHua.Electrical.PDS.Project.Module;
using PDSGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSGraphVerifyService
    {
        private bool BeSuccess { get; set; }   

        public bool Verify(PDSGraph graph)
        {
            BeSuccess = true;
            NodeVerify(graph);
            EdgeVerify(graph);
            return BeSuccess;
        }

        public void NodeVerify(PDSGraph graph)
        {
            var IdToNodes = new Dictionary<string, List<ThPDSProjectGraphNode>>();
            foreach (var node in graph.Vertices)
            {
                var id = node.Load.ID.LoadID;
                if (!IdToNodes.ContainsKey(id))
                {
                    IdToNodes.Add(id, new List<ThPDSProjectGraphNode>());
                }
                IdToNodes[id].Add(node);

                var inEdges = graph.InEdges(node).ToHashSet();
                var outEdgesCount = graph.OutEdges(node).Count();
                if (0 == inEdges.Count && 0 == outEdgesCount)
                {
                    TagNodeSingle(node);
                }
            }
            foreach(var idToNode in IdToNodes)
            {
                if(idToNode.Value.Count > 1)
                {
                    foreach(var node in idToNode.Value)
                    {
                        if (RightDupNodeId(node.Load.ID.LoadID))
                        {
                            continue;
                        }
                        TagNodeDuplicate(node);
                    }
                }
            }
            NodeFire(graph);
            NodeCascadingErrorCheck(graph);
        }

        public void EdgeVerify(PDSGraph graph)
        {
            var IdToEdges = new Dictionary<string, List<ThPDSProjectGraphEdge>>();
            foreach(var edge in graph.Edges)
            {
                var id = edge.Circuit.ID.CircuitNumber.Last();
                if (!IdToEdges.ContainsKey(id))
                {
                    IdToEdges.Add(id, new List<ThPDSProjectGraphEdge>());
                }
                IdToEdges[id].Add(edge);
                //if (edge.Circuit.ID.SourcePanelID.Last().IsNullOrEmpty())
                //{
                //    TagEdgeSingle(edge);
                //}
            }
            foreach(var idToEdge in IdToEdges)
            {
                if (idToEdge.Value.Count > 1)
                {
                    foreach (var edge in idToEdge.Value)
                    {
                        TagEdgeDuplicate(edge);
                    }
                }
            }
            EdgeCascadingErrorCheck(graph);
        }

        private void NodeFire(PDSGraph graph)
        {
            var nodeNoFire = new HashSet<ThPDSProjectGraphNode>();
            foreach (var node in graph.TopologicalSort())
            {
                if (node.Load.FireLoad == false)
                {
                    nodeNoFire.Add(node);
                }
                else
                {
                    foreach(var inEdge in graph.InEdges(node))
                    {
                        if(nodeNoFire.Contains(inEdge.Source))
                        {
                            nodeNoFire.Add(node);
                            TagNodeFire(node);
                            break;
                        }
                    }
                }
            }
        }

        private void NodeCascadingErrorCheck(PDSGraph graph)
        {
            foreach (var node in graph.Vertices)
            {
                if (graph.OutEdges(node).Any(o => o.Details.CascadeCurrent > node.Details.CascadeCurrent))
                {
                    TagNodeCascadingError(node);
                }
            }
        }

        private bool RightDupNodeId(string nodeId)
        {
            if(nodeId == "")
            {
                return true;
            }
            else
            {
                if(nodeId.Length < 2)
                {
                    return false;
                }
                if (nodeId.StartsWith("AC") || nodeId.Contains("AR"))
                {
                    return true;
                }
                return false;
            }
        }

        //Tags
        private void TagNodeDuplicate(ThPDSProjectGraphNode node)
        {
            BeSuccess = false;
            var dupTag = new ThPDSProjectGraphNodeDuplicateTag();
            if(node.Tag.IsNull())
            {
                node.Tag = dupTag;
            }
            else
            {
                node.Tag = new ThPDSProjectGraphNodeCompositeTag
                {
                    ValidateTag = dupTag,
                };
            }
        }

        private void TagNodeSingle(ThPDSProjectGraphNode node)
        {
            BeSuccess = false;
            var singleTag = new ThPDSProjectGraphNodeSingleTag();
            if (node.Tag.IsNull())
            {
                node.Tag = singleTag;
            }
            else if (node.Tag is ThPDSProjectGraphNodeDuplicateTag dupTag)
            {
                node.Tag = new ThPDSProjectGraphNodeCompositeTag
                {
                    DupTag = dupTag,
                    ValidateTag = singleTag
                };
            }
        }

        private void TagNodeFire(ThPDSProjectGraphNode node)
        {
            BeSuccess = false;
            var fireTag = new ThPDSProjectGraphNodeFireTag();
            if (node.Tag.IsNull())
            {
                node.Tag = fireTag;
            }
            else if(node.Tag is ThPDSProjectGraphNodeDuplicateTag dupTag)
            {
                node.Tag = new ThPDSProjectGraphNodeCompositeTag
                {
                    DupTag = dupTag,
                    ValidateTag= fireTag
                };
            }
        }

        private void TagNodeCascadingError(ThPDSProjectGraphNode node)
        {
            BeSuccess = false;
            var CascadingError = new ThPDSProjectGraphNodeCascadingErrorTag();
            if (node.Tag.IsNull())
            {
                node.Tag = CascadingError;
            }
            else if (node.Tag is ThPDSProjectGraphNodeDuplicateTag dupTag)
            {
                node.Tag = new ThPDSProjectGraphNodeCompositeTag
                {
                    DupTag = dupTag,
                    CascadingErrorTag= CascadingError,
                };
            }
            else if (node.Tag is ThPDSProjectGraphNodeValidateTag validateTag)
            {
                node.Tag = new ThPDSProjectGraphNodeCompositeTag
                {
                    ValidateTag = validateTag,
                    CascadingErrorTag= CascadingError,
                };
            }
            else if (node.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
            {
                compositeTag.CascadingErrorTag= CascadingError;
            }
        }

        private void TagEdgeDuplicate(ThPDSProjectGraphEdge edge)
        {
            BeSuccess = false;
            var dupTag = new ThPDSProjectGraphEdgeDuplicateTag();
            if (edge.Tag.IsNull())
            {
                edge.Tag = dupTag;
            }
            else if (edge.Tag is ThPDSProjectGraphEdgeSingleTag singleTag)
            {
                edge.Tag = new ThPDSProjectGraphEdgeCompositeTag
                {
                    DupTag = dupTag,
                    SingleTag = singleTag
                };
            }
        }

        private void EdgeCascadingErrorCheck(PDSGraph graph)
        {
            foreach (var edge in graph.Edges)
            {
                if(edge.Details.CascadeCurrent <= edge.Target.Details.CascadeCurrent)
                {
                    TagEdgeCascadingError(edge);
                }
            }    
        }

        private void TagEdgeSingle(ThPDSProjectGraphEdge edge)
        {
            BeSuccess = false;
            var singleTag = new ThPDSProjectGraphEdgeSingleTag();
            if (edge.Tag.IsNull())
            {
                edge.Tag = singleTag;
            }
            else if (edge.Tag is ThPDSProjectGraphEdgeDuplicateTag dupTag)
            {
                edge.Tag = new ThPDSProjectGraphEdgeCompositeTag
                {
                    DupTag = dupTag,
                    SingleTag = singleTag
                };
            }
        }

        private void TagEdgeCascadingError(ThPDSProjectGraphEdge edge)
        {
            BeSuccess = false;
            var cascadingError = new ThPDSProjectGraphEdgeCascadingErrorTag();
            if (edge.Tag.IsNull())
            {
                edge.Tag = cascadingError;
            }
            else if (edge.Tag is ThPDSProjectGraphEdgeSingleTag singleTag)
            {
                edge.Tag = new ThPDSProjectGraphEdgeCompositeTag
                {
                    CascadingErrorTag = cascadingError,
                    SingleTag = singleTag
                };
            }
            else if (edge.Tag is ThPDSProjectGraphEdgeDuplicateTag dupTag)
            {
                edge.Tag = new ThPDSProjectGraphEdgeCompositeTag
                {
                    CascadingErrorTag = cascadingError,
                    DupTag = dupTag
                };
            }
            else if(edge.Tag is ThPDSProjectGraphEdgeCompositeTag compositeTag)
            {
                compositeTag.CascadingErrorTag = cascadingError;
            }
        }
    }
}
