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
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class UserInteraction
    {
        public List<SingleRegion> RegionList = ProcessedData.RegionList;
        public List<SingleDoor> DoorList = ProcessedData.DoorList;
        public List<SinglePipe> SinglePipeList = new List<SinglePipe>();
        public DoorToDoorDistance[,] DoorToDoorDistanceMap = ProcessedData.DoorToDoorDistanceMap;

        ////成员变量
        public List<TmpPipe> TmpPipeList = new List<TmpPipe>();
        public List<TopoTreeNode> SingleTopoTree = new List<TopoTreeNode>();
        Dictionary<int, int> RegionToNode = new Dictionary<int, int>();
        public Dictionary<int, int> ChildFatherMap = new Dictionary<int, int>();



        ///接口临时变量
        public int MainRegionId = -1;
        public List<List<Connection>> RegionConnection = new List<List<Connection>>();
        public List<List<int>> RegionGraphList = new List<List<int>>();
        public List<Polyline> RegionObbs = new List<Polyline>();

        //C
        public int LeftRightIndex = 0;


        public UserInteraction()
        {

        }


        //输入

        //List<List<int>>
        public void PipelineA(ThRoomSetModel roomSet)
        {
            RawData singleRawdata = new RawData(roomSet);
            DataPreprocess dataPreprocess = new DataPreprocess(singleRawdata);
            dataPreprocess.PipelineA();
            RegionConnection = dataPreprocess.RegionConnection;
            MainRegionId = dataPreprocess.MainRegionId;
            RegionObbs = dataPreprocess.RegionObbs;
            if (MainRegionId == -1)
            {
                MainRegionId = 0;
            }

            BuildRegionGraphList();
        }

        public void BuildRegionGraphList()
        {
            List<int> remainRegion = new List<int>();
            for (int i = 0; i < RegionObbs.Count; i++)
            {
                remainRegion.Add(i);
            }

            List<int> tmpList = new List<int>();
            Queue<int> regionQ = new Queue<int>();
            regionQ.Enqueue(MainRegionId);
            tmpList.Add(MainRegionId);
            remainRegion.Remove(MainRegionId);
            while (regionQ.Count > 0)
            {
                int nowId = regionQ.Dequeue();
                for (int i = 0; i < RegionConnection[nowId].Count; i++)
                {
                    int newId = RegionConnection[nowId][i].RegionId;
                    if (remainRegion.Contains(newId))
                    {
                        tmpList.Add(newId);
                        remainRegion.Remove(newId);
                        regionQ.Enqueue(newId);
                    }
                }
            }
            RegionGraphList.Add(tmpList);


            while (remainRegion.Count > 0)
            {
                tmpList = new List<int>();
                Queue<int> regionQ2 = new Queue<int>();
                regionQ2.Enqueue(remainRegion[0]);
                tmpList.Add(remainRegion[0]);
                remainRegion.RemoveAt(0);


                while (regionQ2.Count > 0)
                {
                    int nowId = regionQ2.Dequeue();
                    for (int i = 0; i < RegionConnection[nowId].Count; i++)
                    {
                        int newId = RegionConnection[nowId][i].RegionId;
                        if (remainRegion.Contains(newId))
                        {
                            tmpList.Add(newId);
                            remainRegion.Remove(newId);
                            regionQ2.Enqueue(newId);
                        }
                    }
                }
                RegionGraphList.Add(tmpList);
            }
        }

        public void PipelineB(ThRoomSetModel roomSet)
        {
            ParameterSetting();

            //数据处理
            RawData singleRawdata = new RawData(roomSet);
            DataPreprocess dataPreprocess = new DataPreprocess(singleRawdata);
            dataPreprocess.Pipeline();

            //分配


            //if (!Parameter.PublicRegionConstraint &&
            //    !Parameter.AuxiliaryRoomConstraint &&
            //    !Parameter.IndependentRoomConstraint)
            //{
            //    DistributionService distributionService = new DistributionService();
            //    distributionService.Pipeline();
            //}
            //else { }

            //DistributionService2 distributionService2 = new DistributionService2();
            //distributionService2.Pipeline();

            DistributionService3 distributionService3 = new DistributionService3();
            distributionService3.Pipeline();

            //寻找出入口
            FindPointService findPointService = new FindPointService();
            findPointService.Pipeline();

            //
            DrawPipe drawPipe = new DrawPipe();
            drawPipe.Pipeline();
        }

        public void PipelineC()
        {
            ParameterSetting();

            LeftRightIndex = 0;
            //Update
            CreateTmpPipeList();
            CreateNowTree();
            CompleteTmpPipeList();
            SaveResults();

            //寻找出入口
            FindPointService findPointService = new FindPointService();
            findPointService.Pipeline();

            //绘制管道
            DrawPipe drawPipe = new DrawPipe();
            drawPipe.Pipeline();
        }

        public void CreateTmpPipeList()
        {

            Dictionary<int, int> indexMap = new Dictionary<int, int>();
            int num = 0;
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (RegionList[i].MainPipe != null && RegionList[i].MainPipe.Count > 0 && RegionList[i].MainPipe[0] >= 0)
                {
                    if (!indexMap.ContainsKey(RegionList[i].MainPipe[0]))
                    {
                        indexMap.Add(RegionList[i].MainPipe[0], num);
                        num++;
                    }
                }
            }

            for (int i = 0; i < RegionList.Count; i++)
            {
                if (RegionList[i].MainPipe != null && RegionList[i].MainPipe.Count > 0 && RegionList[i].MainPipe[0] >= 0)
                {
                    RegionList[i].MainPipe[0] = indexMap[RegionList[i].MainPipe[0]];
                }
            }
            int maxIndex = num; 


            //List<int> indexList = new List<int>();
            //for (int i = 0; i < RegionList.Count; i++) 
            //{
            //    if (RegionList[i].MainPipe!= null && RegionList[i].MainPipe.Count> 0 && RegionList[i].MainPipe[0] >= 0) {
            //        indexList.Add(RegionList[i].MainPipe[0]);
            //    }
            //}
            //int maxIndex = indexList.FindByMax(x => x) + 1;
            //int minIndex = indexList.FindByMin(x => x);

            //for (int i = 0; i < RegionList.Count; i++) 
            //{
            //    if(RegionList[i].MainPipe!=null && RegionList[i].MainPipe.Count > 0)
            //    RegionList[i].MainPipe[0] = RegionList[i].MainPipe[0] - minIndex;
            //}
            //maxIndex = maxIndex - minIndex;

            for (int i = 0; i < maxIndex ; i++)
            {
                TmpPipeList.Add(new TmpPipe(0));
            }

            for (int i = 0; i < RegionList.Count; i++)
            {
                if (RegionList[i].MainPipe != null && RegionList[i].MainPipe.Count > 0 && RegionList[i].MainPipe[0] >= 0)
                {
                    int mainPipeId = RegionList[i].MainPipe[0];
                    TmpPipeList[mainPipeId].DomainIdList.Add(i);
                }
            }
        }

        void CreateNowTree()
        {
            GetChildFatherMap();

            //TopoTreeList = new List<TopoTree>(new TopoTree[RegionList.Count]);
            SingleTopoTree = new List<TopoTreeNode>();
            RegionToNode = new Dictionary<int, int>();
            Queue<int> topoIdQueue = new Queue<int>();
            List<int> visited = new List<int>();
            //int nowIndex = 0; 

            for (int i = 0; i < RegionList.Count; i++)
            {
                visited.Add(0);
                RegionToNode.Add(i, -1);
            }

            visited[0] = 1;

            topoIdQueue.Enqueue(0);
            TopoTreeNode tmpNode = new TopoTreeNode(0, RegionList[0].Level, 0, 0);
            RegionToNode[0] = SingleTopoTree.Count;
            SingleTopoTree.Add(tmpNode);

            while (topoIdQueue.Count > 0)
            {
                int topoIndex = topoIdQueue.Dequeue();
                int regionId = SingleTopoTree[topoIndex].NodeId;
                //int treeIndex = regionToTree[regionId]; 
                List<int> childIdList = new List<int>();
                for (int i = 0; i < RegionList[regionId].ChildRegion.Count; i++)
                {
                    int childRegionId = RegionList[regionId].ChildRegion[i].RegionId;
                    if (visited[childRegionId] == 0 && ChildFatherMap[childRegionId] == regionId)
                    {
                        visited[childRegionId] = 1;
                        tmpNode = new TopoTreeNode(childRegionId, RegionList[childRegionId].Level, regionId, RegionList[regionId].ExportMap[RegionList[childRegionId]].DoorId);
                        RegionToNode[childRegionId] = SingleTopoTree.Count;
                        SingleTopoTree.Add(tmpNode);
                        //
                        childIdList.Add(childRegionId);
                        //
                        topoIdQueue.Enqueue(RegionToNode[childRegionId]);
                        
                        //
                        RegionList[childRegionId].MainEntrance = RegionList[regionId].ExportMap[RegionList[childRegionId]];
                    }
                }
                SingleTopoTree[topoIndex].ChildIdList = childIdList;
            }
        }

        public void GetChildFatherMap()
        {
            for (int i = 0; i < RegionList.Count; i++)
            {
                int nowRegionFix = 0;
                if (RegionList[i].FatherRegion.Count > 1)
                {
                    List<int> parentList = new List<int>();
                    RegionList[i].FatherRegion.ForEach(x => parentList.Add(x.RegionId));

                    for (int j = 0; j < TmpPipeList.Count; j++)
                    {
                        for (int k = 0; k < parentList.Count; k++)
                        {
                            int nowParent = parentList[k];
                            if (TmpPipeList[j].DomainIdList.Contains(nowParent) && TmpPipeList[j].DomainIdList.Contains(i))
                            {
                                if (nowRegionFix == 0)
                                {
                                    ChildFatherMap.Add(i, nowParent);
                                    nowRegionFix = 1;
                                }
                                else return;
                            }
                        }
                    }

                    if (nowRegionFix == 0)
                    {
                        List<SingleRegion> singleRegions = RegionList[i].FatherRegion.OrderByDescending(x => x.ChildRegion.Count).ToList();
                        ChildFatherMap.Add(i, singleRegions[0].RegionId);
                    }
                }
                else if (RegionList[i].FatherRegion.Count == 1)
                {
                    ChildFatherMap.Add(i, RegionList[i].FatherRegion.First().RegionId);
                }
            }
        }

        public void CompleteTmpPipeList()
        {
            for (int i = 0; i < TmpPipeList.Count; i++)
            {
                TmpPipeList[i].Regularization(SingleTopoTree, RegionToNode);
            }

            GetTopoTreeNodeLeftRightIndex(SingleTopoTree, RegionToNode, SingleTopoTree[0]);
            TmpPipeList = PipeListReSort(TmpPipeList, SingleTopoTree, RegionToNode);


            //清空原始数据
            for (int i = 0; i < RegionList.Count; i++)
            {
                RegionList[i].PassingPipeList.Clear();
            }

            List<double> lengthList = new List<double>(new double[TmpPipeList.Count]);
            for (int i = 0; i < TmpPipeList.Count; i++)
            {
                TmpPipe nowPipe = TmpPipeList[i];

                //保存经过的区域
                for (int j = 0; j < nowPipe.RegionIdList.Count; j++)
                {
                    int regionId = nowPipe.RegionIdList[j];
                    RegionList[regionId].PassingPipeList.Add(i);
                }

                //更新mainpipe
                for (int j = 0; j < nowPipe.DomainIdList.Count; j++)
                {
                    int regionId = nowPipe.DomainIdList[j];
                    RegionList[regionId].MainPipe[0] = i;
                }

                lengthList[i] = ComputePipeTreeLength(nowPipe);
            }


            //清空原始数据
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (RegionList[i].MainPipe == null || RegionList[i].MainPipe.Count == 0) 
                {
                    List<int> newList = RegionList[i].PassingPipeList.OrderBy(x => lengthList[x]).ToList();
                    if (newList.Count > 0) 
                    {
                        //放入数据
                        int newIndex = newList.First();
                        RegionList[i].MainPipe = new List<int>();
                        RegionList[i].MainPipe.Add(newIndex);


                        //更新Pipe
                        TmpPipe getPipe = TmpPipeList[newIndex];
                        getPipe.DomainIdList.Add(i);
                        if (!getPipe.RegionIdList.Contains(i))
                        {
                            getPipe.RegionIdList.Add(i);
                        }
                        lengthList[newIndex] = ComputePipeTreeLength(getPipe);
                    }
                }
            }
        }

        public void GetTopoTreeNodeLeftRightIndex(List<TopoTreeNode> treeList, Dictionary<int, int> regionToTree, TopoTreeNode topoTree)
        {
            int left = -1;
            int right = -1;
            for (int i = 0; i < topoTree.ChildIdList.Count; i++)
            {
                int childRegionId = topoTree.ChildIdList[i];
                TopoTreeNode childTree = treeList[regionToTree[childRegionId]];
                GetTopoTreeNodeLeftRightIndex(treeList, regionToTree, childTree);

                if (left != -1)
                {
                    left = Math.Min(childTree.LeftTopoIndex, left);
                }
                else
                {
                    left = childTree.LeftTopoIndex;
                }
                right = Math.Max(childTree.RightTopoIndex, right);
            }

            if (topoTree.ChildIdList.Count == 0)
            {
                left = LeftRightIndex;
                right = LeftRightIndex;
                LeftRightIndex++;
            }

            topoTree.LeftTopoIndex = left;
            topoTree.RightTopoIndex = right;
        }

        public List<TmpPipe> PipeListReSort(List<TmpPipe> tmpPipes, List<TopoTreeNode> treeList, Dictionary<int, int> regionToTree)
        {
            tmpPipes.Sort((a, b) =>
            {
                return TmpPipe.GetLeftRight(a, b, treeList, regionToTree);
            });

            return tmpPipes;
        }

        ////保存结果
        void SaveResults()
        {
            List<TmpPipe> tmpPipeList = TmpPipeList;

            //清空原始数据
            for (int i = 0; i < RegionList.Count; i++) 
            {
                RegionList[i].PassingPipeList.Clear();            
            }

            for (int i = 0; i < DoorList.Count; i++)
            {
                DoorList[i].PipeIdList.Clear();
            }


            for (int i = 0; i < tmpPipeList.Count; i++)
            {
                TmpPipe nowPipe = tmpPipeList[i];

                //init
                nowPipe.RegionIdListToDoorIdList(SingleTopoTree, RegionToNode);

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
                    newPipe.DoorList.Add(doorId);
                }

                //保存经过的区域
                for (int j = 0; j < nowPipe.RegionIdList.Count; j++)
                {
                    int regionId = nowPipe.RegionIdList[j];
                    RegionList[regionId].PassingPipeList.Add(i);
                }

                //更新mainpipe
                for (int j = 0; j < nowPipe.DomainIdList.Count; j++)
                {
                    int regionId = nowPipe.DomainIdList[j];
                    RegionList[regionId].MainPipe[0] = i;
                }
            }


            //记录管道
            ProcessedData.PipeList = SinglePipeList;
        }

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
                int upUpDoorId = SingleTopoTree[topoId].UpDoorId;

                if (tmpPipe.DomainIdList.Contains(nowRegion))
                {
                    totalLength += RegionList[nowRegion].UsedPipeLength;
                }

                if (nowNode.ChildRegionIdList.Count == 1)
                {
                    int childId = nowNode.ChildRegionIdList[0];
                    idQueue.Enqueue(childId);
                    int childTopoId = RegionToNode[childId];
                    int upDoorId = SingleTopoTree[childTopoId].UpDoorId;
                    totalLength += DoorToDoorDistanceMap[upDoorId, upUpDoorId].EstimatedDistance * 2;
                }
                else
                {
                    double maxLength = 0;
                    foreach (int childId in nowNode.ChildRegionIdList)
                    {
                        idQueue.Enqueue(childId);
                        int childTopoId = RegionToNode[childId];
                        int upDoorId = SingleTopoTree[childTopoId].UpDoorId;

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


        public void ParameterSetting() 
        {
            PublicValue.ChangeSDis = 0;
            PublicValue.Clear0 = 1;
            PublicValue.Clear1 = 1;
        
        }
    }
}
