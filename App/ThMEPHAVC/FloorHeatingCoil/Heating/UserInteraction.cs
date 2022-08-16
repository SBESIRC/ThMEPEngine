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
        


        public UserInteraction() 
        {
            
        }

        public void Pipeline() 
        {
            CreateTmpPipeList();
            CreateNowTree();
            CompleteTmpPipeList();
            SaveResults();
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
