using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Assistant;



namespace ThMEPLighting.EmgLightConnect.Service
{
    public class connectSingleSideInGroupService
    {
        public static List<(Point3d, Point3d)> connectAllSingleSide(BlockReference ALE, List<List<ThSingleSideBlocks>> OptimalGroupBlocks)
        {
            var connectList = new List<(Point3d, Point3d)>();

            for (int i = 0; i < OptimalGroupBlocks.Count; i++)
            {

                var sigleSideGroup = OptimalGroupBlocks[i];

                var sigleSideConnectList = connectSingleSide(ALE, sigleSideGroup);

                connectList.AddRange(sigleSideConnectList);
            }

            return connectList;
        }

        private static List<(Point3d, Point3d)> connectSingleSide(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var connectList = new List<(Point3d, Point3d)>();

            //var orderSigleSideGroup = orderSignleSidePrim(ALE, sigleSideGroup);
            //var orderSigleSideGroup = orderSignleSideOrigin(ALE, sigleSideGroup);
            //var orderSigleSideGroup = orderSignleSideLaneOrder(ALE, sigleSideGroup);
            //var orderSigleSideGroup = orderSignleSideDistTypeC(ALE, sigleSideGroup);

            var orderSigleSideGroup = orderSignleSideDist(ALE, sigleSideGroup);

            var blockList = new List<Point3d>();

            if (orderSigleSideGroup.Count > 0)
            {
                for (int i = 0; i < orderSigleSideGroup.Count; i++)
                {
                    var pts = orderSigleSideGroup[i].getTotalBlock();
                    for (int j = 0; j < pts.Count; j++)
                    {
                        DrawUtils.ShowGeometry(new Point3d(pts[j].X + 100, pts[j].Y, 0), j.ToString(), EmgConnectCommon.LayerBlkNo, Color.FromColorIndex(ColorMethod.ByColor, 40), LineWeight.LineWeight025, 200);
                    }
                }

                blockList.AddRange(orderSigleSideGroup[0].getTotalBlock());

                for (int i = 1; i < orderSigleSideGroup.Count; i++)
                {
                    var thisLaneBlock = orderSigleSideGroup[i].getTotalBlock();

                    Dictionary<int, double> returnValueDict = returnValueCalculation.getReturnValueInGroup2(ALE, blockList, thisLaneBlock);//key:blockListIndex value:returnValue
                    List<(int, int, double)> closedDists = returnValueCalculation.getDistMatrix(blockList, thisLaneBlock); //(blocklist index, focused side index, distance)

                    var connectListTemp = returnValueCalculation.findOptimalConnectionInGroup(returnValueDict, closedDists, blockList, thisLaneBlock, orderSigleSideGroup);
                    //var connectListTemp = returnValueCalculation.findOptimalConnectionInGroupFilter(returnValueDict, closedDists, blockList, thisLaneBlock, orderSigleSideGroup);

                    if (connectListTemp.Count > 0)
                    {
                        connectList.AddRange(connectListTemp);
                        orderSigleSideGroup[i].connectPt(connectListTemp[0].Item1, connectListTemp[0].Item2);
                    }

                    blockList.AddRange(thisLaneBlock);

                }
            }
            return connectList;


        }

        private static List<ThSingleSideBlocks> orderSignleSideOrigin(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var sideDistDict = new Dictionary<ThSingleSideBlocks, double>();

            foreach (var side in sigleSideGroup)
            {
                if (side.Count > 0)
                {
                    var dist = side.getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                    sideDistDict.Add(side, dist);
                }
            }

            var orderSingleSide = sideDistDict.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            return orderSingleSide;
        }

        private static List<ThSingleSideBlocks> orderSignleSideDist(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var orderSingleSide = new List<ThSingleSideBlocks>();
            var sideDistDict = new Dictionary<ThSingleSideBlocks, double>();

            foreach (var side in sigleSideGroup)
            {
                if (side.Count > 0)
                {
                    var dist = side.getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                    sideDistDict.Add(side, dist);
                }
            }

            if (sideDistDict.Count > 0)
            {
                var orderSingleSideTemp = sideDistDict.OrderBy(x => x.Value).ToList();

                var notInSide = new List<ThSingleSideBlocks>();

                orderSingleSide.Add(orderSingleSideTemp[0].Key);
                int prevIdx = 0;
                for (int i = 1; i < orderSingleSideTemp.Count; i++)
                {
                    var prevBlk = orderSingleSideTemp[prevIdx].Key;
                    var thisBlk = orderSingleSideTemp[i].Key;

                    var dist = distanceInSide(prevBlk, thisBlk);
                    if (dist <= EmgConnectCommon.TolSaperateGroupMaxDistance)
                    {
                        orderSingleSide.Add(thisBlk);
                        prevIdx = i;
                    }
                    else
                    {
                        notInSide.Add(thisBlk);
                    }

                }
                orderSingleSide.AddRange(notInSide);
            }

            return orderSingleSide;
        }

