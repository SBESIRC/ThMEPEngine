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
using ThMEPHVAC.FloorHeatingCoil.PassageWay;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class DrawPipe
    {
        public List<SingleRegion> RegionList = ProcessedData.RegionList;
        public List<SingleDoor> DoorList = ProcessedData.DoorList;
        public List<SinglePipe> SinglePipeList = ProcessedData.PipeList;
        public Dictionary<Tuple<int, int>, List<Point3d>> DoorPipeToPointMap = ProcessedData.DoorPipeToPointMap;

        public DrawPipe()
        {
           
        }

        public void Pipeline() 
        {
            for (int i = RegionList.Count - 1; i > 0; i--)
            {

                if (i == 16) 
                {
                    int stop = 0;
                }

                SingleRegion nowRegion = RegionList[i];

                if (nowRegion.ChildRegion.Count == 0 || nowRegion.PassingPipeList.Count == 1)
                {
                    double buffer_dis = -nowRegion.SuggestDist;

                    List<Point3d> pipeIn = new List<Point3d>();

                    int doorId = nowRegion.MainEntrance.DoorId;
                    for (int j = 0; j < nowRegion.MainEntrance.PipeIdList.Count; j++) 
                    {
                        int pipeId = nowRegion.MainEntrance.PipeIdList[j];
                        pipeIn.Add(DoorPipeToPointMap[new Tuple<int, int>(doorId, pipeId)][2]);
                        pipeIn.Add(DoorPipeToPointMap[new Tuple<int, int>(doorId, pipeId)][3]);
                    }

                    Vector3d vec0 = pipeIn[1] - pipeIn[0];
                    Point3d circleCenter = pipeIn[0] + vec0 / 2;
                    double radius = vec0.Length / 2;
                    
                    DrawUtils.ShowGeometry(circleCenter, "l1Input1", 170, lineWeightNum: 30, (int)radius, "C");


                    PipeInput pipeInput = new PipeInput(circleCenter, radius);
                    RoomPipeGenerator roomPipeGenerator = new RoomPipeGenerator(nowRegion.ClearedPl, pipeInput, -Parameter.SuggestDistanceWall * 2);


                    ////if (i == 16)
                    ////{
                    ////    DrawUtils.ShowGeometry(nowRegion.ClearedPl, "l1testPl", 10, 30);
                    ////    DrawUtils.ShowGeometry(circleCenter, "l1testPoints", 5, lineWeightNum: 30, (int)radius, "C");
                    ////}

                    //// calculate pipeline

                    roomPipeGenerator.CalculatePipeline();
                    // show result
                    var show = roomPipeGenerator.skeleton;

                    show.ForEach(x => DrawUtils.ShowGeometry(x, "l1RoomPipe", 10, 30));
                }
                else 
                {
                    double buffer_dis = -nowRegion.SuggestDist;
                    List<Point3d> pins = new List<Point3d>();
                    List<double> pins_buffer =new List<double>();
                    List<Point3d> pouts = new List<Point3d>();
                    List<double> pouts_buffer = new List<double>();
                    int main_index = -1;
                    ////pipe in
                    List<Point3d> pipeIn = new List<Point3d>();

                    int updoorId = nowRegion.MainEntrance.DoorId;
                    for (int j = 0; j < nowRegion.MainEntrance.PipeIdList.Count; j++)
                    {
                        int pipeId = nowRegion.MainEntrance.PipeIdList[j];
                        pipeIn.Add(DoorPipeToPointMap[new Tuple<int, int>(updoorId, pipeId)][2]);
                        pipeIn.Add(DoorPipeToPointMap[new Tuple<int, int>(updoorId, pipeId)][3]);

                        Vector3d vec0 = pipeIn[pipeIn.Count-1] - pipeIn[pipeIn.Count-2];
                        Point3d circleCenter = pipeIn[pipeIn.Count - 2] + vec0 / 2;
                        double radius = vec0.Length / 2;
                        pins.Add(circleCenter);
                        pins_buffer.Add(radius);

                        if (pipeId == nowRegion.MainPipe[0]) main_index = j;
                    }

                    ////pipe out
                    List<Point3d> pipeOut = new List<Point3d>();

                    List<int> downDoorIdList = new List<int>();
                    foreach (var child in nowRegion.ChildRegion) 
                    {
                        downDoorIdList.Add(nowRegion.ExportMap[child].DoorId);
                    }
                    List<int> newDownDoorIdList = downDoorIdList.OrderBy(x => Math.Min(DoorList[x].CCWDistance, DoorList[x].CWDistance)).ToList();
                    newDownDoorIdList.Reverse();
                    List<int> PipeHash = new List<int>(new int[SinglePipeList.Count]);
                    for (int a = 0; a < PipeHash.Count; a++) 
                    {
                        PipeHash[a] = -1;
                    }

                    Dictionary<int, List<int>> doorToDrawPipesMap = new Dictionary<int, List<int>>();
                    Dictionary<int, List<int>> doorToNotDrawPipesMap = new Dictionary<int, List<int>>();

                    for (int j = 0; j < newDownDoorIdList.Count; j++) 
                    {
                        int thisDoorId = newDownDoorIdList[j];
                        List<int> drawPipes = new List<int>();
                        List<int> notDrawPipes = new List<int>();
                        for (int k = 0; k < DoorList[thisDoorId].PipeIdList.Count; k++) 
                        {
                            if (PipeHash[DoorList[thisDoorId].PipeIdList[k]] == -1)
                            {
                                drawPipes.Add(DoorList[thisDoorId].PipeIdList[k]);
                                PipeHash[DoorList[thisDoorId].PipeIdList[k]] = thisDoorId;
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
                            pipeOut.Add(DoorPipeToPointMap[new Tuple<int, int>(downDoorId, pipeId)][0]);
                            pipeOut.Add(DoorPipeToPointMap[new Tuple<int, int>(downDoorId, pipeId)][1]);

                            Vector3d vec0 = pipeOut[pipeOut.Count - 1] - pipeOut[pipeOut.Count - 2];
                            Point3d circleCenter = pipeOut[pipeOut.Count - 2] + vec0 / 2;
                            double radius = vec0.Length / 2;
                            pouts.Add(circleCenter);
                            pouts_buffer.Add(radius);
                        }
                    }

                    if (pins.Count != pouts.Count) 
                    {
                        int stop = 0; 
                    }

                    pins.Reverse();
                    pins_buffer.Reverse();
                    pouts.Reverse();
                    pouts_buffer.Reverse();
                    main_index = pins.Count - 1 - main_index;

                    //pins.ForEach(x => DrawUtils.ShowGeometry(x, "l1Input", 7, lineWeightNum: 30, 30, "C"));
                   // pouts.ForEach(x => DrawUtils.ShowGeometry(x, "l1Input", 7, lineWeightNum: 30, 30, "C"));

                    for (int a = 0; a < pins.Count; a++) 
                    {
                        DrawUtils.ShowGeometry(pins[a], "l1Input2", 10, lineWeightNum: 30, (int)pins_buffer[a], "C");
                    }

                    for (int a = 0; a < pouts.Count; a++)
                    {
                        DrawUtils.ShowGeometry(pouts[a], "l1Out2", 8, lineWeightNum: 30, (int)pouts_buffer[a], "C");
                    }

                    //PassagePipeGenerator passagePipeGenerator = new PassagePipeGenerator(nowRegion.ClearedPl, pins, pouts, pins_buffer, pouts_buffer, main_index);
                    //passagePipeGenerator.CalculatePipeline();
                    //var show = passagePipeGenerator.skeleton;
                    //show.ForEach(x => DrawUtils.ShowGeometry(x, "l1passingPipe", 2, 30));
                }
            }
        }
    }
}
