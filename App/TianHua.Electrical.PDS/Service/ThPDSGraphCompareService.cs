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
        private Dictionary<string, Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>> idToNodes { get; set; }
        private Dictionary<string, Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>> idToEdges { get; set; }

        public void DoCompare(ref PDSGraph source, PDSGraph target)
        {
            //1、整理数据
            RecordNodes(source, target);
            RecordEdges(source, target);

            //2.1、记录“交换”变化
            ComapreExChangeForNode(ref source, target);
            //2.2、记录Node变化
            ComapreChangeIdForNode(ref source, target);
            CompareNodes(ref source, target);
            //2.3 记录Edge变化
            ComapreChangeIdForEdge();
            CompareEdges(ref source, target);
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
                if (!idToNodes.ContainsKey(id))
                {
                    idToNodes.Add(id, new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(nodeA, null));
                }
                else
                {
                    throw new NotImplementedException(); //重复nodeIdA
                }
            }
            foreach (var nodeB in graphB.Vertices)
            {
                var id = nodeB.Load.ID.LoadID;
                if (idToNodes.ContainsKey(id))
                {
                    if (idToNodes[id].Item2 != null)
                    {
                        throw new NotImplementedException(); //重复nodeIdB
                    }
                    idToNodes[id] = new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(idToNodes[id].Item1, nodeB);
                }
                else
                {
                    idToNodes.Add(id, new Tuple<ThPDSProjectGraphNode, ThPDSProjectGraphNode>(null, nodeB));
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
                var id = edgeA.Circuit.ID.LoadID;
                if (!idToEdges.ContainsKey(id))
                {
                    idToEdges.Add(id, new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(edgeA, null));
                }
                else
                {
                    throw new NotImplementedException(); //重复edgeIdA
                }
            }
            foreach (var edgeB in graphB.Edges)
            {
                var id = edgeB.Circuit.ID.LoadID;
                if (idToEdges.ContainsKey(id))
                {
                    if (idToEdges[id].Item2 != null)
                    {
                        throw new NotImplementedException(); //重复edgeIdB
                    }
                    idToEdges[id] = new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(idToEdges[id].Item1, edgeB);
                }
                else
                {
                    idToEdges.Add(id, new Tuple<ThPDSProjectGraphEdge, ThPDSProjectGraphEdge>(null, edgeB));
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
        private void ComapreExChangeForNode(ref PDSGraph graphA, PDSGraph graphB)
        {
            Dictionary<string, bool> nodeIdVisit = new Dictionary<string, bool>();
            foreach (var idToNode in idToNodes)
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
                    var nodeA = idToNodes[idA].Item1;
                    var nodeB = idToNodes[idB].Item1;
                    if (idA == idB)
                    {
                        continue;
                    }
                    else if (NodesSameEnvironment(idToNodes[idA].Item1, idToNodes[idB].Item2, graphA, graphB) 
                        && NodesSameEnvironment(idToNodes[idB].Item1, idToNodes[idA].Item2, graphA, graphB))
                    {
                        DataChangeForNode(idToNodes[idA].Item1, idToNodes[idB].Item2);
                        DataChangeForNode(idToNodes[idB].Item1, idToNodes[idA].Item2);
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
        private void ComapreChangeIdForNode(ref PDSGraph graphA, PDSGraph graphB)
        {
            Dictionary<string, bool> nodeIdVisit = new Dictionary<string, bool>();
            foreach (var idToNode in idToNodes)
            {
                if (idToNode.Value.Item1 != null && idToNode.Value.Item2 == null)
                {
                    nodeIdVisit.Add(idToNode.Key, false);
                }
            }
            foreach (var idToNode in idToNodes)
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
                        if (NodesSameEnvironment(idToNodes[idA].Item1, idToNodes[idB].Item2, graphA, graphB))
                        {
                            StructureForChangeNodeID(idToNodes[idA].Item1, idToNode.Value.Item2);
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
            inEdgesA.ForEach(e => inEdgesIdA.Add(e.Circuit.ID.LoadID));
            inEdgesB.ForEach(e => inEdgesIdB.Add(e.Circuit.ID.LoadID));
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
        private void ComapreChangeIdForEdge()
        {
            var line2lineId = new Dictionary<Tuple<string, string>, string>();
            foreach(var idToEdge in idToEdges)
            {
                line2lineId.Add(new Tuple<string, string>(idToEdge.Value.Item1.Source.Load.ID.LoadID, idToEdge.Value.Item1.Target.Load.ID.LoadID), idToEdge.Key);
            }
            foreach(var idToEdge in idToEdges)
            {
                var curEdge = idToEdge.Value.Item2;
                var curId = idToEdge.Key;
                var tmpLine = new Tuple<string, string>(curEdge.Source.Load.ID.LoadID, curEdge.Target.Load.ID.LoadID);
                if (line2lineId.ContainsKey(tmpLine) && curId != line2lineId[tmpLine])
                {
                    var compareEdge = idToEdges[line2lineId[tmpLine]].Item1;
                    if (curEdge.Details == compareEdge.Details) ///////////
                    {
                        StructureForChangeEdgeID(compareEdge, curEdge);
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
        private void CompareNodes(ref PDSGraph graphA, PDSGraph graphB)
        {
            foreach (var idToNode in idToNodes)
            {
                var id = idToNode.Key;
                var nodeA = idToNode.Value.Item1;
                var nodeB = idToNode.Value.Item2;
                bool haveDiff = false;
                var changeNode = nodeA;
                if (nodeA != null && nodeB != null)
                {
                    haveDiff = DataChangeForNode(nodeA, nodeB);

                    var exTag = (ThPDSProjectGraphNodeExchangeTag)nodeA.Tag;
                    var idTag = (ThPDSProjectGraphNodeIdChangeTag)nodeA.Tag;
                    if (exTag.HaveState == true || idTag.HaveState == true)
                    {
                        continue;
                    }
                    else if (NodeStructureMove(nodeA, nodeB, graphA, graphB))
                    {
                        haveDiff = true;
                        StructureForMoveNode(nodeA, nodeB, ref graphA);
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
                    StructureForAddNode(nodeB, ref graphA);
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
        /// <param name="graphB"></param>
        /// <returns></returns>
        private void CompareEdges(ref PDSGraph graphA, PDSGraph graphB)
        {
            foreach (var idToEdge in idToEdges)
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
                        StructureForMoveEdge(edgeA, edgeB, ref graphA);
                    }
                    else if (edgeA.Target == edgeB.Target)
                    {
                        if(!DataChangeForEdge(edgeA, edgeB))
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
                    StructureForAddEdge(edgeB, ref graphA);
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
            graphA.InEdges(nodeA).ForEach(e => inEdgesIdA.Add(e.Circuit.ID.LoadID));
            graphB.InEdges(nodeB).ForEach(e => inEdgesIdB.Add(e.Circuit.ID.LoadID));
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
        private void StructureForChangeNodeID(ThPDSProjectGraphNode nodeA, 
            ThPDSProjectGraphNode nodeB)
        {
            var idChangeTagA = (ThPDSProjectGraphNodeIdChangeTag)nodeA.Tag;
            idChangeTagA.HaveState = true;

            idChangeTagA.ChangeToId = nodeB.Load.ID.LoadID;
            idChangeTagA.ChangeToNode = nodeB;
        }

        private void StructureForExchangeNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB)
        {
            var exchangeTagA = (ThPDSProjectGraphNodeExchangeTag)nodeA.Tag;
            exchangeTagA.HaveState = true;
            var exchangeTagB = (ThPDSProjectGraphNodeExchangeTag)nodeB.Tag;
            exchangeTagB.HaveState = true;

            exchangeTagA.ExchangeToId = nodeB.Load.ID.LoadID;
            exchangeTagB.ExchangeToId = nodeA.Load.ID.LoadID;
            exchangeTagA.ExchangeToNode = nodeB;
            exchangeTagB.ExchangeToNode = nodeA;
        }

        private void StructureForMoveNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB, ref PDSGraph graphA)
        {
            var moveTagA = (ThPDSProjectGraphNodeMoveTag)nodeA.Tag;
            moveTagA.HaveState = true;
            moveTagA.MoveFrom = true;
            moveTagA.MoveTo = false;

            var moveTagB = (ThPDSProjectGraphNodeMoveTag)nodeB.Tag;
            moveTagB.HaveState = true;
            moveTagA.MoveFrom = false;
            moveTagA.MoveTo = true;
            graphA.AddVertex(nodeB);
        }

        private void StructureForAddNode(ThPDSProjectGraphNode nodeB, ref PDSGraph graphA)
        {
            var addTagB = (ThPDSProjectGraphNodeAddTag)nodeB.Tag;
            addTagB.HaveState = true;
            graphA.AddVertex(nodeB);
        }

        private void StructureForDeleteNode(ThPDSProjectGraphNode nodeA)
        {
            var delTagA = (ThPDSProjectGraphNodeDeleteTag)nodeA.Tag;
            delTagA.HaveState = true;
        }
        #endregion

        #region EDGE_STRUCTURE
        private void StructureForChangeEdgeID(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB)
        {
            var idChangeTagA = (ThPDSProjectGraphEdgeIdChangeTag)edgeA.Tag;
            idChangeTagA.HaveState = true;
            idChangeTagA.ChangeToId = edgeB.Circuit.ID.LoadID;
            idChangeTagA.ChangeToEdge = edgeB;
        }

        private void StructureForMoveEdge(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB, ref PDSGraph graphA)
        {
            var moveTagA = (ThPDSProjectGraphEdgeMoveTag)edgeA.Tag;
            moveTagA.HaveState = true;
            moveTagA.MoveFrom = true;
            //moveTagA.MoveTo = false;

            var edgeASourceInGraphA = idToNodes[edgeA.Source.Load.ID.LoadID].Item1;
            var edgeBtargetInGraphA = idToNodes[edgeB.Target.Load.ID.LoadID].Item2; //Item2若没有则说明还没有添加入edgeA,但好像并不影响
            var newEdge = new ThPDSProjectGraphEdge(edgeASourceInGraphA, edgeBtargetInGraphA);
            newEdge.Details = edgeB.Details;
            newEdge.Circuit.ID.CircuitNumber.Add(edgeB.Circuit.ID.CircuitNumber.Last());
            var moveTagB = (ThPDSProjectGraphEdgeMoveTag)edgeB.Tag;
            moveTagB.HaveState = true;
            //moveTagB.MoveFrom = false;
            moveTagB.MoveTo = true;

            graphA.AddEdge(newEdge);
        }

        private void StructureForAddEdge(ThPDSProjectGraphEdge edgeB, ref PDSGraph graphA)
        {
            var addTagB = (ThPDSProjectGraphEdgeAddTag)edgeB.Tag;
            addTagB.HaveState = true;
            var edgeBSourceInGraphA = idToNodes[edgeB.Source.Load.ID.LoadID].Item1;
            var edgeBTargetInGraphA = idToNodes[edgeB.Target.Load.ID.LoadID].Item1;
            var newEdge = new ThPDSProjectGraphEdge(edgeBSourceInGraphA, edgeBTargetInGraphA);
            newEdge.Circuit.ID.CircuitNumber.Add(edgeB.Circuit.ID.CircuitNumber.Last());
            graphA.AddEdge(newEdge);
        }

        private void StructureForDeleteEdge(ThPDSProjectGraphEdge edgeA)
        {
            var delTagA = (ThPDSProjectGraphEdgeDeleteTag)edgeA.Tag;
            delTagA.HaveState = true;
        }
        #endregion

        #region DATA_CHANGE
        private bool DataChangeForNode(ThPDSProjectGraphNode nodeA, ThPDSProjectGraphNode nodeB)
        {
            var dataTagA = (ThPDSProjectGraphNodeDataTag)nodeA.Tag;
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
            if(dataTagA.TagD || dataTagA.TagF || dataTagA.TagP)
            {
                dataTagA.HaveState = true;
            }
            return dataTagA.HaveState;
        }

        private bool DataChangeForEdge(ThPDSProjectGraphEdge edgeA, ThPDSProjectGraphEdge edgeB)
        {
            var dataTagA = (ThPDSProjectGraphEdgeDataTag)edgeA.Tag;
            if (edgeA.Circuit.ID.CircuitNumber.Last() != edgeB.Circuit.ID.CircuitNumber.Last())
            {
                dataTagA.HaveState = true;
                dataTagA.ToLastCircuitNumber = edgeB.Circuit.ID.CircuitNumber.Last();
            }
            return dataTagA.HaveState;
        }
        #endregion

        #region NO_DIFF
        private void NoDiffForNode(ThPDSProjectGraphNode nodeA)
        {
            var noDiffTagA = (ThPDSProjectGraphNodeNoDifferenceTag)nodeA.Tag;
            noDiffTagA.HaveState = true;
        }

        private void NoDiffForEdge(ThPDSProjectGraphEdge edgeA)
        {
            var noDiffTagA = (ThPDSProjectGraphEdgeNoDifferenceTag)edgeA.Tag;
            noDiffTagA.HaveState = true;
        }
        #endregion
    }
}