        /// <summary>
        /// debug:用最小生成树排序
        /// </summary>
        /// <param name="ALE"></param>
        /// <param name="sigleSideGroup"></param>
        /// <returns></returns>
        private static List<ThSingleSideBlocks> orderSignleSidePrim(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var sideDistDict = new Dictionary<ThSingleSideBlocks, double>();
            foreach (var side in sigleSideGroup)
            {
                if (side.Count > 0)
                {
                    var dist = side.getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                    sideDistDict.Add(side, dist);
                }
            }
            var sides = sideDistDict.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            double[,] graph = new double[sides.Count, sides.Count];

            for (int i = 0; i < sides.Count; i++)
            {
                for (int j = i; j < sides.Count; j++)
                {
                    if (i == j)
                    {
                        graph[i, j] = 0;
                    }
                    else
                    {
                        var dist = distanceInSide(sides[i], sides[j]);
                        graph[i, j] = dist;
                        graph[j, i] = dist;
                    }
                }
            }

            Prim(graph, sides.Count, out var parent);

            var orderSides = new List<ThSingleSideBlocks>();
            orderSides.Add(sides[0]);
            var orderP = new Dictionary<int, int>();

            for (int i = 1; i < parent.Count(); i++)
            {
                orderP.Add(i, parent[i]);
            }

            var par = new List<int> { 0 };

            while (orderSides.Count != sides.Count)
            {
                orderInsert(orderP, par, sides, ref orderSides);
            }

            return orderSides;
        }

        //车道线顺序排序
        private static List<ThSingleSideBlocks> orderSignleSideLaneOrder(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            List<ThSingleSideBlocks> orderSides = new List<ThSingleSideBlocks>();

            var laneOrderSide = sigleSideGroup.Where(x => x.Count > 0).ToList();

            var distStart = laneOrderSide[0].getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
            var distEnd = laneOrderSide.Last().getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();

            if (distStart <= distEnd)
            {
                orderSides = laneOrderSide;
            }
            else
            {
                for (int i = laneOrderSide.Count - 1; i >= 0; i--)
                {
                    orderSides.Add(laneOrderSide[i]);
                }
            }


            return orderSides;
        }

        private static List<ThSingleSideBlocks> orderSignleSideDistTypeC(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var sideDistDict = new Dictionary<ThSingleSideBlocks, double>();

            foreach (var side in sigleSideGroup)
            {
                if (side.Count > 0)
                {
                    var dist = side.getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                    sideDistDict.Add(side, dist);
                }
            }

            var orderSingleSide = sideDistDict.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            return orderSingleSide;
        }

        private static void orderInsert(Dictionary<int, int> orderP, List<int> par, List<ThSingleSideBlocks> sides, ref List<ThSingleSideBlocks> orderSides)
        {
            //var child = orderP.Where(x => x.Value == par).ToList();
            var child = new List<KeyValuePair<int, int>>();
            var newPar = new List<int>();

            for (int i = 0; i < par.Count; i++)
            {
                foreach (var p in orderP)
                {
                    if (p.Value == par[i])
                    {
                        child.Add(p);
                    }
                }
            }

            foreach (var c in child)
            {
                orderSides.Add(sides[c.Key]);
                newPar.Add(c.Key);
            }
            if (orderSides.Count != orderP.Count + 1)
            {
                orderInsert(orderP, newPar, sides, ref orderSides);
            }
        }

        private static double distanceInSide(ThSingleSideBlocks sideA, ThSingleSideBlocks sideB)
        {
            //double x = (sideA.laneSide.Last().Item1.EndPoint.X - sideA.laneSide.First().Item1.StartPoint.X) / 2 + sideA.laneSide.First().Item1.StartPoint.X;
            //double y = (sideA.laneSide.Last().Item1.EndPoint.Y - sideA.laneSide.First().Item1.StartPoint.Y) / 2 + sideA.laneSide.First().Item1.StartPoint.Y;
            //var ACenPt = new Point3d(x, y, 0);


            //x = (sideB.laneSide.Last().Item1.EndPoint.X - sideB.laneSide.First().Item1.StartPoint.X) / 2 + sideB.laneSide.First().Item1.StartPoint.X;
            //y = (sideB.laneSide.Last().Item1.EndPoint.Y - sideB.laneSide.First().Item1.StartPoint.Y) / 2 + sideB.laneSide.First().Item1.StartPoint.Y;
            //var BCenPt = new Point3d(x, y, 0);


            //var dist = ACenPt.DistanceTo(BCenPt);

            var listA = sideA.getTotalBlock();
            var listB = sideB.getTotalBlock();

            double dist = 200000;

            for (int i = 0; i < listA.Count; i++)
            {

                for (int j = 0; j < listB.Count; j++)
                {
                    var distTemp = listA[i].DistanceTo(listB[j]);

                    if (distTemp < dist)
                    {
                        dist = distTemp;
                    }
                }
            }

            return dist;
        }

        private static void Prim(double[,] graph, int verticesCount, out int[] parent)
        {
            parent = new int[verticesCount];
            double[] key = new double[verticesCount];
            bool[] mstSet = new bool[verticesCount];

            for (int i = 0; i < verticesCount; ++i)
            {
                key[i] = int.MaxValue;
                mstSet[i] = false;
            }

            key[0] = 0;
            parent[0] = -1;

            for (int count = 0; count < verticesCount - 1; ++count)
            {
                int u = MinKey(key, mstSet, verticesCount);
                mstSet[u] = true;

                for (int v = 0; v < verticesCount; ++v)
                {
                    if (Convert.ToBoolean(graph[u, v]) && mstSet[v] == false && graph[u, v] < key[v])
                    {
                        parent[v] = u;
                        key[v] = graph[u, v];
                    }
                }
            }
        }

        private static int MinKey(double[] key, bool[] set, int verticesCount)
        {
            double min = int.MaxValue;
            int minIndex = 0;

            for (int v = 0; v < verticesCount; ++v)
            {
                if (set[v] == false && key[v] < min)
                {
                    min = key[v];
                    minIndex = v;
                }
            }

            return minIndex;
        }

    }
}
