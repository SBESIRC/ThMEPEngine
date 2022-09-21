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
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class FindPointService
    {
        //外部变量
        public List<SingleRegion> RegionList = ProcessedData.RegionList;
        public List<SingleDoor> DoorList = ProcessedData.DoorList;
        public List<SinglePipe> SinglePipeList = ProcessedData.PipeList;
        public DoorToDoorDistance[,] DoorToDoorDistanceMap = ProcessedData.DoorToDoorDistanceMap;
        public Dictionary<Tuple<int, int>, PipePoint> DoorPipeToPointMap = new Dictionary<Tuple<int, int>, PipePoint>();
        public Dictionary<int, List<int>> RegionToDoorLineIndex = new Dictionary<int, List<int>>();
        public Dictionary<SingleDoor, Tuple<int, int>> DoorLeftRightPipe = new Dictionary<SingleDoor, Tuple<int, int>>();
        
        //内部变量
        List<int> IfSet;
        Dictionary<int, DoorPoinType> DoorPointTypeMap = new Dictionary<int, DoorPoinType>();  //0：均匀，1：推荐，2：自由
       

        public FindPointService()
        {
            //Pipeline();
        }

        public void Pipeline()
        {
            GetDoorLineIndex();

            FindStartRoomPoint();

            //开始找点
            FindPointNew();

            CheckFreedomAgain();

            SaveResult();

            Draw();
        }

        public void GetDoorLineIndex()
        {
            for (int i = 0; i < RegionList.Count; i++)
            {
                List<int> doorLineList = new List<int>();
                for (int j = 0; j < RegionList[i].ChildRegion.Count; j++)
                {
                    doorLineList.Add(RegionList[i].ExportMap[RegionList[i].ChildRegion[j]].UpLineIndex);
                }
                RegionToDoorLineIndex.Add(i, doorLineList);
            }
        }

        public void FindStartRoomPoint()
        {
            double pipeSpaceing = Parameter.PipeSpaceing;
            SingleDoor nowDoor = DoorList[0];

           
            Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;
            Polyline UpPl = nowDoor.UpstreamRegion.ClearedPl;


            int freedom = 0;
            double tmpDis = 0;
            double tmpX = 0;

            if (downDoor.Length < pipeSpaceing * (nowDoor.PipeIdList.Count * 2 - 1)-5)
            {
                tmpDis = downDoor.Length / (nowDoor.PipeIdList.Count * 2 + 1);
                tmpDis = ((int)tmpDis / 10) * 10;
            }
            else 
            {
                tmpDis = pipeSpaceing;
            }

            if (!ProcessedData.LeftToRight)
            {
                for (int j = 0; j < nowDoor.PipeIdList.Count; j++)
                {
                    int pipeId = nowDoor.PipeIdList[j];

                    if (j != 0)
                    {
                        tmpX += tmpDis;
                    }
                    Point3d pt3 = nowDoor.DownSecond + tmpX * downDoor.GetNormal();
                    tmpX += tmpDis;
                    Point3d pt4 = nowDoor.DownSecond + tmpX * downDoor.GetNormal();

                    List<Point3d> point3Ds = new List<Point3d>();
                    Point3d pt1 = new Point3d();
                    Point3d pt2 = new Point3d();
                    point3Ds.Add(pt1);
                    point3Ds.Add(pt2);
                    point3Ds.Add(pt3);  
                    point3Ds.Add(pt4);
                    PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                    DoorPipeToPointMap.Add(new Tuple<int, int>(0, pipeId), pipePoint);
                }
            }
            else //从右向左
            {
                for (int j = 0; j < nowDoor.PipeIdList.Count; j++)
                {
                    int pipeIndex = j;
                    pipeIndex = nowDoor.PipeIdList.Count - 1 - j;
                    int pipeId = nowDoor.PipeIdList[pipeIndex];

                    if (j != 0)
                    {
                        tmpX += tmpDis;
                    }
                    Point3d pt3 = nowDoor.DownFirst- tmpX * downDoor.GetNormal();
                    tmpX += tmpDis;
                    Point3d pt4 = nowDoor.DownFirst - tmpX * downDoor.GetNormal();

                    List<Point3d> point3Ds = new List<Point3d>();
                    Point3d pt1 = new Point3d();
                    Point3d pt2 = new Point3d();
                    point3Ds.Add(pt1);
                    point3Ds.Add(pt2);
                    point3Ds.Add(pt3);    //这里未必是从左到右的
                    point3Ds.Add(pt4);
                    PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                    DoorPipeToPointMap.Add(new Tuple<int, int>(0, pipeId), pipePoint);
                }

            }
        }

        public void FindPoint()
        {
            IfSet = new List<int>(new int[DoorList.Count]);

            for (int i = 1; i < DoorList.Count; i++)
            {
                int nowIfSet = 0;
                if (IfSet[i] == 1) continue; //如果已经设置完毕则跳出

                //SpecialSet1(DoorList[i],ref nowIfSet);  //第一类特殊情形
                if (nowIfSet == 1) continue;


                SingleDoor nowDoor = DoorList[i];
                Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
                Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;
                Polyline UpPl = nowDoor.UpstreamRegion.ClearedPl;
                List<Point3d> ptList = UpPl.GetPoints().ToList();

                int leftAdjacentPipeNum = 0;
                int rightAdjacentPipeNum = 0;
                GetAdjacentPipeNum(nowDoor, ref leftAdjacentPipeNum, ref rightAdjacentPipeNum);
                DoorLeftRightPipe.Add(nowDoor, new Tuple<int, int>(leftAdjacentPipeNum, rightAdjacentPipeNum));

                if (i == 5)
                {
                    int stop = 0;
                }

                if (upDoor.Length < Parameter.SuggestDistanceRoom * (nowDoor.PipeIdList.Count * 2 - 1) + 2 * Parameter.SuggestDistanceWall)
                {
                    int freedom = 0;

                    double tmpDis = upDoor.Length / (nowDoor.PipeIdList.Count * 2 + 1);
                    //tmpDis = ((int)tmpDis / 20) * 20;
                    double tmpX = 0;
                    for (int j = 0; j < nowDoor.PipeIdList.Count; j++)
                    {
                        int pipeId = nowDoor.PipeIdList[j];

                        tmpX += tmpDis;
                        Point3d pt1 = nowDoor.UpFirst + tmpX * upDoor.GetNormal();
                        Point3d pt3 = nowDoor.DownSecond + tmpX * downDoor.GetNormal();
                        tmpX += tmpDis;
                        Point3d pt2 = nowDoor.UpFirst + tmpX * upDoor.GetNormal();
                        Point3d pt4 = nowDoor.DownSecond + tmpX * downDoor.GetNormal();

                        List<Point3d> point3Ds = new List<Point3d>();
                        point3Ds.Add(pt1);
                        point3Ds.Add(pt2);
                        point3Ds.Add(pt3);
                        point3Ds.Add(pt4);
                        PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                        DoorPipeToPointMap.Add(new Tuple<int, int>(i, pipeId), pipePoint);
                    }
                }
                else
                {
                    int freedom = 0;
                    int positionMode = EstimatePositionMode(nowDoor,ref freedom);
                    if (positionMode == 0)
                    {
                        double offset = 0;
                        //Point3d leftPoint = ptList[(nowDoor.UpLineIndex-1 + ptList.Count)%ptList.Count];\
                        Point3d leftPoint = ptList[nowDoor.UpLineIndex];
                        Vector3d leftDis = leftPoint - nowDoor.UpFirst;
                        if (leftDis.Length < Parameter.SuggestDistanceRoom)  //如果过于靠近墙面且有相邻出口，则考虑内缩。
                        {
                            offset = leftAdjacentPipeNum * 2 * nowDoor.UpstreamRegion.SuggestDist;
                        }
                        FixPoint(nowDoor, 0, offset,freedom);
                    }
                    else if (positionMode == 1)
                    {
                        double offset = 0;
                        Point3d rightPoint = ptList[(nowDoor.UpLineIndex + 1) % ptList.Count];
                        Vector3d rightDis = rightPoint - nowDoor.UpSecond;
                        if (rightDis.Length < Parameter.SuggestDistanceRoom)
                        {
                            offset = rightAdjacentPipeNum * 2 * nowDoor.UpstreamRegion.SuggestDist;
                        }
                        FixPoint(nowDoor, 1, offset,freedom);
                    }
                }
            }
        }

        public void FindPointNew()
        {
            for (int i = 0; i < RegionList.Count; i++)
            {
                SingleRegion upRegion = RegionList[i];
                List<SingleRegion> childRegionList = RegionList[i].ChildRegion;
                if (childRegionList.Count == 0) continue;


                //准备
                Polyline UpPl = upRegion.ClearedPl;
                List<Point3d> ptList = UpPl.GetPoints().ToList();


                for (int a = 0;a < childRegionList.Count;a++) 
                {
                   
                    SingleRegion downRegion = childRegionList[a];
                    SingleDoor nowDoor = upRegion.ExportMap[downRegion];
                    if (nowDoor.PipeIdList.Count == 0) continue;
                    Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
                    Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;
                    
                    int tend = UpperTopologicalTendency(nowDoor);

                    if (nowDoor.DoorId == 2)
                    {
                        int stop = 0;
                    }

                    //固定均匀分布
                    if (upDoor.Length < nowDoor.DownstreamRegion.SuggestDist * (nowDoor.PipeIdList.Count * 2 - 1) + 2 * Parameter.SuggestDistanceWall)
                    {
                        DoorPointTypeMap.Add(nowDoor.DoorId, new DoorPoinType(0,-1,0, 0));

                        int freedom = 0;

                        double tmpDis = upDoor.Length / (nowDoor.PipeIdList.Count * 2 + 1);
                        //tmpDis = ((int)tmpDis / 20) * 20;
                        double tmpX = 0;
                        for (int j = 0; j < nowDoor.PipeIdList.Count; j++)
                        {
                            int pipeId = nowDoor.PipeIdList[j];

                            tmpX += tmpDis;
                            Point3d pt1 = nowDoor.UpFirst + tmpX * upDoor.GetNormal();
                            Point3d pt3 = nowDoor.DownSecond + tmpX * downDoor.GetNormal();
                            tmpX += tmpDis;
                            Point3d pt2 = nowDoor.UpFirst + tmpX * upDoor.GetNormal();
                            Point3d pt4 = nowDoor.DownSecond + tmpX * downDoor.GetNormal();

                            List<Point3d> point3Ds = new List<Point3d>();
                            point3Ds.Add(pt1);
                            point3Ds.Add(pt2);
                            point3Ds.Add(pt3);
                            point3Ds.Add(pt4);
                            PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                            DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), pipePoint);
                        }
                    }
                    else  
                    {
                        if (nowDoor.PipeIdList.Count == 1)   
                        {
                            int freedom = 1;
                            int index = -1;
                            int positionMode = EstimatePositionSinglePipe(nowDoor, ref freedom);
                            if (positionMode == 0) index = 0;
                            else if (positionMode == 1) index = -1;

                            FixPointNew(nowDoor, positionMode, index, 0, freedom);
                            
                            double ld = (nowDoor.UpFirst - DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId,nowDoor.PipeIdList[0])].PointList[0]).Length;
                            double rd = (nowDoor.UpSecond - DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId, nowDoor.PipeIdList[0])].PointList[1]).Length;
                            
                            DoorPointTypeMap.Add(nowDoor.DoorId, new DoorPoinType(1, index ,ld, rd));
                        }
                        else
                        {
                            //int freedom = GetFreedom(nowDoor);
                            int freedom = GetFreedom(nowDoor);
                            int left = -1;
                            for (int b = 0; b < nowDoor.PipeIdList.Count; b++)
                            {
                                int isRight = 0; 
                                int pipeId = nowDoor.PipeIdList[b];
                                bool isMainPipe = (pipeId == downRegion.MainPipe[0]);

                                List<SingleRegion> downChildRegionList = downRegion.ChildRegion;

                                //寻找最近的出口
                                //哪个出口近就会从靠近哪个出口的那一侧出去，而不会绕一大圈
                                int downDownDoorId = -1;
                                double minDis = 10000000;
                                for (int j = 0; j < downChildRegionList.Count; j++)
                                {
                                    SingleDoor sd = downRegion.ExportMap[downChildRegionList[j]];
                                    if (sd.PipeIdList.Contains(pipeId))
                                    {
                                        double nowMinDis = Math.Min(DoorToDoorDistanceMap[sd.DoorId, nowDoor.DoorId].CCWDistance, DoorToDoorDistanceMap[sd.DoorId, nowDoor.DoorId].CWDistance);
                                        if (minDis > nowMinDis)
                                        {
                                            downDownDoorId = sd.DoorId;
                                            minDis = nowMinDis;
                                        }                                        
                                    }
                                }

                                //如果存在出口，有可能不是主导管线也有可能是
                                //一旦有主导管线，其他管线都得靠边站
                                if (downDownDoorId != -1)
                                {
                                    if (!isMainPipe)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        double cwDis = DoorToDoorDistanceMap[downDownDoorId, nowDoor.DoorId].CWDistance;
                                        double ccwDis = DoorToDoorDistanceMap[downDownDoorId, nowDoor.DoorId].CCWDistance;
                                        if (ccwDis > cwDis + 1000)
                                        {
                                            isRight = 0;
                                            left = b;
                                            break;
                                        }
                                        else if (cwDis > ccwDis + 1000)
                                        {
                                            isRight = 1;
                                            left = b - 1;
                                            break;
                                        }
                                        else
                                        {
                                            if (tend == 0)
                                            {
                                                left = b;
                                                break;
                                            }
                                            else
                                            {
                                                isRight = 1;
                                                left = b - 1;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else   //没有出口必然是主导管线
                                {
                                    if (tend == 0)
                                    {
                                        left = b;
                                        break;
                                    }
                                    else if (tend == 1) 
                                    {
                                        left = b - 1;
                                        break;
                                    }
                                }
                            }

                            //计算得到left的值
                            FixPointNew(nowDoor, 0, left , 0, freedom);
                            FixPointNew(nowDoor, 1, left,  0, freedom);

                            double ld = (nowDoor.UpFirst - DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId, nowDoor.PipeIdList[0])].PointList[0]).Length;
                            double rd = (nowDoor.UpSecond - DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId, nowDoor.PipeIdList.Last())].PointList[1]).Length;

                            DoorPointTypeMap.Add(nowDoor.DoorId, new DoorPoinType(2,left, ld, rd));
                        }
                    }
                }

                //进行 OffSet
                for (int a = 0; a < childRegionList.Count; a++)
                {
                    SingleRegion downRegion = childRegionList[a];
                    SingleDoor nowDoor = upRegion.ExportMap[downRegion];
                    if (nowDoor.PipeIdList.Count == 0) continue;
                    Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
                    Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;

                    if (nowDoor.DoorId == 2)
                    {
                        int stop = 0;
                    }

                    int leftAdjacentPipeNum = 0;
                    int rightAdjacentPipeNum = 0;
                    GetAdjacentPipeNumNew(nowDoor, ref leftAdjacentPipeNum, ref rightAdjacentPipeNum);
                    DoorLeftRightPipe.Add(nowDoor, new Tuple<int, int>(leftAdjacentPipeNum, rightAdjacentPipeNum));
                    DoorPoinType doorType = DoorPointTypeMap[nowDoor.DoorId];

                    double offsetLeft = 0;
                    double offsetRight = 0;
                    if (leftAdjacentPipeNum > 0 && doorType.Index > -1)  //需要缩进
                    {
                        if (doorType.Type != 0) 
                        {
                            offsetLeft = leftAdjacentPipeNum * 2 * nowDoor.UpstreamRegion.SuggestDist;
                        }
                    }

                    if (rightAdjacentPipeNum > 0 && doorType.Index < nowDoor.PipeIdList.Count-1)  //需要缩进
                    {
                        if (doorType.Type != 0)
                        {
                            offsetRight = rightAdjacentPipeNum * 2 * nowDoor.UpstreamRegion.SuggestDist;
                        }
                    }

                    if (offsetLeft > 0 || offsetRight > 0)
                    {
                        if (doorType.Type == 1) doorType.Type = 2;
                        if (offsetLeft > 0 && offsetRight > 0) 
                        {
                            int offsetOk = CheckOffsetReasonable2(nowDoor, offsetLeft,offsetRight);
                            if (offsetOk == 0)
                            {
                                FixPointAVG(nowDoor, 0, offsetLeft, offsetRight, doorType.Type);
                            }
                            else 
                            {
                                FixPointNew(nowDoor, 0, doorType.Index, offsetLeft, doorType.Type);
                                FixPointNew(nowDoor, 1, doorType.Index, offsetRight, doorType.Type);
                            }
                        }
                        else if (offsetLeft > 0)
                        {
                            int offsetOk = CheckOffsetReasonable(nowDoor, offsetLeft);
                            if (offsetOk == 0) 
                            {
                                offsetLeft = upDoor.Length * (leftAdjacentPipeNum * 2 + 1) / (leftAdjacentPipeNum * 2 + nowDoor.PipeIdList.Count * 2 + 1);
                                offsetRight = upDoor.Length / (leftAdjacentPipeNum * 2 + nowDoor.PipeIdList.Count * 2 + 1);
                                FixPointAVG(nowDoor, 0, offsetLeft, offsetRight , doorType.Type);
                                //FixPointNew(nowDoor, 1, - 1, 0, doorType.Type);
                            }
                            else
                            {
                                FixPointNew(nowDoor, 0, doorType.Index, offsetLeft, doorType.Type);
                            }
                        }
                        else if (offsetRight > 0)
                        {
                            //FixPointNew(nowDoor, 1, doorType.Index, offsetRight, doorType.Type);

                            int offsetOk = CheckOffsetReasonable(nowDoor, offsetRight);
                            if (offsetOk == 0)
                            {
                                offsetRight = upDoor.Length * (rightAdjacentPipeNum * 2 + 1) / (rightAdjacentPipeNum * 2 + nowDoor.PipeIdList.Count * 2 + 1);
                                offsetLeft =  upDoor.Length/ (rightAdjacentPipeNum * 2 + nowDoor.PipeIdList.Count * 2 + 1);
                                FixPointAVG(nowDoor, 1, offsetLeft, offsetRight, doorType.Type);
                                //FixPointNew(nowDoor, 0,nowDoor.PipeIdList.Count-1 , 0, doorType.Type);
                            }
                            else
                            {
                                FixPointNew(nowDoor, 1, doorType.Index, offsetRight, doorType.Type);
                            }
                        }
                    }
                }
            }
        }

        public int EstimatePositionMode(SingleDoor nowDoor,ref int freedom)
        {
            int pMode = 0;
            SingleRegion upRegion = nowDoor.UpstreamRegion;

            
            //第1优先级,根据拓扑关系决定靠哪边
            int tmpMode = EstimatePositionHelper1(nowDoor);
            if (tmpMode != 2)
            {
                freedom = 1;
                return tmpMode;
            }

            //第2优先级，判断是否靠墙
            int pMode1 = EstimatePositionHelper2(nowDoor);
            if (pMode1 != 2)
            {
                freedom = 0;
                return pMode1;
            }

            //第3优先级,根据管道的拓扑关系决定靠哪边
            freedom = 1;

            int totalPipeNum = upRegion.PassingPipeList.Count;
            int leftPipeNum = 0;
            for (int i = 0; i < nowDoor.LeftDoorNum; i++)
            {
                leftPipeNum += upRegion.ExportMap[upRegion.ChildRegion[i]].PipeIdList.Count;
            }
            int rightPipeNum = totalPipeNum - leftPipeNum - nowDoor.PipeIdList.Count;

            if (rightPipeNum < leftPipeNum) pMode = 1;

            return pMode;
        }

        public int EstimatePositionHelper1(SingleDoor nowDoor)
        {
            int pMode = 2;

            int leftNum = DoorLeftRightPipe[nowDoor].Item1;
            int rightNum = DoorLeftRightPipe[nowDoor].Item2;

            if (leftNum > 0 && rightNum == 0) return 1;
            if (leftNum == 0 && rightNum > 0) return 0;

            return 2;
        }

        public int EstimatePositionHelper2(SingleDoor nowDoor)
        {
            int pMode = 2;

            int doorLineIndex = nowDoor.DownLineIndex;
            Polyline clearedPl = nowDoor.DownstreamRegion.ClearedPl;
            Point3d ptf1 = clearedPl.GetPoint3dAt((doorLineIndex - 1 + clearedPl.NumberOfVertices) % clearedPl.NumberOfVertices);
            Point3d pt0 = clearedPl.GetPoint3dAt(doorLineIndex);
            Point3d pt1 = clearedPl.GetPoint3dAt((doorLineIndex + 1) % clearedPl.NumberOfVertices);
            Point3d pt2 = clearedPl.GetPoint3dAt((doorLineIndex + 2) % clearedPl.NumberOfVertices);

            Vector3d vec0 = pt0 - ptf1;
            Vector3d vec2 = pt2 - pt1;
            Vector3d dis0 = nowDoor.DownFirst - pt0;
            Vector3d dis1 = nowDoor.DownSecond - pt1;

            double maxLength = 0;
            int rightOk = 0;
            int leftOk = 0;

            //右侧
            if (dis0.Length < Parameter.SuggestDistanceWall && vec0.Length > Parameter.IsLongSide)
            {
                rightOk = 1;
                maxLength = vec0.Length;
                pMode = 1;
            }
            //左侧
            if (dis1.Length < Parameter.SuggestDistanceWall && vec2.Length > Parameter.IsLongSide)
            {
                leftOk = 1;
                if (vec2.Length > maxLength) 
                {
                    pMode = 0;
                }
            }

            //如果差不多长
            if (leftOk==1 && rightOk == 1 && Math.Abs(vec2.Length - vec0.Length) < 2000)
            {
                pMode = 2;
            }
            return pMode;
        }

        public void GetAdjacentPipeNum(SingleDoor nowDoor, ref int leftNum, ref int rightNum) 
        {
            SingleRegion upRegion = nowDoor.UpstreamRegion;

            int doorLineIndex = nowDoor.UpLineIndex;
            Polyline clearedPl = upRegion.ClearedPl;
            Point3d ptf1 = clearedPl.GetPoint3dAt((doorLineIndex - 1 + clearedPl.NumberOfVertices) % clearedPl.NumberOfVertices);
            Point3d pt0 = clearedPl.GetPoint3dAt(doorLineIndex);
            Point3d pt1 = clearedPl.GetPoint3dAt((doorLineIndex + 1) % clearedPl.NumberOfVertices);
            Point3d pt2 = clearedPl.GetPoint3dAt((doorLineIndex + 2) % clearedPl.NumberOfVertices);

            List<int> indexList = RegionToDoorLineIndex[upRegion.RegionId];
            if (indexList.Contains((doorLineIndex - 1) % clearedPl.NumberOfVertices)) 
            {
                foreach (SingleDoor sd in upRegion.ExportMap.Values.ToList()) 
                {
                    if(sd.UpLineIndex == (doorLineIndex - 1) % clearedPl.NumberOfVertices)
                    {
                        leftNum += sd.PipeIdList.Count();
                    }
                }
            }

            if (indexList.Contains((doorLineIndex + 1) % clearedPl.NumberOfVertices))
            {
                foreach (SingleDoor sd in upRegion.ExportMap.Values.ToList())
                {
                    if (sd.UpLineIndex == (doorLineIndex + 1) % clearedPl.NumberOfVertices)
                    {
                        rightNum += sd.PipeIdList.Count();
                    }
                }
            }
        }

        public void FixPoint(SingleDoor nowDoor, int mode, double offset ,int freedom)
        {
            int offsetOk = CheckOffsetReasonable(nowDoor, offset);
            if (offsetOk == 0) offset = 0;

            Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
            Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;

            double tmpX = Parameter.SuggestDistanceWall + offset;
            double tmpDis = nowDoor.DownstreamRegion.SuggestDist;

            Point3d upStart = new Point3d(), downStart = new Point3d();
            Vector3d dir = new Vector3d();
            
            if (mode == 0)
            {
                upStart = nowDoor.UpFirst;
                downStart = nowDoor.DownSecond;
                dir = upDoor.GetNormal();
            }
            else if (mode == 1)
            {
                upStart = nowDoor.UpSecond;
                downStart = nowDoor.DownFirst;
                dir = -upDoor.GetNormal();
            }

            for (int j = 0; j < nowDoor.PipeIdList.Count; j++)
            {
                int pipeIndex = j;
                if (mode == 1) pipeIndex = nowDoor.PipeIdList.Count - 1 - j;
                int pipeId = nowDoor.PipeIdList[pipeIndex];

                if (j != 0)
                {
                    tmpX += tmpDis;
                }

                Point3d pt1 = upStart + tmpX * dir;
                Point3d pt3 = downStart + tmpX * dir;
                tmpX += tmpDis;
                Point3d pt2 = upStart + tmpX * dir;
                Point3d pt4 = downStart + tmpX * dir;

                List<Point3d> point3Ds = new List<Point3d>();
                point3Ds.Add(pt1);
                point3Ds.Add(pt2);
                point3Ds.Add(pt3);
                point3Ds.Add(pt4);

                PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), pipePoint);
            }
        }

        public int CheckOffsetReasonable(SingleDoor nowDoor, double offset) 
        {
            Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
            if (upDoor.Length < offset + Parameter.SuggestDistanceWall * 2
                + (nowDoor.PipeIdList.Count * 2 - 1) * nowDoor.DownstreamRegion.SuggestDist) 
            {
                return 0;
            }
            return 1;
        }
        public int CheckOffsetReasonable2(SingleDoor nowDoor, double offsetLeft,double offsetRight)
        {
            Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
            if (upDoor.Length < offsetLeft + offsetRight + Parameter.SuggestDistanceWall * 2
                + (nowDoor.PipeIdList.Count * 2 - 1) * nowDoor.DownstreamRegion.SuggestDist)
            {
                return 0;
            }
            return 1;
        }

        public void FixPointAVG(SingleDoor nowDoor, int mode, double offsetLeft , double offsetRight, int freedom) 
        {
            Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
            Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;

            double tmpX = 0;
            double tmpDis = (upDoor.Length - offsetLeft -offsetRight)/ (nowDoor.PipeIdList.Count * 2-1);

            Point3d upStart = new Point3d(), downStart = new Point3d();
            Vector3d dir = new Vector3d();

            int pipeNum = nowDoor.PipeIdList.Count;
            if (mode == 0)
            {
                upStart = nowDoor.UpFirst;
                downStart = nowDoor.DownSecond;
                dir = upDoor.GetNormal();
                tmpX = offsetLeft;
            }
            else if (mode == 1)
            {
                upStart = nowDoor.UpSecond;
                downStart = nowDoor.DownFirst;
                dir = -upDoor.GetNormal();
                tmpX = offsetRight;
            }

            for (int j = 0; j < pipeNum; j++)
            {
                int pipeIndex = j;
                if (mode == 1) pipeIndex = nowDoor.PipeIdList.Count - 1 - j;
                int pipeId = nowDoor.PipeIdList[pipeIndex];

                if (j != 0)
                {
                    tmpX += tmpDis;
                }

                Point3d pt1 = upStart + tmpX * dir;
                Point3d pt3 = downStart + tmpX * dir;
                tmpX += tmpDis;
                Point3d pt2 = upStart + tmpX * dir;
                Point3d pt4 = downStart + tmpX * dir;

                List<Point3d> point3Ds = new List<Point3d>();
                if (mode == 0)
                {
                    point3Ds.Add(pt1);
                    point3Ds.Add(pt2);
                    point3Ds.Add(pt3);
                    point3Ds.Add(pt4);
                }
                else if (mode == 1)
                {
                    point3Ds.Add(pt2);
                    point3Ds.Add(pt1);
                    point3Ds.Add(pt4);
                    point3Ds.Add(pt3);
                }

                PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                if (DoorPipeToPointMap.ContainsKey(new Tuple<int, int>(nowDoor.DoorId, pipeId)))
                {
                    DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId, pipeId)] = pipePoint;
                }
                else
                {
                    DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), pipePoint);
                }
            }
        }

                ///////////////////////////
        //新的找点策略
        public int EstimatePositionSinglePipe(SingleDoor nowDoor, ref int freedom)
        {
            int pMode = 0;
            SingleRegion upRegion = nowDoor.UpstreamRegion;

            //第2优先级，判断是否靠墙
            int pMode1 = EstimatePositionHelper2(nowDoor);
            if (pMode1 != 2)
            {
                freedom = 1;
                return pMode1;
            }

            //第3优先级,根据管道的拓扑关系决定靠哪边
            freedom = 2;
            pMode = 0;

            pMode = UpperTopologicalTendency(nowDoor);

            return pMode;
        }

        public int EstimatePositionHelperNew(SingleDoor nowDoor)
        {
            int pMode = 2;

            int doorLineIndex = nowDoor.DownLineIndex;
            Polyline clearedPl = nowDoor.DownstreamRegion.ClearedPl;
            Point3d ptf1 = clearedPl.GetPoint3dAt((doorLineIndex - 1 + clearedPl.NumberOfVertices) % clearedPl.NumberOfVertices);
            Point3d pt0 = clearedPl.GetPoint3dAt(doorLineIndex);
            Point3d pt1 = clearedPl.GetPoint3dAt((doorLineIndex + 1) % clearedPl.NumberOfVertices);
            Point3d pt2 = clearedPl.GetPoint3dAt((doorLineIndex + 2) % clearedPl.NumberOfVertices);

            Vector3d vec0 = pt0 - ptf1;
            Vector3d vec2 = pt2 - pt1;
            Vector3d dis0 = nowDoor.DownFirst - pt0;
            Vector3d dis1 = nowDoor.DownSecond - pt1;

            double maxLength = 0;
            int rightOk = 0;
            int leftOk = 0;

            //右侧
            if (dis0.Length < Parameter.SuggestDistanceWall && vec0.Length > Parameter.IsLongSide)
            {
                rightOk = 1;
                maxLength = vec0.Length;
                pMode = 1;
            }
            //左侧
            if (dis1.Length < Parameter.SuggestDistanceWall && vec2.Length > Parameter.IsLongSide)
            {
                leftOk = 1;
                if (vec2.Length > maxLength)
                {
                    pMode = 0;
                }
            }

            //如果差不多长
            if (leftOk == 1 && rightOk == 1 && Math.Abs(vec2.Length - vec0.Length) < 1000)
            {
                SingleDoor upDoor = nowDoor.UpstreamRegion.MainEntrance;
                double cwDis  = DoorToDoorDistanceMap[nowDoor.DoorId, upDoor.DoorId].CCWDistance;
                double ccwDis = DoorToDoorDistanceMap[nowDoor.DoorId, upDoor.DoorId].CCWDistance;

                if (ccwDis > cwDis + 1000)
                {
                    pMode = 0;
                }
                else if (cwDis > ccwDis + 1000)
                {
                    pMode = 1;
                }
                else pMode = 2;

            }
            return pMode;
        }

        public int UpperTopologicalTendency(SingleDoor nowDoor) 
        {
            int pMode = 0;

            SingleRegion upRegion = nowDoor.UpstreamRegion;

            int totalPipeNum = upRegion.PassingPipeList.Count;
            int leftPipeNum = 0;
            for (int i = 0; i < nowDoor.LeftDoorNum; i++)
            {
                leftPipeNum += upRegion.ExportMap[upRegion.ChildRegion[i]].PipeIdList.Count;
            }
            int rightPipeNum = totalPipeNum - leftPipeNum - nowDoor.PipeIdList.Count;

            if (rightPipeNum < leftPipeNum) pMode = 1;

            return pMode;
        }

        public void GetAdjacentPipeNumNew(SingleDoor nowDoor, ref int leftNum, ref int rightNum)
        {
            SingleRegion upRegion = nowDoor.UpstreamRegion;

            int doorLineIndex = nowDoor.UpLineIndex;
            Polyline clearedPl = upRegion.ClearedPl;
            Point3d ptf1 = clearedPl.GetPoint3dAt((doorLineIndex - 1 + clearedPl.NumberOfVertices) % clearedPl.NumberOfVertices);
            Point3d pt0 = clearedPl.GetPoint3dAt(doorLineIndex);
            Point3d pt1 = clearedPl.GetPoint3dAt((doorLineIndex + 1) % clearedPl.NumberOfVertices);
            Point3d pt2 = clearedPl.GetPoint3dAt((doorLineIndex + 2) % clearedPl.NumberOfVertices);

            List<int> indexList = RegionToDoorLineIndex[upRegion.RegionId];
            
            //顺时针方向有错误
            if (indexList.Contains((doorLineIndex + clearedPl.NumberOfVertices - 1) % clearedPl.NumberOfVertices))
            {

                foreach (SingleDoor sd in upRegion.ExportMap.Values.ToList())
                {
                    if (sd.UpLineIndex == (doorLineIndex + clearedPl.NumberOfVertices - 1) % clearedPl.NumberOfVertices && sd.PipeIdList.Count > 0)
                    {
                        //如果是同一根管道则偏移减1
                        int reduce = 0;
                        if (nowDoor.PipeIdList.Count > 0 && sd.PipeIdList.Contains(nowDoor.PipeIdList.First()))
                            reduce = 1;

                        double rd = DoorPointTypeMap[sd.DoorId].RightDis;
                        double totalLength = rd + (pt0 - sd.UpSecond).Length;

                        double nowDoorLeft = (nowDoor.UpFirst - pt0).Length; 
                        if (totalLength < 700 && nowDoorLeft < 500)
                        {
                            leftNum += sd.PipeIdList.Count()- reduce;
                        }
                    }
                }
            }

            //逆时针方向有门
            if (indexList.Contains((doorLineIndex + 1) % clearedPl.NumberOfVertices))
            {
                foreach (SingleDoor sd in upRegion.ExportMap.Values.ToList())
                {
                    if (sd.UpLineIndex == (doorLineIndex + 1) % clearedPl.NumberOfVertices && sd.PipeIdList.Count > 0)
                    {
                        //如果是同一根管道则偏移减1
                        int reduce = 0;
                        if (nowDoor.PipeIdList.Count > 0 && sd.PipeIdList.Contains(nowDoor.PipeIdList.Last())) 
                            reduce = 1;

                        double ld = DoorPointTypeMap[sd.DoorId].LeftDis;
                        double totalLength = ld + (pt1 - sd.UpFirst).Length;

                        double nowDoorRight = (nowDoor.UpSecond - pt1).Length;
                        if (totalLength < 700 && nowDoorRight < 500)
                        {
                            rightNum += sd.PipeIdList.Count() - reduce;
                        }
                    }
                }
            }
        }

        public void FixPointNew(SingleDoor nowDoor, int mode, int index, double offset, int freedom)
        {
            int offsetOk = CheckOffsetReasonable(nowDoor, offset);
            if (offsetOk == 0) offset = 0;

            Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
            Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;

            double tmpX = Parameter.SuggestDistanceWall + offset;
            double tmpDis = nowDoor.DownstreamRegion.SuggestDist;

            Point3d upStart = new Point3d(), downStart = new Point3d();
            Vector3d dir = new Vector3d();

            int pipeNum = 0;
            if (mode == 0)
            {
                upStart = nowDoor.UpFirst;
                downStart = nowDoor.DownSecond;
                dir = upDoor.GetNormal();
                pipeNum = index + 1;
            }
            else if (mode == 1)
            {
                upStart = nowDoor.UpSecond;
                downStart = nowDoor.DownFirst;
                dir = -upDoor.GetNormal();
                pipeNum = nowDoor.PipeIdList.Count - index-1;
            }

            for (int j = 0; j <pipeNum; j++)
            {
                int pipeIndex = j;
                if (mode == 1) pipeIndex = nowDoor.PipeIdList.Count - 1 - j;
                int pipeId = nowDoor.PipeIdList[pipeIndex];

                if (j != 0)
                {
                    tmpX += tmpDis;
                }

                Point3d pt1 = upStart + tmpX * dir;
                Point3d pt3 = downStart + tmpX * dir;
                tmpX += tmpDis;
                Point3d pt2 = upStart + tmpX * dir;
                Point3d pt4 = downStart + tmpX * dir;

                List<Point3d> point3Ds = new List<Point3d>();
                if (mode == 0)
                {
                    point3Ds.Add(pt1);
                    point3Ds.Add(pt2);
                    point3Ds.Add(pt3);
                    point3Ds.Add(pt4);
                }
                else if (mode == 1) 
                {
                    point3Ds.Add(pt2);
                    point3Ds.Add(pt1);
                    point3Ds.Add(pt4);
                    point3Ds.Add(pt3);
                }

                PipePoint pipePoint = new PipePoint(nowDoor, SinglePipeList[pipeId], freedom, point3Ds);
                if (DoorPipeToPointMap.ContainsKey(new Tuple<int, int>(nowDoor.DoorId, pipeId)))
                {
                    DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId, pipeId)] = pipePoint;
                }
                else 
                {
                    DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), pipePoint);
                }  
            }
        }

        public int GetFreedom(SingleDoor nowDoor) 
        {
            return 2;
        }

        public void CheckFreedomAgain() 
        {
            for (int i = 1; i < DoorList.Count; i++)
            {

                if (i == 7) 
                {
                    int stop = 0;
                }
                if (DoorPointTypeMap.ContainsKey(i) && DoorPointTypeMap[i].Type == 2) 
                {
                    SingleDoor nowDoor = DoorList[i];

                    if (nowDoor.UpstreamRegion.SuggestDist != nowDoor.DownstreamRegion.SuggestDist && nowDoor.PipeIdList.Count > 2)
                    {
                        Vector3d doorLine = nowDoor.UpFirst - nowDoor.UpSecond;
                        if (doorLine.Length < (nowDoor.PipeIdList.Count * 2 + 1) * nowDoor.UpstreamRegion.SuggestDist)
                        {
                            for (int j = 0; j < nowDoor.PipeIdList.Count; j++) {
                                int pipeId = nowDoor.PipeIdList[j];
                                if (DoorPipeToPointMap.ContainsKey(new Tuple<int, int>(i, pipeId))) 
                                {
                                    DoorPipeToPointMap[new Tuple<int, int>(i, pipeId)].FreeDegree = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SaveResult() 
        {
            ProcessedData.DoorPipeToPointMap = DoorPipeToPointMap;
        }

        public void Draw()
        {
            List<Point3d> point3Ds = new List<Point3d>();
            DoorPipeToPointMap.Values.ToList().ForEach(x => point3Ds.AddRange(x.PointList));
            //uShowGeometry(Point3d pt, string LayerName, int colorIndex = 3, int lineWeightNum = 25, int r = 200, string symbol = "C")
            //var newPoint3dList = point3Ds.SelectMany(i => i).ToList();
            var newPoint3dList = point3Ds;
            newPoint3dList.ForEach(x => DrawUtils.ShowGeometry(x, "l1Points", 5, lineWeightNum: 30, 30, "C"));


            //其他输出
            for (int i = 0; i < RegionList.Count; i++) 
            {
                DrawUtils.ShowGeometry(RegionList[i].ClearedPl, "l9ObbInput", 0, lineWeightNum: 30);
            }
        }
    }


    class DoorPoinType 
    {
        public int Type = -1;         //0：均匀绝对不能动，00：进行过Offset，尽量不能动  ，1：推荐，可移动 ，2：自由
        public int Index = -1;        //放在左侧的管线数量  全右是 -1  全左是PipeList.count-1 
        public double LeftDis = 0;
        public double RightDis = 0;

        public DoorPoinType(int type, int index ,double ld, double rd) 
        {
            Type = type;
            LeftDis = ld;
            RightDis = rd;
            Index = index;
        }
    }

    class PipePoint
    {
        public SingleDoor NowDoor;
        public SinglePipe NowPipe;

        public double PipeWidth;

        public int FreeDegree = 2;

        //点 ： 上左，上右，下左，下右
        //public Point3d upFirst;
        //public Point3d upSecond;
        //public Point3d DownSecond;
        //public Point3d DownFirst;

        public List<Point3d> PointList = new List<Point3d>();

        public PipePoint(SingleDoor nowDoor, SinglePipe nowPipe, int freeDegree, List<Point3d> pointList) 
        {
            this.NowDoor = nowDoor;
            this.NowPipe = nowPipe;
            this.FreeDegree = freeDegree;
            this.PointList = pointList;
        }
    }



}
