using System;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using PDSGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Service
{
    /// <summary>
    /// 图比较器
    /// </summary>
    public class ThPDSGraphCompareService
    {
        private Dictionary<string, List<Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>>> IdToNodes = new Dictionary<string, List<Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>>>();
        private Dictionary<string, List<Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>>> IdToEdges = new Dictionary<string, List<Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>>>();

        public void Diff(PDSGraph source, PDSGraph target)
        {
            //1、整理数据
            RecordNodes(source, target);
            RecordEdges(source, target);

            //2.1、记录Node“交换”变化
            ComapreExChangeForNode(source, target);
            
            //2.2、记录Node其他变化
            ComapreChangeIdForNode(source, target);
            CompareNodes(source, target);

            //2.3 记录Edge变化
            ComapreChangeIdForEdge(source);
            CompareEdges(source);
        }

        #region RECORD
        /// <summary>
        /// 将结点数据记录入字典
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        private void RecordNodes(PDSGraph graphA, PDSGraph graphB)
        {
            foreach (var nodeA in graphA.Vertices)
            {
                var id = nodeA.Load.ID.LoadID;
                if (!IdToNodes.ContainsKey(id))
                {
                    IdToNodes.Add(id, new List<Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>>());
                }
                IdToNodes[id].Add(new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(nodeA, null));
            }
            foreach (var nodeB in graphB.Vertices)
            {
                var id = nodeB.Load.ID.LoadID;
                if (IdToNodes.ContainsKey(id))
                {
                    if(IdToNodes[id].Count == 1 && IdToNodes[id].First().Item1 != null)
                    {
                        IdToNodes[id] = new List<Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>> { new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(IdToNodes[id].First().Item1, nodeB) };
                    }
                    else
                    {
                        IdToNodes[id].Add(new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(null, nodeB));
                    }
                }
                else
                {
                    IdToNodes.Add(id, new List<Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>> { new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(null, nodeB) });
                }
            }
        }

        /// <summary>
        /// 将边数据记录入字典
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        private void RecordEdges(PDSGraph graphA, PDSGraph graphB)
        {
            foreach (var edgeA in graphA.Edges)
            {
                var id = edgeA.Circuit.ID.CircuitNumber.Last();
                if (!IdToEdges.ContainsKey(id))
                {
                    IdToEdges.Add(id, new List<Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>>());
                }
                IdToEdges[id].Add(new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(edgeA, null));
            }
            foreach (var edgeB in graphB.Edges)
            {
                var id = edgeB.Circuit.ID.CircuitNumber.Last();
                if (IdToEdges.ContainsKey(id))
                {
                    if (IdToEdges[id].Count == 1 && IdToEdges[id].First().Item1 != null)
                    {
                        IdToEdges[id] = new List<Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>> { new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(IdToEdges[id].First().Item1, edgeB) };
                    }
                    else
                    {
                        IdToEdges[id].Add(new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(null, edgeB));
                    }
                }
                else
                {
                    IdToEdges.Add(id, new List<Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>> { new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(null, edgeB) });
                }
            }
        }
        #endregion

        #region EX_ID_CHANGE
        /// <summary>
        /// 记录两图中的交换变化
        /// 交换定义：一对结点的父亲id以及孩子id相互交换，这对结点为交换）
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        private void ComapreExChangeForNode(PDSGraph graphA, PDSGraph graphB)
        {
            Dictionary<string, bool> nodeIdVisit = new Dictionary<string, bool>();
            foreach (var idToNode in IdToNodes)
            {
                if (idToNode.Value.Count > 1)
                {
                    continue;
                }
                if (idToNode.Value.First().Item1 != null && idToNode.Value.First().Item2 != null)
                {
                    nodeIdVisit.Add(idToNode.Key, false);
                }
            }
            var nodeIdList = nodeIdVisit.Keys.ToList();
            foreach (var idA in nodeIdList)
            {
                if (nodeIdVisit[idA] == true || IdToNodes[idA].Count > 1)
                {
                    continue;
                }
                foreach (var idB in nodeIdList)
                {
                    if (nodeIdVisit[idB] == true || IdToNodes[idB].Count > 1)
                    {
                        continue;
                    }
                    var nodeA = IdToNodes[idA].First().Item1;
                    var nodeB = IdToNodes[idB].First().Item1;
                    if (idA == idB)
                    {
                        continue;
                    }
                    nodeIdVisit[idA] = true;
                    nodeIdVisit[idB] = true;
                    if (NodesSameEnvironment(IdToNodes[idA].First().Item1, IdToNodes[idB].First().Item2, graphA, graphB)
                        && NodesSameEnvironment(IdToNodes[idB].First().Item1, IdToNodes[idA].First().Item2, graphA, graphB))
                    {
                        DataChangeForNode(IdToNodes[idA].First().Item1, IdToNodes[idB].First().Item2);
                        DataChangeForNode(IdToNodes[idB].First().Item1, IdToNodes[idA].First().Item2);
                        StructureForExchangeNode(nodeA, nodeB);
                    }
                }
            }
        }

        /// <summary>
        /// 记录两图改变结点id的变化
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        private void ComapreChangeIdForNode(PDSGraph graphA, PDSGraph graphB)
        {
            Dictionary<string, bool> nodeIdVisit = new Dictionary<string, bool>();
            foreach (var idToNode in IdToNodes)
            {
                if (idToNode.Value.Count > 1)
                {
                    continue;
                }
                if (idToNode.Value.First().Item1 != null && idToNode.Value.First().Item2 == null)
                {
                    nodeIdVisit.Add(idToNode.Key, false);
                }
            }
            foreach (var idToNode in IdToNodes)
            {
                if (idToNode.Value.Count > 1)
                {
                    continue;
                }
                if (idToNode.Value.First().Item1 == null && idToNode.Value.First().Item2 != null)
                {
                    var nodeIdList = nodeIdVisit.Keys.ToList();
                    var idB = idToNode.Key;
                    foreach (var idA in nodeIdList)
                    {
                        if (nodeIdVisit[idA] == true)
                        {
                            continue;
                        }
                        if (NodesSameEnvironment(IdToNodes[idA].First().Item1, IdToNodes[idB].First().Item2, graphA, graphB))
                        {
                            StructureForChangeNodeID(IdToNodes[idA].First().Item1, idToNode.Value.First().Item2, graphA);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 比较两个结点的上下文(父子结点以及出入边)是否完全一致
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="nodeB"></param>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        /// <returns></returns>
        private bool NodesSameEnvironment(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, PDSGraph graphA, PDSGraph graphB)
        {
            var inEdgesA = graphA.InEdges(nodeA).ToHashSet();
            var inEdgesB = graphB.InEdges(nodeB).ToHashSet();
            if (inEdgesA.Count != inEdgesB.Count)
            {
                return false;
            }
            var outEdgesA = graphA.OutEdges(nodeA).ToHashSet();
            var outEdgesB = graphB.OutEdges(nodeB).ToHashSet();
            if (outEdgesA.Count != outEdgesB.Count)
            {
                return false;
            }

            //入边相等判断
            var inEdgesIdA = new HashSet<string>();
            var inEdgesIdB = new HashSet<string>();
            inEdgesA.ForEach(e => inEdgesIdA.Add(e.Circuit.ID.CircuitNumber.Last()));
            inEdgesB.ForEach(e => inEdgesIdB.Add(e.Circuit.ID.CircuitNumber.Last()));
            foreach (var inEdgeIdA in inEdgesIdA)
            {
                if (!inEdgesIdB.Contains(inEdgeIdA))
                {
                    return false;
                }
            }

            //子结点相等判断
            var outNodesIdA = new HashSet<string>();
            var outNodesIdB = new HashSet<string>();
            outEdgesA.ForEach(e => outNodesIdA.Add(e.Target.Load.ID.LoadID));
            outEdgesB.ForEach(e => outNodesIdB.Add(e.Target.Load.ID.LoadID));
            foreach (var outNodeIdA in outNodesIdA)
            {
                if (!outNodesIdB.Contains(outNodeIdA))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 记录两个图存在的边id修改
        /// </summary>
        private void ComapreChangeIdForEdge(PDSGraph graphA)
        {
            var line2lineId = new Dictionary<Tuple<string, string>, string>();
            foreach (var idToEdge in IdToEdges)
            {
                if (idToEdge.Value.Count > 1)
                {
                    continue;
                }
                if (idToEdge.Value.First().Item1 != null)
                {
                    var curLine = new Tuple<string, string>(idToEdge.Value.First().Item1.Source.Load.ID.LoadID, idToEdge.Value.First().Item1.Target.Load.ID.LoadID);
                    if (line2lineId.ContainsKey(curLine))
                    {
                        if(line2lineId[curLine] != null)
                        {
                            line2lineId[curLine] = null;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        line2lineId.Add(curLine, idToEdge.Key);
                    }
                }
            }
            foreach (var idToEdge in IdToEdges)
            {
                if (idToEdge.Value.Count > 1)
                {
                    continue;
                }
                var curEdge = idToEdge.Value.First().Item2;
                var curId = idToEdge.Key;
                var tmpLine = new Tuple<string, string>(curEdge.Source.Load.ID.LoadID, curEdge.Target.Load.ID.LoadID);
                if (line2lineId.ContainsKey(tmpLine))
                {
                    if(line2lineId[tmpLine] == null)
                    {
                        continue;
                    }
                    else if (curId != line2lineId[tmpLine])
                    {
                        var compareEdge = IdToEdges[line2lineId[tmpLine]].First().Item1;
                        if (curEdge.Circuit.ID.CircuitID.Last() == compareEdge.Circuit.ID.CircuitID.Last())
                        {
                            StructureForChangeEdgeID(compareEdge, curEdge, graphA);
                        }
                    }
                }
            }
        }
        #endregion

        #region MOVE_ADD_DEL
        /// <summary>
        /// 比较结点的变化
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        /// <returns></returns>
        private void CompareNodes(PDSGraph graphA, PDSGraph graphB)
        {
            foreach (var idToNode in IdToNodes)
            {
                if (idToNode.Value.Count > 1)
                {
                    continue;
                }
                var nodeA = idToNode.Value.First().Item1;
                var nodeB = idToNode.Value.First().Item2;
                if (nodeA != null && nodeB != null)
                {
                    if (nodeA.Tag is ThPDSProjectGraphNodeExchangeTag)
                    {
                        continue;
                    }
                    if (nodeA.Tag is ThPDSProjectGraphNodeIdChangeTag)
                    {
                        continue;
                    }
                    if (nodeA.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
                    {
                        if (compositeTag.CompareTag is ThPDSProjectGraphNodeExchangeTag)
                        {
                            continue;
                        }
                        if (compositeTag.CompareTag is ThPDSProjectGraphNodeIdChangeTag)
                        {
                            continue;
                        }
                    }
                    DataChangeForNode(nodeA, nodeB);
                    if (NodeStructureMove(nodeA, nodeB, graphA, graphB))
                    {
                        StructureForMoveNode(nodeA, nodeB, graphA);
                    }
                }
                else if (nodeA != null && nodeB == null)
                {
                    StructureForDeleteNode(nodeA);
                }
                else if (nodeA == null && nodeB != null)
                {
                    StructureForAddNode(nodeB, graphA);
                }
            }
        }

        /// <summary>
        /// 比较边的变化
        /// </summary>
        /// <param name="graphA"></param>
        /// <returns></returns>
        private void CompareEdges(PDSGraph graphA)
        {
            foreach (var idToEdge in IdToEdges)
            {
                if (idToEdge.Value.Count > 1)
                {
                    continue;
                }
                var id = idToEdge.Key;
                var edgeA = idToEdge.Value.First().Item1;
                var edgeB = idToEdge.Value.First().Item2;
                if (edgeA != null && edgeB != null)
                {
                    if (edgeA.Tag is ThPDSProjectGraphEdgeIdChangeTag)
                    {
                        continue;
                    }
                    if (edgeA.Tag is ThPDSProjectGraphEdgeCompositeTag compositeTag)
                    {
                        if (compositeTag.CompareTag is ThPDSProjectGraphEdgeIdChangeTag)
                        {
                            continue;
                        }
                    }
                    if (edgeA.Source.Load.ID.LoadID != edgeB.Source.Load.ID.LoadID)
                    {
                        throw new NotImplementedException(); //两条边同一个id不同起点
                    }
                    if (edgeA.Target.Load.ID.LoadID != edgeB.Target.Load.ID.LoadID)
                    {
                        StructureForMoveEdge(edgeA, edgeB, graphA);
                    }
                }
                else if (edgeA != null && edgeB == null)
                {
                    StructureForDeleteEdge(edgeA);
                }
                else if (edgeA == null && edgeB != null)
                {
                    StructureForAddEdge(edgeB, graphA);
                }
            }
        }

        /// <summary>
        /// 比较结点A是否是移动到结点B
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="nodeB"></param>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        /// <returns></returns>
        public bool NodeStructureMove(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, PDSGraph graphA, PDSGraph graphB)
        {
            if (nodeA.Load.ID.LoadID != nodeB.Load.ID.LoadID)
            {
                return false;
            }

            var inEdgesIdA = new HashSet<string>();
            var inEdgesIdB = new HashSet<string>();
            graphA.InEdges(nodeA).ForEach(e => inEdgesIdA.Add(e.Circuit.ID.CircuitNumber.Last()));
            graphB.InEdges(nodeB).ForEach(e => inEdgesIdB.Add(e.Circuit.ID.CircuitNumber.Last()));
            foreach (var inEdgeIdA in inEdgesIdA)
            {
                if (!inEdgesIdB.Contains(inEdgeIdA))
                {
                    return true;
                }
            }

            var outNodesIdA = new HashSet<string>();
            var outNodesIdB = new HashSet<string>();
            graphA.OutEdges(nodeA).ForEach(e => outNodesIdA.Add(e.Target.Load.ID.LoadID));
            graphB.OutEdges(nodeB).ForEach(e => outNodesIdB.Add(e.Target.Load.ID.LoadID));
            foreach (var outNodeIdA in outNodesIdA)
            {
                if (!outNodesIdB.Contains(outNodeIdA))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region NODE_STRUCTURE
        private void StructureForChangeNodeID(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, PDSGraph graphA)
        {
            var idChangeTagA = new ThPDSProjectGraphNodeIdChangeTag
            {
                ChangeFrom = true,
                ChangedID = nodeB.Load.ID.LoadID,
            };
            AddNodeTag(nodeA, idChangeTagA);

            var idChangeTagB = new ThPDSProjectGraphNodeIdChangeTag
            {
                ChangeFrom = false,
                ChangedID = nodeA.Load.ID.LoadID,
            };
            nodeB.Tag = idChangeTagB;
            graphA.AddVertex(nodeB);
        }

        private void StructureForExchangeNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB)
        {
            var exTagA = new ThPDSProjectGraphNodeExchangeTag
            {
                ExchangeToID = nodeB.Load.ID.LoadID,
                ExchangeToNode = nodeB
            };
            AddNodeTag(nodeA, exTagA);

            var exTagB = new ThPDSProjectGraphNodeExchangeTag
            {
                ExchangeToID = nodeA.Load.ID.LoadID,
                ExchangeToNode = nodeA
            };
            AddNodeTag(nodeB, exTagB);
        }

        private void StructureForMoveNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, PDSGraph graphA)
        {
            var moveTagA = new ThPDSProjectGraphNodeMoveTag
            {
                MoveFrom = true,
                AnotherNode = nodeB,
            };
            AddNodeTag(nodeA, moveTagA);

            var moveTagB = new ThPDSProjectGraphNodeMoveTag
            {
                MoveFrom = false,
                AnotherNode = nodeA,
            };
            AddNodeTag(nodeB, moveTagB);
            graphA.AddVertex(nodeB);
        }

        private void StructureForAddNode(ThPDSProjectGraphNode nodeB, PDSGraph graphA)
        {
            var addTag = new ThPDSProjectGraphNodeAddTag();
            AddNodeTag(nodeB, addTag);
            graphA.AddVertex(nodeB);
        }

        private void StructureForDeleteNode(ThPDSProjectGraphNode nodeA)
        {
            var delTag = new ThPDSProjectGraphNodeDeleteTag();
            AddNodeTag(nodeA, delTag);
        }
        #endregion

        #region EDGE_STRUCTURE
        private void StructureForChangeEdgeID(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB, PDSGraph graphA)
        {
            var newEdge = CreatEdge(IdToNodes[edgeB.Source.Load.ID.LoadID].First().Item2, IdToNodes[edgeB.Target.Load.ID.LoadID].First().Item2,
                edgeB.Circuit.ID.CircuitID, edgeB.Circuit.ID.CircuitNumber.Last());
            var changIdTagA = new ThPDSProjectGraphEdgeIdChangeTag
            {
                ChangeFrom = true,
                ChangedLastCircuitID = newEdge.Circuit.ID.CircuitNumber.Last(),
            };
            AddEdgeTag(edgeA, changIdTagA);

            newEdge.Tag = new ThPDSProjectGraphEdgeIdChangeTag
            {
                ChangeFrom = false,
                ChangedLastCircuitID = edgeA.Circuit.ID.CircuitNumber.Last(),
            };
            graphA.AddEdge(newEdge);
        }

        private void StructureForMoveEdge(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB, PDSGraph graphA)
        {
            var moveTagA = new ThPDSProjectGraphEdgeMoveTag
            {
                MoveFrom = true,
            };
            AddEdgeTag(edgeA, moveTagA);
            var newEdge = CreatEdge(IdToNodes[edgeA.Source.Load.ID.LoadID].First().Item1, IdToNodes[edgeB.Target.Load.ID.LoadID].First().Item2,
                edgeB.Circuit.ID.CircuitID, edgeB.Circuit.ID.CircuitNumber.Last());
            newEdge.Tag = new ThPDSProjectGraphEdgeMoveTag
            {
                MoveFrom = false,
            };
            graphA.AddEdge(newEdge);
        }

        private void StructureForAddEdge(ThPDSProjectGraphEdge edgeB, PDSGraph graphA)
        {
            var newEdge = CreatEdge(IdToNodes[edgeB.Source.Load.ID.CircuitNumber.Last()].First().Item1, IdToNodes[edgeB.Target.Load.ID.CircuitNumber.Last()].First().Item1,
                edgeB.Circuit.ID.CircuitID, edgeB.Circuit.ID.CircuitNumber.Last());
            newEdge.Tag = new ThPDSProjectGraphEdgeAddTag();
            graphA.AddEdge(newEdge);
        }

        private void StructureForDeleteEdge(ThPDSProjectGraphEdge edgeA)
        {
            var delTagA = new ThPDSProjectGraphEdgeDeleteTag();
            AddEdgeTag(edgeA, delTagA);
        }
        #endregion

        #region DATA_CHANGE
        private bool DataChangeForNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB)
        {
            var dataTagA = new ThPDSProjectGraphNodeDataTag();
            if (nodeA.Load.ID.Description != nodeB.Load.ID.Description)
            {
                dataTagA.TagD = true;
                dataTagA.TarD = nodeB.Load.ID.Description;
            }
            if (nodeA.Load.FireLoad != nodeB.Load.FireLoad)
            {
                dataTagA.TagF = true;
                dataTagA.TarF = nodeB.Load.FireLoad;
            }
            if (!nodeA.Load.InstalledCapacity.EqualsTo(nodeB.Load.InstalledCapacity))
            {
                dataTagA.TagP = true;
                dataTagA.TarP = nodeB.Load.InstalledCapacity;
            }
            if (!nodeA.Type.Equals(nodeB.Type))
            {
                dataTagA.TagType = true;
                dataTagA.TarType = nodeB.Type;
            }
            if (nodeA.Load.Phase != nodeB.Load.Phase)
            {
                dataTagA.TagPhase = true;
                dataTagA.TarPhase = nodeB.Load.Phase;
            }
            if (dataTagA.TagD || dataTagA.TagF || dataTagA.TagP || dataTagA.TagType || dataTagA.TagPhase)
            {
                AddNodeTag(nodeA, dataTagA);
                return true;
            }
            return false;
        }
        #endregion

        private ThPDSProjectGraphEdge CreatEdge(ThPDSProjectGraphNode source, ThPDSProjectGraphNode target, List<string> circuitId, string circuitNumber)
        {
            var newEdge = new ThPDSProjectGraphEdge(source, target)
            {
                Circuit = new ThPDSCircuit(),
                Details = new CircuitDetails()
            };
            newEdge.Circuit.ID.CircuitID = circuitId;
            newEdge.Circuit.ID.CircuitNumber.Add(circuitNumber);
            return newEdge;
        }

        #region ADDTAG
        private void AddNodeTag(ThPDSProjectGraphNode node, ThPDSProjectGraphNodeCompareTag cmpareTag)
        {
            if (node.Tag.IsNull())
            {
                node.Tag = cmpareTag;
            }
            else if(cmpareTag is ThPDSProjectGraphNodeDataTag dataCmpTag)
            {
                if (node.Tag is ThPDSProjectGraphNodeDuplicateTag dupTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag()
                    {
                        DataTag = dataCmpTag,
                        DupTag = dupTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeSingleTag sigTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag()
                    {
                        DataTag = dataCmpTag,
                        ValidateTag = sigTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeFireTag fireTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag()
                    {
                        DataTag = dataCmpTag,
                        ValidateTag = fireTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
                {
                    compositeTag.DataTag = dataCmpTag;
                }
            }
            else
            {
                if (node.Tag is ThPDSProjectGraphNodeDuplicateTag dupTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag()
                    {
                        CompareTag = cmpareTag,
                        DupTag = dupTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeSingleTag sigTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag()
                    {
                        CompareTag = cmpareTag,
                        ValidateTag = sigTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeFireTag fireTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag()
                    {
                        CompareTag = cmpareTag,
                        ValidateTag = fireTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeDataTag dataTag)
                {
                    node.Tag = new ThPDSProjectGraphNodeCompositeTag
                    {
                        CompareTag = cmpareTag,
                        DataTag = dataTag
                    };
                }
                else if (node.Tag is ThPDSProjectGraphNodeCompositeTag compositeTag)
                {
                    compositeTag.CompareTag = cmpareTag;
                }
            }
        }

        private void AddEdgeTag(ThPDSProjectGraphEdge edge, ThPDSProjectGraphEdgeCompareTag cmpareTag)
        {
            if (edge.Tag.IsNull())
            {
                edge.Tag = cmpareTag;
            }
            else
            {
                if (edge.Tag is ThPDSProjectGraphEdgeDuplicateTag dupTag)
                {
                    edge.Tag = new ThPDSProjectGraphEdgeCompositeTag()
                    {
                        CompareTag = cmpareTag,
                        DupTag = dupTag
                    };
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeSingleTag singleTag)
                {
                    edge.Tag = new ThPDSProjectGraphEdgeCompositeTag()
                    {
                        CompareTag = cmpareTag,
                        SingleTag = singleTag
                    };
                }
                else if (edge.Tag is ThPDSProjectGraphEdgeCompositeTag compositeTag)
                {
                    compositeTag.CompareTag = cmpareTag;
                }
            }
        }
        #endregion
    }
}
