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
        public Dictionary<int, Dictionary<int, List<Polyline>>> PipeCenterLineListMap = new Dictionary<int, Dictionary<int, List<Polyline>>>();
        public Dictionary<int, List<Polyline>> RegionPipePolyMap = new Dictionary<int, List<Polyline>>();
        public Dictionary<int, Polyline> PipeTotalPolyMap = new Dictionary<int, Polyline>();


        public Dictionary<int, List<Point3d>> PipeFixPointList = new Dictionary<int, List<Point3d>>();
        public List<Polyline> WholePipeList = new List<Polyline>();
        public List<Polyline> FilletedPipeList = new List<Polyline>();

        public List<int> DeletePipeList = new List<int>();

        public DrawPipe()
        {

        }

        public void Pipeline()
        {
            DataInit();

            GetDrawnPipe();

            GetConnector();

            DrawWholePipe();

            Fillet();

            SaveResults();
        }

        public void DataInit()
        {
            for (int i = 0; i < SinglePipeList.Count; i++)
            {
                Dictionary<int, List<Polyline>> keyValuePairs = new Dictionary<int, List<Polyline>>();
                PipePolyListMap.Add(i, keyValuePairs);

                Dictionary<int, List<Polyline>> keyValuePairs2 = new Dictionary<int, List<Polyline>>();
                PipeCenterLineListMap.Add(i, keyValuePairs2);
            }
        }

        public void GetRadiatorPipe()
        {
            int i = ProcessedData.RadiatorRegion;
            SingleRegion nowRegion = RegionList[i];
            double buffer_dis = -nowRegion.SuggestDist;

            List<DrawPipeData> pipeInList = new List<DrawPipeData>();
            List<DrawPipeData> pipeOutList = new List<DrawPipeData>();
            int doorId = nowRegion.MainEntrance.DoorId;
            for (int j = 0; j < nowRegion.MainEntrance.PipeIdList.Count; j++)
            {
                int pipeId = nowRegion.MainEntrance.PipeIdList[j];
                PipePoint nowPipePoint = DoorPipeToPointMap[new Tuple<int, int>(doorId, pipeId)];

                DrawPipeData drawPipeData0 = new DrawPipeData(nowPipePoint.PointList[2], nowPipePoint.PointList[3], nowPipePoint.NowDoor.DownSecond, nowPipePoint.NowDoor.DownFirst, nowPipePoint.FreeDegree, pipeId, doorId);
                pipeInList.Add(drawPipeData0);

                Vector3d vec0 = nowPipePoint.PointList[3] - nowPipePoint.PointList[2];
                Point3d circleCenter = nowPipePoint.PointList[0] + vec0 / 2;
                double radius = vec0.Length / 2;

                DrawUtils.ShowGeometry(circleCenter, "l1Input1", 170, lineWeightNum: 30, (int)radius, "C");
                if (nowPipePoint.FreeDegree != 0)
                {
                    DrawUtils.ShowGeometry(circleCenter, "l3Freedom", 0, lineWeightNum: 30, (int)radius, "C");
                }
            }

            Line drawLine = new Line(pipeInList[0].DoorLeft, pipeInList[0].DoorRight);
            DrawUtils.ShowGeometry(drawLine, "l1Input1Line", 200, lineWeightNum: 30);

            int pipeId2 = nowRegion.MainEntrance.PipeIdList[0];
            DrawPipeData drawPipeData1 = new DrawPipeData(ProcessedData.RadiatorPointList[1], ProcessedData.RadiatorPointList[0], ProcessedData.RadiatorPointList[1], ProcessedData.RadiatorPointList[0], 0, pipeId2, -1);
            pipeOutList.Add(drawPipeData1);
            //////if (i == 16)
            //////{
            //////    DrawUtils.ShowGeometry(nowRegion.ClearedPl, "l1testPl", 10, 30);
            //////    DrawUtils.ShowGeometry(circleCenter, "l1testPoints", 5, lineWeightNum: 30, (int)radius, "C");
            //////}

            //// calculate pipeline

            PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pipeInList, pipeOutList,0, nowRegion.SuggestDist * 2, Parameter.SuggestDistanceWall, 2);
            passagePipeGenerator.CalculatePipeline();
            List<PipeOutput> nowOutputList = passagePipeGenerator.outputs;
            nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.shape, "l4PassingPipe", x.pipe_id % 7 + 1, 30));
            nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.skeleton, "l4PassingSkeleton", x.pipe_id % 7 + 1, 30));

            //局部保存结果
            //List<int> list = passagePipeGenerator.pipe_id;
            for (int n = 0; n < nowOutputList.Count; n++)
            {
                int pipeId = nowOutputList[n].pipe_id;
                var regionPolyMap = PipePolyListMap[pipeId];
                List<Polyline> newList = new List<Polyline>();
                newList.Add(nowOutputList[n].shape);
                regionPolyMap.Add(i, newList);

                var regionCenterMap = PipeCenterLineListMap[pipeId];
                regionCenterMap.Add(i, nowOutputList[n].skeleton);
            }
        }

        public void GetDrawnPipe()
        {
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (i == 1)
                {
                    int stop = 5;
                }

                if (Parameter.HaveRadiator)
                {
                    if (i == ProcessedData.RadiatorRegion)
                    {
                        GetRadiatorPipe();
                        continue;
                    }
                }


                int mode = 0;
                if (i == 0) mode = Parameter.PrivatePublicMode;

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

                        Vector3d vec0 = nowPipePoint.PointList[3] - nowPipePoint.PointList[2];
                        Point3d circleCenter = nowPipePoint.PointList[2] + vec0 / 2;
                        double radius = vec0.Length / 2;

                        DrawUtils.ShowGeometry(circleCenter, "t0Input1", 170, lineWeightNum: 30, (int)radius, "C");
                        if (nowPipePoint.FreeDegree != 0) 
                        {
                            DrawUtils.ShowGeometry(circleCenter, "t0Freedom", 20 , lineWeightNum: 30, (int)radius, "C");
                        }
                    }

                    Line drawLine = new Line(pipeInList[0].DoorLeft, pipeInList[0].DoorRight);
                    DrawUtils.ShowGeometry(drawLine, "t0Input1Line", 200, lineWeightNum: 30);
                    DrawUtils.ShowGeometry(nowRegion.ClearedPl, "t0Region", 0, lineWeightNum: 30);

                    Point3d ptDraw = nowRegion.ClearedPl.GetCenter() + new Vector3d(800, 0, 0);
                    string draw = ((int)nowRegion.SuggestDist).ToString();
                    DrawUtils.ShowGeometry(ptDraw, draw, "t0SuggestDist", 10, 30, 300);

                    //////if (i == 16)
                    //////{
                    //////    DrawUtils.ShowGeometry(nowRegion.ClearedPl, "l1testPl", 10, 30);
                    //////    DrawUtils.ShowGeometry(circleCenter, "l1testPoints", 5, lineWeightNum: 30, (int)radius, "C");
                    //////}

                    //// calculate pipeline

                    RoomPipeGenerator1 roomPipeGenerator = new RoomPipeGenerator1(nowRegion.ClearedPl, pipeInList ,nowRegion.SuggestDist * 2, Parameter.SuggestDistanceWall);
                    roomPipeGenerator.CalculatePipeline();
                    // show result
                    //var show = roomPipeGenerator.skeleton;
                    //show.ForEach(x => DrawUtils.ShowGeometry(x, "l4RoomPipe", pipeInList[0].PipeId % 7 + 1, 30));

                    PipeOutput output = roomPipeGenerator.output;
                    output.skeleton.ForEach(x => DrawUtils.ShowGeometry(x, "l4RoomPipe", pipeInList[0].PipeId % 7 + 1, 30));
                    DrawUtils.ShowGeometry(output.shape, "l4RoomSkeleton", pipeInList[0].PipeId % 7 + 1, 30);
                    ////////////////////
                    int newPipeId = pipeInList[0].PipeId;
                    var regionPolyMap = PipePolyListMap[newPipeId];

                    List<Polyline> newList = new List<Polyline>();
                    newList.Add(output.shape);
                    regionPolyMap.Add(i, newList);

                    var regionCenterMap = PipeCenterLineListMap[newPipeId];
                    regionCenterMap.Add(i, output.skeleton);
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
                        DrawUtils.ShowGeometry(pipeInList[a].CenterPoint, "t0Input2", 10, lineWeightNum: 30, (int)pipeInList[a].HalfPipeWidth, "C");
                        Line drawLine = new Line(pipeInList[a].DoorLeft, pipeInList[a].DoorRight);
                        //DrawUtils.ShowGeometry(drawLine, "l1Inpu21Line", 200, lineWeightNum: 30);
                        if (pipeInList[a].Freedom != 0)
                        {
                            DrawUtils.ShowGeometry(pipeInList[a].CenterPoint, "t0Freedom", 0, lineWeightNum: 30, (int)pipeInList[a].HalfPipeWidth, "C");
                        }
                        if (a == main_index)
                        {
                            DrawUtils.ShowGeometry(pipeInList[a].CenterPoint, "t0MainPipeCircle", 1, lineWeightNum: 30, (int)pipeInList[a].HalfPipeWidth, "C");
                        }
                    }

                    for (int a = 0; a < pipeOutList.Count; a++)
                    {
                        DrawUtils.ShowGeometry(pipeOutList[a].CenterPoint, "t0Out2", 8, lineWeightNum: 30, (int)pipeOutList[a].HalfPipeWidth, "C");
                        Line drawLine = new Line(pipeOutList[a].DoorLeft, pipeOutList[a].DoorRight);
                        //DrawUtils.ShowGeometry(drawLine, "l1OutputLine", 200, lineWeightNum: 30);
                        if (pipeOutList[a].Freedom != 0)
                        {
                            DrawUtils.ShowGeometry(pipeOutList[a].CenterPoint, "t0Freedom", 0, lineWeightNum: 30, (int)pipeOutList[a].HalfPipeWidth, "C");
                        }
                        if (pipeOutList.Count == pipeInList.Count && a == main_index) 
                        {
                            DrawUtils.ShowGeometry(pipeOutList[a].CenterPoint, "t0MainPipeCircle", 1, lineWeightNum: 30, (int)pipeOutList[a].HalfPipeWidth, "C");
                        }
                    }
                    DrawUtils.ShowGeometry(nowRegion.ClearedPl, "t0Region", 0, lineWeightNum: 30);

                    Point3d ptDraw = nowRegion.ClearedPl.GetCenter() + new Vector3d(800, 0, 0);
                    string draw = ((int)nowRegion.SuggestDist).ToString();
                    DrawUtils.ShowGeometry(ptDraw, draw, "t0SuggestDist", 10, 30, 300);
                    

                    //备份
                    List<DrawPipeData> pipeOutListCopy = pipeOutList.Copy();
                    List<DrawPipeData> pipeInListCopy = pipeInList.Copy();


                    //if (pipeInList.Count != pipeOutList.Count) continue;

                    //绘制
                    //PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pins, pouts, pins_buffer, pouts_buffer, main_index);

                    PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pipeInList, pipeOutList, main_index,nowRegion.SuggestDist * 2, Parameter.SuggestDistanceWall,mode);
                    passagePipeGenerator.CalculatePipeline();
                    List<PipeOutput> nowOutputList = passagePipeGenerator.outputs;
                    nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.shape, "l4PassingPipe", x.pipe_id % 7 + 1, 30));
                    nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.skeleton, "l4PassingSkeleton", x.pipe_id % 7 + 1, 30));



                    if (ProcessedData.HaveVirtualPipe)
                    {
                        pipeInList = pipeInListCopy;
                        pipeOutList = pipeOutListCopy;

                        DrawPipeData vOut = DrawPipeData.CreateVOut(ProcessedData.VirtualPlNow, mainPipeId);
                        pipeOutList.Insert(main_index, vOut);
                        passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pipeInList, pipeOutList, main_index, nowRegion.SuggestDist * 2, Parameter.SuggestDistanceWall, mode);
                        passagePipeGenerator.CalculatePipeline();
                        nowOutputList = passagePipeGenerator.outputs;
                        nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.shape, "l8NewPassingPipe", x.pipe_id % 7 + 1, 30));
                        nowOutputList.ForEach(x => DrawUtils.ShowGeometry(x.skeleton, "l8NewPassingSkeleton", x.pipe_id % 7 + 1, 30));
                        ProcessedData.ClearVirtualPipe();
                    }

                    //局部保存结果
                    //List<int> list = passagePipeGenerator.pipe_id;
                    for (int n = 0; n < nowOutputList.Count; n++)
                    {
                        int pipeId = nowOutputList[n].pipe_id;
                        var regionPolyMap = PipePolyListMap[pipeId];
                        List<Polyline> newList = new List<Polyline>();
                        newList.Add(nowOutputList[n].shape);
                        regionPolyMap.Add(i, newList);
                        var regionCenterMap = PipeCenterLineListMap[pipeId];
                        regionCenterMap.Add(i, nowOutputList[n].skeleton);
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

        public void GetConnector2()
        {
            for (int i = 0; i < SinglePipeList.Count; i++)
            {
                PipeFixPointList.Add(i, new List<Point3d>());
                SinglePipe sp = SinglePipeList[i];
                for (int j = 0; j < sp.DoorList.Count; j++)
                {
                    int doorId = sp.DoorList[j];
                    int upRegionId = DoorList[doorId].UpstreamRegion.RegionId;
                    int downRegionId = DoorList[doorId].DownstreamRegion.RegionId;

                    if (doorId == 14) 
                    {
                        int stop = 0;
                    }

                    if (doorId == 0) continue;
                    List<Point3d> ppList = DoorPipeToPointMap[new Tuple<int, int>(doorId, i)].PointList;
                    
                    PipeFixPointList[i].Add(ppList[0]);
                    PipeFixPointList[i].Add(ppList[1]);

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

        public void GetConnector()
        {
            for (int i = 0; i < SinglePipeList.Count; i++)
            {
                PipeFixPointList.Add(i, new List<Point3d>());
                SinglePipe sp = SinglePipeList[i];
                for (int j = 0; j < sp.DoorList.Count; j++)
                {
                    int doorId = sp.DoorList[j];
                    int upRegionId = DoorList[doorId].UpstreamRegion.RegionId;
                    int downRegionId = DoorList[doorId].DownstreamRegion.RegionId;

                    if (doorId == 1)
                    {
                        int stop = 0;
                    }

                    if (doorId == 0) continue;
                    List<Point3d> ppList = DoorPipeToPointMap[new Tuple<int, int>(doorId, i)].PointList;

                    PipeFixPointList[i].Add(ppList[0]);
                    PipeFixPointList[i].Add(ppList[1]);

                    double internalDistanceHalf = (ppList[0] - ppList[2]).Length / 2;
                    Point3d centerLeft = ppList[0] + (ppList[2] - ppList[0]) / 2;
                    Point3d centerRight = ppList[1] + (ppList[3] - ppList[1]) / 2;

                    Polyline upPl = PipePolyListMap[i][upRegionId][0];
                    Polyline downPl = PipePolyListMap[i][downRegionId][0];

                    //DrawUtils.ShowGeometry(downPl, "l8Test", 1, 30);
                    if (upPl == new Polyline() || upPl.Area == 0 || downPl.Area == 0 || downPl == new Polyline()) continue;

                    double upBufferLength;
                    upBufferLength = internalDistanceHalf + GetConnectorOuterDis(ppList[0], ppList[1], upPl);
                    double downBufferLength;
                    downBufferLength = internalDistanceHalf + GetConnectorOuterDis(ppList[2], ppList[3], downPl);

                    Polyline pl = PolylineProcessService.CreateRectangle3(centerLeft, centerRight, upBufferLength, downBufferLength);

                    DBObjectCollection a = new DBObjectCollection();
                    a.Add(upPl as DBObject);
                    List<Polyline> overlap = pl.Intersection(a).OfType<Polyline>().ToList();
                    if (overlap.Count == 0)
                    {
                        Polyline center = PipeCenterLineListMap[i][upRegionId][0];
                        Polyline otherConnector = GetOtherConnector(ppList[0], ppList[1], center, upRegionId);
                        List<Polyline> listToUnion = new List<Polyline>();
                        listToUnion.Add(pl);
                        listToUnion.Add(otherConnector);

                        pl = listToUnion.ToCollection().UnionPolygons(false).Cast<Polyline>().First();
                    }
                    DrawUtils.ShowGeometry(pl, "l3ChaTou", 200, 30);
                    PipePolyListMap[i][upRegionId].Add(pl);
                }
            }
        }

        public Polyline GetOtherConnector(Point3d left, Point3d right, Polyline centerLine, int regionId)
        {
            Polyline newChatou = new Polyline();

            var coords = PassageWayUtils.GetPolyPoints(centerLine);
            Point3d centerPt = left + (right - left) * 0.5;

            double minDis = 10000000;
            Point3d closePoint = new Point3d();
            for (int i = 0; i < coords.Count; i++)
            {
                double nowDis = centerPt.DistanceTo(coords[i]);
                if (nowDis < minDis)
                {
                    minDis = nowDis;
                    closePoint = coords[i];
                }
            }

            Vector3d dir = (right - left).GetNormal();
            Vector3d dirOut = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();

            double dx = closePoint.X - centerPt.X;
            double dy = closePoint.Y - centerPt.Y;
            Point3d newPoint = new Point3d();
            if (Math.Abs(dirOut.X) > 0.7)
            {
                newPoint = new Point3d(centerPt.X + dx, centerPt.Y, centerPt.Z);
            }
            else
            {
                newPoint = new Point3d(centerPt.X, centerPt.Y + dy, centerPt.Z);
            }
            List<Point3d> ptList = new List<Point3d>();
            ptList.Add(centerPt);
            ptList.Add(newPoint);
            ptList.Add(closePoint);

            List<double> buff = new List<double>();
            buff.Add((right - left).Length * 0.5);
            buff.Add(RegionList[regionId].SuggestDist * 0.5);

            if (centerPt == newPoint) 
            {
                ptList.RemoveAt(0);
                buff.RemoveAt(0);
            }

            if (ptList.Count == 2 && (ptList[0] - ptList[1]).Length < 1) 
            {
                ptList[1] = ptList[0] + dirOut * 5; 
            }
            BufferPoly bp = new BufferPoly(ptList, buff);
            newChatou = bp.Buffer();
            DrawUtils.ShowGeometry(newChatou, "l5OtherChaTou", 200, 30);

            return newChatou;
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

                for (int j = tmpPolyList.Count-1; j >= 0 ; j--)
                {
                    if (tmpPolyList[j] == new Polyline()) tmpPolyList.RemoveAt(j);
                    else tmpPolyList[j].Closed = true;
                    //DrawUtils.ShowGeometry(tmpPolyList[i], "l4Why", 5, 30);
                }

                Polyline pl = new Polyline();
                var plList2 = tmpPolyList.ToArray().ToCollection().UnionPolygons(false).Cast<Polyline>().ToList();

                if (plList2.Count > 0)
                {
                    pl = plList2.First();
                }

                WholePipeList.Add(pl);
                DrawUtils.ShowGeometry(pl, "l3WholePipe", 0, 30);

                //var plList2 = tmpPolyList.ToArray().ToCollection().UnionPolygons(false).Cast<Polyline>().ToList();
                //if (plList2.Count > 1)
                //{
                //    DrawUtils.ShowGeometry(plList2, "l3DisconnectedWholePipe", 0, 30);
                //}
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
                return Math.Min(disLeft, disRight) + 100;
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

        public void Fillet() 
        {
            var pipeInfo = DoorPipeToPointMap[new Tuple<int, int>(0, 0)];
            Line startLine = new Line(pipeInfo.NowDoor.DownFirst, pipeInfo.NowDoor.DownFirst);

            for (int i = 0; i < WholePipeList.Count; i++) 
            {
                if (WholePipeList[i] == new Polyline() || WholePipeList[i].Area == 0)
                {
                    FilletedPipeList.Add(WholePipeList[i]);
                    continue;
                }
                var nowPipeInfo  = DoorPipeToPointMap[new Tuple<int, int>(0, i)];
                Point3d pt0 = nowPipeInfo.PointList[2];
                Point3d pt1 = nowPipeInfo.PointList[3];

                List<Point3d> fixList = new List<Point3d>();
                fixList.Add(pt0);
                fixList.Add(pt1);
                fixList.AddRange(PipeFixPointList[i]);
                
                //修线
                WholePipeList[i] = PolylineProcessService.PlClearSmall(WholePipeList[i], fixList, 101);
                DrawUtils.ShowGeometry(WholePipeList[i], "l5ClearSmall", 20 , 30);

                //修正入口偏移
                WholePipeList[i] = WaterSeparator.EntranceCorrection(WholePipeList[i], ProcessedData.WaterOffset, fixList);
                Point3d pt2 = pt0 + ProcessedData.WaterOffset;
                Point3d pt3 = pt1 + ProcessedData.WaterOffset;

                //倒角
                var fillet_poly = FilletUtils.FilletPolyline(WholePipeList[i] , pt2, pt3);
                DrawUtils.ShowGeometry(pt0,"l5StartTest",20);
                
                DrawUtils.ShowGeometry(fillet_poly, "l4FilletedPipe", 0, 30);
                FilletedPipeList.Add(fillet_poly);
            }
        }

        public void SaveResults()
        {
            for (int i = 0; i < FilletedPipeList.Count; i++) 
            {
                SinglePipeList[i].PipeId = i;

                if (WholePipeList[i] != new Polyline() && WholePipeList[i].Area != 0)
                {
                    SinglePipeList[i].ResultPolys.Add(FilletedPipeList[i]);
                }
                
            }
        }
    }


    public class DrawPipeData
    {
        public int PipeId = -1;    //全局PipeId
        public int DoorId = -1;    //全局门Id，传出来
        public Point3d LeftPoint;    //管道精确点位
        public Point3d RightPoint;   //管道精确点位
        public Point3d CenterPoint;
        public Vector3d OutDir;

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


        static public DrawPipeData CreateVOut(Polyline vpl, int pipeId) 
        {
            Point3d focus = vpl.GetMaximumInscribedCircleCenter();
            return new DrawPipeData(focus, 50, 0, pipeId);
            DrawUtils.ShowGeometry(focus, "l6VCenter", 0, 50, 50);
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
