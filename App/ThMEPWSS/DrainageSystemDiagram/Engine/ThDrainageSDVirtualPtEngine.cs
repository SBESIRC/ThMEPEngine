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
    public class ThDrainageSDVirtualPtEngine
    {
        public static List<ThToilateGJson> getVirtualPtOfGroup(Point3d supplyStart, Dictionary<string, List<ThTerminalToilate>> groupList, Dictionary<string, (string, string)> islandPair, List<ThToilateRoom> roomList, out Dictionary<ThTerminalToilate, Point3d> virtualPtDict, out Dictionary<string, List<ThTerminalToilate>> allToiInGroup)
        {
            //拿到起始点
            List<Point3d> allRoomPt = new List<Point3d>();
            roomList.ForEach(x => allRoomPt.AddRange(x.outlinePtList));
            var startPt = allRoomPt.OrderBy(x => x.DistanceTo(supplyStart)).First();

            //全空间成图
            var cost = ThDrainageSDShortestPathService.createGraphForArea(roomList, out var allPtGraph);

            ////debug drawing
            //int inf = 1000000;
            //for (int i = 0; i < cost.GetLength(0); i++)
            //{
            //    for (int j = i; j < cost.GetLength(0); j++)
            //    {
            //        if (cost[i, j] > 0 && cost[i, j] < inf)
            //        {
            //            var l = new Line(allPtGraph[i], allPtGraph[j]);
            //            DrawUtils.ShowGeometry(l, "l2graph");
            //        }
            //    }
            //}
            ///////

            //每一个点 找最短路径
            var ptDistDict = shortestPathForEachPt(roomList, allPtGraph, cost, startPt);

            //找每个组最小的
            var ptForGroup = findVirtualPtGroup(groupList, ptDistDict, islandPair, out allToiInGroup);

            //平移点位
            virtualPtDict = moveVirtualPt(groupList, ptForGroup, islandPair);

            //生成虚拟点位
            List<ThToilateGJson> virtualPtList = new List<ThToilateGJson>();
            virtualPtList.AddRange(ThDrainageSDToGJsonService.toVirtualPt(virtualPtDict));
            virtualPtList.AddRange(ThDrainageSDToGJsonService.toVirtualPt(allToiInGroup.SelectMany(x => x.Value).ToDictionary(x => x, x => x.SupplyCoolOnBranch)));

            return virtualPtList;
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

                            var path = ThDrainageSDShortestPathService.ShortestPath(cost, start, end);

                            double dist = 0;

                            var l = new Polyline();
                            l.AddVertexAt(l.NumberOfVertices, allPtGraph[path[0]].ToPoint2d(), 0, 0, 0);
                            for (int p = 1; p < path.Count; p++)
                            {
                                dist = dist + cost[path[p - 1], path[p]];
                                l.AddVertexAt(l.NumberOfVertices, allPtGraph[path[p]].ToPoint2d(), 0, 0, 0);
                            }
                            ////debug drawing
                            //DrawUtils.ShowGeometry(l, "l3shortPath", 20);

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
        private static Dictionary<string, Point3d> findVirtualPtGroup(Dictionary<string, List<ThTerminalToilate>> groupList, Dictionary<Point3d, double> ptDistDict, Dictionary<string, (string, string)> islandPair, out Dictionary<string, List<ThTerminalToilate>> allToiInGroup)
        {
            var ptForGroup = new Dictionary<string, Point3d>();
            allToiInGroup = new Dictionary<string, List<ThTerminalToilate>>();

            List<string> loopedGroup = new List<string>();

            foreach (var group in groupList)
            {
                if (loopedGroup.Contains(group.Key))
                {
                    //跳过处理过的岛
                    continue;
                }

                var ptSupply = group.Value.SelectMany(x => x.SupplyCoolOnBranch).ToList();
                if (islandPair.ContainsKey(group.Key))
                {
                    //岛
                    var keyPT = findNearPtInIsland(groupList, ptDistDict, islandPair[group.Key]);
                    ptForGroup.Add(keyPT.Key, keyPT.Value);

                    loopedGroup.Add(islandPair[group.Key].Item1);
                    loopedGroup.Add(islandPair[group.Key].Item2);

                }
                else if (ptSupply.Count > 0 && ptDistDict.ContainsKey(ptSupply[0]))
                {
                    //普通组
                    var pt = ptSupply.OrderBy(x => ptDistDict[x]).First();
                    ptForGroup.Add(group.Key, pt);
                }
                else
                {
                    //小房间
                    allToiInGroup.Add(group.Key, group.Value);
                }
            }

            return ptForGroup;

        }

        private static KeyValuePair<string, Point3d> findNearPtInIsland(Dictionary<string, List<ThTerminalToilate>> groupList, Dictionary<Point3d, double> ptDistDict, (string, string) island)
        {
            var island1 = groupList[island.Item1];
            var island2 = groupList[island.Item2];

            var pts1 = island1.SelectMany(x => x.SupplyCoolOnBranch).ToList();
            var pts2 = island2.SelectMany(x => x.SupplyCoolOnBranch).ToList();

            var matrix1 = ThDrainageSDCommonService.getGroupMatrix(pts1);

            var pts1Dict = pts1.ToDictionary(x => x, x => x.TransformBy(matrix1.Inverse()));
            pts1 = pts1Dict.OrderBy(x => x.Value.X).Select(x => x.Key).ToList();

            var pts2Dict = pts2.ToDictionary(x => x, x => x.TransformBy(matrix1.Inverse()));
            pts2 = pts2Dict.OrderBy(x => x.Value.X).Select(x => x.Key).ToList();

            var pts = new List<Point3d>();
            pts.Add(pts1.First());
            pts.Add(pts1.Last());
            pts.Add(pts2.First());
            pts.Add(pts2.Last());

            var pt = pts.OrderBy(x => ptDistDict[x]).First();
            var aaa = pts.ToDictionary(x => x, x => ptDistDict[x]);

            var tol = new Tolerance(10, 10);
            if (pts1.First().IsEqualTo(pt, tol))
            {
                if (pts1Dict[pts1.First()].X > pts2Dict[pts2.First()].X)
                {
                    pt = pts2.First();
                }
            }
            if (pts1.Last().IsEqualTo(pt, tol))
            {
                if (pts1Dict[pts1.Last()].X < pts2Dict[pts2.Last()].X)
                {
                    pt = pts2.Last();
                }
            }
            if (pts2.First().IsEqualTo(pt, tol))
            {
                if (pts2Dict[pts2.First()].X > pts1Dict[pts1.First()].X)
                {
                    pt = pts1.First();
                }
            }
            if (pts2.Last().IsEqualTo(pt, tol))
            {
                if (pts2Dict[pts2.Last()].X < pts1Dict[pts1.Last()].X)
                {
                    pt = pts1.Last();
                }
            }

            string groupName = island.Item1;

            if (pts2.Contains(pt))
            {
                groupName = island.Item2;
            }
            var keyPT = new KeyValuePair<string, Point3d>(groupName, pt);

            return keyPT;

        }

        /// <summary>
        /// key: 组主点的toilate信息， value：移动后的点位 
        /// </summary>
        /// <param name="groupList"></param>
        /// <param name="ptForGroup"></param>
        /// <returns></returns>
        private static Dictionary<ThTerminalToilate, Point3d> moveVirtualPt(Dictionary<string, List<ThTerminalToilate>> groupList, Dictionary<string, Point3d> ptForGroup, Dictionary<string, (string, string)> islandPair)
        {
            var ptForVirtualDict = new Dictionary<ThTerminalToilate, Point3d>();

            //no small room
            foreach (var virtualPt in ptForGroup)
            {
                var movedVirtualPt = moveVirtualPtInGroup(virtualPt.Value, groupList[virtualPt.Key]);
                ptForVirtualDict.Add(movedVirtualPt.Key, movedVirtualPt.Value);
            }

            return ptForVirtualDict;

        }

        /// <summary>
        /// key：移动点 value:对应的厕所信息
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="groupToilate"></param>
        /// <returns></returns>
        private static KeyValuePair<ThTerminalToilate, Point3d> moveVirtualPtInGroup(Point3d pt, List<ThTerminalToilate> groupToilate)
        {

            KeyValuePair<ThTerminalToilate, Point3d> movedPt;
            Point3d ptTemp = pt;
            Vector3d moveDir;

            var toilate = groupToilate.Where(x => x.SupplyCoolOnBranch.Contains(pt)).First();
            var pts = groupToilate.SelectMany(x => x.SupplyCoolOnBranch).ToList();
            var ptsOrder = ThDrainageSDCommonService.orderPtInStrightLine(pts);

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

                //moveDir = new Vector3d(0, 0, 0);
            }

            ptTemp = pt + moveDir * ThDrainageSDCommon.MoveDistVirtualPt;

            movedPt = new(toilate, ptTemp);

            return movedPt;
        }



    }
}
