using System.Collections.Generic;
using System.Linq;
using ThMEPElectrical.FireAlarm.Data;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.IO;
using ThMEPElectrical.FireAlarm.Service;
using ThCADExtension;
using ThMEPEngineCore.Config;

namespace ThMEPElectrical.FireAlarm.Logic
{
    public class ThDisplayDeviceFixedPointLayoutService : ThFixedPointLayoutService
    {
        public BuildingType BuildingType { get; set; }
       
        //public ThMEPEngineCore.Algorithm.ThMEPOriginTransformer Transformer { get; set; }
        
        public ThDisplayDeviceFixedPointLayoutService(List<ThGeometry> datas, List<string> LayoutBlkName, List<string> AvoidBlkName) : base(datas, LayoutBlkName, AvoidBlkName)
        {
        }

        public override List<KeyValuePair<Point3d, Vector3d>> Layout()
        {
            List<KeyValuePair<Point3d, Vector3d>> ans = new List<KeyValuePair<Point3d, Vector3d>>();
            List<Polyline> rooms = new List<Polyline>();
            int reservedLength = 500;
            int bufferValue = 0;
            double visualRange = 4000; //视程设为4米

            //根据建筑类型提取房间
            if (BuildingType == BuildingType.Resident)
                rooms = FindRoomForResidentalBuildings().Cast<Polyline>().ToList();
            else if (BuildingType == BuildingType.Public)
                rooms = FindRoomForPublicBuilding().Cast<Polyline>().ToList();
            //rooms.ForEach(r => Transformer.Reset(r));
            //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(rooms.Cast<Entity>().ToList(), AcHelper.Active.Database, 1);

            rooms = DataQueryWorker.GetDecorableOutlineBase(rooms);
            var spatialCrossingDoorsIndex = new ThCADCoreNTSSpatialIndex(DataQueryWorker.DoorOpenings.Select(x => x.Boundary).ToCollection());
            var releventElementSet = DataQueryWorker.Avoidence.Select(x => x.Boundary).ToCollection();//避让元素（门窗）
            var spatialAvoidenceIndex = new ThCADCoreNTSSpatialIndex(releventElementSet);
            //DataQueryWorker.Avoidence.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0avoide", 4));
            foreach (Polyline room in rooms)
            {
                DrawUtils.ShowGeometry(room, "l0Room", 3);
                var avoidenceSet = spatialAvoidenceIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();
                List<Polyline> doorWall = new List<Polyline>(); //有门的墙
                var avoidencePt = DataQueryWorker.GetAvoidPoints(room, avoidenceSet);

                Point3d locatePt = new Point3d();
                var crossingDoors = spatialCrossingDoorsIndex.SelectCrossingPolygon(room);
                List<Polyline> outdoorWall = new List<Polyline>();
                int num = room.NumberOfVertices;
                int[] tag = new int[num];  //Polyline中点num-1的tag实际记到0处
                bool[] viewable = new bool[num];
                int viewedNum = 0;
                int numNotTag = 0;
                List<int> notTagIndex = new List<int>();
                List<Polyline> decorableBaseLine = new List<Polyline>();

                Tolerance tol = new Tolerance(5, 5);
                
                List<Point3d> viewPoint = new List<Point3d>();
                
                foreach (Polyline door in crossingDoors) // 遍历door记录tag,同时记录有门的墙体，墙体点位存储顺序为角落到门
                {
                    var bufferedDoor = door.Buffer(bufferValue).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    var doorPt = DataQueryWorker.GetAvoidPoints(room, new List<Polyline> { door });
                    if (doorPt.Count == 0)
                        continue;
                    //记录门所在墙体
                    var doorPaired = new List<KeyValuePair<int, Point3d>>();
                    foreach (Point3d pt in doorPt[0])
                    {
                        doorPaired.Add(new KeyValuePair<int, Point3d>(DataQueryWorker.FindIndex(room, pt), pt));
                    }
                    doorPaired.OrderBy(o => o.Key);
                    int rightIndex = -1;  //记录door为左或右的index值
                    int leftIndex = -1;
                    for (int i = 1; i < doorPaired.Count; ++i)
                    {
                        if (doorPaired[i].Key - doorPaired[i - 1].Key > 1)
                        {
                            rightIndex = i - 1;
                            leftIndex = i;
                            break;
                        }
                    }
                    if (rightIndex == -1)
                    {
                        rightIndex = doorPaired.Count - 1;
                        leftIndex = 0;
                    }
                    var rightHandWall = new Polyline();  //门两侧墙
                    var leftHandWall = new Polyline();
                    //使门两侧墙的点位存储顺序为由角落开始
                    rightHandWall.AddVertexAt(0, room.GetPoint3dAt((doorPaired[rightIndex].Key + 1) % num).ToPoint2D(), 0, 0, 0);
                    rightHandWall.AddVertexAt(1, doorPaired[rightIndex].Value.ToPoint2D(), 0, 0, 0);
                    leftHandWall.AddVertexAt(0, room.GetPoint3dAt(doorPaired[leftIndex].Key).ToPoint2D(), 0, 0, 0);
                    leftHandWall.AddVertexAt(1, doorPaired[leftIndex].Value.ToPoint2D(), 0, 0, 0);


                    Polyline walls = new Polyline();  //其他墙体顺时针存储，应为不闭合polyline
                    for (int i = 0; i < num - doorPaired.Count; ++i)
                    {
                        walls.AddVertexAt(i, room.GetPoint3dAt((i + doorPaired[rightIndex].Key) % num).ToPoint2D(), 0, 0, 0);
                    }

                    bool isOutdoor = DataQueryWorker.Query(door).Properties[ThExtractorPropertyNameManager.TagPropertyName].ToString().Contains("外门");
                    if (rightHandWall.Length > 2 * reservedLength)
                    {
                        if (isOutdoor)
                            outdoorWall.Add(rightHandWall);
                        else
                            doorWall.Add(rightHandWall);
                    }
                    if (leftHandWall.Length > 2 * reservedLength)
                    {
                        if (isOutdoor)
                            outdoorWall.Add(leftHandWall);
                        else
                            doorWall.Add(rightHandWall);
                    }
                    for (int i = 0; i < doorPaired.Count - 1; ++i)
                    {
                        ++tag[doorPaired[i].Key];
                        ++tag[doorPaired[i + 1].Key];
                    }

                    for (int i = 0; i < num - 1; ++i)
                    {
                        foreach (Point3d pt in doorPt[0])
                        {
                            if (pt.DistanceTo(room.GetPoint3dAt(i)) < visualRange)
                            {
                                viewable[i] = true;
                                break;
                            }
                        }
                    }
                }
                //foreach (Polyline door in crossingDoors) // 遍历door记录tag,同时记录有门的墙体，墙体点位存储顺序为角落到门
                //{
                //    GetDoorTaggedAndViewed(door, room, ref tag, ref viewable, ref doorWall, ref outdoorWall);
                //}

                tag[num - 1] = tag[0];
                viewable[num - 1] = viewable[0];

                int[] isConvexPt = new int[num];
                
                for (int i = 0; i < num - 1; ++i) // 得到每个顶点凹凸，并记录没有tag的点个数
                {
                    isConvexPt[i] = DataQueryWorker.IsConvexPoint(room, room.GetPoint3dAt(i), room.GetPoint3dAt((i + 1) % (num - 1)), room.GetPoint3dAt((i + num - 2) % (num - 1)));
                    if (tag[i] == 0)
                    {
                        ++numNotTag;
                        notTagIndex.Add(i);
                    }
                    if(viewable[i] == true)
                            ++viewedNum;
                }

                int flag = 0; //判断是否找到布点

                if (viewedNum < num - 1) // 非全可视
                {//找不可一眼见的墙体
                    for (int i = 0; i < num - 1; ++i)
                    {
                        if (viewable[i] == false && viewable[i + 1] == false)
                        {
                            Polyline tempWall = new Polyline();
                            tempWall.AddVertexAt(0, room.GetPoint2dAt(i), 0, 0, 0);
                            tempWall.AddVertexAt(1, room.GetPoint2dAt(i + 1), 0, 0, 0);
                            decorableBaseLine.Add(tempWall);
                        }
                    }
                    if (decorableBaseLine.Count > 0)
                    {
                        decorableBaseLine.OrderByDescending(o => o.Length);
                        foreach (Polyline wall in decorableBaseLine)
                        {
                            locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                            if (!locatePt.IsPositiveInfinity())
                            {
                                flag = 1;
                                break;
                            }
                        }
                    }
                    if (flag == 0)//没找到则找次一眼看不到的墙体
                    {
                        decorableBaseLine.RemoveRange(0, decorableBaseLine.Count);
                        for (int i = 0; i < num - 1; ++i)
                        {
                            if (viewable[i] == false || viewable[i + 1] == false)
                            {
                                Polyline tempWall = new Polyline();
                                tempWall.AddVertexAt(0, room.GetPoint2dAt(i), 0, 0, 0);
                                tempWall.AddVertexAt(1, room.GetPoint2dAt(i + 1), 0, 0, 0);
                                decorableBaseLine.Add(tempWall);
                            }
                        }
                    }
                    if (decorableBaseLine.Count > 0)
                    {
                        decorableBaseLine.OrderByDescending(o => o.Length);
                        foreach (Polyline wall in decorableBaseLine)
                        {
                            locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                            if (!locatePt.IsPositiveInfinity())
                            {
                                flag = 1;
                                break;
                            }
                        }
                    }
                }
                else//全可视
                {
                    for (int k = 0; k < num - 1; ++k)
                    {
                        if (tag[k] == 0 && tag[k + 1] == 0)
                        {
                            Polyline tempWall = new Polyline();
                            tempWall.AddVertexAt(0, room.GetPoint2dAt(k), 0, 0, 0);
                            tempWall.AddVertexAt(1, room.GetPoint2dAt(k + 1), 0, 0, 0);
                            decorableBaseLine.Add(tempWall);
                        }
                    }
                    if (decorableBaseLine.Count > 0)
                    {
                        decorableBaseLine.OrderByDescending(o => o.Length);
                        foreach (Polyline wall in decorableBaseLine)
                        {
                            locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                            if (!locatePt.IsPositiveInfinity())
                            {
                                flag = 1;
                                break;
                            }
                        }
                    }
                }
                if (flag == 0) // 全可视，或非全可视的情况以上方法没找到
                {
                    foreach (int index in notTagIndex) //优先找凹槽填
                    {                        
                        if (tag[(index + 1) % (num - 1)] == 0 && isConvexPt[(index + 1) % (num - 1)] == -1 && tag[(index - 1+num-1) % (num - 1)] == 0 && isConvexPt[(index - 1+num-1) % (num - 1)] == -1)
                        {
                            Polyline tempWall = new Polyline();
                            tempWall.AddVertexAt(0, room.GetPoint2dAt(index), 0, 0, 0);
                            tempWall.AddVertexAt(1, room.GetPoint2dAt((index + 1) % (num - 1)), 0, 0, 0);
                            decorableBaseLine.Add(tempWall);
                            tempWall.RemoveVertexAt(1);
                            tempWall.AddVertexAt(1, room.GetPoint2dAt((index - 1+num-1) % (num - 1)), 0, 0, 0);
                            decorableBaseLine.Add(tempWall);
                        }
                        if (decorableBaseLine.Count > 0)
                        {
                            decorableBaseLine.OrderBy(o => o.Length);
                            foreach (Polyline wall in decorableBaseLine)
                            {
                                locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                                if (!locatePt.IsPositiveInfinity())
                                {
                                    flag = 1;
                                    break;
                                }
                            }
                        }
                        
                        if (flag == 1)
                            break;
                    }
                    if (flag == 0)
                    {
                        if (outdoorWall.Count != 0)
                        {
                            outdoorWall.OrderByDescending(o => o.Length);
                            foreach (Polyline wall in outdoorWall)
                            {
                                locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                                if (!locatePt.IsPositiveInfinity())
                                {
                                    flag = 1;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            doorWall.OrderByDescending(o => o.Length);
                            foreach (Polyline wall in doorWall)
                            {
                                locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                                if (!locatePt.IsPositiveInfinity())
                                {
                                    flag = 1;
                                    break;
                                }
                            }
                        }
                        if(flag == 0)
                        {
                            for (int i = 0; i < num - 1; ++i)
                            {
                                Polyline tempWall = new Polyline();
                                tempWall.AddVertexAt(0, room.GetPoint2dAt(i), 0, 0, 0);
                                tempWall.AddVertexAt(1, room.GetPoint2dAt(i + 1), 0, 0, 0);
                                decorableBaseLine.Add(tempWall);
                            }
                        
                            if (decorableBaseLine.Count > 0)
                            {
                                decorableBaseLine.OrderByDescending(o => o.Length);
                                foreach (Polyline wall in decorableBaseLine)
                                {
                                    locatePt = DataQueryWorker.WallsFindLocation(wall, avoidencePt, reservedLength);
                                    if (!locatePt.IsPositiveInfinity())
                                    {
                                        flag = 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!locatePt.IsPositiveInfinity())
                {               
                    ans.Add(DataQueryWorker.FindVector(locatePt, room));
                }
            }
            return ans;

        }

        private DBObjectCollection FindRoomForResidentalBuildings()
        {
            var stairwell = DataQueryWorker.Rooms
                .Where(o => o.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString().Contains("楼梯间"))
                .Select(o => o.Boundary is MPolygon ? (o.Boundary as MPolygon).Shell() : o.Boundary)
                .Cast<Polyline>().ToList().ToCollection();
            var doorSet = DataQueryWorker.DoorOpenings.Select(x => x.Boundary).ToCollection();
            var rooms = DataQueryWorker.Rooms.Select(x => x.Boundary).ToCollection();
            var spatialCrossingDoorsIndex = new ThCADCoreNTSSpatialIndex(doorSet);
            var spatialRoomsIndex = new ThCADCoreNTSSpatialIndex(rooms);
            var indoorSet = new DBObjectCollection();
            var locateRoomSet = new DBObjectCollection();
            foreach (Entity staircase in stairwell) //找楼梯间相连的内门
            {
                var doors = spatialCrossingDoorsIndex.SelectCrossingPolygon(staircase);
                foreach (Entity door in doors)
                {
                    if (!DataQueryWorker.Query(door).Properties[ThExtractorPropertyNameManager.TagPropertyName].ToString().Contains("外门"))
                        indoorSet.Add(door);
                }
            }
            foreach (Entity door in indoorSet) //内门相连的房间框线
            {
                var crossingRooms = spatialRoomsIndex.SelectCrossingPolygon(door);
                foreach (Entity room in crossingRooms)
                    if (!DataQueryWorker.Query(room).Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString().Contains("楼梯间"))
                        locateRoomSet.Add(room is MPolygon ? (room as MPolygon).Shell() : room );
            }
            return locateRoomSet;
        }
        private DBObjectCollection FindRoomForPublicBuilding()
        {
            var locateRoomSet = new DBObjectCollection();
            int fireApartNum = DataQueryWorker.FireAparts.Count;
            var roomsArranged = DataQueryWorker.ClassifyByFireApart(DataQueryWorker.Rooms);
            var doorsArranged = DataQueryWorker.ClassifyByFireApart(DataQueryWorker.DoorOpenings);
            List<string> selectOrder = new List<string> { "消防电梯前室","前室", "走道", "门厅", "中庭" };
            List<List<string>> selectOrderMap = new List<List<string>>();
            DataQueryWorker.ReadRoomConfigTable();
            selectOrderMap.AddRange(selectOrder.Select(o => RoomConfigTreeService.CalRoomLst(DataQueryWorker.roomTableConfig, o)));
            foreach (string fireApartName in DataQueryWorker.FireApartMap) // 遍历每个防火分区
            {
                if (!roomsArranged.ContainsKey(fireApartName)) 
                    continue;
                var currentRoomSet = new List<ThGeometry>();
                var tempRoomSet = new List<ThGeometry>();
                currentRoomSet.AddRange(roomsArranged[fireApartName]);
                foreach (List<string> tempNameList in selectOrderMap)  //按照selectOrder里的优先级按顺序选取匹配的房间
                {
                    foreach (ThGeometry room in currentRoomSet)
                    {
                        foreach(string name in tempNameList)
                        {
                            if (room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString().Contains(name))
                            {
                                tempRoomSet.Add(room);
                            }
                        }
                    }
                    if (tempRoomSet.Count != 0)
                        break;
                }
                if (tempRoomSet.Count != 0)  //同一类房间里选面积最小的房间布点
                {
                    var polylineRoom = tempRoomSet
                        .Select(o => o.Boundary is MPolygon ? (o.Boundary as MPolygon).Shell() : o.Boundary as Polyline)
                        .ToList();
                    polylineRoom.OrderByDescending(o=>o.Area);
                    locateRoomSet.Add(polylineRoom[0]);
                }

            }
            
            return locateRoomSet;
        }

        private void GetDoorTaggedAndViewed(Polyline door, Polyline room, ref int[] tag, ref bool[] viewable, ref List<Polyline> doorWall, ref List<Polyline> outdoorWall)
        {
            int reservedLength = 250;
            int bufferValue = 0;
            double visualRange = 4000; //视程设为4米
            int num = room.NumberOfVertices;
            var bufferedDoor = door.Buffer(bufferValue).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var doorPt = DataQueryWorker.GetAvoidPoints(room, new List<Polyline> { door });
            if (doorPt.Count == 0)
                //continue;
                return;
            //记录门所在墙体
            var doorPaired = new List<KeyValuePair<int, Point3d>>();
            foreach (Point3d pt in doorPt[0])
            {
                doorPaired.Add(new KeyValuePair<int, Point3d>(DataQueryWorker.FindIndex(room, pt), pt));
            }
            doorPaired.OrderBy(o => o.Key);
            int rightIndex = -1;  //记录door为左或右的index值
            int leftIndex = -1;
            for (int i = 1; i < doorPaired.Count; ++i)
            {
                if (doorPaired[i].Key - doorPaired[i - 1].Key > 1)
                {
                    rightIndex = i - 1;
                    leftIndex = i;
                    break;
                }
            }
            if (rightIndex == -1)
            {
                rightIndex = doorPaired.Count - 1;
                leftIndex = 0;
            }
            var rightHandWall = new Polyline();  //门两侧墙
            var leftHandWall = new Polyline();
            //使门两侧墙的点位存储顺序为由角落开始
            rightHandWall.AddVertexAt(0, room.GetPoint3dAt((doorPaired[rightIndex].Key + 1) % num).ToPoint2D(), 0, 0, 0);
            rightHandWall.AddVertexAt(1, doorPaired[rightIndex].Value.ToPoint2D(), 0, 0, 0);
            leftHandWall.AddVertexAt(0, room.GetPoint3dAt(doorPaired[leftIndex].Key).ToPoint2D(), 0, 0, 0);
            leftHandWall.AddVertexAt(1, doorPaired[leftIndex].Value.ToPoint2D(), 0, 0, 0);

            Polyline walls = new Polyline();  //其他墙体顺时针存储，应为不闭合polyline
            for (int i = 0; i < num - doorPaired.Count; ++i)
            {
                walls.AddVertexAt(i, room.GetPoint3dAt((i + doorPaired[rightIndex].Key) % num).ToPoint2D(), 0, 0, 0);
            }

            bool isOutdoor = DataQueryWorker.Query(door).Properties[ThExtractorPropertyNameManager.TagPropertyName].ToString().Contains("外门");
            if (rightHandWall.Length > 2 * reservedLength)
            {
                if (isOutdoor)
                    outdoorWall.Add(rightHandWall);
                else
                    doorWall.Add(rightHandWall);
            }
            if (leftHandWall.Length > 2 * reservedLength)
            {
                if (isOutdoor)
                    outdoorWall.Add(leftHandWall);
                else
                    doorWall.Add(rightHandWall);
            }
            for (int i = 0; i < doorPaired.Count - 1; ++i)
            {
                ++tag[doorPaired[i].Key];
                ++tag[doorPaired[i + 1].Key];
            }

            for (int i = 0; i < num - 1; ++i)
            {
                foreach (Point3d pt in doorPt[0])
                {
                    if (pt.DistanceTo(room.GetPoint3dAt(i)) < visualRange)
                    {
                        viewable[i] = true;
                        break;
                    }
                }
            }
        }

    }
}
