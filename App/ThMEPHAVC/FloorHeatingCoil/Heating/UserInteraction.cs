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
        public List<SinglePipe> SinglePipeList = ProcessedData.PipeList;


        ////成员变量
        public List<TmpPipe> TmpPipeList = new List<TmpPipe>();
        public List<TopoTreeNode> SingleTopoTree;
        Dictionary<int, int> RegionToNode = new Dictionary<int, int>();
        public Dictionary<int, int> ChildFatherMap = new Dictionary<int, int>();
        


        ///接口临时变量
        public int MainRegionId = -1;
        public List<List<Connection>> RegionConnection;
        public List<List<int>> RegionGraphList;
        public List<Polyline> RegionObbs;

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
            dataPreprocess.Pipeline();
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
            LeftRightIndex = 0;
            //Update
            CreateTmpPipeList();
            CreateNowTree();
            CompleteTmpPipeList();
            SaveResults();

            //管道分配
            DistributionService3 distributionService3 = new DistributionService3();
            distributionService3.Pipeline();

            //寻找出入口
            FindPointService findPointService = new FindPointService();
            findPointService.Pipeline();

            //绘制管道
            DrawPipe drawPipe = new DrawPipe();
            drawPipe.Pipeline();
        }

        public void CreateTmpPipeList() 
        {
            int maxIndex = RegionList.FindByMax(x => x.MainPipe[0]).MainPipe[0];

            for (int i = 0; i < maxIndex; i++)
            {
                TmpPipeList.Add(new TmpPipe(0));
            }

            for (int i = 0; i < RegionList.Count; i++) 
            {
                int mainPipeId = RegionList[i].MainPipe[0];
                TmpPipeList[mainPipeId].DomainIdList.Add(i);
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

                    for (int j = 0; j < TmpPipeList.Count; i++)
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
    }
}
