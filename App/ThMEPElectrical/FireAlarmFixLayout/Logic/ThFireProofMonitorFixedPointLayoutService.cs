﻿using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;

using ThMEPElectrical.FireAlarmFixLayout.Data;

namespace ThMEPElectrical.FireAlarmFixLayout.Logic
{
    public class ThFireProofMonitorFixedPointLayoutService
    {
        /// <summary>
        /// 读取房间配置表设置
        /// </summary>
        static string evacuationConfigUrl = ThCADCommon.SupportPath() + "\\疏散方向判断.xlsx";
        public List<List<string>> MonitorRoomNameConfigMap = new List<List<string>>();       //
        protected Dictionary<string, int> routeMap = new Dictionary<string, int>();  //房间名编号
        protected List<Dictionary<int, List<int>>> ConfigRouteMap = new List<Dictionary<int, List<int>>>
                                            { new Dictionary<int, List<int>>(), new Dictionary<int, List<int>> (),
                                             new Dictionary<int, List<int>> (), new Dictionary<int, List<int>> ()};  //不同楼层路径优先级

        //public ThFireProofMonitorFixedPointLayoutService(List<ThGeometry> data, List<string> CleanBlkName, List<string> AvoidBlkName) : base(data, CleanBlkName, AvoidBlkName)
        //{
        //}

        protected ThAFASFixDataQueryService DataQueryWorker;

        public ThFireProofMonitorFixedPointLayoutService(ThAFASFixDataQueryService dataQueryWorker)
        {
            DataQueryWorker = dataQueryWorker;
        }

        //public ThMEPEngineCore.Algorithm.ThMEPOriginTransformer Transformer { get; set; }

        public List<KeyValuePair<Point3d, Vector3d>> Layout()
        {

            //List<int[][]> Priority = new List<int[][]> { undergroundPriority, firstFloorPriority, higherLevelPriority , roof};

            int priKey = 0;  //地下优先级
            if (DataQueryWorker.FloorTag.Equals(""))
                priKey = 3;  //大屋面优先级
            else if (DataQueryWorker.FloorTag[0].Equals('1'))
                priKey = 1;  // 一层
            else if (!DataQueryWorker.FloorTag[0].Equals('B'))
                priKey = 2;   //二层及以上

            SetConfigTableForMonitor();

            //取与防火门相邻房间
            var fireDoorSet = DataQueryWorker.GetFireDoors().Select(x => x.Boundary).ToCollection();
            var roomSet = DataQueryWorker.Rooms.Select(x => x.Boundary).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(roomSet);
            List<KeyValuePair<Point3d, Vector3d>> results = new List<KeyValuePair<Point3d, Vector3d>>();
            double bufferValue = 10;


            //判断相邻房间优先级确定点位
            foreach (Polyline door in fireDoorSet)
            {
                var bufferedDoor = door.Buffer(bufferValue).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                var crossingRooms = spatialIndex.SelectCrossingPolygon(bufferedDoor); //DBObject

                if (crossingRooms.Count < 2)
                    continue;
                List<string> crossingRoomName = new List<string>();
                crossingRoomName.AddRange(crossingRooms
                    .Cast<Entity>()
                    .Select(o => DataQueryWorker.Query(o))
                    .Select(o => o.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString()));
                var roomNameA = crossingRoomName[0];
                var roomNameB = crossingRoomName[1];
                int IndexA = -1;//判断该防火门是否需要布点,并记录房间所属编号
                int IndexB = -1;
                for (int i = 0; i < MonitorRoomNameConfigMap.Count; ++i)
                {
                    foreach (string o in MonitorRoomNameConfigMap[i])
                    {
                        //if (o.Contains(roomNameA))
                        if (RoomConfigTreeService.CompareRoom(roomNameA, o))
                        {
                            IndexA = i;
                            break;
                        }
                    }
                    if (IndexA != -1)
                    {
                        break;
                    }
                }
                if (IndexA == -1)
                    continue;

                for (int i = 0; i < MonitorRoomNameConfigMap.Count; ++i)
                {
                    foreach (string o in MonitorRoomNameConfigMap[i])
                    {
                        // if (o.Contains(roomNameB))
                        if (RoomConfigTreeService.CompareRoom(roomNameB, o))
                        {
                            IndexB = i;
                            break;
                        }
                    }
                    if (IndexB != -1)
                    {
                        break;
                    }
                }
                if (IndexB == -1)
                    continue;

                int locateNum = 0;
                if (ConfigRouteMap[priKey].ContainsKey(IndexA) && ConfigRouteMap[priKey][IndexA].IndexOf(IndexB) != -1)
                    locateNum = 1;

                var locateRoom = crossingRooms[locateNum] as Polyline;
                var interPt = ThCADCoreNTSEntityExtension.Intersection(locateRoom, door, false).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                var ptNum = interPt.NumberOfVertices;
                if (ptNum == 0)
                    continue;
                var sum = interPt.GetPoint3dAt(0);
                for (int i = 1; i < ptNum; ++i)
                {
                    sum = interPt.GetPoint3dAt(i) + sum.GetAsVector();
                }
                var avg = sum / ptNum;
                var ansPoint = locateRoom.GetClosestPointTo(avg, false);
                if (locateRoom.IsCCW())
                    locateRoom.ReverseCurve(); //房间框线转为顺时针
                results.Add(DataQueryWorker.FindVector(ansPoint, locateRoom));  //返回点位和方向
            }
            return results;
        }


        public void SetConfigTableForMonitor()
        {
            ReadEvacuationConfig();
            //DataQueryWorker.roomTableConfig = ThFireAlarmUtils.ReadRoomConfigTable(DataQueryWorker.roomConfigUrl);
            MonitorRoomNameConfigMap
                .AddRange(routeMap.Keys.Select(o => RoomConfigTreeService.CalRoomLst(DataQueryWorker.RoomTableConfig, o)));
        }
        /// <summary>
        /// 读取疏散方向判断表
        /// </summary>
        private void ReadEvacuationConfig()
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(evacuationConfigUrl, true);
            string roomAColumn = "房间A";
            string roomBColumn = "房间B";
            string floorColumn = "楼层编号的形式（黑字为固定的部分）";
            int index = 0;
            foreach (System.Data.DataTable table in dataSet.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    string roomA = row[roomAColumn].ToString();
                    string roomB = row[roomBColumn].ToString();
                    string floor = row[floorColumn].ToString();
                    int a = -1;
                    int b = -1;
                    int floorIndex = -1;
                    if (floor[0] == 'B')  //地下
                        floorIndex = 0;
                    else if (floor[0] == '1')   //一楼
                        floorIndex = 1;
                    else if (floor[0] == 'M') //二层以上
                        floorIndex = 2;
                    else if (floor[0] == '大')  //大屋面
                        floorIndex = 3;
                    if (!routeMap.ContainsKey(roomA))
                    {
                        routeMap.Add(roomA, index);
                        a = index++;
                    }
                    else
                    {
                        a = routeMap[roomA];
                    }
                    if (!routeMap.ContainsKey(roomB))
                    {
                        routeMap.Add(roomB, index);
                        b = index++;
                    }
                    else
                    {
                        b = routeMap[roomB];
                    }
                    if (!ConfigRouteMap[floorIndex].ContainsKey(a))
                    {
                        ConfigRouteMap[floorIndex].Add(a, new List<int> { b });
                    }
                    else if (ConfigRouteMap[floorIndex][a].IndexOf(b) == -1)
                    {
                        ConfigRouteMap[floorIndex][a].Add(b);
                    }
                }
            }
        }
    }
}
