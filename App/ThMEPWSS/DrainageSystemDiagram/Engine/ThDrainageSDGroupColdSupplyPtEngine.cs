using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;




namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDGroupColdSupplyPtEngine
    {
        public static List<ThToilateGJson> getVirtualPtOfGroup(Point3d supplyStart, Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList, List<ThToilateRoom> roomList, out Dictionary<ThIfcSanitaryTerminalToilate, Point3d> virtualPtDict)
        {
            //拿到起始点
            List<Point3d> allRoomPt = new List<Point3d>();
            roomList.ForEach(x => allRoomPt.AddRange(x.outlinePtList));
            var startPt = allRoomPt.OrderBy(x => x.DistanceTo(supplyStart)).First();

            //全空间成图
            var cost = createGraphForArea(roomList, out var allPtGraph);

            ////debug drawing
            int inf = 1000000;
            for (int i = 0; i < cost.GetLength(0); i++)
            {
                for (int j = i; j < cost.GetLength(0); j++)
                {
                    if (cost[i, j] > 0 && cost[i, j] < inf)
                    {
                        var l = new Line(allPtGraph[i], allPtGraph[j]);
                        DrawUtils.ShowGeometry(l, "l2graph");
                    }
                }
            }
            ///////

            //每一个点 找最短路径
            var ptDistDict = shortestPathForEachPt(roomList, allPtGraph, cost, startPt);

            //找每个组最小的
            var ptForGroup = findNearPtInGroup(groupList, ptDistDict, out var allToiInGroup);

            //平移点位
            virtualPtDict = displacePtOfGroup(groupList, ptForGroup);

            //生成虚拟点位
            List<ThToilateGJson> virtualPtList = new List<ThToilateGJson>();
            virtualPtList.AddRange(ThDrainageSDToGJsonService.toVirtualPt(virtualPtDict));
            virtualPtList.AddRange(ThDrainageSDToGJsonService.toVirtualPt(allToiInGroup.SelectMany(x => x.Value).ToDictionary(x => x, x => x.SupplyCoolOnBranch)));

            return virtualPtList;
        }

        private static double[,] createGraphForArea(List<ThToilateRoom> roomList, out List<Point3d> allPtGraph)
        {
            allPtGraph = new List<Point3d>();
            double[,] cost = null;

            var roomPlList = roomList.Select(x => x.outline).ToList();
            var room0 = roomList[0];
            cost = createGraphForRoom(room0, out var ptGraph0);
            allPtGraph.AddRange(ptGraph0);
            for (int i = 1; i < roomList.Count; i++)
            {
                var room1 = roomList[i];
                var cost1 = createGraphForRoom(room1, out var ptGraph1);

                cost = ThDrainageSDColdPtProcessService.mergeCost(cost, allPtGraph, cost1, ptGraph1, roomPlList);

                allPtGraph.AddRange(ptGraph1);
            }

            return cost;
        }

        /// <summary>
        /// 点，点的最短路径距离
        /// </summary>
        /// <param name="roomList"></param>
        /// <param name="allPtGraph"></param>
        /// <param name="cost"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private static Dictionary<Point3d, double> shortestPathForEachPt(List<ThToilateRoom> roomList, List<Point3d> allPtGraph, double[,] cost, Point3d startPt)
        {
            int start = allPtGraph.IndexOf(startPt);

            Dictionary<Point3d, double> ptDistDict = new Dictionary<Point3d, double>();
            foreach (var room in roomList)
            {
                if (room.type == 1)
                {
                    for (int i = 0; i < room.toilate.Count; i++)
                    {
                        for (int j = 0; j < room.toilate[i].SupplyCoolOnBranch.Count; j++)
                        {
                            int end = allPtGraph.IndexOf(room.toilate[i].SupplyCoolOnBranch[j]);

                            var path = ThDrainageSDColdPtProcessService.ShortestPath(cost, start, end);

                            double dist = 0;
                            var l = new Polyline();
                            l.AddVertexAt(l.NumberOfVertices, allPtGraph[path[0]].ToPoint2d(), 0, 0, 0);
                            for (int p = 1; p < path.Count; p++)
                            {
                                dist = dist + cost[path[p - 1], path[p]];
                                //debug drawing
                                l.AddVertexAt(l.NumberOfVertices, allPtGraph[path[p]].ToPoint2d(), 0, 0, 0);
                            }
                            //debug drawing
                            DrawUtils.ShowGeometry(l, "l3shortPath", 20);

                            ptDistDict.Add(room.toilate[i].SupplyCoolOnBranch[j], dist);
                        }
                    }
                }
            }
            return ptDistDict;
        }

        /// <summary>
        /// key: 组名， value：最近点位
        /// allToiInGroup :  直接用supplyOnWall点位（小空间）
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="ptDistDict"></param>
        /// <returns></returns>
        private static Dictionary<string, Point3d> findNearPtInGroup(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList, Dictionary<Point3d, double> ptDistDict, out Dictionary<string, List<ThIfcSanitaryTerminalToilate>> allToiInGroup)
        {
            var ptForGroup = new Dictionary<string, Point3d>();
            allToiInGroup = new Dictionary<string, List<ThIfcSanitaryTerminalToilate>>();

            foreach (var group in groupList)
            {
                var ptSupply = group.Value.SelectMany(x => x.SupplyCoolOnBranch).ToList();
                if (ptSupply.Count > 0 && ptDistDict.ContainsKey(ptSupply[0]))
                {
                    var pt = ptSupply.OrderBy(x => ptDistDict[x]).First();
                    ptForGroup.Add(group.Key, pt);
                }
                else
                {
                    allToiInGroup.Add(group.Key, group.Value);
                }
            }

            return ptForGroup;

        }

        /// <summary>
        /// key: 组主点的toilate信息， value：移动后的点位 
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="ptForGroup"></param>
        /// <returns></returns>
        private static Dictionary<ThIfcSanitaryTerminalToilate, Point3d> displacePtOfGroup(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList, Dictionary<string, Point3d> ptForGroup)
        {
            var ptForVirtualDict = new Dictionary<ThIfcSanitaryTerminalToilate, Point3d>();

            foreach (var group in ptForGroup)
            {
                var virtualPt = moveVirtualPtOfGroup(group.Value, groupList[group.Key]);
                ptForVirtualDict.Add(virtualPt.Key, virtualPt.Value);
            }

            return ptForVirtualDict;

        }

        /// <summary>
        /// key：移动点 value:对应的厕所信息
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="groupToilate"></param>
        /// <returns></returns>
        private static KeyValuePair<ThIfcSanitaryTerminalToilate, Point3d> moveVirtualPtOfGroup(Point3d pt, List<ThIfcSanitaryTerminalToilate> groupToilate)
        {
          
            KeyValuePair<ThIfcSanitaryTerminalToilate, Point3d> movedPt;
            Point3d ptTemp = pt;
            Vector3d moveDir;

            var toilate = groupToilate.Where(x => x.SupplyCoolOnBranch.Contains(pt)).First();
            var ptsOrder = ThDrainageSDColdPtProcessService.orderSupplyPtInGroup(groupToilate);

            if (ptsOrder.Count > 1)
            {
                ////组里面两个点以上
                var ptIdx = ptsOrder.IndexOf(pt);
                if (ptIdx == 0 || ptIdx == ptsOrder.Count - 1)
                {
                    //主点在两边
                    var otherPtInGroup = ptsOrder.Where(x => x.IsEqualTo(pt, new Tolerance(10, 10)) == false).First();
                    moveDir = -(otherPtInGroup - pt).GetNormal();
                }
                else
                {
                    //主点在中间,向中间方向偏移
                    var toFirst = pt.DistanceTo(ptsOrder.First());
                    var toLast = pt.DistanceTo(ptsOrder.Last());

                    if (toFirst < toLast)
                    {
                        moveDir = (ptsOrder.Last() - pt).GetNormal();
                    }
                    else
                    {
                        moveDir = (ptsOrder.First() - pt).GetNormal();
                    }
                }
            }
            else
            {
                //只有一个点
                //马桶方向左边
                var dir = toilate.Dir;
                moveDir = dir.RotateBy(90 * Math.PI / 180, -Vector3d.ZAxis);
            }

            ptTemp = pt + moveDir * DrainageSDCommon .MovedLength;

            movedPt = new(toilate, ptTemp);

            return movedPt;
        }

        private static double[,] createGraphForRoom(ThToilateRoom room, out List<Point3d> ptGraph)
        {
            ptGraph = new List<Point3d>();
            var roomPt = room.outlinePtList;

            ptGraph.AddRange(roomPt);
            ptGraph.AddRange(room.toilate.SelectMany(x => x.SupplyCoolOnBranch));

            var cost = ThDrainageSDColdPtProcessService.createGraph(ptGraph, room.outline);

            return cost;

        }

    }
}
