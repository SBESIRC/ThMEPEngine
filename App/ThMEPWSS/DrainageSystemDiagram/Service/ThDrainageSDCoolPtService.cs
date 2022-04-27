using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;

using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCoolPtService
    {
        /// <summary>
        /// 找最近的墙面。
        /// 同时校核厕所块方向。如果方向放反会转过来。更改toilet. supportPt， Dir 属性
        /// </summary>
        /// <param name="roomList"></param>
        /// <param name="toiletList"></param>
        /// <param name="aloneToilet"></param>
        public static void findCoolSupplyPt(List<ThToiletRoom> roomList, List<ThTerminalToilet> toiletList, out List<ThTerminalToilet> aloneToilet)
        {
            aloneToilet = new List<ThTerminalToilet>();
            var islandToilet = new List<ThTerminalToilet>();

            foreach (var terminal in toiletList)
            {
                var room = roomList.Where(x => x.toilet.Contains(terminal));
                if (room.Count() == 0)
                {
                    aloneToilet.Add(terminal);
                }
                else
                {
                    var wallList = room.First().wallList;
                    List<Point3d> ptOnWall = findPtOnWall(wallList, terminal, ThDrainageSDCommon.TolToiletToWall, islandToilet);
                    terminal.SupplyCoolOnWall = ptOnWall;
                }
            }
            checkIslandDirection(islandToilet);
        }

        private static List<Point3d> findPtOnWall(List<Line> wallList, ThTerminalToilet terminal, int TolClosedWall, List<ThTerminalToilet> islandToilet)
        {
            var moveTol = 0.01;
            List<string> noPriority = new List<string> { "A-Toilet-3", "A-Toilet-7", "给水角阀平面" };
            List<Point3d> ptOnWall = new List<Point3d>();

            var closeWall = findNearbyWall(wallList, terminal, TolClosedWall);

            var sideWallDict = findNearestParallelWall(closeWall, terminal);

            KeyValuePair<int, Line> turnCloseWallSet;
            if (noPriority.Contains(terminal.Type))
            {
                turnCloseWallSet = findNesrestWall(sideWallDict, false);
            }
            else
            {
                turnCloseWallSet = findNesrestWall(sideWallDict, true);
            }

            //turn boundary
            if (turnCloseWallSet.Key > 0)
            {
                //不是岛的情况
                //岛也有可能反但是没有处理
                terminal.Boundary = ThDrainageSDCommonService.turnBoundary(terminal.Boundary, turnCloseWallSet.Key);
                terminal.setDir();
                terminal.setInfo();

            }

            //find point on wall
            Line closestWall = turnCloseWallSet.Value;
            if (closestWall != null)
            {
                //靠墙
                foreach (var pt in terminal.SupplyCool)
                {
                    var ptOnWallTemp = closestWall.GetClosestPointTo(pt, false);
                    ptOnWallTemp = ptOnWallTemp + terminal.Dir * moveTol;
                    ptOnWall.Add(ptOnWallTemp);
                }
                //terminal.SupplyCool.ForEach(x => ptOnWall.Add(closestWall.GetClosestPointTo(x, false)));
            }
            else
            {   //岛，有可能反的
                terminal.SupplyCool.ForEach(x => ptOnWall.Add(x));
                islandToilet.Add(terminal);
            }

            return ptOnWall;
        }

        /// <summary>
        /// 返回boundary边中心点 对应的平行的最近的墙。如果没有就是null
        /// </summary>
        /// <param name="closeWallList"></param>
        /// <returns></returns>
        private static Dictionary<Point3d, Line> findNearestParallelWall(Dictionary<Point3d, List<Line>> closeWallList, ThTerminalToilet terminal)
        {
            var tol = new Tolerance(10, 10);
            var sideWallDict = new Dictionary<Point3d, Line>();

            for (int i = 0; i < closeWallList.Count; i++)
            {
                var wallList = closeWallList.ElementAt(i).Value;
                var midP = closeWallList.ElementAt(i).Key;
                //var sideDir = (closeWallList.ElementAt((i + 2) % 4).Key - closeWallList.ElementAt((i) % 4).Key).GetNormal();
                var sideDir = (terminal.Boundary.GetPoint3dAt((i) % 4) - terminal.Boundary.GetPoint3dAt((i + 1) % 4)).GetNormal();
                sideWallDict.Add(midP, null);
                var parallelWall = new List<Line>();

                if (wallList.Count > 0)
                {
                    foreach (var wall in wallList)
                    {
                        var ptOnWall = wall.GetClosestPointTo(midP, true);
                        var ptToWallDir = (midP - ptOnWall).GetNormal();
                        var angle = ptToWallDir.GetAngleTo(sideDir);

                        if (Math.Abs(Math.Cos(angle)) >= Math.Cos(10 * Math.PI / 180))
                        {
                            parallelWall.Add(wall);
                        }
                    }
                }

                if (parallelWall.Count > 0)
                {
                    var parallelWallDistDict = parallelWall.ToDictionary(x => x, x => x.GetDistToPoint(midP, false));
                    parallelWallDistDict = parallelWallDistDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    sideWallDict[midP] = parallelWallDistDict.First().Key;
                }
            }

            return sideWallDict;
        }

        /// <summary>
        /// 根究两边优先或者四边优先找到boundary中心点和对应的最近的墙。
        /// key int：boundary中心点的index，同时也是boundary顺时针转点数
        /// 如果返回 int = -1， 则找不到最近墙。判定为岛。
        /// </summary>
        /// <param name="sideWallDict"></param>
        /// <returns></returns>
        private static KeyValuePair<int, Line> findNesrestWall(Dictionary<Point3d, Line> sideWallDict, bool topBottomPriority)
        {
            Dictionary<int, double> distDict = new Dictionary<int, double>();
            var re = new KeyValuePair<int, Line>(-1, null);
            int turnIndex = -1;

            for (int i = 0; i < sideWallDict.Count; i++)
            {
                var item = sideWallDict.ElementAt(i);
                distDict.Add(i, -1);
                if (item.Value != null)
                {
                    distDict[i] = item.Value.GetDistToPoint(item.Key, false);
                }
            }

            var distDictNoIsland = distDict.Where(x => x.Value != -1).ToDictionary(x => x.Key, x => x.Value);

            if (distDictNoIsland.Count > 0)
            {
                if (topBottomPriority == false)
                {
                    turnIndex = distDictNoIsland.OrderBy(x => x.Value).First().Key;
                }
                else
                {
                    var topBottom = distDictNoIsland.Where(x => x.Key == 0 || x.Key == 2).ToDictionary(x => x.Key, x => x.Value);
                    if (topBottom.Count > 0)
                    {
                        turnIndex = topBottom.OrderBy(x => x.Value).First().Key;
                    }
                    else
                    {
                        var leftRight = distDictNoIsland.Where(x => x.Key == 1 || x.Key == 3).ToDictionary(x => x.Key, x => x.Value);
                        if (leftRight.Count > 0)
                        {
                            turnIndex = leftRight.OrderBy(x => x.Value).First().Key;
                        }
                    }
                }
            }

            if (turnIndex != -1)
            {
                re = new KeyValuePair<int, Line>(turnIndex, sideWallDict.ElementAt(turnIndex).Value);
            }

            return re;
        }

        /// <summary>
        /// dictionary顺序：boundary边中心： 上，右，下，左, value：boundary边800内的墙（有可能不平行）
        /// </summary>
        /// <param name="wallList"></param>
        /// <param name="terminal"></param>
        /// <param name="TolClosedWall"></param>
        /// <returns></returns>
        private static Dictionary<Point3d, List<Line>> findNearbyWall(List<Line> wallList, ThTerminalToilet terminal, int TolClosedWall)
        {
            Dictionary<Point3d, List<Line>> closeWall = new Dictionary<Point3d, List<Line>>();

            if (wallList.Count > 0)
            {
                var ptTop = virtualSupplyPt(terminal.Boundary, 0, terminal.Type);
                var ptRight = virtualSupplyPt(terminal.Boundary, 1, terminal.Type);
                var ptBottom = virtualSupplyPt(terminal.Boundary, 2, terminal.Type);
                var ptLeft = virtualSupplyPt(terminal.Boundary, 3, terminal.Type);

                closeWall.Add(ptTop, new List<Line> { });
                closeWall.Add(ptRight, new List<Line> { });
                closeWall.Add(ptBottom, new List<Line> { });
                closeWall.Add(ptLeft, new List<Line> { });

                var tol = new Tolerance(10, 10);

                foreach (var wall in wallList)
                {
                    var ptLeftWall = wall.GetClosestPointTo(ptLeft, true);
                    var ptRightWall = wall.GetClosestPointTo(ptRight, true);
                    var ptTopWall = wall.GetClosestPointTo(ptTop, true);
                    var ptBottomWall = wall.GetClosestPointTo(ptBottom, true);

                    if (ptLeft.DistanceTo(ptLeftWall) <= TolClosedWall && wall.ToCurve3d().IsOn(ptLeftWall, tol))
                    {
                        closeWall[ptLeft].Add(wall);
                    }
                    if (ptRight.DistanceTo(ptRightWall) <= TolClosedWall && wall.ToCurve3d().IsOn(ptRightWall, tol))
                    {
                        closeWall[ptRight].Add(wall);
                    }
                    if (ptTop.DistanceTo(ptTopWall) <= TolClosedWall && wall.ToCurve3d().IsOn(ptTopWall, tol))
                    {
                        closeWall[ptTop].Add(wall);
                    }
                    if (ptBottom.DistanceTo(ptBottomWall) <= TolClosedWall && wall.ToCurve3d().IsOn(ptBottomWall, tol))
                    {
                        closeWall[ptBottom].Add(wall);
                    }
                }
            }

            return closeWall;
        }

        private static void checkIslandDirection(List<ThTerminalToilet> islandToi)
        {
            groupIslandDirection(islandToi, out var islandGroup, out var islandGroupPair);

            var needTurn = new List<ThTerminalToilet>();
            var passed = new List<int>();
            foreach (var pair in islandGroupPair)
            {
                var i1 = islandGroup.TryGetValue(pair.Key, out var group1);
                var i2 = islandGroup.TryGetValue(pair.Value, out var group2);

                if (passed.Contains(pair.Key) == false && i1 && i2 && group1.Count > 0 && group2.Count > 0)
                {
                    var item1 = group1.First();
                    var item2 = group2.OrderBy(x => x.Boundary.GetCenter().DistanceTo(item1.Boundary.GetCenter())).First();

                    var top1 = midPoint(item1.Boundary.GetPoint3dAt(1), item1.Boundary.GetPoint3dAt(2));
                    var bottom1 = midPoint(item1.Boundary.GetPoint3dAt(0), item1.Boundary.GetPoint3dAt(3));
                    var top2 = midPoint(item2.Boundary.GetPoint3dAt(1), item2.Boundary.GetPoint3dAt(2));
                    var bottom2 = midPoint(item2.Boundary.GetPoint3dAt(0), item2.Boundary.GetPoint3dAt(3));

                    var distDict = new Dictionary<int, double>();
                    distDict.Add(0, top1.DistanceTo(top2));
                    distDict.Add(1, top1.DistanceTo(bottom2));
                    distDict.Add(2, bottom1.DistanceTo(top2));
                    distDict.Add(3, bottom1.DistanceTo(bottom2));

                    var close = distDict.OrderBy(x => x.Value).First();
                    if (close.Key == 0)
                    {
                        findNotDirInGroup(item1, group1, needTurn);
                        findNotDirInGroup(item2, group2, needTurn);
                    }
                    if (close.Key == 1)
                    {
                        //item1方向对 item2方向错
                        findDirInGroup(item2, group2, needTurn);
                        findNotDirInGroup(item1, group1, needTurn);
                    }
                    if (close.Key == 2)
                    {
                        //item1方向错 item2方向对
                        findDirInGroup(item1, group1, needTurn);
                        findNotDirInGroup(item2, group2, needTurn);
                    }
                    if (close.Key == 3)
                    {
                        findDirInGroup(item2, group2, needTurn);
                        findDirInGroup(item1, group1, needTurn);
                    }
                    passed.Add(pair.Key);
                    passed.Add(pair.Value);
                }
            }

            //转island boundary
            foreach (var terminal in needTurn)
            {
                terminal.Boundary = ThDrainageSDCommonService.turnBoundary(terminal.Boundary, 2);
                terminal.setDir();
                terminal.setInfo();
                terminal.SupplyCoolOnWall = terminal.SupplyCool;
            }
        }

        private static void findDirInGroup(ThTerminalToilet item, List<ThTerminalToilet> group, List<ThTerminalToilet> needTurn)
        {
            needTurn.Add(item);
            var sameDirInGroup = group.Where(x =>
            {
                var bReturn = false;
                var angle = x.Dir.GetAngleTo(item.Dir);
                if (Math.Cos(angle) >= Math.Cos(5 * Math.PI / 180) && x != item)
                {
                    bReturn = true;
                }
                return bReturn;

            });
            needTurn.AddRange(sameDirInGroup);
        }

        private static void findNotDirInGroup(ThTerminalToilet item, List<ThTerminalToilet> group, List<ThTerminalToilet> needTurn)
        {
            var notSameDirInGroup = group.Where(x =>
            {
                var bReturn = false;
                var angle = x.Dir.GetAngleTo(item.Dir);
                if (Math.Cos(angle) < Math.Cos(5 * Math.PI / 180) && x != item)
                {
                    bReturn = true;
                }
                return bReturn;

            });
            needTurn.AddRange(notSameDirInGroup);
        }

        /// <summary>
        /// UCS不是xy时可能有bug
        /// </summary>
        /// <param name="islandToi"></param>
        /// <param name="islandGroup"></param>
        /// <param name="islandGroupPair"></param>
        private static void groupIslandDirection(List<ThTerminalToilet> islandToi, out Dictionary<int, List<ThTerminalToilet>> islandGroup, out Dictionary<int, int> islandGroupPair)
        {
            var islandTol = 2500;
            islandGroup = new Dictionary<int, List<ThTerminalToilet>>();
            islandGroupPair = new Dictionary<int, int>();
            islandToi = islandToi.OrderBy(x => x.Boundary.GetCenter().X).ToList();

            for (int i = 0; i < islandToi.Count; i++)
            {
                var island = islandGroup.Where(x => x.Value.Where(y => y.Boundary.GetCenter().DistanceTo(islandToi[i].Boundary.GetCenter()) <= islandTol).Count() > 0);

                if (island.Count() > 0)
                {
                    var closeIsland = island.SelectMany(x => x.Value).OrderBy(y => y.Boundary.GetCenter().DistanceTo(islandToi[i].Boundary.GetCenter())).First();

                    var isSameGroup = checkTwoIslandInSameGroup(islandToi[i], closeIsland);

                    if (isSameGroup == true)
                    {
                        var index = island.Where(x => x.Value.Contains(closeIsland)).First().Key;
                        islandGroup[index].Add(islandToi[i]);
                    }
                    else
                    {
                        var index = -1;
                        var oppoSide = island.Where(x => x.Value.Contains(closeIsland));

                        if (oppoSide.Count() > 0)
                        {
                            if (islandGroupPair.ContainsKey(oppoSide.First().Key))
                            {
                                index = islandGroupPair[oppoSide.First().Key];
                            }
                        }

                        if (index == -1)
                        {
                            index = islandGroup.Count == 0 ? 0 : islandGroup.Last().Key + 1;
                            islandGroupPair.Add(island.First().Key, index);
                            islandGroupPair.Add(index, island.First().Key);
                            islandGroup.Add(index, new List<ThTerminalToilet> { });
                        }

                        islandGroup[index].Add(islandToi[i]);
                    }
                }
                else
                {
                    var index = islandGroup.Count == 0 ? 0 : islandGroup.Last().Key + 1;
                    islandGroup.Add(index, new List<ThTerminalToilet> { islandToi[i] });
                }
            }
        }


        private static bool checkTwoIslandInSameGroup(ThTerminalToilet item1, ThTerminalToilet item2)
        {
            var bReturn = false;

            var top1 = midPoint(item1.Boundary.GetPoint3dAt(1), item1.Boundary.GetPoint3dAt(2));
            var bottom1 = midPoint(item1.Boundary.GetPoint3dAt(0), item1.Boundary.GetPoint3dAt(3));
            var left1 = midPoint(item1.Boundary.GetPoint3dAt(0), item1.Boundary.GetPoint3dAt(1));
            var right1 = midPoint(item1.Boundary.GetPoint3dAt(2), item1.Boundary.GetPoint3dAt(3));

            var top2 = midPoint(item2.Boundary.GetPoint3dAt(1), item2.Boundary.GetPoint3dAt(2));
            var bottom2 = midPoint(item2.Boundary.GetPoint3dAt(0), item2.Boundary.GetPoint3dAt(3));
            var left2 = midPoint(item2.Boundary.GetPoint3dAt(0), item2.Boundary.GetPoint3dAt(1));
            var right2 = midPoint(item2.Boundary.GetPoint3dAt(2), item2.Boundary.GetPoint3dAt(3));

            var distDict = new Dictionary<int, double>();
            distDict.Add(0, top1.DistanceTo(top2));
            distDict.Add(1, top1.DistanceTo(bottom2));
            distDict.Add(2, bottom1.DistanceTo(top2));
            distDict.Add(3, bottom1.DistanceTo(bottom2));
            distDict.Add(4, left1.DistanceTo(left2));
            distDict.Add(5, left1.DistanceTo(right2));
            distDict.Add(6, right1.DistanceTo(left2));
            distDict.Add(7, right1.DistanceTo(right2));

            var sortIdx = distDict.OrderBy(x => x.Value).First().Key;

            if (sortIdx >= 4)
            {
                bReturn = true;
            }

            return bReturn;
        }

        private static Point3d virtualSupplyPt(Polyline boundary, int turn, string type)
        {

            var pl = ThDrainageSDCommonService.turnBoundary(boundary, turn);
            var pt = ThTerminalToilet.CalculateSupplyCoolPoint(type, pl);

            return pt.First();
        }

        private static Point3d midPoint(Point3d pt1, Point3d pt2)
        {
            var midPt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            return midPt;
        }
    }
}

