using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    class graphService
    {
        //此逻辑只适用于最外环
        public static void createOutterGraph(List<List<(Line, int)>> orderedLaneSideList, Dictionary<int, List<(Line, int)>> sideDict, List<ThSingleSideBlocks> singleSideBlocks, out ThLaneSideGraph sideGraph)
        {
            sideGraph = new ThLaneSideGraph();

            foreach (var pair in sideDict)
            {
                sideGraph.AddVertex(pair.Key);
            }

            linkSameRingGraph(orderedLaneSideList, sideDict, sideGraph);

            //不同环之间的关系
            for (int i = 0; i < orderedLaneSideList.Count - 1; i++)
            {
                var closeLaneSide = IsLaneSideListToRing(orderedLaneSideList[i], orderedLaneSideList[i + 1], singleSideBlocks);

                var fromIndex = closeLaneSide.Item1;
                var toIndex = closeLaneSide.Item2;

                if (fromIndex != -1 && toIndex != -1 && fromIndex != toIndex)
                {
                    sideGraph.AddEdge(fromIndex, toIndex);
                }

            }
        }

        private static void linkSameRingGraph(List<List<(Line, int)>> orderedLaneSideList, Dictionary<int, List<(Line, int)>> sideDict, ThLaneSideGraph sideGraph)
        {
            for (int i = 0; i < orderedLaneSideList.Count; i++)
            {
                for (int j = 0; j < orderedLaneSideList[i].Count; j++)
                {
                    var fromIndex = -1;
                    var toIndex = -1;
                    if (j != orderedLaneSideList[i].Count - 1)
                    {
                        fromIndex = findLaneSideNo(orderedLaneSideList[i][j], sideDict);
                        toIndex = findLaneSideNo(orderedLaneSideList[i][j + 1], sideDict);
                    }
                    else
                    {
                        fromIndex = findLaneSideNo(orderedLaneSideList[i][j], sideDict);
                        toIndex = findLaneSideNo(orderedLaneSideList[i][0], sideDict);
                    }

                    if (fromIndex != toIndex)
                    {
                        sideGraph.AddEdge(fromIndex, toIndex);
                    }
                }
            }
        }

        /// <summary>
        ///找 ring1 和 ring2 相邻的车道边。 返回（ring1 边的 laneSideNo, ring2 边的laneSideNo)
        /// </summary>
        /// <param name="firstRing"></param>
        /// <param name="secondRing"></param>
        /// <param name="singleSideBlocks"></param>
        /// <returns></returns>
        private static (int, int) IsLaneSideListToRing(List<(Line, int)> firstRing, List<(Line, int)> secondRing, List<ThSingleSideBlocks> singleSideBlocks)
        {
            var firstRingBlocks = new List<ThSingleSideBlocks>();
            var secondRingBlocks = new List<ThSingleSideBlocks>();

            firstRing.ForEach(x =>
                    {
                        firstRingBlocks.AddRange(singleSideBlocks.Where(y => y.laneSide.Contains(x)).Distinct().ToList());
                    });

            secondRing.ForEach(x =>
                    {
                        secondRingBlocks.AddRange(singleSideBlocks.Where(y => y.laneSide.Contains(x)).Distinct().ToList());
                    });

            var firstRingItem = firstRingBlocks.SelectMany(each => each.mainBlk).ToList();
            var secRingItem = secondRingBlocks.SelectMany(each => each.mainBlk).ToList();

            double minDist = EmgConnectCommon.TolSaperateGroupMaxDistance;
            (Point3d, Point3d) ringBlock = (new Point3d(), new Point3d());
            var closeSide = (-1, -1);

            if (firstRingItem != null && secRingItem != null)
            {
                for (int i = 0; i < firstRingItem.Count; i++)
                {
                    for (int j = 0; j < secRingItem.Count; j++)
                    {
                        var dist = (firstRingItem[i] - secRingItem[j]).Length;
                        if (dist < minDist)
                        {
                            ringBlock = (firstRingItem[i], secRingItem[j]);
                            minDist = dist;
                        }
                    }
                }

                if (ringBlock.Item1 != Point3d.Origin && ringBlock.Item2 != Point3d.Origin)
                {
                    var firstRingIndex = singleSideBlocks.Where(x => x.mainBlk.Contains(ringBlock.Item1)).First().laneSideNo;
                    var secRingIndex = singleSideBlocks.Where(x => x.mainBlk.Contains(ringBlock.Item2)).First().laneSideNo;

                    closeSide = (firstRingIndex, secRingIndex);

                }
            }

            return closeSide;

        }


        private static int findLaneSideNo((Line, int) laneSide, Dictionary<int, List<(Line, int)>> sideDict)
        {
            int index = -1;

            index = sideDict.Where(x => x.Value.Contains(laneSide)).FirstOrDefault().Key;

            return index;
        }

        public static void createInnerGraph(Dictionary<int, List<List<(Line, int)>>> orderedAllLaneSideList, Dictionary<int, List<(Line, int)>> sideDict, ThLaneSideGraph sideGraph)
        {
            for (int levelIndex = 1; levelIndex < orderedAllLaneSideList.Count; levelIndex++)
            {
                var orderedLaneSideList = orderedAllLaneSideList.ElementAt(levelIndex).Value;

                //同一个环内关系
                linkSameRingGraph(orderedLaneSideList, sideDict, sideGraph);

                Dictionary<int, int> alreadyLinked = new Dictionary<int, int>();

                //不同环之间的关系
                for (int i = 0; i < orderedLaneSideList.Count; i++)
                {
                    var thisLane = orderedLaneSideList[i];

                    var index = findNeighbor(thisLane, orderedLaneSideList, alreadyLinked, i);

                    if (index.Item1 != -1)
                    {
                        alreadyLinked.Add(i, index.Item2);

                        var fromIndex = findLaneSideNo(orderedLaneSideList[i][index.Item1], sideDict);
                        var toIndex = findLaneSideNo(orderedLaneSideList[index.Item2][index.Item3], sideDict);

                        if (fromIndex != -1 && toIndex != -1 && fromIndex != toIndex)
                        {
                            sideGraph.AddEdge(fromIndex, toIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 找同一层不同环之间的关系。返回 laneSide 里 同边的 隔壁环roRing 里的最近边lane .返回值均为list 里面的index
        /// (thisLaneIndex, (toRing, toLane))
        /// </summary>
        /// <param name="LaneSide"></param>
        /// <param name="orderedLaneSideList"></param>
        /// <param name="alreadyLinked"></param>
        /// <param name="thisIndex"></param>
        /// <returns></returns>
        private static (int, int, int) findNeighbor(List<(Line, int)> LaneSide, List<List<(Line, int)>> orderedLaneSideList, Dictionary<int, int> alreadyLinked, int thisIndex)
        {
            int thisLaneIndex = -1;
            int toRing = -1;
            int toLane = -1;

            for (int i = 0; i < LaneSide.Count; i++)
            {
                if (thisLaneIndex != -1 && toRing != -1 && toLane != -1)
                {
                    break;
                }

                for (int j = 0; j < orderedLaneSideList.Count; j++)
                {
                    if (thisLaneIndex != -1 && toRing != -1 && toLane != -1)
                    {
                        break;
                    }
                    if ((alreadyLinked.ContainsKey(thisIndex) && alreadyLinked.ContainsValue(j)) || (alreadyLinked.ContainsKey(j) && alreadyLinked.ContainsValue(thisIndex)))
                    {
                        continue;
                    }
                    if (LaneSide != orderedLaneSideList[j])
                    {
                        for (int r = 0; r < orderedLaneSideList[j].Count; r++)
                        {
                            if (LaneSide[i].Item1 == orderedLaneSideList[j][r].Item1 && LaneSide[i].Item2 != orderedLaneSideList[j][r].Item2)
                            {
                                thisLaneIndex = i;
                                toRing = j;
                                toLane = r;
                                break;
                            }

                        }
                    }
                }
            }

            return (thisLaneIndex, toRing, toLane);

        }


        public static List<List<List<int>>> SeachGraph(ThLaneSideGraph sideGraph)
        {
            List<List<List<int>>> allPath = new List<List<List<int>>>();

            for (int s = 0; s < sideGraph.sideVertexNodeList.Count; s++)
            {

                List<List<int>> pathFromOneStart = new List<List<int>>();

                int[] visited = new int[sideGraph.sideVertexNodeList.Count];
                var startPath = new List<int>();
                pathFromOneStart.Add(startPath);
                sideGraph.traverse(s, ref visited, ref startPath, ref pathFromOneStart);

                for (int i = 0; i < pathFromOneStart.Count; i++)
                {
                    var pathThis = new List<List<int>>();
                    pathThis.Add(pathFromOneStart[i]);

                    findNextPath(sideGraph, pathThis, allPath);

                }
            }

            return allPath;
        }

        private static void findNextPath(ThLaneSideGraph sideGraph, List<List<int>> pathThis, List<List<List<int>>> allPath)
        {

            var visited = new int[sideGraph.sideVertexNodeList.Count];
            pathThis.ForEach(x => x.ForEach(y => visited[y] = 1));

            var newStartList = remainIndex(visited);
            if (newStartList.Count > 0)
            {
                foreach (var newStart in newStartList)
                {
                    var visitedNew = new int[sideGraph.sideVertexNodeList.Count];
                    pathThis.ForEach(x => x.ForEach(y => visitedNew[y] = 1));

                    var startPathThis = new List<int>();
                    var startPath = new List<List<int>>();
                    startPath.Add(startPathThis);
                    sideGraph.traverse(newStart, ref visitedNew, ref startPathThis, ref startPath);

                    foreach (var path in startPath)
                    {
                        if (path.Count > 0)
                        {
                            var newPath = new List<List<int>>();
                            newPath.AddRange(pathThis);
                            newPath.Add(path);
                            findNextPath(sideGraph, newPath, allPath);
                        }
                    }
                }
            }
            else
            {
                //if (alreadyInAllPath(allPath, pathThis) == false)
                //{
                allPath.Add(pathThis);
                //}
            }
        }

        private static bool alreadyInAllPath(List<List<List<int>>> allPath, List<List<int>> pathThis)
        {
            bool bReturn = false;

            foreach (var path in allPath)
            {
                if (path.Count == pathThis.Count)
                {

                    if (compareList(path, pathThis) == true)
                    {
                        bReturn = true;
                        break;
                    }
                }
            }
            return bReturn;
        }

        private static bool compareList(List<List<int>> path, List<List<int>> pathThis)
        {
            var bReturn = true;
            for (int i = 0; i < path.Count; i++)
            {
                if (pathThis.Where(x => path[i].SequenceEqual(x)).Count() == 0)
                {
                    bReturn = false;
                    break;
                }
            }

            return bReturn;
        }

        private static List<int> remainIndex(int[] visited)
        {
            var indexL = new List<int>();
            for (int i = 0; i < visited.Length; i++)
            {
                if (visited[i] == 0)
                {
                    indexL.Add(i);
                }
            }
            return indexL;
        }
    }
}
