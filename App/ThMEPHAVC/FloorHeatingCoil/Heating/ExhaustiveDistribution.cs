using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class ExhaustiveDistribution
    {
        //外部变量
        public List<SingleRegion> RegionList = ProcessedData.RegionList;
        public List<SingleDoor> DoorList = ProcessedData.DoorList;
        public DoorToDoorDistance[,] DoorToDoorDistanceMap = ProcessedData.DoorToDoorDistanceMap;
        //public List<List<Connection>> RegionConnection = ProcessedData.RegionConnection;
        Dictionary<int, double> RegionUsedLength = new Dictionary<int, double>();

        //结果存储
        public List<SinglePipe> SinglePipeList = new List<SinglePipe>();
        public Dictionary<int, List<FeasibleSolution>> FeasibleSolutionMap = new Dictionary<int, List<FeasibleSolution>>(); 

        //最外层穷举-树的结构
        public List<List<TopoTreeNode>> TopoTreeList = new List<List<TopoTreeNode>>();
        public List<Dictionary<int, int>> RegionToNodeList = new List<Dictionary<int, int>>();

        //固定树结构后的共用变量
        public List<TopoTreeNode> NowSingleTopoTree;
        Dictionary<int, int> RegionToNode = new Dictionary<int, int>();
        public List<TmpPipe> TmpPipeList = new List<TmpPipe>();

        //DP临时变量
        public Dictionary<int, List<List<TmpPipe>>> RemainingCombinationsMap = new Dictionary<int, List<List<TmpPipe>>>();
        List<List<TmpPipe>> ResultPipeLists = new List<List<TmpPipe>>();
        List<TmpPipe> TmpPipesForDFS = new List<TmpPipe>();
        //结果存储
        public Dictionary<int, List<SubScheme>> RegionSchemeMap = new Dictionary<int, List<SubScheme>>();

        public ExhaustiveDistribution()
        {

        }

        public void Pipeline()
        {
            //构造拓扑树列表;
            CreateTopoTreeList();
   
            for (int i = 0; i < TopoTreeList.Count; i++)
            {
                //init
                NowSingleTopoTree = TopoTreeList[i];
                RegionToNode = RegionToNodeList[i];
                TmpPipeList.Clear();

                //针对当前的拓扑树，搜索分配管道的可行解
                CreateFeasibleSolutionList();
            }

            //保存结果
            //SaveResults();

            //打印结果
            //PrintResults();
        }


        public void CreateTopoTreeList()
        {
            //CreateIdTree();
        }

        public List<FeasibleSolution> CreateFeasibleSolutionList() 
        {
            List<FeasibleSolution> tmpFeasibleSolutions = new List<FeasibleSolution>();
            List<int> bigPassage = new List<int>();
            List<int> smallPassage = new List<int>();
            List<int> room = new List<int>();

            //区分大小过道
            for (int i = 0; i < RegionList.Count; i++) 
            {
                if (RegionList[i].ChildRegion.Count > 0)
                {
                    if (RegionUsedLength[i] > Parameter.SmallPassingThreshold * 1.5) bigPassage.Add(i);
                    else smallPassage.Add(i);
                }
                else 
                {
                    room.Add(i);
                }
            }

            //创建房间Pipe列表
            room.OrderBy(x => RegionToNode[x]);
            List<TmpPipe> originalPipeList = new List<TmpPipe>();

            //对子节点进行处理
            for (int i = 0; i < room.Count; i++) 
            {
                TmpPipe nowPipe = CreateTmpPipe2(room[i]);
                TopoTreeNode nowNode = NowSingleTopoTree[RegionToNode[room[i]]];
                while (true)
                {
                    if (nowNode.FatherId == 0) break;
                    TopoTreeNode fatherNode = NowSingleTopoTree[RegionToNode[nowNode.FatherId]];
                    if (fatherNode.ChildIdList.Count == 1)
                    {
                        TmpPipe addPipe = CreateTmpPipe2(nowNode.FatherId);
                        TmpPipe newPipe = new TmpPipe(0);
                        if (AbleToMerge(nowPipe, addPipe, ref newPipe))
                        {
                            if (bigPassage.Contains(nowNode.FatherId))
                            {
                                bigPassage.Remove(nowNode.FatherId);
                            }
                            else smallPassage.Remove(nowNode.FatherId);

                            nowPipe = newPipe;
                            nowNode = fatherNode;
                        }
                        else break;
                    }
                    else
                    {
                        break;
                    }
                }
                originalPipeList.Add(nowPipe);
            }

            //先对子节点进行排列组合
            ExhaustiveMerger(originalPipeList);

            //分配大过道，Level从大到小分配，直接贪心，能并则并。
            //for (int i = 0; i <  ) 
            //{
                
            //}
            //分配小过道

            //选择优化/不优化

            return tmpFeasibleSolutions;
        }

        void ExhaustiveMerger(List<TmpPipe> originalPipeList) 
        {
            ResultPipeLists.Clear();
            MergerDFS(0, originalPipeList);
        }

        List<List<TmpPipe>> MergerDFS(int index , List<TmpPipe> originalPipeList) 
        {
            if (RemainingCombinationsMap.ContainsKey(index)) return RemainingCombinationsMap[index];
            if (index == originalPipeList.Count)
            {
                RemainingCombinationsMap.Add(index, new List<List<TmpPipe>>());
                return new List<List<TmpPipe>>();
            }
            //返回值
            List<List<TmpPipe>> results = new List<List<TmpPipe>>();
 

            List<TmpPipe> firstPipe = new List<TmpPipe>();
            firstPipe.Add(originalPipeList[index]);
            TmpPipe newPipe = new TmpPipe(0);
            for (int i = 1; i < originalPipeList.Count - index; i++) 
            {
                TmpPipe addPipe  = originalPipeList[index + i];
                if (AbleToMerge(firstPipe.Last(), addPipe, ref newPipe))
                {
                    firstPipe.Add(newPipe);
                }
                else break;
            }

            for (int i = 0; i < firstPipe.Count; i++) 
            {
                TmpPipesForDFS.Add(firstPipe[i]);
                int newIndex = index + i + 1;

                List<List<TmpPipe>> remainPipeLists = MergerDFS(newIndex, originalPipeList);
                List<TmpPipe> mergedRemainPipes = new List<TmpPipe>();
                mergedRemainPipes.Add(firstPipe[i]);
                if (remainPipeLists.Count == 0)
                {
                    results.Add(mergedRemainPipes);
                }
                else 
                {
                    for (int j = 0; j < remainPipeLists.Count; j++)
                    {
                        mergedRemainPipes.AddRange(remainPipeLists[j]);
                        results.Add(mergedRemainPipes);
                    }
                }
            }

            //保留结果
            RemainingCombinationsMap.Add(index, results);
            return results;
        }

        public List<FeasibleSolution> CreateFeasibleSolutionList2()
        {
            List<FeasibleSolution> tmpFeasibleSolutions = new List<FeasibleSolution>();

            //区分大小过道

            //先对子节点进行排列组合

            //分配大过道，Level从大到小分配，直接贪心，能并则并。

            //分配小过道

            //选择优化/不优化

            return tmpFeasibleSolutions;
        }



        //预处理
        //Idtree 完全满足拓扑关系,与原来的List<SingleRegion>不完全相同。
        //void CreateIdTree()
        //{
        //    //TopoTreeList = new List<TopoTree>(new TopoTree[RegionList.Count]);
        //    TopoTreeList = new List<TopoTreeNode>();
        //    RegionToTree = new Dictionary<int, int>();
        //    Queue<int> topoIdQueue = new Queue<int>();
        //    List<int> visited = new List<int>();
        //    //int nowIndex = 0; 

        //    for (int i = 0; i < RegionList.Count; i++)
        //    {
        //        visited.Add(0);
        //        RegionToTree.Add(i, -1);
        //    }

        //    visited[0] = 1;

        //    topoIdQueue.Enqueue(0);
        //    TopoTreeNode tmpTree = new TopoTreeNode(0, RegionList[0].Level, 0, 0);
        //    RegionToTree[0] = TopoTreeList.Count;
        //    TopoTreeList.Add(tmpTree);

        //    while (topoIdQueue.Count > 0)
        //    {
        //        int topoIndex = topoIdQueue.Dequeue();
        //        int regionId = TopoTreeList[topoIndex].NodeId;
        //        //int treeIndex = regionToTree[regionId]; 
        //        List<int> childIdList = new List<int>();
        //        for (int i = 0; i < RegionList[regionId].ChildRegion.Count; i++)
        //        {
        //            int childRegionId = RegionList[regionId].ChildRegion[i].RegionId;
        //            if (visited[childRegionId] == 0)
        //            {
        //                visited[childRegionId] = 1;
        //                tmpTree = new TopoTreeNode(childRegionId, RegionList[childRegionId].Level, regionId, RegionList[regionId].ExportMap[RegionList[childRegionId]].DoorId);
        //                RegionToTree[childRegionId] = TopoTreeList.Count;
        //                TopoTreeList.Add(tmpTree);
        //                //
        //                childIdList.Add(childRegionId);
        //                //
        //                topoIdQueue.Enqueue(RegionToTree[childRegionId]);
        //            }
        //        }
        //        TopoTreeList[topoIndex].ChildIdList = childIdList;
        //    }
        //}

        /// <summary>
        /// 分配部分
        /// </summary>

        //主函数
        //void Distribute()
        //{
        //    //开始迭代
        //    List<List<int>> levelOrderRegion = TopoTreeNode.LevelOrder(TopoTreeList);
        //    int level = levelOrderRegion.Count - 1;
        //    for (; level >= 0; level--)  //level >=0 
        //    {
        //        if (level == 0)
        //        {
        //            int stop = 0;
        //        }
        //        for (int i = 0; i < levelOrderRegion[level].Count; i++)
        //        {
        //            int topoIndex = levelOrderRegion[level][i];
        //            TopoTreeNode nowNode = TopoTreeList[topoIndex];
        //            int regionId = nowNode.NodeId;

        //            if (regionId == 2)
        //            {
        //                int stop = 0;
        //            }

        //            if (nowNode.ChildIdList.Count == 0)
        //            {
        //                CreateTmpPipe(regionId);
        //                continue;
        //            }
        //            else //如果不是子节点，尝试并管  
        //            {
        //                //贪心，小的往大的身上并。
        //                //区域单通管道 >= 3才考虑穷举 ，太不优雅了
        //                //穷举的话，去掉靠墙和加载一个出口中间的管道，DFS穷举（带域的）很麻烦，因此考虑初始解+扰动
        //                MergePipesMode0(regionId);
        //            }
        //        }
        //    }

        //    TmpPipeList = RegionSchemeMap[0][0].TmpPipeList;
        //}

        void CreateTmpPipe(int regionId)
        {
            //整理需要的数据
            int topoIndex = RegionToNode[regionId];
            SingleRegion nowRegion = RegionList[regionId];
            int fatherId = NowSingleTopoTree[topoIndex].FatherId;
            SingleRegion fatherRegion = RegionList[fatherId];
            SingleDoor upDoor = nowRegion.EntranceMap[fatherRegion];
            int upDoorId = upDoor.DoorId;

            //构造子方案
            SubScheme subScheme = new SubScheme(regionId);
            TmpPipe tmpPipe = new TmpPipe(0);
            tmpPipe.RegionIdList.Add(regionId);
            //tmpPipe.DoorIdList.Add(upDoorId);
            tmpPipe.DomainIdList.Add(regionId);
            tmpPipe.DownstreamLength = nowRegion.UsedPipeLength;
            RegionUsedLength.Add(regionId, nowRegion.UsedPipeLength);
            tmpPipe.UpstreamLength = ComputeTotalDistance(upDoorId, 0) * 2;
            tmpPipe.TotalLength = tmpPipe.UpstreamLength + tmpPipe.DownstreamLength;
            subScheme.TmpPipeList.Add(tmpPipe);

            //将子方案放入map
            if (RegionSchemeMap.ContainsKey(regionId))
            {
                RegionSchemeMap[regionId].Add(subScheme);
            }
            else
            {
                List<SubScheme> subSchemes = new List<SubScheme>();
                subSchemes.Add(subScheme);
                RegionSchemeMap.Add(regionId, subSchemes);
            }
        }

        TmpPipe CreateTmpPipe2(int regionId)
        {
            //整理需要的数据
            int topoIndex = RegionToNode[regionId];
            SingleRegion nowRegion = RegionList[regionId];
            int fatherId = NowSingleTopoTree[topoIndex].FatherId;
            SingleRegion fatherRegion = RegionList[fatherId];
            SingleDoor upDoor = nowRegion.EntranceMap[fatherRegion];
            int upDoorId = upDoor.DoorId;

            //构造子方案
            TmpPipe tmpPipe = new TmpPipe(0);
            tmpPipe.RegionIdList.Add(regionId);
            //tmpPipe.DoorIdList.Add(upDoorId);
            tmpPipe.DomainIdList.Add(regionId);
            tmpPipe.DownstreamLength = nowRegion.UsedPipeLength;
            RegionUsedLength.Add(regionId, nowRegion.UsedPipeLength);
            tmpPipe.UpstreamLength = ComputeTotalDistance(upDoorId, 0) * 2;
            tmpPipe.TotalLength = tmpPipe.UpstreamLength + tmpPipe.DownstreamLength;

            return tmpPipe;
        }


        ////将pipe2并入pipe1
        bool AbleToMerge(TmpPipe pipe1, TmpPipe pipe2, ref TmpPipe newPipe)
        {
            bool flag = false;
            newPipe.DomainIdList.AddRange(pipe1.DomainIdList);
            newPipe.DomainIdList.AddRange(pipe2.DomainIdList);
            newPipe.Regularization(NowSingleTopoTree,RegionToNode);
            double length = ComputePipeTreeLength(newPipe);
            if (length < Parameter.TotalLength)
            {
                newPipe.TotalLength = length;
                return true;
            }
            else return false;
        }

        //void MergePipesMode0(int regionId)
        //{
        //    //获取常用变量
        //    int topoIndex = RegionToTree[regionId];
        //    SingleRegion nowRegion = RegionList[regionId];
        //    int fatherId = TopoTreeList[topoIndex].FatherId;
        //    SingleRegion fatherRegion = RegionList[fatherId];
        //    SingleDoor upDoor = nowRegion.EntranceMap[fatherRegion];
        //    int upDoorId = upDoor.DoorId;

        //    //定义变量
        //    List<SubScheme> nowSubSchemes = CreateSubSchemeList(regionId);
        //    List<TmpPipe> tmpPipes = new List<TmpPipe>();
        //    double usedPipeLength = ComputeTotalDistance(upDoorId, 0) * 2;
        //    int nowRegionDistributed = 0;
        //    double mergeThreshold = (Parameter.TotalLength - usedPipeLength) / 2;

        //    //创建本区域的tmplist
        //    for (int i = 0; i < nowSubSchemes.Count; i++)
        //    {
        //        for (int j = 0; j < nowSubSchemes[i].TmpPipeList.Count; j++)
        //        {
        //            tmpPipes.Add(nowSubSchemes[i].TmpPipeList[j]);
        //        }
        //    }

        //    //创建当前区域临时管道
        //    TmpPipe nowPipe = new TmpPipe(0);
        //    nowPipe.DomainIdList.Add(regionId);
        //    //nowPipe.RegionIdList.Add(regionId);
        //    //nowPipe.DoorIdList.Add(upDoorId);
        //    //长度预估
        //    int bigPipeNum = 0;
        //    int smallPipeNum = 0;
        //    double smallPipeLength = 0;
        //    for (int i = 0; i < tmpPipes.Count; i++)
        //    {
        //        if (tmpPipes[i].TotalLength > (Parameter.TotalLength * 2 / 3))
        //        {
        //            bigPipeNum++;
        //        }
        //        else smallPipeLength += tmpPipes[i].TotalLength;
        //    }
        //    double estimatedNum = bigPipeNum + smallPipeLength / Parameter.TotalLength;

        //    double estimateLength = nowRegion.UsedPipeLength - estimatedNum * nowRegion.ClearedPl.Length * 0.7;
        //    estimateLength = Math.Max(1000, estimateLength);

        //    //载入长度
        //    RegionUsedLength.Add(regionId, estimateLength);
        //    nowPipe.DownstreamLength = estimateLength;
        //    nowPipe.UpstreamLength = ComputeTotalDistance(upDoorId, 0) * 2;
        //    nowPipe.TotalLength = nowPipe.UpstreamLength + nowPipe.DownstreamLength;

        //    //
        //    List<TmpPipe> tmpPipesCopy = new List<TmpPipe>(tmpPipes);
        //    tmpPipesCopy.Add(nowPipe);
        //    int cannotMerge = 0;
        //    while (true)
        //    {
        //        if (cannotMerge == 1) break;
        //        if (nowRegionDistributed == 0)
        //        {
        //            List<int> indexList = new List<int>();
        //            int nowPipeId = tmpPipesCopy.Count - 1;

        //            for (int i = 0; i < tmpPipesCopy.Count; i++)
        //            {
        //                indexList.Add(i);
        //            }
        //            indexList = indexList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList();

        //            for (int i = 0; i < tmpPipesCopy.Count; i++)
        //            {
        //                int pipeToMergeId = indexList[i];
        //                //Merge成功一次就跳出
        //                //如果发现已经无法Merge，则跳出
        //                if (tmpPipesCopy[pipeToMergeId].DownstreamLength > mergeThreshold || tmpPipesCopy.Count < 2)
        //                {
        //                    cannotMerge = 1;
        //                    break;
        //                }
        //                //如果需要Merge的是过道剩余
        //                if (pipeToMergeId == nowPipeId)
        //                {
        //                    int id = indexList[i + 1];
        //                    TmpPipe newPipe = new TmpPipe(0);
        //                    if (AbleToMerge(regionId, tmpPipesCopy[id], tmpPipesCopy[pipeToMergeId], ref newPipe))
        //                    {
        //                        tmpPipesCopy.Insert(id, newPipe);
        //                        tmpPipesCopy.RemoveAt(id + 1);
        //                        tmpPipesCopy.RemoveAt(tmpPipesCopy.Count - 1);
        //                        nowRegionDistributed = 1;
        //                        //tmpPipes = tmpPipesCopy;
        //                        break;
        //                    }
        //                    else
        //                    {
        //                        continue;
        //                    }
        //                }
        //                else //如果需要Merge的不是过道剩余
        //                {
        //                    List<int> AdjacentIdList = new List<int>();
        //                    //nowPipe一定可以作为选项
        //                    AdjacentIdList.Add(tmpPipesCopy.Count - 1);
        //                    //左侧管道
        //                    if (pipeToMergeId > 0)
        //                    {
        //                        AdjacentIdList.Add(pipeToMergeId - 1);
        //                    }
        //                    //右侧管道
        //                    if (pipeToMergeId < tmpPipesCopy.Count - 2)
        //                    {
        //                        AdjacentIdList.Add(pipeToMergeId + 1);
        //                    }

        //                    int optId = AdjacentIdList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList().First();

        //                    //如果选中的是过道剩余
        //                    if (optId == tmpPipesCopy.Count - 1)
        //                    {
        //                        TmpPipe newPipe = new TmpPipe(0);
        //                        if (AbleToMerge(regionId, tmpPipesCopy[pipeToMergeId], tmpPipesCopy[optId], ref newPipe))
        //                        {
        //                            tmpPipesCopy.Insert(pipeToMergeId, newPipe);
        //                            tmpPipesCopy.RemoveAt(pipeToMergeId + 1);
        //                            tmpPipesCopy.RemoveAt(tmpPipesCopy.Count - 1);
        //                            nowRegionDistributed = 1;
        //                            tmpPipes = tmpPipesCopy;
        //                            break;
        //                        }
        //                        else
        //                        {
        //                            continue;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        TmpPipe newPipe = new TmpPipe(0);
        //                        if (AbleToMerge(regionId, tmpPipesCopy[pipeToMergeId], tmpPipesCopy[optId], ref newPipe))
        //                        {
        //                            int start = Math.Min(pipeToMergeId, optId);
        //                            tmpPipesCopy.Insert(start, newPipe);
        //                            tmpPipesCopy.RemoveAt(start + 1);
        //                            tmpPipesCopy.RemoveAt(start + 1);
        //                            //此处删除过道节点。
        //                            //tmpPipesCopy.RemoveAt(tmpPipesCopy.Count - 1);
        //                            //tmpPipes = tmpPipesCopy;
        //                            break;
        //                        }
        //                        else
        //                        {
        //                            continue;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else      //如果过道剩余已经被合并
        //        {
        //            List<int> indexList = new List<int>();
        //            for (int i = 0; i < tmpPipesCopy.Count; i++)
        //            {
        //                indexList.Add(i);
        //            }
        //            indexList = indexList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList();

        //            for (int i = 0; i < tmpPipesCopy.Count; i++)
        //            {
        //                int pipeToMergeId = indexList[i];
        //                if (tmpPipesCopy[pipeToMergeId].DownstreamLength > mergeThreshold || tmpPipesCopy.Count < 2)
        //                {
        //                    cannotMerge = 1;
        //                    break;
        //                }

        //                List<int> AdjacentIdList = new List<int>();
        //                //左侧管道
        //                if (pipeToMergeId > 0)
        //                {
        //                    AdjacentIdList.Add(pipeToMergeId - 1);
        //                }
        //                //右侧管道
        //                if (pipeToMergeId < tmpPipesCopy.Count - 1)
        //                {
        //                    AdjacentIdList.Add(pipeToMergeId + 1);
        //                }
        //                int optId = AdjacentIdList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList().First();

        //                //
        //                TmpPipe newPipe = new TmpPipe(0);
        //                if (AbleToMerge(regionId, tmpPipesCopy[pipeToMergeId], tmpPipesCopy[optId], ref newPipe))
        //                {
        //                    int start = Math.Min(pipeToMergeId, optId);
        //                    tmpPipesCopy.Insert(start, newPipe);
        //                    tmpPipesCopy.RemoveAt(start + 1);
        //                    tmpPipesCopy.RemoveAt(start + 1);
        //                    //tmpPipes = tmpPipesCopy;
        //                    break;
        //                }
        //                else
        //                {
        //                    continue;
        //                }
        //            }

        //        }
        //    }

        //    //如果始终没有分配过主导管道，此时主导管道单独存在于最右侧
        //    if (nowRegionDistributed == 0)
        //    {
        //        for (int i = 0; i < tmpPipesCopy.Count - 1; i++)
        //        {
        //            TmpPipe nowPipeCopy = tmpPipesCopy[i].Copy();

        //            nowPipeCopy.Regularization(TopoTreeList, RegionToTree);
        //            int pipeTreeId = nowPipeCopy.RegionToPipeTree[regionId];

        //            double minlength = Parameter.TotalLength;
        //            int isleft = 0;
        //            for (int j = 0; j < nowPipeCopy.PipeTreeList[pipeTreeId].ChildRegionIdList.Count; j++)
        //            {
        //                int downRegionId = nowPipeCopy.PipeTreeList[pipeTreeId].ChildRegionIdList[j];
        //                int downDoorId = TopoTreeList[RegionToTree[downRegionId]].UpDoorId;
        //                if (DoorToDoorDistanceMap[downDoorId, upDoorId].CCWDistance > DoorToDoorDistanceMap[downDoorId, upDoorId].CWDistance)
        //                {
        //                    if (DoorToDoorDistanceMap[downDoorId, upDoorId].CWDistance < minlength)
        //                    {
        //                        minlength = DoorToDoorDistanceMap[downDoorId, upDoorId].CWDistance;
        //                        isleft = 1;
        //                    }
        //                }
        //                else
        //                {
        //                    if (DoorToDoorDistanceMap[downDoorId, upDoorId].CCWDistance < minlength)
        //                    {
        //                        minlength = DoorToDoorDistanceMap[downDoorId, upDoorId].CCWDistance;
        //                        isleft = 0;
        //                    }
        //                }
        //            }

        //            if (isleft == 1) continue;
        //            else
        //            {
        //                TmpPipe nowRegionPipe = tmpPipesCopy.Last();
        //                tmpPipesCopy.Insert(i, nowRegionPipe);
        //                tmpPipesCopy.RemoveAt(tmpPipesCopy.Count - 1);
        //                break;
        //            }
        //        }
        //    }


        //    tmpPipes = tmpPipesCopy;

        //    //合并完毕后,整理管道
        //    for (int i = 0; i < tmpPipes.Count; i++)
        //    {
        //        TmpPipe mergedPipe = tmpPipes[i];
        //        mergedPipe.InternalId = i;
        //        mergedPipe.RegionIdList.Add(regionId);
        //        //mergedPipe.DoorIdList.Add(upDoorId);
        //        double upLength = ComputeTotalDistance(upDoorId, 0) * 2;
        //        double nowLength = mergedPipe.UpstreamLength - upLength;
        //        mergedPipe.UpstreamLength = upLength;
        //        mergedPipe.DownstreamLength += nowLength;
        //    }

        //    //构造子方案
        //    SubScheme subScheme = new SubScheme(regionId);
        //    subScheme.TmpPipeList.AddRange(tmpPipes);

        //    //将子方案放入map
        //    if (RegionSchemeMap.ContainsKey(regionId))
        //    {
        //        RegionSchemeMap[regionId].Add(subScheme);
        //    }
        //    else
        //    {
        //        List<SubScheme> subSchemes = new List<SubScheme>();
        //        subSchemes.Add(subScheme);
        //        RegionSchemeMap.Add(regionId, subSchemes);
        //    }
        //}

        //public List<SubScheme> CreateSubSchemeList(int regionId)
        //{
        //    List<SubScheme> subSchemes = new List<SubScheme>();
        //    SingleRegion nowRegion = RegionList[regionId];
        //    int topoId = RegionToTree[regionId];
        //    TopoTreeNode nowNode = TopoTreeList[topoId];

        //    for (int i = 0; i < nowNode.ChildIdList.Count; i++)
        //    {
        //        int childRegionId = nowNode.ChildIdList[i];
        //        subSchemes.Add(RegionSchemeMap[childRegionId][0]);
        //    }

        //    return subSchemes;
        //}

        ///// <summary>
        /////  优化部分
        ///// </summary>
        //void Optimization()
        //{
        //    List<TmpPipe> bestTmpPipeList = TmpPipeList;

        //    foreach (var pipe in bestTmpPipeList)
        //    {
        //        pipe.CreatePipeTree(TopoTreeList, RegionToTree);
        //        pipe.GetLeftRegionIdList();
        //        pipe.GetRightRegionIdList();
        //    }

        //    int stop = 0;
        //    while (stop == 0)
        //    {
        //        stop = 1;
        //        List<CompareModel> compareModels = new List<CompareModel>();
        //        List<TmpPipe> minPipes = bestTmpPipeList.OrderBy(x => x.TotalLength).ToList();

        //        for (int minIndex = 0; minIndex < minPipes.Count; minIndex++)
        //        {
        //            TmpPipe nowPipe = minPipes[minIndex];
        //            double originalMinLength = nowPipe.TotalLength;
        //            if (nowPipe.TotalLength > Parameter.TotalLength * Parameter.OptimizationThreshold)
        //            {
        //                break;
        //            }

        //            //进行优化
        //            int nowIndex = nowPipe.InternalId;
        //            List<int> adjacentPipes = new List<int>();
        //            if (nowIndex > 0)
        //            {
        //                adjacentPipes.Add(nowIndex - 1);
        //            }
        //            if (nowIndex < bestTmpPipeList.Count - 1)
        //            {
        //                adjacentPipes.Add(nowIndex + 1);
        //            }

        //            for (int i = 0; i < adjacentPipes.Count; i++)
        //            {
        //                TmpPipe nowAdjacentPipe = bestTmpPipeList[adjacentPipes[i]];
        //                if (nowAdjacentPipe.DomainIdList.Count < 2) continue;
        //                for (int j = 0; j < nowAdjacentPipe.DomainIdList.Count; j++)
        //                {
        //                    int regionId = nowAdjacentPipe.DomainIdList[j];
        //                    int topoId = RegionToTree[regionId];
        //                    List<TmpPipe> testPipesList = bestTmpPipeList.Copy();

        //                    //区分左右
        //                    int isRight = 0;
        //                    if (adjacentPipes[i] == nowIndex + 1) isRight = 1;

        //                    //如果可以转移
        //                    if (IsMoveAble(nowPipe, nowAdjacentPipe, regionId, isRight))
        //                    {
        //                        //如果是主导管线
        //                        int move = 0;
        //                        double newMinLength = 0;
        //                        MoveTest(nowIndex, adjacentPipes[i], regionId, ref move, ref testPipesList, ref newMinLength);
        //                        if (move == 1)
        //                        {
        //                            compareModels.Add(new CompareModel(testPipesList, newMinLength));
        //                        }
        //                    }
        //                }
        //            }

        //            if (compareModels.Count > 0)
        //            {
        //                CompareModel bestModel = compareModels.FindByMax(x => x.MinLength);
        //                if (bestModel.MinLength > originalMinLength)
        //                {
        //                    stop = 0;
        //                    bestTmpPipeList = bestModel.TmpPipeList;
        //                    for (int i = 0; i < bestTmpPipeList.Count; i++)
        //                    {
        //                        bestTmpPipeList[i].InternalId = i;
        //                    }

        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    TmpPipeList = bestTmpPipeList;
        //}

        //public bool IsMoveAble(TmpPipe getPipe, TmpPipe providePipe, int regionId, int dir)
        //{
        //    bool flag = false;

        //    if (IsPipeRegionConnect(getPipe, regionId) == 1)
        //    {
        //        if (dir == 0) //左侧管线移动给右侧管线
        //        {
        //            if (providePipe.RightRegionIdList.Contains(regionId)) flag = true;
        //        }
        //        else
        //        {
        //            if (providePipe.LeftRegionIdList.Contains(regionId)) flag = true;
        //        }
        //    }



        //    return flag;
        //}

        //public int IsPipeRegionConnect(TmpPipe nowPipe, int regionId)
        //{
        //    int flag = 0;
        //    List<int> connectionList = new List<int>();
        //    //foreach (SingleRegion sr in RegionList[regionId].FatherRegion) 
        //    //{
        //    //    connectionList.Add(sr.RegionId);
        //    //}
        //    //foreach (SingleRegion sr in RegionList[regionId].ChildRegion)
        //    //{
        //    //    connectionList.Add(sr.RegionId);
        //    //}

        //    int topoIndex = RegionToTree[regionId];
        //    connectionList.Add(TopoTreeList[topoIndex].FatherId);
        //    connectionList.AddRange(TopoTreeList[topoIndex].ChildIdList);

        //    for (int i = 0; i < nowPipe.RegionIdList.Count; i++)
        //    {
        //        if (connectionList.Contains(nowPipe.RegionIdList[i]))
        //        {
        //            flag = 1;
        //            continue;
        //        }
        //    }
        //    return flag;
        //}

        //public bool IsPipeIndexExchange(TmpPipe pipe1, TmpPipe pipe2, int isLeftOld)
        //{
        //    bool flag = false;

        //    int isLeftNow = 1;

        //    int region1 = pipe1.RegionIdList.OrderBy(x => x).ToList().Last();
        //    int region2 = pipe2.RegionIdList.OrderBy(x => x).ToList().Last();

        //    if (RegionList[region1].Level > RegionList[region2].Level)
        //    {
        //        int topoIndex1 = RegionToTree[region1];
        //        TopoTreeNode nowNode = TopoTreeList[topoIndex1];
        //        TopoTreeNode fatherTree = TopoTreeList[RegionToTree[nowNode.FatherId]];
        //        if (DoorToDoorDistanceMap[nowNode.UpDoorId, fatherTree.UpDoorId].CCWDistance > DoorToDoorDistanceMap[nowNode.UpDoorId, fatherTree.UpDoorId].CWDistance)
        //        {
        //            isLeftNow = 1;
        //        }
        //        else isLeftNow = -1;
        //    }
        //    else if (RegionList[region1].Level < RegionList[region2].Level)
        //    {
        //        int topoIndex2 = RegionToTree[region2];
        //        TopoTreeNode nowNode = TopoTreeList[topoIndex2];
        //        TopoTreeNode fatherTree = TopoTreeList[RegionToTree[nowNode.FatherId]];
        //        if (DoorToDoorDistanceMap[nowNode.UpDoorId, fatherTree.UpDoorId].CCWDistance > DoorToDoorDistanceMap[nowNode.UpDoorId, fatherTree.UpDoorId].CWDistance)
        //        {
        //            isLeftNow = -1;
        //        }
        //        else isLeftNow = 1;
        //    }

        //    if (isLeftNow * isLeftOld < 0)
        //    {
        //        flag = true;
        //    }
        //    return flag;
        //}

        //public void MoveTest(int nowIndex, int nowAdjacentPipe, int regionId, ref int move, ref List<TmpPipe> testPipesList, ref double newMinLength)
        //{
        //    TmpPipe nowPipe = testPipesList[nowIndex];
        //    TmpPipe adPipe = testPipesList[nowAdjacentPipe];
        //    double minLength = nowPipe.TotalLength;

        //    TmpPipe newNowPipe = AddDomainRegion(nowPipe, regionId);
        //    TmpPipe newAdPipe = DeleteDomainRegion(adPipe, regionId);

        //    if (Math.Min(newAdPipe.TotalLength, newNowPipe.TotalLength) > minLength && Math.Max(newAdPipe.TotalLength, newNowPipe.TotalLength) < Parameter.TotalLength)
        //    {
        //        move = 1;
        //        newMinLength = Math.Min(newAdPipe.TotalLength, newNowPipe.TotalLength);
        //        newNowPipe.Regularization(TopoTreeList, RegionToTree);
        //        newAdPipe.Regularization(TopoTreeList, RegionToTree);

        //        if (!IsPipeIndexExchange(newNowPipe, newAdPipe, nowAdjacentPipe - nowIndex))
        //        {
        //            testPipesList[nowIndex] = newNowPipe;
        //            testPipesList[nowAdjacentPipe] = newAdPipe;
        //        }
        //        else
        //        {
        //            testPipesList[nowIndex] = newAdPipe;
        //            testPipesList[nowAdjacentPipe] = newNowPipe;
        //        }
        //    }
        //    else
        //    {
        //        move = 0;
        //    }
        //}

        //public TmpPipe AddDomainRegion(TmpPipe oldPipe, int regionId)
        //{
        //    TmpPipe newPipe = new TmpPipe(0);

        //    newPipe.DomainIdList = oldPipe.DomainIdList;
        //    newPipe.DomainIdList.Add(regionId);
        //    newPipe.RegionIdList = oldPipe.RegionIdList;
        //    List<int> passingRegionIdLIst = GetPassingRegion(regionId);
        //    foreach (int id in passingRegionIdLIst)
        //    {
        //        if (!newPipe.RegionIdList.Contains(id)) newPipe.RegionIdList.Add(id);
        //    }
        //    newPipe.CreatePipeTree(TopoTreeList, RegionToTree);

        //    newPipe.TotalLength = ComputePipeTreeLength(newPipe);

        //    return newPipe;
        //}

        //public TmpPipe DeleteDomainRegion(TmpPipe oldPipe, int regionId)
        //{
        //    TmpPipe newPipe = new TmpPipe(0);

        //    newPipe.DomainIdList = oldPipe.DomainIdList;
        //    newPipe.DomainIdList.Remove(regionId);
        //    newPipe.DomainIdListToRegionIdList(TopoTreeList, RegionToTree);

        //    newPipe.CreatePipeTree(TopoTreeList, RegionToTree);

        //    newPipe.TotalLength = ComputePipeTreeLength(newPipe);

        //    return newPipe;
        //}

        //public List<int> GetPassingRegion(int domainRegionId)
        //{
        //    List<int> passingRegionIdList = new List<int>();
        //    int topoId = RegionToTree[domainRegionId];
        //    while (true)
        //    {
        //        passingRegionIdList.Add(TopoTreeList[topoId].NodeId);
        //        if (topoId == 0) break;
        //        topoId = RegionToTree[TopoTreeList[topoId].FatherId];
        //    }
        //    return passingRegionIdList;
        //}

        public double ComputePipeTreeLength(TmpPipe tmpPipe)
        {
            double totalLength = 0;
            Queue<int> idQueue = new Queue<int>();
            idQueue.Enqueue(0);
            while (idQueue.Count > 0)
            {
                int nowRegion = idQueue.Dequeue();
                PipeTreeNode nowNode = tmpPipe.PipeTreeList[tmpPipe.RegionToPipeTree[nowRegion]];
                int topoId = RegionToNode[nowRegion];
                int upUpDoorId = NowSingleTopoTree[topoId].UpDoorId;

                if (tmpPipe.DomainIdList.Contains(nowRegion))
                {
                    totalLength += RegionUsedLength[nowRegion];
                }

                if (nowNode.ChildRegionIdList.Count == 1)
                {
                    int childId = nowNode.ChildRegionIdList[0];
                    idQueue.Enqueue(childId);
                    int childTopoId = RegionToNode[childId];
                    int upDoorId = NowSingleTopoTree[childTopoId].UpDoorId;
                    totalLength += DoorToDoorDistanceMap[upDoorId, upUpDoorId].EstimatedDistance * 2;
                }
                else
                {
                    double maxLength = 0;
                    foreach (int childId in nowNode.ChildRegionIdList)
                    {
                        idQueue.Enqueue(childId);
                        int childTopoId = RegionToNode[childId];
                        int upDoorId = NowSingleTopoTree[childTopoId].UpDoorId;

                        double nowLength = DoorToDoorDistanceMap[upDoorId, upUpDoorId].EstimatedDistance * 2;
                        if (nowLength > maxLength)
                        {
                            maxLength = nowLength;
                        }
                    }
                    totalLength += maxLength;
                }

            }
            return totalLength;
        }


        //////保存结果
        //void SaveResults()
        //{
        //    List<TmpPipe> tmpPipeList = TmpPipeList;

        //    for (int i = 0; i < tmpPipeList.Count; i++)
        //    {
        //        TmpPipe nowPipe = tmpPipeList[i];

        //        //init
        //        nowPipe.RegionIdListToDoorIdList(TopoTreeList, RegionToTree);

        //        //保存pipe
        //        SinglePipe newPipe = new SinglePipe(i);
        //        newPipe.DomaintRegionList = nowPipe.DomainIdList;
        //        newPipe.PassedRegionList = nowPipe.RegionIdList;
        //        //newPipe.DoorList = nowPipe.DoorIdList;
        //        newPipe.TotalLength = nowPipe.TotalLength;
        //        SinglePipeList.Add(newPipe);

        //        //保存经过门的信息
        //        for (int j = 0; j < nowPipe.DoorIdList.Count; j++)
        //        {
        //            int doorId = nowPipe.DoorIdList[j];
        //            DoorList[doorId].PipeIdList.Add(i);
        //        }

        //        //保存经过的区域
        //        for (int j = 0; j < nowPipe.RegionIdList.Count; j++)
        //        {
        //            int regionId = nowPipe.RegionIdList[j];
        //            RegionList[regionId].PassingPipeList.Add(i);
        //        }

        //        //保存主导的区域
        //        for (int j = 0; j < nowPipe.DomainIdList.Count; j++)
        //        {
        //            int regionId = nowPipe.DomainIdList[j];
        //            RegionList[regionId].MainPipe.Add(i);
        //        }
        //    }

        //    //记录管道
        //    ProcessedData.PipeList = SinglePipeList;

        //    //记录主入口
        //    for (int i = 0; i < RegionList.Count; i++)
        //    {
        //        for (int j = 0; j < RegionList[i].EntranceMap.Count; j++)
        //        {
        //            if (RegionList[i].EntranceMap[RegionList[i].FatherRegion[j]].PipeIdList.Count != 0)
        //            {
        //                RegionList[i].MainEntrance = RegionList[i].EntranceMap[RegionList[i].FatherRegion[j]];
        //                break;
        //            }
        //        }
        //    }
        //}

        //void PrintResults()
        //{
        //    for (int i = 0; i < RegionList.Count; i++)
        //    {
        //        SingleRegion sr = RegionList[i];
        //        Point3d ptDraw = sr.ClearedPl.GetCenter() + new Vector3d(400, 0, 0);
        //        string draw = sr.MainPipe[0].ToString();
        //        Point3d ptDraw12 = sr.ClearedPl.GetCenter() + new Vector3d(800, 0, 0);
        //        string draw2 = ((int)RegionUsedLength[i]).ToString();
        //        //DrawUtils.ShowGeometry(sr.ClearedPl, "l1ClearedPl", 2, lineWeightNum: 30);
        //        DrawUtils.ShowGeometry(ptDraw, draw, "l1MainPipe", 10, 30, 300);
        //        DrawUtils.ShowGeometry(ptDraw12, draw2, "l1RegionLength", 10, 30, 300);
        //    }

        //    string lengthList = "";
        //    for (int i = 0; i < TmpPipeList.Count; i++)
        //    {
        //        lengthList += i.ToString() + ':' + ((int)TmpPipeList[i].TotalLength).ToString() + "\n";
        //    }
        //    Point3d ptDraw2 = RegionList[0].ClearedPl.GetCenter() + new Vector3d(4000, 0, 0);
        //    DrawUtils.ShowGeometry(ptDraw2, lengthList, "l1LengthList", 2, 30, 100);
        //}

        //////tool
        double ComputeTotalDistance(int doorIdEnd, int doorIdStart)
        {
            double distante = 0;
            int nowDoorId = doorIdEnd;
            int lastPosition = -1;
            if (doorIdEnd == doorIdStart) return 0;

            while (true)
            {
                int upRegionId = DoorList[nowDoorId].UpstreamRegion.RegionId;
                int topoIndex = RegionToNode[upRegionId];
                int upDoorId = NowSingleTopoTree[topoIndex].UpDoorId;

                distante += DoorToDoorDistanceMap[nowDoorId, upDoorId].EstimatedDistance;
                int nowPosition = -1;
                if (DoorToDoorDistanceMap[nowDoorId, upDoorId].DoorPositionProportion <= 0.5)
                {
                    nowPosition = 0;
                }
                else
                {
                    nowPosition = 1;
                }
                if ((lastPosition != -1 && nowPosition != lastPosition))
                {
                    Vector3d doorLine = DoorList[nowDoorId].DownFirst - DoorList[nowDoorId].DownSecond;
                    distante += doorLine.Length;
                }

                lastPosition = nowPosition;

                nowDoorId = upDoorId;
                if (upDoorId == doorIdStart)
                {
                    break;
                }
            }
            return distante;
        }

        ///////////////////////////////////////////////////////////////////////////
        ////全局贪心 + 优化
        //public void Distribute2()
        //{
        //    List<TmpPipe> tmpRoomPipes = new List<TmpPipe>();
        //    Dictionary<int, TmpPipe> bigTmpPassingPipes = new Dictionary<int, TmpPipe>();
        //    Dictionary<int, TmpPipe> smallTmpPassingPipes = new Dictionary<int, TmpPipe>();

        //    for (int i = 0; i < TopoTreeList.Count; i++)
        //    {
        //        if (TopoTreeList[i].ChildIdList.Count == 0)
        //        {
        //            tmpRoomPipes.Add(CreateNewPipe2(TopoTreeList[i]));
        //        }
        //        else
        //        {
        //            TmpPipe newPipe = CreateNewPipe2(TopoTreeList[i]);
        //            if (newPipe.DownstreamLength < Parameter.SmallPassingThreshold)
        //            {
        //                smallTmpPassingPipes.Add(TopoTreeList[i].NodeId, newPipe);
        //            }
        //            else
        //            {
        //                bigTmpPassingPipes.Add(TopoTreeList[i].NodeId, newPipe);
        //            }
        //        }
        //    }

        //    //整理管道
        //    List<int> postOrderList = new List<int>();
        //    TopoTreeNode.PostOrder(TopoTreeList, RegionToTree, TopoTreeList[0], ref postOrderList);
        //    Dictionary<int, int> regionToIndex = new Dictionary<int, int>();
        //    for (int i = 0; i < postOrderList.Count; i++)
        //    {
        //        regionToIndex.Add(postOrderList[i], i);
        //    }
        //    tmpRoomPipes.OrderBy(x => regionToIndex[x.DomainIdList[0]]);
        //    for (int i = 0; i < tmpRoomPipes.Count; i++)
        //    {
        //        tmpRoomPipes[i].Regularization(TopoTreeList, RegionToTree);
        //    }

        //    //开始迭代
        //    int stop = 0;
        //    while (stop == 0)
        //    {
        //        List<TmpPipe> sortPipeList = new List<TmpPipe>();
        //        sortPipeList.AddRange(bigTmpPassingPipes.Values.ToList());
        //        sortPipeList.AddRange(tmpRoomPipes);

        //        sortPipeList.OrderBy(x => x.TotalLength);

        //        if (sortPipeList.Count > 0) break;
        //    }
        //}

        //public TmpPipe CreateNewPipe2(TopoTreeNode topoTree)
        //{
        //    int regionId = topoTree.NodeId;
        //    SingleRegion nowRegion = RegionList[regionId];
        //    int upDoorId = topoTree.UpDoorId;
        //    SingleDoor upDoor = DoorList[topoTree.UpDoorId];

        //    //生产管道
        //    TmpPipe tmpPipe = new TmpPipe(0);
        //    tmpPipe.RegionIdList.Add(regionId);
        //    //tmpPipe.DoorIdList.Add(upDoorId);
        //    tmpPipe.DomainIdList.Add(regionId);
        //    tmpPipe.DownstreamLength = RegionUsedLength[regionId];
        //    RegionUsedLength.Add(regionId, nowRegion.UsedPipeLength);
        //    tmpPipe.UpstreamLength = ComputeTotalDistance(upDoorId, 0) * 2;
        //    tmpPipe.TotalLength = tmpPipe.UpstreamLength + tmpPipe.DownstreamLength;

        //    return tmpPipe;
        //}

        ////局部穷举 + 优化
        //public void Distribute3()
        //{

        //}

        //public List<List<SubScheme>> CreateSubSchemeCombinationList(int regionId)
        //{
        //    List<List<SubScheme>> SubSchemeCombinationList = new List<List<SubScheme>>();
        //    SingleRegion nowRegion = RegionList[regionId];
        //    List<List<SubScheme>> allSubScheme = new List<List<SubScheme>>();
        //    List<int> subSchemeIdNum = new List<int>();

        //    for (int i = 0; i < nowRegion.ChildRegion.Count; i++)
        //    {
        //        int childRegionId = nowRegion.ChildRegion[i].RegionId;
        //        allSubScheme.Add(RegionSchemeMap[childRegionId]);
        //        subSchemeIdNum.Add(RegionSchemeMap[childRegionId].Count);
        //    }

        //    List<SubScheme> tmpSubScheme = new List<SubScheme>();
        //    SubSchemeDFS(0, nowRegion.ChildRegion.Count, subSchemeIdNum, allSubScheme, tmpSubScheme, ref SubSchemeCombinationList);
        //    return SubSchemeCombinationList;
        //}

        //void SubSchemeDFS(int n, int m, List<int> idList, List<List<SubScheme>> allSubScheme, List<SubScheme> tmpSubScheme, ref List<List<SubScheme>> SubSchemeCombinationList)
        //{
        //    if (n == m)
        //    {
        //        if (tmpSubScheme.Count != m)
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            //Polyline a = RegionList[0].ClearedPl.Copy();
        //            //Polyline b = RegionList[0].ClearedPl;
        //            //Polyline c = RegionList[0].ClearedPl.Clone();

        //            List<SubScheme> clone = new List<SubScheme>(tmpSubScheme);
        //            SubSchemeCombinationList.Add(clone);
        //        }
        //    }
        //    if (n < m)
        //    {
        //        for (int i = 0; i < idList[n]; i++)
        //        {
        //            tmpSubScheme.Add(allSubScheme[n][i]);
        //            SubSchemeDFS(n + 1, m, idList, allSubScheme, tmpSubScheme, ref SubSchemeCombinationList);
        //            tmpSubScheme.RemoveAt(tmpSubScheme.Count - 1);
        //        }
        //    }
        //}


    }


    class FeasibleSolution
    {
        public List<TmpPipe> tmpPipes = new List<TmpPipe>();
        public double minPipeLength = 1000000;

        public FeasibleSolution() { }
        public FeasibleSolution(List<TmpPipe> tmpPipes) 
        {
            tmpPipes = tmpPipes;
        }
    }

}
