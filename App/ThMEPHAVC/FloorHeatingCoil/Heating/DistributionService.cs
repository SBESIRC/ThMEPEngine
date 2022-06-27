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

    class DistributionService
    {
        //外部变量
        public List<SingleRegion> RegionList = ProcessedData.RegionList;
        public List<SingleDoor> DoorList = ProcessedData.DoorList;
        public DoorToDoorDistance[,] DoorToDoorDistanceMap = ProcessedData.DoorToDoorDistanceMap;
        
        ////成员变量
        public List<TopoTree> TopoTreeList;
        Dictionary<int, int> RegionToTree = new Dictionary<int, int>();
        Dictionary<int, double> RegionUsedLength = new Dictionary<int, double>();
        public List<TmpPipe> TmpPipeList = new List<TmpPipe>();

        //树的遍历
        public List<int> RegionPostOrder = new List<int>();
        
        //结果存储
        public Dictionary<int, List<SubScheme>> RegionSchemeMap = new Dictionary<int, List<SubScheme>>();
        //public List<TmpPipe> TmpPipeList = new List<TmpPipe>();
        public List<SinglePipe> SinglePipeList = new List<SinglePipe>();
        
        public DistributionService() 
        {
            
        }

        public void Pipeline() 
        {
            //构造当前用到的树
            CreateIdTree();
            
            //分配管道
            Distribute();
            
            //优化
            Optimization();

            //保存结果
            SaveResults();

            //打印结果
            PrintResults();
        }

        //预处理
        //Idtree 完全满足拓扑关系,与原来的List<SingleRegion>不完全相同。
        void CreateIdTree()
        {
            //TopoTreeList = new List<TopoTree>(new TopoTree[RegionList.Count]);
            TopoTreeList = new List<TopoTree>();
            RegionToTree = new Dictionary<int, int>();
            Queue<int> topoIdQueue = new Queue<int>();
            List<int> visited = new List<int>();
            //int nowIndex = 0; 
            
            for (int i = 0; i < RegionList.Count; i++)
            {
                visited.Add(0);
                RegionToTree.Add(i,-1);
            }

            visited[0] = 1;
          
            topoIdQueue.Enqueue(0);
            TopoTree tmpTree = new TopoTree(0, RegionList[0].Level, 0, 0);
            RegionToTree[0] = TopoTreeList.Count;
            TopoTreeList.Add(tmpTree);
            
            while (topoIdQueue.Count > 0)
            {   
                int topoIndex = topoIdQueue.Dequeue();
                int regionId = TopoTreeList[topoIndex].NodeId;
                //int treeIndex = regionToTree[regionId]; 
                List<int> childIdList = new List<int>();
                for (int i = 0; i < RegionList[regionId].ChildRegion.Count; i++)
                {
                    int childRegionId = RegionList[regionId].ChildRegion[i].RegionId;
                    if (visited[childRegionId] == 0)
                    {
                        visited[childRegionId] = 1;
                        tmpTree = new TopoTree(childRegionId, RegionList[childRegionId].Level, regionId, RegionList[regionId].ExportMap[RegionList[childRegionId]].DoorId);
                        RegionToTree[childRegionId] = TopoTreeList.Count;
                        TopoTreeList.Add(tmpTree);
                        //
                        childIdList.Add(childRegionId);
                        //
                        topoIdQueue.Enqueue(RegionToTree[childRegionId]);
                    }
                }
                TopoTreeList[topoIndex].ChildId = childIdList;
            }
        }

        /// <summary>
        /// 分配部分
        /// </summary>
        
        //主函数
        void Distribute()
        {
            //开始迭代
            List<List<int>> levelOrderRegion = TopoTree.LevelOrder(TopoTreeList);
            int level = levelOrderRegion.Count-1;
            for (; level >= 0; level--)  //level >=0 
            {
                if (level == 0) 
                {
                    int stop = 0;
                }
                for (int i = 0; i < levelOrderRegion[level].Count; i++)
                {
                    int topoIndex = levelOrderRegion[level][i];
                    TopoTree nowTree = TopoTreeList[topoIndex];
                    int regionId = nowTree.NodeId;

                    if (nowTree.ChildId.Count == 0)
                    {
                        CreateTmpPipe(regionId);
                        continue;
                    }
                    else //如果不是子节点，尝试并管  
                    {
                        //贪心，小的往大的身上并。
                        //区域单通管道 >= 3才考虑穷举 ，太不优雅了
                        //穷举的话，去掉靠墙和加载一个出口中间的管道，DFS穷举（带域的）很麻烦，因此考虑初始解+扰动
                        MergePipesMode0(regionId);
                    }
                }
            }

            TmpPipeList = RegionSchemeMap[0][0].TmpPipeList; ;
        }

        void CreateTmpPipe(int regionId)
        {
            //整理需要的数据
            int topoIndex = RegionToTree[regionId];
            SingleRegion nowRegion = RegionList[regionId];
            int fatherId = TopoTreeList[topoIndex].FatherId;
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
            tmpPipe.UpstreamLength = ComputeTotalDistance(upDoorId,0);
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

        //将pipe2并入pipe1
        void MergeTmpPipe(int regionId, TmpPipe pipe1, TmpPipe pipe2)
        {
            
        }

        bool AbleToMerge(int upDoorId, TmpPipe pipe1, TmpPipe pipe2, ref TmpPipe newPipe)
        {
            bool flag = false;
            double nowRegionLength = Math.Max(pipe1.UpstreamLength, pipe2.UpstreamLength);
            double upLength = ComputeTotalDistance(upDoorId,0);
            if (pipe1.DownstreamLength + pipe2.DownstreamLength + nowRegionLength < Parameter.TotalLength) 
            {
                flag = true;
                TmpPipe tmpNewPipe = new TmpPipe(0);
                tmpNewPipe.RegionIdList.AddRange(pipe1.RegionIdList);
                tmpNewPipe.RegionIdList.AddRange(pipe2.RegionIdList);
                //tmpNewPipe.DoorIdList.AddRange(pipe1.DoorIdList);
                //tmpNewPipe.DoorIdList.AddRange(pipe2.DoorIdList);
                tmpNewPipe.DomainIdList.AddRange(pipe1.DomainIdList);
                tmpNewPipe.DomainIdList.AddRange(pipe2.DomainIdList);
                tmpNewPipe.DownstreamLength = pipe1.DownstreamLength + pipe2.DownstreamLength;
                tmpNewPipe.UpstreamLength = nowRegionLength;
                tmpNewPipe.TotalLength = tmpNewPipe.UpstreamLength + tmpNewPipe.DownstreamLength;
                newPipe = tmpNewPipe;
            }

            return flag;
        }

        void MergePipesMode0(int regionId)
        {
            //获取常用变量
            int topoIndex = RegionToTree[regionId];
            SingleRegion nowRegion = RegionList[regionId];
            int fatherId = TopoTreeList[topoIndex].FatherId;
            SingleRegion fatherRegion = RegionList[fatherId];
            SingleDoor upDoor = nowRegion.EntranceMap[fatherRegion];
            int upDoorId = upDoor.DoorId;

            //定义变量
            List<SubScheme> nowSubSchemes = CreateSubSchemeList(regionId);
            List<TmpPipe> tmpPipes = new List<TmpPipe>();
            double usedPipeLength = ComputeTotalDistance(upDoorId, 0);
            int nowRegionDistributed =  0;
            double mergeThreshold = (Parameter.TotalLength - usedPipeLength)/2;

            //创建本区域的tmplist
            for (int i = 0; i < nowSubSchemes.Count; i++)
            {
                for (int j = 0; j < nowSubSchemes[i].TmpPipeList.Count; j++)
                {
                    tmpPipes.Add(nowSubSchemes[i].TmpPipeList[j]);
                }
            }

            //创建当前区域临时管道
            TmpPipe nowPipe = new TmpPipe(0);
            nowPipe.DomainIdList.Add(regionId);
            //nowPipe.RegionIdList.Add(regionId);
            //nowPipe.DoorIdList.Add(upDoorId);
            double estimateLength = nowRegion.UsedPipeLength - tmpPipes.Count / 3 * nowRegion.ClearedPl.Length;
            RegionUsedLength.Add(regionId, estimateLength);
            nowPipe.DownstreamLength = estimateLength;
            nowPipe.UpstreamLength = ComputeTotalDistance(upDoorId, 0);
            nowPipe.TotalLength = nowPipe.UpstreamLength + nowPipe.DownstreamLength;
            
            //
            int cannotMerge = 0;
            while (true)
            {
                if (cannotMerge == 1) break;
                if (nowRegionDistributed == 0)
                {
                    List<int> indexList = new List<int>();
                    List<TmpPipe> tmpPipesCopy = new List<TmpPipe>(tmpPipes);
                    tmpPipesCopy.Add(nowPipe);
                    int nowPipeId = tmpPipesCopy.Count - 1;

                    for (int i = 0; i < tmpPipesCopy.Count; i++)
                    {
                        indexList.Add(i);
                    }
                    indexList = indexList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList();

                    for (int i = 0; i < tmpPipesCopy.Count; i++)
                    {
                        int pipeToMergeId = indexList[i];
                        //Merge成功一次就跳出
                        //如果发现已经无法Merge，则跳出
                        if (tmpPipesCopy[pipeToMergeId].DownstreamLength > mergeThreshold || tmpPipesCopy.Count < 2) 
                        {
                            cannotMerge = 1;
                            break;
                        }
                        //如果需要Merge的是过道剩余
                        if (pipeToMergeId == nowPipeId)
                        {
                            int id = indexList[i + 1];
                            TmpPipe newPipe = new TmpPipe(0);
                            if (AbleToMerge(regionId, tmpPipesCopy[id], tmpPipesCopy[pipeToMergeId], ref newPipe))
                            {
                                tmpPipesCopy.Insert(id, newPipe);
                                tmpPipesCopy.RemoveAt(id + 1);
                                tmpPipesCopy.RemoveAt(tmpPipesCopy.Count-1);
                                nowRegionDistributed = 1;
                                tmpPipes = tmpPipesCopy;
                                break;
                            }
                            else 
                            {
                                continue;
                            }
                        }
                        else //如果需要Merge的不是过道剩余
                        {
                            List<int> AdjacentIdList = new List<int>();
                            //nowPipe一定可以作为选项
                            AdjacentIdList.Add(tmpPipesCopy.Count - 1);
                            //左侧管道
                            if (pipeToMergeId > 0) 
                            {
                                AdjacentIdList.Add(pipeToMergeId - 1);
                            }
                            //右侧管道
                            if (pipeToMergeId < tmpPipesCopy.Count - 2)  
                            {
                                AdjacentIdList.Add(pipeToMergeId + 1);
                            }
                            
                            int optId = AdjacentIdList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList().First();

                            //如果选中的是过道剩余
                            if (optId == tmpPipesCopy.Count - 1)
                            {
                                TmpPipe newPipe = new TmpPipe(0);
                                if (AbleToMerge(regionId, tmpPipesCopy[pipeToMergeId], tmpPipesCopy[optId], ref newPipe))
                                {
                                    tmpPipesCopy.Insert(pipeToMergeId, newPipe);
                                    tmpPipesCopy.RemoveAt(pipeToMergeId + 1);
                                    tmpPipesCopy.RemoveAt(tmpPipesCopy.Count - 1);
                                    nowRegionDistributed = 1;
                                    tmpPipes = tmpPipesCopy;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else 
                            {
                                TmpPipe newPipe = new TmpPipe(0);
                                if (AbleToMerge(regionId, tmpPipesCopy[pipeToMergeId], tmpPipesCopy[optId], ref newPipe))
                                {
                                    int start = Math.Min(pipeToMergeId, optId);
                                    tmpPipesCopy.Insert(start, newPipe);
                                    tmpPipesCopy.RemoveAt(start+1);
                                    tmpPipesCopy.RemoveAt(start+2);
                                    //此处删除过道节点。
                                    tmpPipesCopy.RemoveAt(tmpPipesCopy.Count - 1);
                                    tmpPipes = tmpPipesCopy;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }                          
                        }
                    }
                }
                else      //如果过道剩余已经被合并
                {
                    List<int> indexList = new List<int>();
                    List<TmpPipe> tmpPipesCopy = new List<TmpPipe>(tmpPipes);
                    for (int i = 0; i < tmpPipesCopy.Count; i++)
                    {
                        indexList.Add(i);
                    }
                    indexList = indexList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList();

                    for (int i = 0; i < tmpPipesCopy.Count; i++)
                    {
                        int pipeToMergeId = indexList[i];
                        if (tmpPipesCopy[pipeToMergeId].DownstreamLength > mergeThreshold  || tmpPipesCopy.Count < 2)
                        {
                            cannotMerge = 1;
                            break;
                        }

                        List<int> AdjacentIdList = new List<int>();
                        //左侧管道
                        if (pipeToMergeId > 0)
                        {
                            AdjacentIdList.Add(pipeToMergeId - 1);
                        }
                        //右侧管道
                        if (pipeToMergeId < tmpPipesCopy.Count - 2)
                        {
                            AdjacentIdList.Add(pipeToMergeId + 1);
                        }
                        int optId = AdjacentIdList.OrderBy(x => tmpPipesCopy[x].DownstreamLength).ToList().First();

                        //
                        TmpPipe newPipe = new TmpPipe(0);
                        if (AbleToMerge(regionId, tmpPipesCopy[pipeToMergeId], tmpPipesCopy[optId], ref newPipe))
                        {
                            int start = Math.Min(pipeToMergeId, optId);
                            tmpPipesCopy.Insert(start, newPipe);
                            tmpPipesCopy.RemoveAt(start + 1);
                            tmpPipesCopy.RemoveAt(start + 1);
                            tmpPipes = tmpPipesCopy;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                }
            }

            //合并完毕后,整理管道
            for (int i = 0; i < tmpPipes.Count; i++)
            {
                TmpPipe mergedPipe = tmpPipes[i];
                mergedPipe.InternalId = i;
                mergedPipe.RegionIdList.Add(regionId);
                //mergedPipe.DoorIdList.Add(upDoorId);
                double upLength = ComputeTotalDistance(upDoorId, 0);
                double nowLength = mergedPipe.UpstreamLength - upLength;
                mergedPipe.UpstreamLength = upLength;
                mergedPipe.DownstreamLength += nowLength;
            }

            //构造子方案
            SubScheme subScheme = new SubScheme(regionId);
            subScheme.TmpPipeList.AddRange(tmpPipes);

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

        public List<SubScheme> CreateSubSchemeList(int regionId)
        {
            List<SubScheme> subSchemes = new List<SubScheme>();
            SingleRegion nowRegion = RegionList[regionId];

            for (int i = 0; i < nowRegion.ChildRegion.Count; i++)
            {
                int childRegionId = nowRegion.ChildRegion[i].RegionId;
                subSchemes.Add(RegionSchemeMap[childRegionId][0]);
            }

            return subSchemes;
        }

        /// <summary>
        ///  优化部分
        /// </summary>
        void Optimization()
        {
            List<TmpPipe> bestTmpPipeList = TmpPipeList;

            foreach (var pipe in bestTmpPipeList) 
            {
                pipe.CreatePipeTree(TopoTreeList, RegionToTree);
                pipe.GetLeftRegionIdList();
                pipe.GetRightRegionIdList();
            }

            int stop = 0;
            while (stop == 0)
            {
                stop = 1;
                List<CompareModel> compareModels = new List<CompareModel>();
                List<TmpPipe> minPipes = bestTmpPipeList.OrderBy(x => x.TotalLength).ToList();
                TmpPipe nowPipe = minPipes[0];
                double originalMinLength = nowPipe.TotalLength;
                if (nowPipe.TotalLength > Parameter.TotalLength / 2)
                {
                    break;
                }

                //进行优化
                int nowIndex = nowPipe.InternalId;
                List<int> adjacentPipes = new List<int>();
                if (nowIndex > 0)
                {
                    adjacentPipes.Add(nowIndex - 1);
                }
                if (nowIndex < bestTmpPipeList.Count - 1)
                {
                    adjacentPipes.Add(nowIndex + 1);
                }

                for (int i = 0; i < adjacentPipes.Count; i++)
                {
                    TmpPipe nowAdjacentPipe = bestTmpPipeList[adjacentPipes[i]];
                    for (int j = 0; j < nowAdjacentPipe.DomainIdList.Count; j++)
                    {
                        int regionId = nowAdjacentPipe.DomainIdList[j];
                        int topoId = RegionToTree[regionId];
                        List<TmpPipe> testPipesList = bestTmpPipeList.Copy();

                        //区分左右
                        int isRight = 0;
                        if (adjacentPipes[i] == nowIndex + 1) isRight = 1;

                        //如果可以转移
                        if (IsMoveAble(nowAdjacentPipe, regionId,isRight)) 
                        {
                            //如果是主导管线
                            int move = 0;
                            double newMinLength = 0;
                            MoveTest(nowIndex, adjacentPipes[i], regionId,ref move, ref testPipesList,ref newMinLength);
                            if (move == 1)
                            {
                                compareModels.Add(new CompareModel(testPipesList, newMinLength));
                            }
                        }     
                    }
                }

                if (compareModels.Count > 0)
                {
                    CompareModel bestModel = compareModels.FindByMax(x => x.MinLength);
                    if (bestModel.MinLength > originalMinLength)
                    {
                        stop = 0;
                        bestTmpPipeList = bestModel.TmpPipeList;
                    }
                }
            }

            TmpPipeList = bestTmpPipeList;
        }

        public bool IsMoveAble(TmpPipe nowPipe,int regionId,int dir) 
        {
            bool flag = false;

            if (dir == 0) //左侧管线移动给右侧管线
            {
                if (nowPipe.RightRegionIdList.Contains(regionId)) flag = true;
            }
            else 
            {
                if (nowPipe.LeftRegionIdList.Contains(regionId)) flag = true;
            }
            return flag; 
        }

        public void MoveTest(int nowIndex, int nowAdjacentPipe, int regionId, ref int move, ref List<TmpPipe> testPipesList, ref double newMinLength) 
        {
            TmpPipe nowPipe = testPipesList[nowIndex];
            TmpPipe adPipe = testPipesList[nowAdjacentPipe];
            double minLength = nowPipe.TotalLength;

            TmpPipe newNowPipe = AddDomainRegion(nowPipe, regionId);
            TmpPipe newAdPipe = DeleteDomainRegion(nowPipe, regionId);

            if (Math.Min(newAdPipe.TotalLength, newNowPipe.TotalLength) > minLength && Math.Max(newAdPipe.TotalLength, newNowPipe.TotalLength) < Parameter.TotalLength)
            {
                move = 1;
                newMinLength = Math.Min(newAdPipe.TotalLength, newNowPipe.TotalLength);
                testPipesList[nowIndex] = newNowPipe;
                testPipesList[nowAdjacentPipe] = newAdPipe;
            }
            else 
            {
                move = 0;
            }
        }

        public TmpPipe AddDomainRegion(TmpPipe oldPipe,int regionId) 
        {
            TmpPipe newPipe = new TmpPipe(0);

            newPipe.DomainIdList = oldPipe.DomainIdList;
            newPipe.DomainIdList.Add(regionId);
            newPipe.RegionIdList = oldPipe.RegionIdList;
            List<int> passingRegionIdLIst = GetPassingRegion(regionId);
            foreach (int id in passingRegionIdLIst)
            {
                if (!newPipe.RegionIdList.Contains(id))  newPipe.RegionIdList.Add(id);
            }
            newPipe.CreatePipeTree(TopoTreeList, RegionToTree);

            newPipe.TotalLength = ComputePipeTreeLength(newPipe);

            return newPipe;
        }

        public TmpPipe DeleteDomainRegion(TmpPipe oldPipe, int regionId)
        {
            TmpPipe newPipe = new TmpPipe(0);

            newPipe.DomainIdList = oldPipe.DomainIdList;
            newPipe.DomainIdList.Remove(regionId);
            newPipe.DomainIdListToRegionIdList(TopoTreeList, RegionToTree);
            
            newPipe.CreatePipeTree(TopoTreeList, RegionToTree);

            newPipe.TotalLength = ComputePipeTreeLength(newPipe);

            return newPipe;
        }

        public List<int> GetPassingRegion(int domainRegionId) 
        {
            List<int> passingRegionIdList = new List<int>();
            int topoId = RegionToTree[domainRegionId];
            while (true) 
            {
                passingRegionIdList.Add(TopoTreeList[topoId].NodeId);
                if (topoId == 0) break;
                topoId = RegionToTree[TopoTreeList[topoId].FatherId];
            }
            return passingRegionIdList;
        }

        public double ComputePipeTreeLength(TmpPipe tmpPipe) 
        {
            double totalLength = 0;
            Queue<int> idQueue = new Queue<int>();
            idQueue.Enqueue(0);
            while (idQueue.Count > 0) 
            {
                int nowRegion = idQueue.Dequeue();
                PipeTree nowTree = tmpPipe.PipeTreeList[tmpPipe.RegionToPipeTree[nowRegion]];
                int topoId = RegionToTree[nowRegion];
                int upUpDoorId = TopoTreeList[topoId].UpDoorId;

                if (tmpPipe.DomainIdList.Contains(nowRegion))
                {
                    totalLength += RegionUsedLength[nowRegion];
                }
                else
                {
                    if (nowTree.ChildRegionIdList.Count == 1)
                    {
                        int childId = nowTree.ChildRegionIdList[0];
                        idQueue.Enqueue(childId);
                        int childTopoId = RegionToTree[childId];
                        int upDoorId = TopoTreeList[childTopoId].UpDoorId;
                        totalLength += DoorToDoorDistanceMap[upDoorId, upUpDoorId].EstimatedDistance;
                    }
                    else
                    {
                        double maxLength = 0;
                        foreach (int childId in nowTree.ChildRegionIdList) 
                        {
                            idQueue.Enqueue(childId);
                            int childTopoId = RegionToTree[childId];
                            int upDoorId = TopoTreeList[childTopoId].UpDoorId;

                            double nowLength = DoorToDoorDistanceMap[upDoorId, upUpDoorId].EstimatedDistance;
                            if (nowLength > maxLength) 
                            {
                                maxLength = nowLength;
                            }
                        }
                        totalLength += maxLength;
                    }
                }             
            }
            return totalLength;
        }
        

        ////保存结果
        void SaveResults() 
        {
            List<TmpPipe> tmpPipeList = TmpPipeList;

            for (int i = 0; i < tmpPipeList.Count; i++) 
            {
                TmpPipe nowPipe = tmpPipeList[i];

                //init
                nowPipe.RegionIdListToDoorIdList(TopoTreeList);

                //保存pipe
                SinglePipe newPipe = new SinglePipe(i);
                newPipe.DomaintRegionList = nowPipe.DomainIdList;
                newPipe.PassedRegionList = nowPipe.RegionIdList;
                //newPipe.DoorList = nowPipe.DoorIdList;
                newPipe.TotalLength = nowPipe.TotalLength;
                SinglePipeList.Add(newPipe);

                //保存经过门的信息
                for (int j = 0; j < nowPipe.DoorIdList.Count; j++) 
                {
                    int doorId = nowPipe.DoorIdList[j];
                    DoorList[doorId].PipeIdList.Add(i);
                }

                //保存经过的区域
                for (int j = 0; j < nowPipe.RegionIdList.Count; j++)
                {
                    int regionId = nowPipe.RegionIdList[j];
                    RegionList[regionId].PassingPipeList.Add(i);
                }

                //保存主导的区域
                for (int j = 0; j < nowPipe.DomainIdList.Count; j++)
                {
                    int regionId = nowPipe.DomainIdList[j];
                    RegionList[regionId].MainPipe.Add(i);
                }
            }
            
            //记录管道
            ProcessedData.PipeList = SinglePipeList;

            //记录主入口
            for (int i = 0; i < RegionList.Count; i++) 
            {
                for (int j = 0; j < RegionList[i].EntranceMap.Count; j++) 
                {
                    if (RegionList[i].EntranceMap[RegionList[i].FatherRegion[j]].PipeIdList.Count != 0) 
                    {
                        RegionList[i].MainEntrance = RegionList[i].EntranceMap[RegionList[i].FatherRegion[j]];
                        break;
                    }
                }
            }
        }

        void PrintResults() 
        {
            for (int i = 0; i < RegionList.Count; i++)
            {
                SingleRegion sr = RegionList[i];
                Point3d ptDraw = sr.ClearedPl.GetCenter();
                string draw = sr.MainPipe[0].ToString();
                //DrawUtils.ShowGeometry(sr.ClearedPl, "l1ClearedPl", 2, lineWeightNum: 30);
                DrawUtils.ShowGeometry(ptDraw, draw, "l1MainPipe", 2, 30, 100);
            }
        }

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
                int topoIndex = RegionToTree[upRegionId];
                int upDoorId = TopoTreeList[topoIndex].UpDoorId;
                
                distante += DoorToDoorDistanceMap[nowDoorId,upDoorId].EstimatedDistance;
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


        /////////////////////////////////////////////////////////////////////////
        public void Distribute2() 
        {
            
        }

        public List<List<SubScheme>> CreateSubSchemeCombinationList(int regionId)
        {
            List<List<SubScheme>> SubSchemeCombinationList = new List<List<SubScheme>>();
            SingleRegion nowRegion = RegionList[regionId];
            List<List<SubScheme>> allSubScheme = new List<List<SubScheme>>();
            List<int> subSchemeIdNum = new List<int>();

            for (int i = 0; i < nowRegion.ChildRegion.Count; i++)
            {
                int childRegionId = nowRegion.ChildRegion[i].RegionId;
                allSubScheme.Add(RegionSchemeMap[childRegionId]);
                subSchemeIdNum.Add(RegionSchemeMap[childRegionId].Count);
            }

            List<SubScheme> tmpSubScheme = new List<SubScheme>();
            SubSchemeDFS(0, nowRegion.ChildRegion.Count, subSchemeIdNum, allSubScheme, tmpSubScheme, ref SubSchemeCombinationList);
            return SubSchemeCombinationList;
        }

        void SubSchemeDFS(int n, int m, List<int> idList, List<List<SubScheme>> allSubScheme, List<SubScheme> tmpSubScheme, ref List<List<SubScheme>> SubSchemeCombinationList)
        {
            if (n == m)
            {
                if (tmpSubScheme.Count != m)
                {
                    return;
                }
                else
                {
                    //Polyline a = RegionList[0].ClearedPl.Copy();
                    //Polyline b = RegionList[0].ClearedPl;
                    //Polyline c = RegionList[0].ClearedPl.Clone();

                    List<SubScheme> clone = new List<SubScheme>(tmpSubScheme);
                    SubSchemeCombinationList.Add(clone);
                }
            }
            if (n < m)
            {
                for (int i = 0; i < idList[n]; i++)
                {
                    tmpSubScheme.Add(allSubScheme[n][i]);
                    SubSchemeDFS(n + 1, m, idList, allSubScheme, tmpSubScheme, ref SubSchemeCombinationList);
                    tmpSubScheme.RemoveAt(tmpSubScheme.Count - 1);
                }
            }
        }

        
    }

    class SubScheme
    {
        public int RegionId = -1;
        public List<TmpPipe> TmpPipeList = new List<TmpPipe>();
        public SubScheme(int regionId)
        {
            RegionId = regionId;
        }
    }

    class TmpPipe
    {
        public int InternalId = -1;
        public double TotalLength = 0;
        public List<int> DomainIdList = new List<int>();


        //可生成的属性
        public List<int> RegionIdList = new List<int>();
        public List<int> DoorIdList = new List<int>();
        
        //非必要属性
        public double UpstreamLength = 0;
        public double DownstreamLength = 0;
        
        //管道树属性
        public List<PipeTree> PipeTreeList = new List<PipeTree>();
        public List<int> LeftRegionIdList = new List<int>();
        public List<int> RightRegionIdList = new List<int>();
        public Dictionary<int, int> RegionToPipeTree = new Dictionary<int, int>();

        public TmpPipe(int iid)
        {   
            InternalId = iid;
        }

        public void CreatePipeTree(List<TopoTree> treeList, Dictionary<int, int> regionToTree) 
        {
            PipeTreeList.Clear();
            List<int> sortedRegionIdList = RegionIdList.OrderBy(x => regionToTree[x]).ToList();
            for (int i = 0; i < sortedRegionIdList.Count; i++) 
            {
                int topoId = regionToTree[sortedRegionIdList[i]];
                List<int> childRegionIdList = new List<int>();
                for (int j = 0; j < treeList[topoId].ChildId.Count; j++) 
                {
                    int childId = treeList[topoId].ChildId[j];
                    if (RegionIdList.Contains(childId)) childRegionIdList.Add(childId);
                }
                int fatherRegionId = treeList[topoId].FatherId;
                PipeTree nowPipeTree = new PipeTree(sortedRegionIdList[i], fatherRegionId, treeList[topoId].Level);
                RegionToPipeTree.Add(sortedRegionIdList[i], PipeTreeList.Count);
                nowPipeTree.ChildRegionIdList = childRegionIdList;
                PipeTreeList.Add(nowPipeTree);
            }
        }

        public void DomainIdListToRegionIdList(List<TopoTree> treeList, Dictionary<int, int> regionToTree) 
        {
            RegionIdList.Clear();

            List<int> visited = new List<int>();
            for (int i = 0; i < treeList.Count; i++)
            {
                visited.Add(0);
            }

            for (int i = 0; i < DomainIdList.Count; i++)
            {
                List<int> nowPassingList = GetPassingRegion(DomainIdList[i], treeList, regionToTree);
                for (int j = 0; j < nowPassingList.Count; j++)
                {
                    if (visited[nowPassingList[j]] == 0)
                    {
                        visited[nowPassingList[j]] = 1;
                        RegionIdList.Add(nowPassingList[j]);
                    }
                }
            }          
        }

        public void RegionIdListToDoorIdList(List<TopoTree> topoTreeList) 
        {
            DoorIdList.Clear();
            for (int i = 0; i < RegionIdList.Count; i++) 
            {
                DoorIdList.Add(topoTreeList[RegionIdList[i]].UpDoorId);
            }
        }

        public void GetLeftRegionIdList()
        {
            LeftRegionIdList.Clear();
            int nowPipeTreeIndex = 0;

            while (true) 
            {
                LeftRegionIdList.Add(PipeTreeList[nowPipeTreeIndex].NowRegionId);
                if (PipeTreeList[nowPipeTreeIndex].ChildRegionIdList.Count > 0)
                {
                    int leftChildId = PipeTreeList[nowPipeTreeIndex].ChildRegionIdList.First();
                    nowPipeTreeIndex = RegionToPipeTree[leftChildId];
                }
                else 
                {
                    break;
                }
            }
        }

        public void GetRightRegionIdList()
        {
            RightRegionIdList.Clear();
            int nowPipeTreeIndex = 0;

            while (true)
            {
                RightRegionIdList.Add(PipeTreeList[nowPipeTreeIndex].NowRegionId);
                if (PipeTreeList[nowPipeTreeIndex].ChildRegionIdList.Count > 0)
                {
                    int rightChildId = PipeTreeList[nowPipeTreeIndex].ChildRegionIdList.Last();
                    nowPipeTreeIndex = RegionToPipeTree[rightChildId];
                }
                else
                {
                    break;
                }
            }
        }

        public List<int> GetPassingRegion(int domainRegionId ,List<TopoTree> treeList, Dictionary<int, int> regionToTree)
        {
            List<int> passingRegionIdList = new List<int>();
            int topoId = regionToTree[domainRegionId];
            while (true)
            {
                passingRegionIdList.Add(treeList[topoId].NodeId);
                if (topoId == 0) break;
                topoId = regionToTree[treeList[topoId].FatherId];
            }
            return passingRegionIdList;
        }

    }

    class TopoTree
    {
        public int NodeId = -1;
        public int Level = -1;
        public int FatherId = -1;
        public int UpDoorId = -1;
        public List<int> ChildId = new List<int>();

        public TopoTree(int nodeId, int level, int fatherId, int upDoorId)
        {
            this.NodeId = nodeId;
            this.Level = level;
            this.FatherId = fatherId;
            this.UpDoorId = upDoorId;
            //this.ChildId = childId;
        }

        static public List<List<int>> levelOrder(List<TopoTree> treeNode)
        {

            //层次遍历
            Queue<int> queue = new Queue<int>();
            List<List<int>> list = new List<List<int>>();
            TopoTree parentNode = treeNode[0];

            queue.Enqueue(parentNode.NodeId);
            while (queue.Count > 0)
            {
                //出一个，进n个
                //出一个
                int nodeId = queue.Dequeue();
                list[0].Add(nodeId);
                //进n个
                //List<IdTree> childens = node.ChildId;
                //for (IdTree childNode : childens)
                //{
                //    queue.add(childNode);
                //}
            }
            return list;
        }

        static public List<List<int>> LevelOrder(List<TopoTree> treeNode)
        {
            List<List<int>> list = new List<List<int>>();
            int nowLevel = 0;
            List<int> tmpList = new List<int>();
            for (int i = 0; i < treeNode.Count; i++)
            {
                tmpList.Add(i);
                if (i == treeNode.Count - 1 || treeNode[i + 1].Level > nowLevel)
                {
                    List<int> newList = new List<int>(tmpList);
                    list.Add(newList);
                    nowLevel++;
                    tmpList.Clear();
                }
            }
            return list;
        }
        
        static public void PostOrder(List<TopoTree> treeList, Dictionary<int,int> regionToTree, TopoTree topoTree, ref List<int> postOrderList)
        {
            for (int i = 0; i < topoTree.ChildId.Count; i++) 
            {
                int childRegionId = topoTree.ChildId[i];
                TopoTree childTree = treeList[regionToTree[childRegionId]];
                TopoTree.PostOrder(treeList, regionToTree, childTree, ref postOrderList);
            }
            postOrderList.Add(topoTree.NodeId);
        }
    }

    class PipeTree
    {
        public int Level = -1;
        public int NowRegionId = -1;
        public int FatherRegionId = -1;
        public List<int> ChildRegionIdList = new List<int>();
        public PipeTree(int nowRegion, int fatherRegion,int level) 
        {
            NowRegionId = nowRegion;
            FatherRegionId = fatherRegion;
            Level = level;
        }
    }

    class CompareModel 
    {
        public double MinLength;
        public List<TmpPipe> TmpPipeList;

        public CompareModel(List<TmpPipe> tmpPipes, double minLength) 
        {
            this.TmpPipeList = tmpPipes;
            this.MinLength = minLength;
        }

        public CompareModel(List<TmpPipe> tmpPipes)
        {
            this.TmpPipeList = tmpPipes;
            this.MinLength = tmpPipes.FindByMin(x => x.TotalLength).TotalLength;
        }
    }

    ////废弃

    //class MergeElement
    //{
    //    int Id = -1;
    //    int IsPassing = 0;
    //    List<int> AdjacentId = new List<int>();
    //    double InRegionDistance = 0;

    //    public MergeElement() 
    //    {

    //    }
    //}

    //class DistanceStorage 
    //{
    //    public double upPassagewayLength = 0;
    //    public double downPassagewayLength = 0;
    //    public double anticlockwiseLength = 0;
    //    public double clockwiseLength = 0;
    //}


    //    foreach (int id in passingRegionIdLIst)
    //{
    //    if (newPipe.RegionIdList.Contains(id)) 
    //    {
    //        int index = oldPipe.RegionToPipeTree[regionId];
    //        if (!newPipe.DomainIdList.Contains(id) && oldPipe.PipeTreeList[index].ChildRegionIdList.Count< 2) 
    //        {
    //            newPipe.RegionIdList.Remove(id);
    //        }
    //    }
    //}
}
