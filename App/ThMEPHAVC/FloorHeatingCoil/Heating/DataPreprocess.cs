using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;

using ThMEPHVAC.FloorHeatingCoil.Heating;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class DataPreprocess
    {
        public RawData rawData1;
        public List<Polyline> RegionObbs = new List<Polyline>();
        public List<Polyline> Door1Obbs = new List<Polyline>();
        public List<Polyline> Door2Obbs = new List<Polyline>();
        public List<Polyline> Door2Line = new List<Polyline>();
        public int MainRegionId = -1;

        public Dictionary<Polyline, int> RegionToIndex = new Dictionary<Polyline, int>();
        //public Dictionary<int, List<Connection>> RegionConnection = new Dictionary<int, List<Connection>>();
        public List<List<Connection>> RegionConnection;
        public List<SingleRegion> RegionList = new List<SingleRegion>();
        public List<SingleDoor> DoorList = new List<SingleDoor>();

        //DoorToDoorDistance
        public DoorToDoorDistance [,] DoorToDoorDistanceMap;
        public Dictionary<SingleDoor, int> SingleDoorToIndex = new Dictionary<SingleDoor, int>();

        public DataPreprocess(RawData rawData)
        {
            //
            rawData1 = rawData;

        }

        //零碎变量
        Line WaterLine = new Line();
        Vector3d WaterDir = new Vector3d();

        public void Pipeline()
        {
            //读入数据
            ReadPureData();
            MainRegionId = FindMainRegion();

            //处理障碍物
            //RemoveObstacles();

            //寻找连通性
            FindConnectivity1(RegionObbs, Door1Obbs);
            FindConnectivity2(RegionObbs, Door2Obbs, Door2Line);

            //建图
            CreateGraph();

            //处理类中一部分问题
            ModelProcess();

            //计算门与门之间的距离并保存
            ComputeDoorToDoorDistance();

            //继续处理类中的一部分问题
            ModelProcess2();

            //估算门与门之间的真实途经距离并保存
            EstimatedDoorToDoorDistance();

            //输出结果
            SaveResults();
        }

        public void ReadPureData()
        {
            //处理Room
            foreach (ThFloorHeatingRoom a in rawData1.Room)
            {
                Polyline pl = a.RoomBoundary;
                RegionObbs.Add(pl);
            }
            ThCADCoreNTSSpatialIndex originalRegionIndex = new ThCADCoreNTSSpatialIndex(RegionObbs.ToCollection());
            ProcessedData.RegionIndex = originalRegionIndex;
            for (int i = 0; i < RegionObbs.Count; i++)
            {
                RegionToIndex.Add(RegionObbs[i], i);
            }

            //处理门

            foreach (Polyline b in rawData1.Door)
            {
                Door1Obbs.Add(b.Clone() as Polyline);
            }
            
            ThCADCoreNTSSpatialIndex originalDoorIndex = new ThCADCoreNTSSpatialIndex(Door1Obbs.ToCollection());
           
            foreach (Polyline c in rawData1.RoomSeparateLine)
            {
                //var bigBuffers = ThCADCoreNTSOperation.BufferFlatPL(c, 100).OfType<Polyline>().ToList();
                //var bigBuffer = bigBuffers.OrderByDescending(x => x.Area).First();

                //if (originalDoorIndex.SelectCrossingPolygon(bigBuffer).Count > 0) continue;
                if (c.Length < 200) continue;
                Door2Line.Add(c);
                var pls = ThCADCoreNTSOperation.BufferFlatPL(c, 20).OfType<Polyline>().ToList();
                var pl = pls.OrderByDescending(x => x.Area).First();
                Door2Obbs.Add(pl);
            }
            

            //建立空间索引
            

            //
            RegionConnection = new List<List<Connection>>(new List<Connection>[RegionObbs.Count]);
            for (int i = 0; i < RegionObbs.Count; i++) 
            {
                RegionConnection[i] = new List<Connection>();
            }
            RegionList = new List<SingleRegion>(new SingleRegion[RegionObbs.Count]);
            //Thread.SpinWait(100000);
        }

        //寻找集水器所在的房间
        public int FindMainRegion()
        {
            //处理集水器
            if (rawData1.WaterSeparator.OBB == null) return 0 ;
            Polyline waterPl = rawData1.WaterSeparator.OBB;
            if (!waterPl.IsCCW()) waterPl.ReverseCurve();
            for (int i = 0; i < waterPl.NumberOfVertices; i++) 
            {
                Point3d pt0 = waterPl.GetPoint3dAt(i);
                Point3d pt1 = waterPl.GetPoint3dAt((i + 1) % waterPl.NumberOfVertices);
                Line thisLine = new Line(pt0, pt1);

                if (thisLine.DistanceTo(rawData1.WaterSeparator.StartPts.Last(), false) < Parameter.SmallTolerance) 
                {
                    WaterLine = thisLine;
                    Vector3d dir = pt1 - pt0;
                    WaterDir = new Vector3d(dir.Y, -dir.X, dir.Z).GetNormal();
                }
            }


            int mainRegionId = -1;

            for (int i = 0;  i < this.RegionObbs.Count; i++)
            {
                if (RegionObbs[i].Contains(rawData1.WaterSeparator.StartPts[0]))
                {
                    mainRegionId = i;
                    break;
                }
            }

            double maxArea = 0;
            if (mainRegionId == -1) //如果没找到 
            {
                Polyline searchArea = PolylineProcessService.CreateRectangle2(WaterLine.EndPoint,WaterLine.StartPoint,1000);
                DrawUtils.ShowGeometry(searchArea, "l1WaterArea", 60, 30);
                for (int i = 0; i < this.RegionObbs.Count; i++)
                {
                    DBObjectCollection db = new DBObjectCollection();
                    db.Add(RegionObbs[i]);
                    List<Polyline> overlapAreaList = searchArea.Intersection(db).OfType<Polyline>().ToList();
                    if (overlapAreaList.Count > 0) 
                    {
                        if (overlapAreaList.OrderByDescending(x => x.Area).First().Area > maxArea) 
                        {
                            mainRegionId = i;
                            maxArea = overlapAreaList.OrderByDescending(x => x.Area).First().Area;
                        }
                    }
                }

                //for (int i = 0; i < this.RegionObbs.Count; i++)
                //{
                //    if (RegionObbs[i].DistanceTo(rawData1.WaterSeparator.StartPts[0],false) < minDis)
                //    {
                //        minDis = RegionObbs[i].DistanceTo(rawData1.WaterSeparator.StartPts[0], false);
                //        mainRegionId = i;
                //    }
                //}
            }

            return mainRegionId;
        }

        public void RemoveObstacles()
        {
            ThCADCoreNTSSpatialIndex ObstaclesIndex = new ThCADCoreNTSSpatialIndex(rawData1.Obstacle.ToCollection());

            for(int i = 0;i< RegionObbs.Count;i++)
            {
                var selectObstacles = ObstaclesIndex.SelectCrossingPolygon(RegionObbs[i]);
                Polyline newObb = RegionObbs[i].Difference(selectObstacles).OfType<Polyline>().ToList().FindByMax(x=>x.Area);
                RegionObbs[i] = newObb;
            }
        }

        //
        //寻找连通关系，以 原始门id，原始区域id，原始区域id 的格式保存。
        public void FindConnectivity3(List<Polyline> regionObbs, List<Polyline> door1Obbs)
        {
            foreach (Polyline doorPl in door1Obbs)
            {
                List<int> FoundRegionId = new List<int>();

                List<Polyline> selectRooms = ProcessedData.RegionIndex.SelectCrossingPolygon(doorPl).OfType<Polyline>().ToList();
                foreach (Polyline room in selectRooms)
                {
                    var roomObj = new DBObjectCollection();
                    roomObj.Add(room);
                    //rawData0.Door.ForEach(x => doorObjs.Add(x));
                    Polyline overlapArea = room.Intersection(roomObj).OfType<Polyline>().FindByMax(x => x.Area);
                    if (overlapArea.Area > Parameter.ConnectionThresholdArea)
                    {
                        FoundRegionId.Add(RegionToIndex[room]);
                    }
                }

                if (FoundRegionId.Count != 2) Thread.SpinWait(100000);
                else
                {
                    int region0 = FoundRegionId[0];
                    int region1 = FoundRegionId[1];

                    ////互相连通
                    //if (RegionConnection.ContainsKey(region0))
                    //{
                    //    RegionConnection[region0].Add(new Connection(doorPl, region1));
                    //}
                    //else 
                    //{
                    //    List<Connection> tmpConnections = new List<Connection>();
                    //    tmpConnections.Add(new Connection(doorPl, region1));
                    //    RegionConnection.Add(region0, tmpConnections);
                    //}
                    ////互相连通
                    //if (RegionConnection.ContainsKey(region1))
                    //{
                    //    RegionConnection[region1].Add(new Connection(doorPl, region0));
                    //}
                    //else
                    //{
                    //    List<Connection> tmpConnections = new List<Connection>();
                    //    tmpConnections.Add(new Connection(doorPl, region0));
                    //    RegionConnection.Add(region1, tmpConnections);
                    //}
                }
            }
        }

        public void FindConnectivity1(List<Polyline> regionObbs, List<Polyline> door1Obbs)
        {
            foreach (Polyline doorPl in door1Obbs)
            {
                List<int> foundRegionId = new List<int>();

                List<Polyline> selectRooms = ProcessedData.RegionIndex.SelectCrossingPolygon(doorPl).OfType<Polyline>().ToList();
                if (selectRooms.Count < 2) continue;

                foreach (Polyline room in selectRooms)
                {
                    var roomObj = new DBObjectCollection();
                    roomObj.Add(room);
                    //rawData0.Door.ForEach(x => doorObjs.Add(x));
                    Polyline overlapArea = room.Intersection(roomObj).OfType<Polyline>().FindByMax(x => x.Area);
                    if (overlapArea.Area > Parameter.ConnectionThresholdArea)
                    {
                        foundRegionId.Add(RegionToIndex[room]);
                    }
                }

                //if (FoundRegionId.Count != 2)
                //{
                //    //Thread.SpinWait(100000);
                //    continue;
                //}
                //else
                //{
                //    int region0 = FoundRegionId[0];
                //    int region1 = FoundRegionId[1];

                //    //互相连通
                //    RegionConnection[region0].Add(new Connection(doorPl, region1));
                //    RegionConnection[region1].Add(new Connection(doorPl, region0));
                //}

                if (foundRegionId.Count < 2)
                {
                    Thread.SpinWait(100000);
                    continue;
                }
                else 
                {
                    if (foundRegionId.Contains(2)) 
                    {
                        int stop = 0;
                    }

                    List<Line> foundDoorLine = new List<Line>();
                    for (int i = 0; i < foundRegionId.Count; i++) 
                    {
                        int nowRegionId = foundRegionId[i];
                        Line doorLine = regionObbs[nowRegionId].Trim(doorPl).OfType<Polyline>().ToList().FindByMax(x=>x.Length).ToLines().FindByMax(x =>x.Length);

                        DrawUtils.ShowGeometry(doorLine, "l1tmpPl", 10, lineWeightNum: 30);
                        foundDoorLine.Add(doorLine);
                    }


                    for (int j = 0; j < foundRegionId.Count - 1; j++)
                    {
                        for (int k = j + 1; k < foundRegionId.Count; k++)
                        {
                            int region0 = foundRegionId[j];
                            int region1 = foundRegionId[k];

                            if (Math.Min(foundDoorLine[j].Length,foundDoorLine[k].Length) > Parameter.ConnectionThresholdLength) 
                            {
                                int isFound = 0;
                                Polyline newDoorPl = GetNewDoorPolyline(doorPl,foundDoorLine[j], foundDoorLine[k],ref isFound);
                                if (isFound == 1) 
                                {
                                    RegionConnection[region0].Add(new Connection(newDoorPl, region1,0));
                                    RegionConnection[region1].Add(new Connection(newDoorPl, region0,0));
                                    DrawUtils.ShowGeometry(newDoorPl, "l1newDoorPl", 8, lineWeightNum: 30);
                                }
                            } 
                        }
                    }
                }
            }
        }

        public void FindConnectivity2(List<Polyline> regionObbs, List<Polyline> door2Obbs, List<Polyline> door2Line)
        {
            for (int i = 0; i < door2Obbs.Count; i++)
            {
                var doorPl = door2Obbs[i];
                var doorLine = door2Line[i];

                List<int> FoundRegionId = new List<int>();
                List<Polyline> FoundDoorLine = new List<Polyline>();

                List<Polyline> selectRooms = ProcessedData.RegionIndex.SelectCrossingPolygon(doorPl).OfType<Polyline>().ToList();
                foreach (Polyline room in selectRooms)
                {
                    var bufferedRoom = room.Buffer(5).OfType<Polyline>().ToList().FindByMax(x => x.Area);
                    //var trimedLine = bufferedRoom.Trim(doorLine).OfType<Polyline>().ToList().First();
                    var trimedLineList = bufferedRoom.Trim(doorLine).OfType<Polyline>().ToList();
                    Polyline trimedLine = new Polyline();
                    if (trimedLineList.Count == 0)
                    {
                        trimedLine = doorLine;
                    }
                    else 
                    {
                        trimedLine = bufferedRoom.Trim(doorLine).OfType<Polyline>().ToList().First();
                    }

                    if (trimedLine.Length > Parameter.ConnectionThresholdLength)
                    {
                        FoundRegionId.Add(RegionToIndex[room]);
                        FoundDoorLine.Add(trimedLine);
                    }
                }

                if (FoundRegionId.Count < 2)
                {
                    Thread.SpinWait(100000);
                    continue;
                }
                else
                {
                    if (FoundRegionId.Contains(MainRegionId)) 
                    {
                        int stop = 0;
                    }

                    for (int j = 0; j < FoundRegionId.Count - 1; j++)
                    {
                        for (int k = j+1 ; k < FoundRegionId.Count; k++)
                        {
                            int region0 = FoundRegionId[j];
                            int region1 = FoundRegionId[k];

                            var bufferedLines = ThCADCoreNTSOperation.BufferFlatPL(FoundDoorLine[j], 20).OfType<Polyline>().ToList();
                            var bufferedLine = bufferedLines.OrderByDescending(x => x.Area).First();
                            //Polyline overlapLine = FoundDoorLine[k].Trim(bufferedLine).OfType<Polyline>().ToList().First();
                            var overlapLineList = bufferedLine.Trim(FoundDoorLine[k]).OfType<Polyline>().ToList();
                            Polyline overlapLine = new Polyline();

                            if (bufferedLine.Contains(FoundDoorLine[k])) 
                            {
                                overlapLine = FoundDoorLine[k];
                            }
                            else if (overlapLineList.Count == 0)
                            {
                                continue;
                                //overlapLine = FoundDoorLine[k];
                            }
                            else
                            {
                                overlapLine = bufferedLine.Trim(FoundDoorLine[k]).OfType<Polyline>().ToList().First();
                            }

                            if (overlapLine.Length > Parameter.ConnectionThresholdLength)
                            {
                                //if (IsRepetition(region0, region1)) continue;
                                var newDoorPl = ThCADCoreNTSOperation.BufferFlatPL(overlapLine, 20).OfType<Polyline>().ToList().FindByMax(x => x.Area);
                                RegionConnection[region0].Add(new Connection(newDoorPl, region1,1));
                                RegionConnection[region1].Add(new Connection(newDoorPl, region0,1));
                                DrawUtils.ShowGeometry(newDoorPl, "l1newDoorPl", 8, lineWeightNum: 30);
                            }
                        }
                    }
                }
            }
        }

        public Polyline GetNewDoorPolyline(Polyline originalDoorPl, Line line0, Line line1, ref int IsFound) 
        {
            Polyline newPl = new Polyline();
            int index0 = -1;
            int index1 = -1;
            Vector3d dir0 = line0.EndPoint - line0.StartPoint;
            Vector3d dir1 = line1.EndPoint - line1.StartPoint;
            double shortSide = 0;
            PolylineProcessService.ClearPolyline(ref originalDoorPl);

            int num = originalDoorPl.NumberOfVertices;
            for (int i = 0; i < num; i++)
            {
                var pt1 = originalDoorPl.GetPoint3dAt(i);
                var pt2 = originalDoorPl.GetPoint3dAt((i + 1) % num);
                Line nowLine = new Line(pt1, pt2);
                Vector3d dir = pt2 - pt1;
                
                if (nowLine.Length < Parameter.ConnectionThresholdLength) 
                {
                    shortSide = nowLine.Length;
                    continue;
                }

                if (nowLine.DistanceTo(line0.StartPoint,false) < Parameter.SmallTolerance && nowLine.DistanceTo(line0.EndPoint,false) < Parameter.SmallTolerance) 
                {
                    index0 = i;
                    double angle = dir.GetAngleTo(dir0, Vector3d.ZAxis);
                    if (angle > 0.1 && angle < 2 * Math.PI - 0.1) line0 = new Line(line0.EndPoint,line0.StartPoint); 
                }
                if (nowLine.DistanceTo(line1.StartPoint,false) < Parameter.SmallTolerance && nowLine.DistanceTo(line1.EndPoint,false) < Parameter.SmallTolerance)
                {
                    index1 = i;
                    double angle = dir.GetAngleTo(dir1, Vector3d.ZAxis);
                    if (angle > 0.1 && angle < 2 * Math.PI - 0.1) line1 = new Line(line1.EndPoint, line1.StartPoint);
                }
            }

            if (Math.Min(index1, index0) == -1 || index0 == index1)
            {
                IsFound = 0;
                return newPl;
            }
            else 
            {
                IsFound = 1;
                if (line0.Length < line1.Length)
                {
                    newPl = PolylineProcessService.CreateRectangle2(line0.StartPoint, line0.EndPoint, shortSide);
                    return newPl;
                }
                else 
                {
                    newPl = PolylineProcessService.CreateRectangle2(line1.StartPoint, line1.EndPoint, shortSide);
                    return newPl;
                }
            }

            return newPl;
        } 

        public bool IsRepetition(int region0 ,int region1) 
        {
            bool flag = false;
            foreach (var connection in RegionConnection[region0]) 
            {
                if (connection.RegionId == region1) return true;
            }
            return flag;
        }
        
        //整理拓扑关系,建图
        public void CreateGraph()
        {
            //构造队列
            Queue<int> regionIdQueue = new Queue<int>();
            List<int> visited = new List<int>();
            List<int> stopped = new List<int>();
            List<int> level = new List<int>();
            int regionCount = 0;
            int doorCount = 0;

            for (int i = 0; i < RegionObbs.Count; i++)
            {
                visited.Add(0);
                stopped.Add(0);
                level.Add(12);
            }


            //构造第一个Region
            regionIdQueue.Enqueue(MainRegionId);
            RegionList[MainRegionId] = new SingleRegion(regionCount, RegionObbs[MainRegionId], rawData1.Room[MainRegionId].SuggestDist);
            regionCount++;
            RegionList[MainRegionId].Level = 0;
            visited[MainRegionId] = 1;
            level[MainRegionId] = 0;

            //构造第一个Door
            Polyline zeroDoorObb = new Polyline();
            SingleDoor zeroDoor = new SingleDoor(doorCount, RegionList[MainRegionId], RegionList[MainRegionId], zeroDoorObb,0);
            DoorList.Add(zeroDoor);
            doorCount++;
            RegionList[MainRegionId].EntranceMap.Add(RegionList[MainRegionId], zeroDoor);
            RegionList[MainRegionId].FatherRegion.Add(RegionList[MainRegionId]);

            while (regionIdQueue.Count > 0)
            {
                int nowId = regionIdQueue.Dequeue();
                stopped[nowId] = 1;

                for (int i = 0; i < RegionConnection[nowId].Count; i++)
                {
                    Connection connection = RegionConnection[nowId][i];
                    int nextRegionId = connection.RegionId;
                    Polyline doorObb = connection.DoorObb;
                    int type = connection.ConnectionType;

                    //如果这一Region曾出队，则停止
                    if (stopped[nextRegionId] == 1) continue;
                    //如果同level，不考虑
                    if (level[nowId] >= level[nextRegionId]) continue;

                    //如果没访问这一Region
                    if (visited[nextRegionId] == 0)
                    {
                        regionIdQueue.Enqueue(nextRegionId);
                        visited[nextRegionId] = 1;
                        level[nextRegionId] = level[nowId] + 1;

                        RegionList[nextRegionId] = new SingleRegion(regionCount, RegionObbs[nextRegionId], rawData1.Room[nextRegionId].SuggestDist);
                        regionCount++;
                        RegionList[nextRegionId].Level = level[nextRegionId];
                        SingleDoor newDoor = new SingleDoor(doorCount, RegionList[nowId], RegionList[nextRegionId], doorObb,type);
                        DoorList.Add(newDoor);
                        doorCount++;
                        RegionList[nowId].ExportMap.Add(RegionList[nextRegionId], newDoor);
                        RegionList[nextRegionId].EntranceMap.Add(RegionList[nowId],newDoor);
                        RegionList[nowId].ChildRegion.Add(RegionList[nextRegionId]);
                        RegionList[nextRegionId].FatherRegion.Add(RegionList[nowId]);
                    }
                    else  //如果曾访问过这一Region
                    {
                        if (RegionList[nowId].ExportMap.ContainsKey(RegionList[nextRegionId])) continue;

                        SingleDoor newDoor = new SingleDoor(doorCount, RegionList[nowId], RegionList[nextRegionId], doorObb,type);
                        DoorList.Add(newDoor);
                        doorCount++;
                        RegionList[nowId].ExportMap.Add(RegionList[nextRegionId], newDoor);
                        RegionList[nextRegionId].EntranceMap.Add(RegionList[nowId], newDoor);
                        RegionList[nowId].ChildRegion.Add(RegionList[nextRegionId]);
                        RegionList[nextRegionId].FatherRegion.Add(RegionList[nowId]);
                    }
                }
            }
        }

        //修改一些类的属性
        public void ModelProcess()
        {
            //处理Region
            RegionPretreatment();

            //处理Door
            DoorPretreatment();

            //修正门的大小
            ResizeTheDoor();
        }

        //区域预处理
        public void RegionPretreatment() 
        {
            List<int> deleteList = new List<int>();
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (RegionList[i] == null)
                {
                    deleteList.Add(i);
                }
            }
            for (int i = deleteList.Count - 1; i >= 0; i--)
            {
                RegionList.RemoveAt(deleteList[i]);
            }

            RegionList = RegionList.OrderBy(x => x.RegionId).ToList();
            foreach (SingleRegion sr in RegionList)
            {
                //清理Region框线
                if (sr.RegionId == 1)
                {
                    int stop = 0;
                }
                //DrawUtils.ShowGeometry(sr.OriginalPl, "l1OriginalPl", 170, lineWeightNum: 30);
                sr.ClearedPl = PolylineProcessService.PlRegularization2(sr.OriginalPl, Parameter.ClearThreshold);

                //调整推荐间距
                if (sr.SuggestDist == 0) sr.SuggestDist = Parameter.SuggestDistanceRoom;

                //计算区域 UsedPipeLength
                sr.UsedPipeLength = EstimateService.ComputeUsedPipeLength(sr.ClearedPl, Parameter.SuggestDistanceWall, sr.SuggestDist);

                //draw
                Point3d ptDraw = sr.ClearedPl.GetCenter();
                string draw = sr.RegionId.ToString();
                DrawUtils.ShowGeometry(sr.ClearedPl, "l1ClearedPl", 2, lineWeightNum: 30);
                DrawUtils.ShowGeometry(ptDraw, draw, "l1RegionId", 2, 30, 100);
            }
        }

        //门预处理
        public void DoorPretreatment() 
        {
            foreach (SingleDoor sd in DoorList)
            {
                if (sd.DoorId == 0)
                {
                    VirtualDoorProcess(sd);
                    continue;
                }
                //清理Door框线
                PolylineProcessService.ClearPolyline(ref sd.OriginalPl);
                Point3d ptDraw = sd.OriginalPl.GetCenter();
                DrawUtils.ShowGeometry(ptDraw, sd.DoorId.ToString(), "l1DoorId", 5, 30, 100);

                if (sd.DoorId == 8)
                {
                    int empyty = 0;
                }

                //载入门信息，寻找门的位置
                sd.SetRecInfo();
                Polyline bufferedDoor = PolylineProcessService.CreateBoundary(sd.Center, sd.ShortSide.Length + Parameter.DoorBufferValue, sd.LongSide.Length, sd.ShortDir);
                DrawUtils.ShowGeometry(bufferedDoor, "l1bufferedDoor", 8, lineWeightNum: 30);
                //门的上部
                Line upLine = DoorToPoint3d(bufferedDoor, sd.UpstreamRegion.ClearedPl);
                //DrawUtils.ShowGeometry(upLine, "l1DoorLine", 3, lineWeightNum: 30);
                int reverse = 0;
                int index = 0;
                FindPosition(upLine, sd.UpstreamRegion.ClearedPl, ref index, ref reverse);
                sd.UpLineIndex = index;
                if (reverse == 1) upLine = new Line(upLine.EndPoint, upLine.StartPoint);
                //门的下部
                Line downLine = DoorToPoint3d(bufferedDoor, sd.DownstreamRegion.ClearedPl);
                //DrawUtils.ShowGeometry(downLine, "l1DoorLine", 3, lineWeightNum: 30);
                reverse = 0;
                index = 0;
                FindPosition(downLine, sd.DownstreamRegion.ClearedPl, ref index, ref reverse);
                sd.DownLineIndex = index;
                if (reverse == 1) downLine = new Line(downLine.EndPoint, downLine.StartPoint);

                CheckDoorLineFound(ref upLine, ref downLine, sd);

                sd.UpFirst = upLine.StartPoint;
                sd.UpSecond = upLine.EndPoint;

                sd.DownFirst = downLine.StartPoint;
                sd.DownSecond = downLine.EndPoint;

            }

        }

        //修正门的大小
        public void ResizeTheDoor() 
        {
            for (int i = 1; i < DoorList.Count; i++)
            {
                int downLineIndex = DoorList[i].DownLineIndex;
                Polyline downClearedPl = DoorList[i].DownstreamRegion.ClearedPl;
                Point3d pt0 = downClearedPl.GetPoint3dAt(downLineIndex);
                Point3d pt1 = downClearedPl.GetPoint3dAt((downLineIndex+1) % downClearedPl.NumberOfVertices);

                int upLineIndex = DoorList[i].UpLineIndex;
                Polyline upClearedPl = DoorList[i].UpstreamRegion.ClearedPl;
                Point3d pt2 = upClearedPl.GetPoint3dAt(upLineIndex);
                Point3d pt3 = upClearedPl.GetPoint3dAt((upLineIndex + 1) % upClearedPl.NumberOfVertices);
                Line testLine = new Line(pt2, pt3);

                if (pt0.DistanceTo(DoorList[i].DownFirst) < Parameter.SuggestDistanceWall * 1.5) 
                {
                    Vector3d ex = pt0 - DoorList[i].DownFirst;
                    
                    Point3d newUp = DoorList[i].UpSecond + ex;
                    if (testLine.DistanceTo(newUp, false) < 5)
                    {
                        DoorList[i].DownFirst = pt0;
                        DoorList[i].UpSecond = newUp;
                    }
                }
                if (pt1.DistanceTo(DoorList[i].DownSecond) < Parameter.SuggestDistanceWall * 1.5)
                {
                    Vector3d ex = pt1 - DoorList[i].DownSecond;
                    Point3d newUp = DoorList[i].UpFirst + ex;
                    if (testLine.DistanceTo(newUp, false) < 5)
                    {
                        DoorList[i].DownSecond = pt1;
                        DoorList[i].UpFirst = newUp;
                    }
                }
                Line upLine = new Line(DoorList[i].UpFirst, DoorList[i].UpSecond);
                Line downLine = new Line(DoorList[i].DownFirst, DoorList[i].DownSecond);

                DrawUtils.ShowGeometry(upLine, "l1DoorLine", 3, lineWeightNum: 30);
                DrawUtils.ShowGeometry(downLine, "l1DoorLine", 3, lineWeightNum: 30);
            }
        }

        //修改一些类的属性2
        public void ModelProcess2()
        {
            //此处需修改
            //整理子区域之间的拓扑关系
            for (int i = 0; i < RegionList.Count; i++)
            {
                SingleRegion nowRegion = RegionList[i];
                int upDoorId = nowRegion.EntranceMap[nowRegion.FatherRegion[0]].DoorId;

                nowRegion.ChildRegion = nowRegion.ChildRegion.OrderBy(x => DoorToDoorDistanceMap[nowRegion.ExportMap[x].DoorId,upDoorId].CWDistance).ToList();
                for (int j = 0; j < nowRegion.ChildRegion.Count; j++) 
                {
                    SingleDoor nowDoor = nowRegion.ExportMap[nowRegion.ChildRegion[j]];
                    nowDoor.LeftDoorNum = j;
                    nowDoor.DoorNum = nowRegion.ChildRegion.Count;
                    nowDoor.CWDistance = DoorToDoorDistanceMap[nowDoor.DoorId, upDoorId].CWDistance;
                    nowDoor.CCWDistance = DoorToDoorDistanceMap[nowDoor.DoorId, upDoorId].CCWDistance;
                }
            }

            IdentifyTheTypeOfRoom();
           

        }

        public void IdentifyTheTypeOfRoom() 
        {
            //判断区域关键类型（只能有一条管线）
            foreach (SingleRegion sr in RegionList)
            {
                if (sr.ChildRegion.Count > 1)
                {
                    sr.RegionType = 0;
                    continue; 
                }
                ThMEPMaximumInscribedRectangle a = new ThMEPMaximumInscribedRectangle();
                Polyline rec = a.GetRectangle(sr.ClearedPl);
                Vector3d shortSide = new Vector3d();
                Point3d tmpPt = new Point3d();
                Vector3d tmpVec0 = new Vector3d();
                Vector3d tmpVec1 = new Vector3d();
                PolylineProcessService.GetRecInfo(rec, ref tmpPt, ref tmpVec0, ref tmpVec1, ref shortSide);
                if (shortSide.Length > Parameter.KeyRoomShortSide)
                {
                    sr.RegionType = 2;
                }
                else 
                {
                    if (sr.Level >= 2)
                    {
                        sr.RegionType = 1;
                    }
                    else sr.RegionType = 0;
                }
            }

            //判断区域公区类型
            Queue<int> regionIdQueue = new Queue<int>();
            List<int> visited = new List<int>();
            List<int> stopped = new List<int>();
            List<int> level = new List<int>();

            for (int i = 0; i < RegionObbs.Count; i++)
            {
                visited.Add(0);
                stopped.Add(0);
                level.Add(12);
            }

            regionIdQueue.Enqueue(0);
            RegionList[0].IsPublicRegion = 1;

            int nowId = regionIdQueue.Dequeue();
            List<SingleDoor> ChildDoorList = RegionList[nowId].ExportMap.Values.ToList();
            for (int i = 0; i < ChildDoorList.Count; i++) 
            {
                SingleDoor nowDoor = ChildDoorList[i];
                SingleRegion downRegion = nowDoor.DownstreamRegion;
                RegionList[downRegion.RegionId].IsPublicRegion = 1;
                regionIdQueue.Enqueue(downRegion.RegionId);
            }

            while (regionIdQueue.Count > 0)
            {
                nowId = regionIdQueue.Dequeue();
                ChildDoorList = RegionList[nowId].ExportMap.Values.ToList();
                for (int i = 0; i < ChildDoorList.Count; i++)
                {
                    SingleDoor nowDoor = ChildDoorList[i];
                    if (nowDoor.DoorType == 1)
                    {
                        SingleRegion downRegion = nowDoor.DownstreamRegion;
                        RegionList[downRegion.RegionId].IsPublicRegion = 1;
                        regionIdQueue.Enqueue(downRegion.RegionId);
                    }
                }
            }
        }

        //Check
        public void CheckDoorLineFound(ref Line upLine,ref Line downLine, SingleDoor sd) 
        {
            //出现没对齐情况
            if (Math.Max(downLine.Length, upLine.Length) - Math.Min(downLine.Length, upLine.Length) > 2)
            {
                if (downLine.Length > upLine.Length) ParallelLineClipping(ref upLine, ref downLine);
                else ParallelLineClipping(ref downLine,ref upLine);
            }
        }
        
        public void ParallelLineClipping(ref Line line0,ref Line line1) 
        {
            DrawUtils.ShowGeometry(line1, "l1test", 2, lineWeightNum: 30);
            Polyline test0 = PolylineProcessService.CreateRectangle3(line0.EndPoint, line0.StartPoint,Parameter.DoorBufferValue*2,Parameter.DoorBufferValue);
            DrawUtils.ShowGeometry(test0, "l1ParallelLineClipping", 150, lineWeightNum: 30);
            List<Polyline> newLine1List = test0.Trim(line1).OfType<Polyline>().ToList();
            Line newLine1 = newLine1List.FindByMax(x => x.Length).ToLines().ToList().FindByMax(x =>x.Length);
            line1 = newLine1;
        }

        public Line DoorToPoint3d(Polyline doorObb, Polyline regionObb)
        {
            //功能：输入门的Polyline，获得门在某一个区域框线上的位置;
            //描述：doorObb.Trim(regionObb) ,用门去Trim房间框线，获得房间框线上对应门的线段。
            //


            //var trimedLine = regionObb.Trim(doorObb).OfType<Polyline>().ToList().FindByMax(x => x.Length);

            var trimedLineList = doorObb.Trim(regionObb).OfType<Polyline>().ToList();

            if (trimedLineList.Count > 0)
            {
                var trimedLine = trimedLineList.FindByMax(x => x.Length);
                DrawUtils.ShowGeometry(doorObb, "l1DoorObb", 6, lineWeightNum: 30);
                DrawUtils.ShowGeometry(regionObb, "l1RegionObb", 7, lineWeightNum: 30);
                DrawUtils.ShowGeometry(trimedLine, "l1trimedLiine", 5, lineWeightNum: 30);

                //有可能Trim出其他线，取最长的一条；
                Line doorLine = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
                double maxLength = 0;
                for (int i = 0; i < trimedLine.NumberOfVertices - 1; i++)
                {
                    var pt0 = trimedLine.GetPoint3dAt(i);
                    //var pt1 = trimedLine.GetPoint3dAt((i + 1) % trimedLine.NumberOfVertices);
                    var pt1 = trimedLine.GetPoint3dAt(i + 1);

                    Vector3d line0 = pt1 - pt0;

                    if (line0.Length > maxLength)
                    {
                        maxLength = line0.Length;
                        doorLine = new Line(pt0, pt1);
                    }
                }
                return doorLine;
            }
            else 
            {
                return new Line(new Point3d(0,0,0),new Point3d(0,0,0));
            }
        }

        public void FindPosition(Line line0, Polyline regionObb,ref int index,ref int reverse) 
        {
            Point3d pt0 = line0.StartPoint;
            Point3d pt1 = line0.EndPoint;
            Vector3d dir = pt1 - pt0;
            Point3d ptCenter = pt0 + 0.5 * dir;

            for (int i = 0; i < regionObb.NumberOfVertices; i++)
            {
                var pt00 = regionObb.GetPoint3dAt(i);
                var pt10 = regionObb.GetPoint3dAt((i + 1) % regionObb.NumberOfVertices);

                Line line1 = new Line(pt00, pt10);
                if (ptCenter.IsPointOnLine(line1, Parameter.SmallTolerance))
                {
                    index = i;
                    Vector3d l0 = pt0 - pt00;
                    Vector3d l1 = pt1 - pt00;
                    if (l0.Length > l1.Length) 
                    {
                        reverse = 1;
                    }
                    break;
                }
                else 
                {
                    continue;
                }
            }


        }

        public Line WaterSeparatorToDoorLine(ref Polyline regionObb) 
        {
            Line doorLine = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
            Point3d start = WaterLine.StartPoint;
            Point3d end = WaterLine.EndPoint;
            Vector3d waterDir = WaterDir;
            //waterDir = new Vector3d(-waterDir.Y, waterDir.X, waterDir.Z);
            Vector3d offset = waterDir.GetNormal() * Parameter.WaterSeparatorDis;

            DrawUtils.ShowGeometry(regionObb, "l3tmpRegionObb", 3, lineWeightNum: 30);

            Line line0 = new Line(start- offset, start + offset);
            Line line1 = new Line(end - offset, end + offset);

            //List<Point3d> doorFirstList = line0.Intersect(regionObb, Intersect.OnBothOperands).ToList();
            //List<Point3d> doorSecondList = line1.Intersect(regionObb, Intersect.OnBothOperands).ToList();

            Point3dCollection pts0 = new Point3dCollection();
            Point3dCollection pts1 = new Point3dCollection();
            line0.IntersectWith(regionObb, Intersect.OnBothOperands, pts0, (IntPtr)0, (IntPtr)0);
            line1.IntersectWith(regionObb, Intersect.OnBothOperands, pts1, (IntPtr)0, (IntPtr)0);
            List<Point3d> doorFirstList = pts0.OfType<Point3d>().ToList();
            List<Point3d> doorSecondList = pts1.OfType<Point3d>().ToList();

            double dis1 = 0;
            double dis2 = 0;
            Point3d doorFirst = new Point3d();
            Point3d doorSecond = new Point3d();
            if (doorFirstList.Count > 0) 
            {
                doorFirst = doorFirstList.FindByMin(x => x.DistanceTo(start));
                dis1 = doorFirst.DistanceTo(start);
                DrawUtils.ShowGeometry(doorFirst, "l1WaterPoint", 5, lineWeightNum: 30, 30, "C");
            }
            if (doorSecondList.Count > 0) 
            {
                doorSecond = doorSecondList.FindByMin(x => x.DistanceTo(end));
                DrawUtils.ShowGeometry(doorSecond, "l1WaterPoint", 5, lineWeightNum: 30, 30, "C");
                dis2 = doorSecond.DistanceTo(end);
            }
            if (dis1 > dis2)
            {
                doorLine = new Line(doorFirst, doorFirst + (end - start));
            }
            else 
            {
                doorLine = new Line(doorSecond + (start - end) ,doorSecond);
            }

            Polyline differArea = PolylineProcessService.CreateRectangle2(doorLine.StartPoint, doorLine.EndPoint, 5000);
            regionObb = regionObb.Difference(differArea).OfType<Polyline>().ToList().FindByMax(x => x.Area);
            PolylineProcessService.ClearPolyline(ref regionObb);


            //删除超出的边
            Polyline newPl = new Polyline();
            newPl.Closed = false;
            newPl.AddVertexAt(0, doorLine.StartPoint.ToPoint2D(), 0, 0, 0);
            newPl.AddVertexAt(1, doorLine.EndPoint.ToPoint2D(), 0, 0, 0);

            var pls = ThCADCoreNTSOperation.BufferFlatPL(newPl, 20).OfType<Polyline>().ToList();
            var pl = pls.OrderByDescending(x => x.Area).First();
            var plList = pl.Trim(regionObb).OfType<Polyline>().ToList();
            if (plList.Count > 0) 
            {
                doorLine = plList.FindByMax(x => x.Length).ToLines().ToList().FindByMax(x => x.Length);
            }

            //doorLine = new Line(start, end);
            return doorLine;
        }

        public void VirtualDoorProcess(SingleDoor sd) 
        {
            Line downLine = WaterSeparatorToDoorLine(ref sd.DownstreamRegion.ClearedPl);
            DrawUtils.ShowGeometry(downLine, "l1DoorLine", 3, lineWeightNum: 30);

            
            int index = 0;
            int reverse = 0;
            //FindPosition(downLine, sd.DownstreamRegion.ClearedPl, ref index, ref reverse);
            sd.DownLineIndex = index;
            if (reverse == 1)
            {
                sd.DownFirst = downLine.EndPoint;
                sd.DownSecond = downLine.StartPoint;
            }
            else
            {
                sd.DownFirst = downLine.StartPoint;
                sd.DownSecond = downLine.EndPoint;
            }
        }

        /// <summary>
        /// 计算门与门之间的距离
        /// </summary>
        public void ComputeDoorToDoorDistance()
        {
            //处理要用的数据结构
            DoorToDoorDistanceMap = new DoorToDoorDistance[DoorList.Count, DoorList.Count];
            for (int i = 0; i < DoorList.Count; i++)
            { 
                SingleDoorToIndex.Add(DoorList[i],i);
            }

            //第一扇门不算
            for (int i = 1; i < DoorList.Count; i++)
            {
                if (i == 13)
                {
                    int empty = 0;
                }

                SingleDoor downDoor = DoorList[i];
                List<SingleDoor> tmpSingleDoors = new List<SingleDoor>();
                for (int j = 0; j < downDoor.UpstreamRegion.FatherRegion.Count; j++) 
                {
                    SingleRegion upUpRegion = downDoor.UpstreamRegion.FatherRegion[j];
                    tmpSingleDoors.Add(downDoor.UpstreamRegion.EntranceMap[upUpRegion]);
                }
                //tmpSingleDoors.AddRange(downDoor.UpstreamRegion.Entrance);
                
                for (int j = 0; j < tmpSingleDoors.Count; j++)
                {
                    SingleDoor upDoor = tmpSingleDoors[j];
                    int upDoorId = SingleDoorToIndex[upDoor];
                    if (upDoorId == i) continue;
                    if (upDoorId == 0) 
                    {
                        //Point3d
                        DoorToDoorDistanceMap[i, upDoorId] = new DoorToDoorDistance(i, upDoorId, 2000 , 1, 2000, 1);
                    }
                    // ccw
                    double ccwLength = 0;
                    int ccwTurning = 0;
                    ComputePointToPointDistance(downDoor.UpstreamRegion.ClearedPl, downDoor.UpSecond, downDoor.UpLineIndex,
                        upDoor.DownFirst, upDoor.DownLineIndex, 1 ,ref ccwLength, ref ccwTurning);
                    //cw
                    double cwLength = 0;
                    int cwTurning = 0;
                    ComputePointToPointDistance(downDoor.UpstreamRegion.ClearedPl, downDoor.UpFirst, downDoor.UpLineIndex,
                        upDoor.DownSecond, upDoor.DownLineIndex, 0, ref cwLength, ref cwTurning);

                    //记录信息
                    DoorToDoorDistanceMap[i, upDoorId] = new DoorToDoorDistance(i, upDoorId, ccwLength,ccwTurning,cwLength,cwTurning);

                    //Draw
                    string ccw = "CCW："+ ((int)ccwLength).ToString();
                    string cw = "CW：" + ((int)cwLength).ToString();
                    Point3d ptDraw1 = downDoor.UpSecond;
                    Point3d ptDraw2 = downDoor.UpFirst;
                    DrawUtils.ShowGeometry(ptDraw1, ccw , "l1ccwDoorToDoorLength", 4, 30, 100);
                    DrawUtils.ShowGeometry(ptDraw2, cw , "l1cwDoorToDoorLength", 3, 30, 100);
                }
            }
        }
        
        public void ComputePointToPointDistance(Polyline regionObb,Point3d pt1,int index1,Point3d pt2,int index2 ,int IsCCW ,ref double Length,ref int Turning) 
        {
            Length = 0;
            Turning = 0;

            if (IsCCW == 1) 
            {
                if (index1 == index2)
                {
                    var ptTest0 = regionObb.GetPoint3dAt(index1);
                    //var ptTest1 = regionObb.GetPoint3dAt((index1 + 1) % regionObb.NumberOfVertices);
                    Vector3d line1 = pt1 - ptTest0;
                    Vector3d line2 = pt2 - ptTest0;
                    double tmpLength = line2.Length - line1.Length;
                    if (tmpLength > 0)  //如果正常 
                    {
                        Length = tmpLength;
                        Turning = 0;
                    }
                    else 
                    {
                        Length = regionObb.Length + tmpLength;
                    }
                }
                else 
                {

                    for (int i = index1; i < index1 + regionObb.NumberOfVertices; i++)
                    {
                        var pt00 = regionObb.GetPoint3dAt(i % regionObb.NumberOfVertices);
                        var pt01 = regionObb.GetPoint3dAt((i + 1) % regionObb.NumberOfVertices);

                        if (i == index1) 
                        {
                            Vector3d tmpVec = pt01 - pt1;
                            Length = Length + tmpVec.Length; 
                        }
                        else if ((i % regionObb.NumberOfVertices) == index2)
                        {
                            Vector3d tmpVec = pt2 - pt00;
                            Length = Length + tmpVec.Length;
                            Turning++;
                            return;
                        }
                        else 
                        {
                            Vector3d tmpVec = pt01 - pt00;
                            Length = Length + tmpVec.Length;
                            Turning++;
                        }
                    }
                }
            }

            else if (IsCCW == 0)
            {
                if (index1 == index2)
                {
                    //var ptTest0 = regionObb.GetPoint3dAt(index1);
                    var ptTest0 = regionObb.GetPoint3dAt((index1 + 1) % regionObb.NumberOfVertices);
                    Vector3d line1 = pt1 - ptTest0;
                    Vector3d line2 = pt2 - ptTest0;
                    double tmpLength = line2.Length - line1.Length;
                    if (tmpLength > 0)  //如果正常 
                    {
                        Length = tmpLength;
                        Turning = 0;
                    }
                    else
                    {
                        Length = regionObb.Length + tmpLength ;
                    }
                }
                else
                {
                    for (int i = index1; i > index1 - regionObb.NumberOfVertices; i--)
                    {
                        var pt00 = regionObb.GetPoint3dAt((i+ regionObb.NumberOfVertices) % regionObb.NumberOfVertices);
                        var pt01 = regionObb.GetPoint3dAt((i+ regionObb.NumberOfVertices + 1) % regionObb.NumberOfVertices);

                        if (i == index1)
                        {
                            Vector3d tmpVec = pt1 - pt00;
                            Length = Length + tmpVec.Length;
                        }
                        else if (((i + regionObb.NumberOfVertices) % regionObb.NumberOfVertices) == index2)
                        {
                            Vector3d tmpVec = pt01- pt2;
                            Length = Length + tmpVec.Length;
                            Turning++;
                            return;
                        }
                        else
                        {
                            Vector3d tmpVec = pt01 - pt00;
                            Length = Length + tmpVec.Length;
                            Turning++;
                        }
                    }
                }
            }
        }

        public void EstimatedDoorToDoorDistance()
        {
            for (int i = 1; i < DoorList.Count; i++)
            {
                SingleDoor downDoor = DoorList[i];
                List<SingleDoor> tmpSingleDoors = new List<SingleDoor>();
                for (int j = 0; j < downDoor.UpstreamRegion.FatherRegion.Count; j++)
                {
                    SingleRegion upUpRegion = downDoor.UpstreamRegion.FatherRegion[j];
                    tmpSingleDoors.Add(downDoor.UpstreamRegion.EntranceMap[upUpRegion]);
                }

                for (int j = 0; j < tmpSingleDoors.Count; j++)
                {
                    SingleDoor upDoor = tmpSingleDoors[j];
                    int upDoorId = upDoor.DoorId;
                    if (upDoorId == i) continue;

                    DoorToDoorDistance dtdD = DoorToDoorDistanceMap[i, upDoorId];
                    dtdD.LeftDoorNum = downDoor.LeftDoorNum;
                    dtdD.DoorNum = downDoor.DoorNum;
                    dtdD.DoorPositionProportion = dtdD.LeftDoorNum / dtdD.DoorNum; 
                  
                    double newCCWD = dtdD.CCWDistance - dtdD.CCWTurning * Parameter.SuggestDistanceWall + 4 * Parameter.SuggestDistanceWall;
                    double newCWD = dtdD.CWDistance - dtdD.CWTurning * Parameter.SuggestDistanceWall + 4 * Parameter.SuggestDistanceWall;

                    //int leftNum = 
                    dtdD.newCCWD = newCCWD * 0.8 + newCWD * 0.2;
                    dtdD.newCWD = newCCWD * 0.2 + newCWD * 0.8;

                    //此处可修改
                    dtdD.EstimatedDistance = Math.Min(dtdD.newCWD, dtdD.newCCWD);
                    if (Math.Max(dtdD.newCWD, dtdD.newCCWD) - Math.Min(dtdD.newCWD, dtdD.newCCWD) > 10000) 
                    {
                        dtdD.EstimatedDistance = Math.Min(dtdD.newCWD, dtdD.newCCWD);
                    }
                }
            }
        }

        //保存结果
        public void SaveResults()
        {
            ProcessedData.RegionList = RegionList;
            ProcessedData.DoorList = DoorList;
            ProcessedData.DoorToDoorDistanceMap = DoorToDoorDistanceMap;
            ProcessedData.RegionConnection = RegionConnection;
        }




        //接口
        public void PipelineA() 
        {
            //读入数据
            ReadPureData();
            MainRegionId = FindMainRegion();

            //处理障碍物
            //RemoveObstacles();

            //寻找连通性
            FindConnectivity1(RegionObbs, Door1Obbs);
            FindConnectivity2(RegionObbs, Door2Obbs, Door2Line);
        }

    }

    class Connection 
    {
        public int ConnectionType = -1;
        public Polyline DoorObb;
        public int DoorId;
        public int RegionId;
        public Connection(Polyline doorObb,int regionId,int ccType) 
        {
            this.DoorObb = doorObb;
            this.RegionId = regionId;
            this.ConnectionType = ccType;
        }
    }

    class DoorToDoorDistance
    {   
        //真实值
        public int DownDoorId = -1;
        public int UpDoorId = -1;

        public double CCWDistance = 0;
        public int CCWTurning = 0;
        public double CWDistance = 0;
        public int CWTurning = 0;
        public int LeftDoorNum = 0;
        public int DoorNum = 0;
        public double DoorPositionProportion = 0;
        //估算变量
        public double EstimatedDistance = 0;
        public double newCCWD = 0;
        public double newCWD = 0;

        public DoorToDoorDistance(int downId, int upId,double ccwd,int ccwt, double cwd, int cwt) 
        {
            DownDoorId = downId;
            UpDoorId = upId;
            CCWDistance = ccwd;
            CCWTurning = ccwt;
            CWDistance = cwd;
            CWTurning = cwt;
        }
    }
}
