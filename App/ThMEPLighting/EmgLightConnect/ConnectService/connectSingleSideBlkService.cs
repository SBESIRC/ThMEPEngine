using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLightConnect.Service;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Service;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class connectSingleSideBlkService
    {
        public static void connectMainToMain(List<ThSingleSideBlocks> sigleSideGroup)
        {
            for (int i = 0; i < sigleSideGroup.Count; i++)
            {
                var side = sigleSideGroup[i];
                if (side.Count > 0)
                {
                    regroupMainSec(side);
                    side.orderReMainBlk();

                    for (int j = 1; j < side.reMainBlk.Count; j++)
                    {
                        side.connectPt(side.reMainBlk[j - 1], side.reMainBlk[j]);
                    }
                }
            }
        }

        public static void connectSecToMain(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            List<Point3d> tempMain = null;
            var allMain = sigleSideGroup.SelectMany(x => x.reMainBlk).ToList();

            for (int i = 0; i < sigleSideGroup.Count; i++)
            {
                var side = sigleSideGroup[i];

                for (int j = 0; j < side.reSecBlk.Count; j++)
                {
                    Point3d mainBlk = new Point3d();

                    if (side.reMainBlk.Count > 0)
                    {
                        tempMain = side.reMainBlk;
                    }
                    else
                    {
                        tempMain = allMain;
                    }

                    var closeMainBlk = tempMain.ToDictionary(x => x, x => x.DistanceTo(side.reSecBlk[j])).OrderBy(x => x.Value).ToList();

                    for (int r = 0; r < closeMainBlk.Count; r++)
                    {
                        if (side.blkConnectNo(closeMainBlk[r].Key) < EmgConnectCommon.TolMaxConnect)
                        {
                            mainBlk = closeMainBlk[r].Key;
                            break;
                        }
                    }
                    side.connectPt(side.reSecBlk[j], mainBlk);

                }
            }
        }

        private static void regroupMainSec(ThSingleSideBlocks side)
        {
            List<Point3d> regroupMain = new List<Point3d>();
            List<double> yTransValue = new List<double>();
            var allBlk = side.getTotalBlock();

            var allBlkDict = allBlk.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();

            if (side.mainBlk.Count > 0)
            {
                yTransValue = allBlkDict.Where(x => side.mainBlk.Contains(x.Key)).Select(x => x.Value.Y).ToList();

                var YminTemp = yTransValue.Min() - EmgConnectCommon.TolPtOnSameLineYRange;
                var YmaxTemp = yTransValue.Max() + EmgConnectCommon.TolPtOnSameLineYRange;

                double Ymin = 0;
                double YMax = 0;

                if (Math.Abs(YminTemp) > Math.Abs(YmaxTemp)) //有可能是负数
                {
                    YMax = Math.Abs(YminTemp);
                    Ymin = Math.Abs(YmaxTemp);
                }
                else
                {
                    Ymin = Math.Abs(YminTemp);
                    YMax = Math.Abs(YmaxTemp);
                }
                regroupMain = allBlkDict.Where(x => Ymin <= Math.Abs(x.Value.Y) && Math.Abs(x.Value.Y) <= YMax).Select(x => x.Key).ToList();

                regroupMain.AddRange(side.mainBlk);
                regroupMain = regroupMain.Distinct().ToList();
            }
            else if (side.secBlk.Count > 0)
            {
                yTransValue = allBlkDict.Where(x => side.secBlk.Contains(x.Key)).Select(x => x.Value.Y).ToList();

                double ySecMean = 20000;
                ySecMean = yTransValue
                           .OrderBy(x => x)
                           .GroupBy(x => Math.Floor(x / 10))
                           .OrderByDescending(x => x.Count())
                           .First()
                           .ToList()
                           .First();

                if (ySecMean != 20000)
                {
                    regroupMain = allBlkDict.Where(x => Math.Abs(Math.Abs(x.Value.Y) - Math.Abs(ySecMean)) < EmgConnectCommon.TolPtOnSameLineYRange).Select(x => x.Key).ToList();
                }


            }

            var regroupSecBlk = allBlk.Where(x => regroupMain.Contains(x) == false).ToList();

            side.setReMainBlk(regroupMain);
            side.setReSecBlk(regroupSecBlk);

        }

        public static void connecSecToMain3(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var allMain = sigleSideGroup.SelectMany(x => x.reMainBlk).ToList();

            //debug 没有main 只有sec的情况处理不了
            foreach (var side in sigleSideGroup)
            {


                findGroupMainSec(side);







            }
        }

        private static void findGroupMainSec(ThSingleSideBlocks side)
        {
            //所有散点到所有主点的距离
            List<(int, int, double)> distM = new List<(int, int, double)>();
            for (int j = 0; j < side.reSecBlk.Count; j++)
            {
                for (int i = 0; i < side.reMainBlk.Count; i++)
                {
                    var dist = side.reSecBlk[j].DistanceTo(side.reMainBlk[i]);
                    distM.Add((i, j, dist));
                }
            }

            bool[] visit = new bool[side.reSecBlk.Count];
            visit.ForEach(x => x = false);
            for (int j = 0; j < side.reSecBlk.Count; j++)
            {
                if (visit[j] == false)
                {
                    //找最近的主块，加入图
                    var mainI = distM.Where(x => x.Item2 == j).OrderBy(x => x.Item3).First().Item1;
                    var ptList = new List<Point3d>();
                    ptList.Add(side.reMainBlk[mainI]);

                    //找到散点到这个主块最小的散点list,加入图
                    var secPtIndexList = findCloseSecPtList(distM, mainI, visit);
                    ptList.AddRange(side.reSecBlk.Where(x => secPtIndexList.Contains(side.reSecBlk.IndexOf(x))).ToList());

                    //找到散点的中心,最大最小xy长方形中点
                    var cenPt = findSecPtCenter(ptList);

                    //以中心画半径r 10000 的圆找散点，加入图
                    if (cenPt != Point3d.Origin)
                    {
                        side.reSecBlk.Where(x => x.DistanceTo(cenPt) < 8000 && visit[side.reSecBlk.IndexOf(x)] == false).ForEach(x => ptList.Add(x));
                        ptList = ptList.Distinct().ToList();

                        //主点和散点找最小生成树
                        buildMST(ptList, side, out var parent);

                        for (int i = 0; i < ptList.Count; i++)
                        {
                            if (parent[i] >= 0)
                            {
                                side.connectPt(ptList[i], ptList[parent[i]]);
                            }
                        }

                        //visit里面mark散点已经遍历过
                        ptList.Where(x => side.reSecBlk.Contains(x)).ForEach(x => visit[side.reSecBlk.IndexOf(x)] = true);
                    }
                    //下一个没遍历过的散点
                }

            }

        }

        private static List<int> findCloseSecPtList(List<(int, int, double)> distM, int mainI, bool[] visit)
        {
            List<int> secPtIndexList = new List<int>();
            int secCount = distM.Select(x => x.Item2).Max();

            for (int j = 0; j <= secCount; j++)
            {
                if (visit[j] == false)
                {
                    var minMainIndex = distM.Where(x => x.Item2 == j).OrderBy(x => x.Item3).FirstOrDefault().Item1;

                    if (minMainIndex == mainI)
                    {
                        secPtIndexList.Add(j);
                    }
                }

            }
            return secPtIndexList;
        }

        private static Point3d findSecPtCenter(List<Point3d> ptList)
        {
            Point3d cenPt = new Point3d();
            if (ptList.Count > 0)
            {
                var xMax = ptList.Select(pt => pt.X).Max();
                var xMin = ptList.Select(pt => pt.X).Min();
                var yMax = ptList.Select(pt => pt.Y).Max();
                var yMin = ptList.Select(pt => pt.Y).Min();

                cenPt = new Point3d((xMax + xMin) / 2, (yMax + yMin) / 2, 0);
            }


            return cenPt;
        }

        private static void buildMST(List<Point3d> ptList, ThSingleSideBlocks side, out int[] parent)
        {

            double[,] graph = new double[ptList.Count, ptList.Count];

            for (int i = 0; i < ptList.Count; i++)
            {
                for (int j = i; j < ptList.Count; j++)
                {
                    if (i == j)
                    {
                        graph[i, j] = 0;
                    }
                    var dist = ptList[i].DistanceTo(ptList[j]);
                    graph[i, j] = dist;
                    graph[j, i] = dist;
                }
            }
            int[] connectNo = new int[ptList.Count];
            ptList.ForEach(x => connectNo[ptList.IndexOf(x)] = side.blkConnectNo(x));

            Prim2(graph, ptList.Count, connectNo, out parent);
            //Prim(graph, ptList.Count, out parent);

        }

        //public static void connectSecToMain2(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        //{
        //    //var side = sigleSideGroup[2];

        //    var side = sigleSideGroup[6];

        //    var ptList = new List<Point3d>();
        //    //ptList.Add(side.mainBlk[5]);
        //    ptList.Add(side.mainBlk[0]);

        //    side.secBlk.Where(x => x.DistanceTo(side.mainBlk[0]) < 21000).ForEach(x => ptList.Add(x));

        //    double[,] graph = new double[ptList.Count, ptList.Count];

        //    for (int i = 0; i < ptList.Count; i++)
        //    {
        //        for (int j = i; j < ptList.Count; j++)
        //        {
        //            if (i == j)
        //            {
        //                graph[i, j] = 0;
        //            }
        //            var dist = ptList[i].DistanceTo(ptList[j]);
        //            graph[i, j] = dist;
        //            graph[j, i] = dist;


        //        }
        //    }

        //    Prim(graph, ptList.Count, out var parent);

        //    for (int i = 0; i < ptList.Count; i++)
        //    {
        //        if (parent[i] >= 0)
        //        {
        //            side.connectPt(ptList[i], ptList[parent[i]]);
        //        }
        //    }
        //}

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

        /// <summary>
        /// prim变体，所有都联通所以找最短的就好了。
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="verticesCount"></param>
        /// <param name="connectNo"></param>
        /// <param name="parent"></param>
        private static void Prim2(double[,] graph, int verticesCount, int[] connectNo, out int[] parent)
        {
            parent = new int[verticesCount];
            bool[] mstSet = new bool[verticesCount];

            for (int i = 0; i < verticesCount; i++)
            {
                mstSet[i] = false;
            }

            parent[0] = -1;
            mstSet[0] = true;

            for (int i = 0; i < verticesCount; i++)
            {
                double min = int.MaxValue;
                int minIndex = -1;
                int minParent = -1;

                for (int v = 0; v < verticesCount; v++)
                {
                    if (mstSet[v] == false)
                    {
                        for (int u = 0; u < verticesCount; u++)
                        {
                            if (mstSet[u] == true && Convert.ToBoolean(graph[u, v]) && graph[u, v] < min && 
                                connectNo[v] < EmgConnectCommon.TolMaxConnect && connectNo[u] < EmgConnectCommon.TolMaxConnect)
                            {
                                min = graph[u, v];
                                minIndex = v;
                                minParent = u;
                            }
                        }
                    }
                }
                if (minIndex!=-1 && minParent != -1)
                {
                    mstSet[minIndex] = true;
                    parent[minIndex] = minParent;
                    connectNo[minIndex] = connectNo[minIndex] + 1;
                    connectNo[minParent] = connectNo[minParent] + 1;
                }
              



            }

        }

        private static int MinKey2(double[,] graph, bool[] set, int[] connectNo, int verticesCount)
        {
            double min = int.MaxValue;
            int minIndex = -1;
            int minParent = -1;

            for (int v = 0; v < verticesCount; v++)
            {
                for (int u = 0; u < verticesCount; u++)
                {
                    if (set[u] == false && graph[u, v] < min && connectNo[v] < EmgConnectCommon.TolMaxConnect && connectNo[u] < EmgConnectCommon.TolMaxConnect)
                    {
                        min = graph[u, v];
                        minIndex = v;
                        minParent = u;
                    }
                }
            }


            return minIndex;
        }


    }


}
