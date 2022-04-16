using System;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
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
        private Dictionary<string, Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>> IdToNodes { get; set; }
        private Dictionary<string, Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>> IdToEdges { get; set; }

        public void Diff(PDSGraph source, PDSGraph target)
        {
            //1、整理数据
            RecordNodes(source, target);
            RecordEdges(source, target);

            //2.1、记录“交换”变化
            ComapreExChangeForNode(source, target);
            //2.2、记录Node变化
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
                if (id == "")
                {
                    continue;
                }
                if (!IdToNodes.ContainsKey(id))
                {
                    IdToNodes.Add(id, new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(nodeA, null));
                }
                else
                {
                    throw new NotImplementedException(); //重复nodeIdA
                }
            }
            foreach (var nodeB in graphB.Vertices)
            {
                var id = nodeB.Load.ID.LoadID;
                if (id == "")
                {
                    continue;
                }
                if (IdToNodes.ContainsKey(id))
                {
                    if (IdToNodes[id].Item2 != null)
                    {
                        throw new NotImplementedException(); //重复nodeIdB
                    }
                    if(IdToNodes[id].Item1 != null)
                    {
                        IdToNodes[id] = new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(IdToNodes[id].Item1, nodeB);
                    }
                }
                else
                {
                    IdToNodes.Add(id, new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(null, nodeB));
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
                if(id == "")
                {
                    continue;
                }
                if (!IdToEdges.ContainsKey(id))
                {
                    IdToEdges.Add(id, new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(edgeA, null));
                }
                else
                {
                    throw new NotImplementedException(); //重复edgeIdA
                }
            }
            foreach (var edgeB in graphB.Edges)
            {
                var id = edgeB.Circuit.ID.CircuitNumber.Last();
                if (id == "")
                {
                    continue;
                }
                if(edgeB == null)
                {
                    throw new NotImplementedException();
                }
                if (IdToEdges.ContainsKey(id))
                {
                    if (IdToEdges[id].Item2 != null)
                    {
                        throw new NotImplementedException(); //重复edgeIdB
                    }
                    if(IdToEdges[id].Item1 != null)
                    {
                        IdToEdges[id] = new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(IdToEdges[id].Item1, edgeB);
                    }
                }
                else
                {
                    IdToEdges.Add(id, new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(null, edgeB));
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
                if (idToNode.Value.Item1 != null && idToNode.Value.Item2 != null)
                {
                    nodeIdVisit.Add(idToNode.Key, false);
                }
            }
            var nodeIdList = nodeIdVisit.Keys.ToList();
            foreach (var idA in nodeIdList)
            {
                //疑问：交换可能是多对多（双胞胎上下文一样）并非一对一的交换，如何处理？a可能和b、c或d之一交换？
                if (nodeIdVisit[idA] == true)
                {
                    continue;
                }
                foreach (var idB in nodeIdList)
                {
                    if (nodeIdVisit[idB] == true)
                    {
                        continue;
                    }
                    var nodeA = IdToNodes[idA].Item1;
                    var nodeB = IdToNodes[idB].Item1;
                    if (idA == idB)
                    {
                        continue;
                    }
                    nodeIdVisit[idA] = true;
                    nodeIdVisit[idB] = true;
                    if (NodesSameEnvironment(IdToNodes[idA].Item1, IdToNodes[idB].Item2, graphA, graphB)
                        && NodesSameEnvironment(IdToNodes[idB].Item1, IdToNodes[idA].Item2, graphA, graphB))
                    {
                        DataChangeForNode(IdToNodes[idA].Item1, IdToNodes[idB].Item2);
                        DataChangeForNode(IdToNodes[idB].Item1, IdToNodes[idA].Item2);
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
                if (idToNode.Value.Item1 != null && idToNode.Value.Item2 == null)
                {
                    nodeIdVisit.Add(idToNode.Key, false);
                }
            }
            foreach (var idToNode in IdToNodes)
            {
                if (idToNode.Value.Item1 == null && idToNode.Value.Item2 != null)
                {
                    var nodeIdList = nodeIdVisit.Keys.ToList();
                    var idB = idToNode.Key;
                    foreach (var idA in nodeIdList)
                    {
                        if (nodeIdVisit[idA] == true)
                        {
                            continue;
                        }
                        if (NodesSameEnvironment(IdToNodes[idA].Item1, IdToNodes[idB].Item2, graphA, graphB))
                        {
                            StructureForChangeNodeID(IdToNodes[idA].Item1, idToNode.Value.Item2, graphA);
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
                line2lineId.Add(new Tuple<string, string>(idToEdge.Value.Item1.Source.Load.ID.LoadID, idToEdge.Value.Item1.Target.Load.ID.LoadID), idToEdge.Key);
            }
            foreach (var idToEdge in IdToEdges)
            {
                var curEdge = idToEdge.Value.Item2;
                var curId = idToEdge.Key;
                var tmpLine = new Tuple<string, string>(curEdge.Source.Load.ID.LoadID, curEdge.Target.Load.ID.LoadID);
                if (line2lineId.ContainsKey(tmpLine) && curId != line2lineId[tmpLine])
                {
                    var compareEdge = IdToEdges[line2lineId[tmpLine]].Item1;
                    if (curEdge.Details == compareEdge.Details) ///////////
                    {
                        StructureForChangeEdgeID(compareEdge, curEdge, graphA);
                    }
                }
            }
        }
        #endregion

        #region MOVE_ADD_DEL
        /// <summary>
        /// 比较结点的变化
        /// （结点移动定义：某一结点的所处的位置变了（父母（入）和孩子（出）和原图比没一个相同的），但并没有从原图中删除）
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        /// <returns></returns>
        private void CompareNodes(PDSGraph graphA, PDSGraph graphB)
        {
            foreach (var idToNode in IdToNodes)
            {
                var id = idToNode.Key;
                var nodeA = idToNode.Value.Item1;
                var nodeB = idToNode.Value.Item2;
                bool haveDiff = false;
                var changeNode = nodeA;
                if (nodeA != null && nodeB != null)
                {
                    haveDiff = DataChangeForNode(nodeA, nodeB);
                    bool exChang = false;
                    bool idChang = false;
                    if (nodeA.Tag is ThPDSProjectGraphNodeExchangeTag exTag)
                    {
                        exChang = exTag.HaveState;
                    }
                    if (nodeA.Tag is ThPDSProjectGraphNodeIdChangeTag idTag)
                    {
                        idChang = idTag.HaveState;
                    }
                    if (exChang || idChang)
                    {
                        continue;
                    }
                    else if (NodeStructureMove(nodeA, nodeB, graphA, graphB))
                    {
                        haveDiff = true;
                        StructureForMoveNode(nodeA, nodeB, graphA);
                    }
                }
                else if (nodeA != null && nodeB == null)
                {
                    haveDiff = true;
                    StructureForDeleteNode(nodeA);
                }
                else if (nodeA == null && nodeB != null)
                {
                    haveDiff = true;
                    StructureForAddNode(nodeB, graphA);
                    changeNode = nodeB;
                }
                if (!haveDiff)
                {
                    NoDiffForNode(changeNode);
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
                var id = idToEdge.Key;
                var edgeA = idToEdge.Value.Item1;
                var edgeB = idToEdge.Value.Item2;
                if (edgeA != null && edgeB != null)
                {
                    if (edgeA.Source != edgeB.Source)
                    {
                        throw new NotImplementedException(); //两条边同一个id不同起点
                    }
                    else if (edgeA.Target != edgeB.Target)
                    {
                        StructureForMoveEdge(edgeA, edgeB, graphA);
                    }
                    else if (edgeA.Target == edgeB.Target)
                    {
                        if (!DataChangeForEdge(edgeA, edgeB))
                        {
                            NoDiffForEdge(edgeA);
                        }
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
                if (inEdgesIdB.Contains(inEdgeIdA))
                {
                    return false;
                }
            }

            var outNodesIdA = new HashSet<string>();
            var outNodesIdB = new HashSet<string>();
            graphA.OutEdges(nodeA).ForEach(e => outNodesIdA.Add(e.Target.Load.ID.LoadID));
            graphB.OutEdges(nodeB).ForEach(e => outNodesIdB.Add(e.Target.Load.ID.LoadID));
            foreach (var outNodeIdA in outNodesIdA)
            {
                if (outNodesIdB.Contains(outNodeIdA))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region NODE_STRUCTURE
        private void StructureForChangeNodeID(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, PDSGraph graphA)
        {
            nodeA.Tag = new ThPDSProjectGraphNodeIdChangeTag
            {
                HaveState = true,
                ChangeFrom = true,
                ChangeTo = false,
                ChangeFromID = nodeA.Load.ID.LoadID,
                ChangeToID = nodeB.Load.ID.LoadID,
            };
            nodeB.Tag = new ThPDSProjectGraphNodeIdChangeTag
            {
                HaveState = true,
                ChangeFrom = false,
                ChangeTo = true,
                ChangeFromID = nodeA.Load.ID.LoadID,
                ChangeToID = nodeB.Load.ID.LoadID,
            };
            graphA.AddVertex(nodeB);
        }

        private void StructureForExchangeNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB)
        {
            nodeA.Tag = new ThPDSProjectGraphNodeExchangeTag
            {
                HaveState = true,
                ExchangeToID = nodeB.Load.ID.LoadID,
                ExchangeToNode = nodeB
            };
            nodeB.Tag = new ThPDSProjectGraphNodeExchangeTag
            {
                HaveState = true,
                ExchangeToID = nodeA.Load.ID.LoadID,
                ExchangeToNode = nodeA
            };
        }

        private void StructureForMoveNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, PDSGraph graphA)
        {
            nodeA.Tag = new ThPDSProjectGraphNodeMoveTag
            {
                HaveState = true,
                MoveFrom = true,
                MoveTo = false
            };
            nodeB.Tag = new ThPDSProjectGraphNodeMoveTag
            {
                HaveState = true,
                MoveFrom = false,
                MoveTo = true
            };
            graphA.AddVertex(nodeB);
        }

        private void StructureForAddNode(ThPDSProjectGraphNode nodeB, PDSGraph graphA)
        {
            nodeB.Tag = new ThPDSProjectGraphNodeDeleteTag
            {
                HaveState = true,
            };
            graphA.AddVertex(nodeB);
        }

        private void StructureForDeleteNode(ThPDSProjectGraphNode nodeA)
        {
            nodeA.Tag = new ThPDSProjectGraphNodeDeleteTag
            {
                HaveState = true,
            };
        }
        #endregion

        #region EDGE_STRUCTURE
        private void StructureForChangeEdgeID(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB, PDSGraph graphA)
        {
            var edgeBSource = IdToNodes[edgeB.Source.Load.ID.LoadID].Item2;
            var edgeBtarget = IdToNodes[edgeB.Target.Load.ID.LoadID].Item2; //Item2若没有则说明还没有添加入edgeA,但好像并不影响
            var newEdge = new ThPDSProjectGraphEdge(edgeBSource, edgeBtarget);
            newEdge.Circuit.ID.CircuitID = edgeB.Circuit.ID.CircuitID;
            newEdge.Circuit.ID.CircuitNumber.Add(edgeB.Circuit.ID.CircuitNumber.Last());
            edgeA.Tag = new ThPDSProjectGraphEdgeIdChangeTag
            {
                HaveState = true,
                ChangeFrom = true,
                ChangeTo = false,
                ChangeFromLastCircuitID = edgeA.Circuit.ID.CircuitNumber.Last(),
                ChangeToLastCircuitID = newEdge.Circuit.ID.CircuitNumber.Last(),
            };
            newEdge.Tag = new ThPDSProjectGraphEdgeIdChangeTag
            {
                HaveState = true,
                ChangeFrom = false,
                ChangeTo = true,
                ChangeFromLastCircuitID = edgeA.Circuit.ID.CircuitNumber.Last(),
                ChangeToLastCircuitID = newEdge.Circuit.ID.CircuitNumber.Last(),
            };
            graphA.AddEdge(newEdge);
        }

        private void StructureForMoveEdge(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB, PDSGraph graphA)
        {
            edgeA.Tag = new ThPDSProjectGraphEdgeMoveTag
            {
                HaveState = true,
                MoveFrom = true,
                //MoveTo = false
            };
            var edgeASourceInGraphA = IdToNodes[edgeA.Source.Load.ID.LoadID].Item1;
            var edgeBtargetInGraphA = IdToNodes[edgeB.Target.Load.ID.LoadID].Item2; //Item2若没有则说明还没有添加入edgeA,但好像并不影响
            var newEdge = new ThPDSProjectGraphEdge(edgeASourceInGraphA, edgeBtargetInGraphA);
            newEdge.Circuit.ID.CircuitNumber.Add(edgeB.Circuit.ID.CircuitNumber.Last());
            newEdge.Circuit.ID.CircuitID = edgeB.Circuit.ID.CircuitID;
            newEdge.Tag = new ThPDSProjectGraphEdgeMoveTag
            {
                HaveState = true,
                //MoveFrom = false,
                MoveTo = true
            };
            graphA.AddEdge(newEdge);
        }

        private void StructureForAddEdge(ThPDSProjectGraphEdge edgeB, PDSGraph graphA)
        {
            var edgeBSourceInGraphA = IdToNodes[edgeB.Source.Load.ID.CircuitNumber.Last()].Item1;
            var edgeBTargetInGraphA = IdToNodes[edgeB.Target.Load.ID.CircuitNumber.Last()].Item1;
            var newEdge = new ThPDSProjectGraphEdge(edgeBSourceInGraphA, edgeBTargetInGraphA);
            newEdge.Circuit.ID.CircuitNumber.Add(edgeB.Circuit.ID.CircuitNumber.Last());
            newEdge.Circuit.ID.CircuitID = edgeB.Circuit.ID.CircuitID;
            newEdge.Tag = new ThPDSProjectGraphEdgeAddTag
            {
                HaveState = true,
            };
            graphA.AddEdge(newEdge);
        }

        private void StructureForDeleteEdge(ThPDSProjectGraphEdge edgeA)
        {
            edgeA.Tag = new ThPDSProjectGraphEdgeDeleteTag
            {
                HaveState = true,
            };
        }
        #endregion

        #region DATA_CHANGE
        private bool DataChangeForNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB)
        {
            var dataTagA = new ThPDSProjectGraphNodeDataTag();
            if (nodeA.Load.ID.Description != nodeB.Load.ID.Description)
            {
                dataTagA.TagD = true;
                //dataTagA.SouD = nodeA.Load.ID.Description;
                dataTagA.TarD = nodeB.Load.ID.Description;
            }
            if (nodeA.Load.FireLoad != nodeB.Load.FireLoad)
            {
                dataTagA.TagF = true;
                //dataTagA.SouF = nodeA.Load.FireLoad;
                dataTagA.TarF = nodeB.Load.FireLoad;
            }
            if (nodeA.Load.InstalledCapacity != nodeB.Load.InstalledCapacity)
            {
                dataTagA.TagP = true;
                //dataTagA.SouP = nodeA.Load.InstalledCapacity;
                dataTagA.TarP = nodeB.Load.InstalledCapacity;
            }
            if (dataTagA.TagD || dataTagA.TagF || dataTagA.TagP)
            {
                dataTagA.HaveState = true;
            }
            nodeA.Tag = dataTagA;
            return dataTagA.HaveState;
        }

        private bool DataChangeForEdge(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB)
        {
            var dataTagA = new ThPDSProjectGraphEdgeDataTag();
            if (edgeA.Circuit.ID.CircuitID.Last() != edgeB.Circuit.ID.CircuitID.Last())
            {
                dataTagA.HaveState = true;
                dataTagA.ToLastCircuitID = edgeB.Circuit.ID.CircuitID.Last();
            }
            edgeA.Tag = dataTagA;
            return dataTagA.HaveState;
        }
        #endregion

        #region NO_DIFF
        private void NoDiffForNode(ThPDSProjectGraphNode nodeA)
        {
            nodeA.Tag = new ThPDSProjectGraphNodeDataTag
            {
                HaveState = true
            };
        }

        private void NoDiffForEdge(ThPDSProjectGraphEdge edgeA)
        {
            edgeA.Tag = new ThPDSProjectGraphEdgeNoDifferenceTag
            {
                HaveState = true
            };
        }
        #endregion
    }
}
