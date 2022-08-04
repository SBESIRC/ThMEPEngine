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
using ThMEPHVAC.FloorHeatingCoil;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class DrawPipe
    {
        public List<SingleRegion> RegionList = ProcessedData.RegionList;
        public List<SingleDoor> DoorList = ProcessedData.DoorList;
        public List<SinglePipe> SinglePipeList = ProcessedData.PipeList;
        public Dictionary<Tuple<int, int>, PipePoint> DoorPipeToPointMap = ProcessedData.DoorPipeToPointMap;
        public Dictionary<int, Dictionary<int, List<Polyline>>> PipePolyListMap = new Dictionary<int, Dictionary<int, List<Polyline>>>();
        public Dictionary<int, List<Polyline>> RegionPipePolyMap = new Dictionary<int, List<Polyline>>();
        public Dictionary<int, Polyline> PipeTotalPolyMap = new Dictionary<int, Polyline>();

        public DrawPipe()
        {

        }


        public void Pipeline()
        {
            DataInit();

            GetDrawnPipe();

            //GetConnector();

            //DrawWholePipe();

            //SaveResults();
        }

        public void DataInit()
        {
            for (int i = 0; i < SinglePipeList.Count; i++)
            {
                Dictionary<int, List<Polyline>> keyValuePairs = new Dictionary<int, List<Polyline>>();
                PipePolyListMap.Add(i, keyValuePairs);
            }
        }

        public void GetDrawnPipe()
        {
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (i == 1)
                {
                    int stop = 0;
                }

                SingleRegion nowRegion = RegionList[i];

                //修改
                if (nowRegion.ChildRegion.Count == 0 || nowRegion.PassingPipeList.Count == 1)
                {
                    double buffer_dis = -nowRegion.SuggestDist;

                    List<DrawPipeData> pipeInList = new List<DrawPipeData>();

                    int doorId = nowRegion.MainEntrance.DoorId;
                    for (int j = 0; j < nowRegion.MainEntrance.PipeIdList.Count; j++)
                    {
                        int pipeId = nowRegion.MainEntrance.PipeIdList[j];
                        PipePoint nowPipePoint = DoorPipeToPointMap[new Tuple<int, int>(doorId, pipeId)];

                        DrawPipeData drawPipeData0 = new DrawPipeData(nowPipePoint.PointList[2], nowPipePoint.PointList[3], nowPipePoint.NowDoor.DownSecond, nowPipePoint.NowDoor.DownFirst, nowPipePoint.FreeDegree, pipeId, doorId);
                        pipeInList.Add(drawPipeData0);

                        Vector3d vec0 = nowPipePoint.PointList[1] - nowPipePoint.PointList[0];
                        Point3d circleCenter = nowPipePoint.PointList[0] + vec0 / 2;
                        double radius = vec0.Length / 2;

                        DrawUtils.ShowGeometry(circleCenter, "l1Input1", 170, lineWeightNum: 30, (int)radius, "C");
                    }

                    Line drawLine = new Line(pipeInList[0].DoorLeft, pipeInList[0].DoorRight);
                    DrawUtils.ShowGeometry(drawLine, "l1Input1Line", 200, lineWeightNum: 30);

                    //////if (i == 16)
                    //////{
                    //////    DrawUtils.ShowGeometry(nowRegion.ClearedPl, "l1testPl", 10, 30);
                    //////    DrawUtils.ShowGeometry(circleCenter, "l1testPoints", 5, lineWeightNum: 30, (int)radius, "C");
                    //////}

                    //// calculate pipeline

                    RoomPipeGenerator roomPipeGenerator = new RoomPipeGenerator(nowRegion.ClearedPl, pipeInList, Parameter.SuggestDistanceWall);
                    roomPipeGenerator.CalculatePipeline();
                    // show result
                    var show = roomPipeGenerator.skeleton;
                    show.ForEach(x => DrawUtils.ShowGeometry(x, "l1RoomPipe", pipeInList[0].PipeId % 7 + 1, 30));

                    ////////////////////
                    int newPipeId = pipeInList[0].PipeId;
                    var regionPolyMap = PipePolyListMap[newPipeId];

                    List<Polyline> newList = new List<Polyline>();
                    newList.Add(roomPipeGenerator.skeleton[0]);
                    regionPolyMap.Add(i, newList);
                }
                else
                {
                    double buffer_dis = -nowRegion.SuggestDist;
                    List<Point3d> pins = new List<Point3d>();
                    List<double> pins_buffer = new List<double>();
                    List<Point3d> pouts = new List<Point3d>();
                    List<double> pouts_buffer = new List<double>();
                    int main_index = -1;
                    int mainPipeId = -1;
                    ////pipe in
                    List<DrawPipeData> pipeInList = new List<DrawPipeData>();

                    int updoorId = nowRegion.MainEntrance.DoorId;
                    for (int j = 0; j < nowRegion.MainEntrance.PipeIdList.Count; j++)
                    {
                        int pipeId = nowRegion.MainEntrance.PipeIdList[j];

                        PipePoint nowPipePoint = DoorPipeToPointMap[new Tuple<int, int>(updoorId, pipeId)];
                        DrawPipeData drawPipeData0 = new DrawPipeData(nowPipePoint.PointList[2], nowPipePoint.PointList[3], nowPipePoint.NowDoor.DownSecond, nowPipePoint.NowDoor.DownFirst, nowPipePoint.FreeDegree, pipeId, updoorId);
                        pipeInList.Add(drawPipeData0);

                        Vector3d vec0 = nowPipePoint.PointList[3] - nowPipePoint.PointList[2];
                        Point3d circleCenter = nowPipePoint.PointList[2] + vec0 / 2;
                        double radius = vec0.Length / 2;
                        //pins.Add(circleCenter);
                        //pins_buffer.Add(radius);

                        if (pipeId == nowRegion.MainPipe[0])
                        {
                            main_index = j;
                            mainPipeId = pipeId;
                        }
                    }

                    ////pipe out
                    List<DrawPipeData> pipeOutList = new List<DrawPipeData>();

                    List<int> downDoorIdList = new List<int>();
                    foreach (var child in nowRegion.ChildRegion)
                    {
                        downDoorIdList.Add(nowRegion.ExportMap[child].DoorId);
                    }
                    List<int> newDownDoorIdListMin = downDoorIdList.OrderByDescending(x => Math.Min(DoorList[x].CCWDistance, DoorList[x].CWDistance)).ToList();
                    List<int> newDownDoorIdListCCW = downDoorIdList.OrderByDescending(x => DoorList[x].CCWDistance).ToList();
                    List<int> newDownDoorIdListCW = downDoorIdList.OrderByDescending(x => DoorList[x].CWDistance).ToList();

                    List<int> PipeHash = new List<int>(new int[SinglePipeList.Count]);
                    for (int a = 0; a < PipeHash.Count; a++)
                    {
                        PipeHash[a] = -1;
                    }

                    Dictionary<int, List<int>> doorToDrawPipesMap = new Dictionary<int, List<int>>();
                    Dictionary<int, List<int>> doorToNotDrawPipesMap = new Dictionary<int, List<int>>();

                    for (int j = 0; j < nowRegion.MainEntrance.PipeIdList.Count; j++)
                    {
                        List<int> drawPipes = new List<int>();
                        int pipeId = nowRegion.MainEntrance.PipeIdList[j];
                        if (pipeId < mainPipeId)
                        {
                            for (int a = 0; a < newDownDoorIdListCW.Count; a++)
                            {
                                int thisDoorId = newDownDoorIdListCW[a];
                                if (DoorList[thisDoorId].PipeIdList.Contains(pipeId))
                                {
                                    PipeHash[pipeId] = thisDoorId;
                                    break;
                                }
                            }
                        }
                        else if (pipeId > mainPipeId)
                        {
                            for (int a = 0; a < newDownDoorIdListCCW.Count; a++)
                            {
                                int thisDoorId = newDownDoorIdListCCW[a];
                                if (DoorList[thisDoorId].PipeIdList.Contains(pipeId))
                                {
                                    PipeHash[pipeId] = thisDoorId;
                                    break;
                                }
                            }
                        }
                        else if (pipeId == mainPipeId)
                        {
                            for (int a = 0; a < newDownDoorIdListMin.Count; a++)
                            {
                                int thisDoorId = newDownDoorIdListMin[a];
                                if (DoorList[thisDoorId].PipeIdList.Contains(pipeId))
                                {
                                    PipeHash[pipeId] = thisDoorId;
                                    break;
                                }
                            }
                        }
                    }

                    for (int j = 0; j < downDoorIdList.Count; j++)
                    {
                        int thisDoorId = downDoorIdList[j];
                        List<int> drawPipes = new List<int>();
                        List<int> notDrawPipes = new List<int>();
                        for (int k = 0; k < DoorList[thisDoorId].PipeIdList.Count; k++)
                        {
                            if (PipeHash[DoorList[thisDoorId].PipeIdList[k]] == thisDoorId)
                            {
                                drawPipes.Add(DoorList[thisDoorId].PipeIdList[k]);
                            }
                            else
                            {
                                notDrawPipes.Add(DoorList[thisDoorId].PipeIdList[k]);
                            }
                        }
                        if (drawPipes.Count > 0)
                        {
                            doorToDrawPipesMap.Add(thisDoorId, drawPipes);
                        }
                        if (notDrawPipes.Count > 0)
                        {
                            doorToNotDrawPipesMap.Add(thisDoorId, notDrawPipes);
                        }
                    }

                    //用于查询pipe是否曾被占用
                    for (int j = 0; j < downDoorIdList.Count; j++)
                    {
                        int downDoorId = downDoorIdList[j];
                        if (!doorToDrawPipesMap.ContainsKey(downDoorId)) continue;

                        List<int> drawPipeList = doorToDrawPipesMap[downDoorId];

                        for (int k = 0; k < drawPipeList.Count; k++)
                        {
                            int pipeId = drawPipeList[k];

                            PipePoint nowPipePoint = DoorPipeToPointMap[new Tuple<int, int>(downDoorId, pipeId)];
                            DrawPipeData drawPipeData0 = new DrawPipeData(nowPipePoint.PointList[0], nowPipePoint.PointList[1], nowPipePoint.NowDoor.UpFirst, nowPipePoint.NowDoor.UpSecond, nowPipePoint.FreeDegree, pipeId, downDoorId);
                            pipeOutList.Add(drawPipeData0);
                        }
                    }

                    if (pins.Count != pouts.Count)
                    {
                        int stop = 0;
                    }

                    pipeInList.Reverse();
                    pipeOutList.Reverse();
                    main_index = pipeInList.Count - 1 - main_index;

                    //if (pins.Count == pouts.Count + 1)
                    //{
                    //    pins.RemoveAt(main_index);
                    //    pins_buffer.RemoveAt(main_index);
                    //}

                    //pins.ForEach(x => DrawUtils.ShowGeometry(x, "l1Input", 7, lineWeightNum: 30, 30, "C"));
                    // pouts.ForEach(x => DrawUtils.ShowGeometry(x, "l1Input", 7, lineWeightNum: 30, 30, "C"));

                    for (int a = 0; a < pipeInList.Count; a++)
                    {
                        DrawUtils.ShowGeometry(pipeInList[a].CenterPoint, "l1Input2", 10, lineWeightNum: 30, (int)pipeInList[a].HalfPipeWidth, "C");
                        Line drawLine = new Line(pipeInList[a].DoorLeft, pipeInList[a].DoorRight);
                        //DrawUtils.ShowGeometry(drawLine, "l1Inpu21Line", 200, lineWeightNum: 30);
                    }

                    for (int a = 0; a < pipeOutList.Count; a++)
                    {
                        DrawUtils.ShowGeometry(pipeOutList[a].CenterPoint, "l1Out2", 8, lineWeightNum: 30, (int)pipeOutList[a].HalfPipeWidth, "C");
                        Line drawLine = new Line(pipeOutList[a].DoorLeft, pipeOutList[a].DoorRight);
                        //DrawUtils.ShowGeometry(drawLine, "l1OutputLine", 200, lineWeightNum: 30);
                    }


                    //if (pipeInList.Count != pipeOutList.Count) continue;

                    //绘制
                    ////PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pins, pouts, pins_buffer, pouts_buffer, main_index);

                    PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pipeInList, pipeOutList, main_index, 600 , Parameter.SuggestDistanceWall);
                    passagePipeGenerator.CalculatePipeline();
                    List<PipeOutput> nowOutputList = passagePipeGenerator.outputs; 
                    nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.shape, "l1passingPipe", x.pipe_id%7 + 1, 30));
                    nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.skeleton, "l2PassingSkeleton", x.pipe_id % 7 + 1, 30));

                    //局部保存结果
                    //List<int> list = passagePipeGenerator.pipe_id;
                    for (int n = 0; n < nowOutputList.Count; n++)
                    {
                        int pipeId = nowOutputList[n].pipe_id;
                        var regionPolyMap = PipePolyListMap[pipeId];
                        List<Polyline> newList = new List<Polyline>();
                        newList.Add(nowOutputList[n].shape);
                        regionPolyMap.Add(i, newList);
                    }

                    //后处理
                    List<ChangePointData> changePointDatas = passagePipeGenerator.change_point_datas;
                    ChangePoint(changePointDatas);
                }
            }
        }

        public void ChangePoint(List<ChangePointData> changePointDatas)
        {
            //后调整
            //List<ChangePointData> changePointDatas = new List<ChangePointData>();
            for (int n = 0; n < changePointDatas.Count; n++)
            {
                ChangePointData changePointData = changePointDatas[n];

                PipePoint nowPipeData = DoorPipeToPointMap[new Tuple<int, int>(changePointData.DoorId, changePointData.PipeId)];

                List<Point3d> newList = new List<Point3d>();
                if (Math.Abs(changePointData.LeftPoint.X - changePointData.RightPoint.X) < 5)  //竖着的门
                {
                    newList.Add(changePointData.LeftPoint);
                    newList.Add(changePointData.RightPoint);
                    newList.Add(new Point3d(nowPipeData.PointList[2].X, changePointData.LeftPoint.Y, 0));
                    newList.Add(new Point3d(nowPipeData.PointList[3].X, changePointData.RightPoint.Y, 0));
                }
                else
                {
                    newList.Add(changePointData.LeftPoint);
                    newList.Add(changePointData.RightPoint);
                    newList.Add(new Point3d(changePointData.LeftPoint.X, nowPipeData.PointList[2].Y, 0));
                    newList.Add(new Point3d(changePointData.RightPoint.X, nowPipeData.PointList[3].Y, 0));
                }
                nowPipeData.PointList = newList;
            }
        }

        public void GetConnector()
        {
            for (int i = 0; i < SinglePipeList.Count; i++)
            {
                SinglePipe sp = SinglePipeList[i];
                for (int j = 0; j < sp.DoorList.Count; j++)
                {
                    int doorId = sp.DoorList[j];
                    int upRegionId = DoorList[doorId].UpstreamRegion.RegionId;
                    int downRegionId = DoorList[doorId].DownstreamRegion.RegionId;

                    if (doorId == 2) 
                    {
                        int stop = 0;
                    }

                    if (doorId == 0) continue;
                    List<Point3d> ppList = DoorPipeToPointMap[new Tuple<int, int>(doorId, i)].PointList;
                    double internalDistanceHalf = (ppList[0] - ppList[2]).Length / 2;
                    Point3d centerLeft = ppList[0] + (ppList[2] - ppList[0]) / 2;
                    Point3d centerRight = ppList[1] + (ppList[3] - ppList[1]) / 2;

                    Polyline upPl = PipePolyListMap[i][upRegionId][0];
                    Polyline downPl = PipePolyListMap[i][downRegionId][0];

                    double upBufferLength = internalDistanceHalf + GetConnectorOuterDis(ppList[0], ppList[1], upPl);
                    double downBufferLength = internalDistanceHalf + GetConnectorOuterDis(ppList[2], ppList[3], downPl);
                    Polyline pl = PolylineProcessService.CreateRectangle3(centerLeft, centerRight,upBufferLength, downBufferLength);

                    DrawUtils.ShowGeometry(pl, "l3ChaTou", 200, 30);
                    PipePolyListMap[i][upRegionId].Add(pl);
                }
            }
        }

        public void DrawWholePipe()
        {
            for (int i = 0; i < SinglePipeList.Count; i++) 
            {
                List<Polyline> tmpPolyList = new List<Polyline>();
                foreach (var plList in PipePolyListMap[i]) 
                {
                    tmpPolyList.AddRange(plList.Value);
                }
                var pl = tmpPolyList.ToArray().ToCollection().UnionPolygons(true).Cast<MPolygon>().First();

                DrawUtils.ShowGeometry(pl, "l3WholePipe", 0, 30);
            }
           
           
        }

        public double GetConnectorOuterDis(Point3d left, Point3d right, Polyline oldPl)
        {
            double dis = 0;

            var pl = oldPl;
            double disLeft = pl.GetClosePoint(left).DistanceTo(left);
            double disRight = pl.GetClosePoint(right).DistanceTo(right);
            if (Math.Abs(disLeft - disRight) > 5)
            {
                return Math.Min(disLeft, disRight + 100);
            }
            else if (Math.Max(disLeft, disRight) < 20)
            {
                return 50;
            }
            else
            {
                return disLeft + 50;
            }

            return dis;
        }

        public void SaveResults()
        {

        }
    }


    public class DrawPipeData
    {
        public int PipeId = -1;    //全局PipeId
        public int DoorId = -1;    //全局门Id，传出来
        public Point3d LeftPoint;    //管道精确点位
        public Point3d RightPoint;   //管道精确点位
        public Point3d CenterPoint;

        public Point3d DoorLeft;   //门的范围
        public Point3d DoorRight;
        public int Freedom = 0;    //自由度，0代表不能动，1代表能动

        public double HalfPipeWidth = 0;

        public DrawPipeData(Point3d leftPoint, Point3d rightPoint, Point3d doorLeft, Point3d doorRight, int freedom,int pipeId, int doorId)
        {
            this.PipeId = pipeId;
            this.DoorId = doorId;
            this.LeftPoint = leftPoint;
            this.RightPoint = rightPoint;
            this.DoorLeft = doorLeft;
            this.DoorRight = doorRight;
            this.Freedom = freedom;
            HalfPipeWidth = (leftPoint - rightPoint).Length / 2;
            CenterPoint = leftPoint + (rightPoint - leftPoint) / 2;
        }

        public DrawPipeData(Point3d centerPoint, double halfPipeWidth, int freedom, int pipeId)
        {
            this.PipeId = pipeId;
            this.Freedom = freedom;
            this.HalfPipeWidth = halfPipeWidth;
            this.CenterPoint = centerPoint;
        }
    }

    //点位移动信息
    public class ChangePointData
    {
        public int PipeId = -1;  
        public int DoorId = -1;  
        public Point3d LeftPoint = new Point3d();     //新的精确点位
        public Point3d RightPoint = new Point3d();    //新的精确点位

        public ChangePointData(int pipeId, int doorId, Point3d leftPoint, Point3d rightPoint)
        {
            this.PipeId = pipeId;
            this.DoorId = doorId;
            this.LeftPoint = leftPoint;
            this.RightPoint = rightPoint;
        }
    }
    
}
