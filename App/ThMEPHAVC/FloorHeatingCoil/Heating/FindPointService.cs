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
        public Dictionary<Tuple<int, int>, List<Point3d>> DoorPipeToPointMap = new Dictionary<Tuple<int, int>, List<Point3d>>();
        public Dictionary<int, List<int>> RegionToDoorLineIndex = new Dictionary<int, List<int>>();

        //内部变量
        List<int> IfSet;
        public FindPointService()
        {
            //Pipeline();
        }

        public void Pipeline()
        {
            GetDoorLineIndex();

            FindPoint();

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

        public void FindPoint()
        {
            IfSet = new List<int>(new int[DoorList.Count]);
            IfSet.ForEach(x => x = 0);

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

                if (i == 6)
                {
                    int stop = 0;
                }

                if (upDoor.Length < Parameter.SuggestDistanceRoom * (nowDoor.PipeIdList.Count * 2 - 1) + 2 * Parameter.SuggestDistanceWall)
                {
                    double tmpDis = upDoor.Length / (nowDoor.PipeIdList.Count * 2 + 1);
                    tmpDis = ((int)tmpDis / 20) * 20;
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
                        DoorPipeToPointMap.Add(new Tuple<int, int>(i, pipeId), point3Ds);
                    }
                }
                else
                {
                    int positionMode = EstimatePositionMode(nowDoor);
                    if (positionMode == 0)
                    {
                        double offset = 0;
                        Point3d leftPoint = ptList[(nowDoor.UpLineIndex-1 + ptList.Count)%ptList.Count];
                        Vector3d leftDis = leftPoint - nowDoor.UpFirst;
                        if (leftDis.Length < Parameter.SuggestDistanceWall * Parameter.IgnoreWall) 
                        {
                            offset = leftAdjacentPipeNum * 2 * nowDoor.UpstreamRegion.SuggestDist;
                        }
                        FixPoint(nowDoor, 0, offset);
                    }
                    else if (positionMode == 1)
                    {
                        double offset = 0;
                        Point3d rightPoint = ptList[(nowDoor.UpLineIndex + 1) % ptList.Count];
                        Vector3d rightDis = rightPoint - nowDoor.UpSecond;
                        if (rightDis.Length < Parameter.SuggestDistanceWall * Parameter.IgnoreWall)
                        {
                            offset = rightAdjacentPipeNum * 2 * nowDoor.UpstreamRegion.SuggestDist;
                        }
                        FixPoint(nowDoor, 1, offset);
                    }
                }
            }
        }

        public int EstimatePositionMode(SingleDoor nowDoor)
        {
            int pMode = 0;
            SingleRegion upRegion = nowDoor.UpstreamRegion;

            //第一优先级，判断是否靠墙
            int pMode1 = EstimatePositionHelper1(nowDoor);
            if (pMode1 != 2)
            {
                return pMode1;
            }

            //第二优先级
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

        public void FixPoint(SingleDoor nowDoor, int mode, double offset)
        {
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
                int pipeId = nowDoor.PipeIdList[j];

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
                DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), point3Ds);
            }
        }

        public void FixPointAVG(SingleDoor nowDoor, int mode, double offset) 
        {
            Vector3d upDoor = nowDoor.UpSecond - nowDoor.UpFirst;
            Vector3d downDoor = nowDoor.DownFirst - nowDoor.DownSecond;

            double tmpX = offset;
            double tmpDis = (upDoor.Length - offset - Parameter.SuggestDistanceWall) /(nowDoor.PipeIdList.Count*2);
            tmpDis = ((int)tmpDis / 20) * 20;

            Point3d upStart = new Point3d(), downStart = new Point3d();
            Vector3d dir = new Vector3d();

            if (mode == 1)
            {
                upStart = nowDoor.UpFirst;
                downStart = nowDoor.DownSecond;
                dir = upDoor.GetNormal();
            }

            for (int j = 0; j < nowDoor.PipeIdList.Count; j++)
            {
                int pipeId = nowDoor.PipeIdList[j];

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
                //DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), point3Ds);
                if (DoorPipeToPointMap.ContainsKey(new Tuple<int, int>(nowDoor.DoorId, pipeId)))
                {
                    DoorPipeToPointMap[new Tuple<int, int>(nowDoor.DoorId, pipeId)] = point3Ds;
                }
                else 
                {
                    DoorPipeToPointMap.Add(new Tuple<int, int>(nowDoor.DoorId, pipeId), point3Ds);
                }
            }
        }

        public void SpecialSet1(SingleDoor sd,ref int ifSet) 
        {
            Vector3d doorLineDir0 = sd.DownSecond - sd.DownFirst;
            Line sdLine = new Line(sd.DownFirst, sd.DownSecond);

            SingleRegion upRegion = sd.UpstreamRegion;
            foreach (SingleDoor export in upRegion.ExportMap.Values.ToList())
            {
                Vector3d doorLineDir1 = export.UpSecond - export.UpFirst;
                Line exportLine = new Line(export.UpFirst, export.UpSecond);
                Line newEntrance = new Line();
                if (doorLineDir0.GetNormal() == -doorLineDir1.GetNormal()) ;
                {
                    //Reset
                    newEntrance = new Line();
                    //获取双方重合的Line
                    Polyline test1 = PolylineProcessService.CreateRectangle2(sd.DownFirst, sd.DownSecond, 20000);
                    if (test1.Contains(exportLine)) 
                    {
                       Polyline test2 = PolylineProcessService.CreateRectangle2(export.UpFirst, export.UpSecond, 20000);
                       newEntrance = test2.Trim(sdLine).OfType<Line>().ToList().FindByMax(x => x.Length);
                    }
                    double entranceOffset = (newEntrance.StartPoint - sd.DownFirst).Length; 


                    int totalPipeNum = upRegion.PassingPipeList.Count;
                    int leftPipeNum = 0;
                    for (int i = 0; i < export.LeftDoorNum; i++)
                    {
                        leftPipeNum += upRegion.ExportMap[upRegion.ChildRegion[i]].PipeIdList.Count;
                    }
                    int rightPipeNum = totalPipeNum - leftPipeNum - export.PipeIdList.Count;

                    int ifAdjust = 0;
                    for (int j = 0; j < export.PipeIdList.Count(); j++) 
                    {
                        int pipeId = export.PipeIdList[j];
                        List<Point3d> pts = DoorPipeToPointMap[new Tuple<int, int>(sd.DoorId, pipeId)];
                        //Vector3d vec0 = pts[0] - sd.DownFirst;
                        //Vector3d vec1 = pts[1] - sd.DownFirst;
                        if (!pts[0].IsPointOnLine(newEntrance) || !pts[1].IsPointOnLine(newEntrance)) 
                        {
                            ifAdjust = 1;
                            break;
                        }
                    }

                    if (ifAdjust == 1)
                    {
                        if (leftPipeNum > rightPipeNum && rightPipeNum == 0) 
                        {
                            double avgDis = (sdLine.Length - entranceOffset - Parameter.SuggestDistanceWall) / (sd.PipeIdList.Count * 2);
                            if (avgDis < sd.DownstreamRegion.SuggestDist) 
                            {
                                FixPointAVG(sd, 1, entranceOffset);

                                for (int j = 0; j < export.PipeIdList.Count(); j++)
                                {
                                    int pipeId = export.PipeIdList[j];
                                    List<Point3d> pts = DoorPipeToPointMap[new Tuple<int, int>(sd.DoorId, pipeId)];
                                    Vector3d vec0 = pts[0] - newEntrance.StartPoint;
                                    Vector3d vec1 = pts[1] - newEntrance.StartPoint;

                                    //Point3d pt1 = upStart + tmpX * dir;
                                    //Point3d pt3 = downStart + tmpX * dir;
                                    //tmpX += tmpDis;
                                    //Point3d pt2 = upStart + tmpX * dir;
                                    //Point3d pt4 = downStart + tmpX * dir;

                                    //List<Point3d> point3Ds = new List<Point3d>();
                                    //point3Ds.Add(pt1);
                                    //point3Ds.Add(pt2);
                                    //point3Ds.Add(pt3);
                                    //point3Ds.Add(pt4);
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
            List<List<Point3d>> point3Ds = DoorPipeToPointMap.Values.ToList();
            //uShowGeometry(Point3d pt, string LayerName, int colorIndex = 3, int lineWeightNum = 25, int r = 200, string symbol = "C")
            var newPoint3dList = point3Ds.SelectMany(i => i).ToList();
            newPoint3dList.ForEach(x => DrawUtils.ShowGeometry(x, "l1Points", 5, lineWeightNum: 30, 30, "C"));
        }
    }
}
