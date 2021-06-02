using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class connectSingleSideBlkService
    {

        public static void connectMainToMain(List<ThSingleSideBlocks> singleSideBlocks)
        {
            for (int i = 0; i < singleSideBlocks.Count; i++)
            {
                var side = singleSideBlocks[i];
                if (side.Count > 0)
                {
                    for (int j = 1; j < side.reMainBlk.Count; j++)
                    {
                        side.connectPt(side.reMainBlk[j - 1], side.reMainBlk[j]);
                    }
                }
            }
        }

        public static void connecSecToMain(Point3d ALE, List<ThSingleSideBlocks> singleSideBlocks, Polyline frame)
        {

            //debug 没有main 只有sec的情况处理不了
            foreach (var side in singleSideBlocks)
            {
                if (side.reMainBlk.Count > 0)
                {
                    var regroupSecBlk = side.getTotalBlock().Where(x => side.reMainBlk.Contains(x) == false).ToList();
                    side.setReSecBlk(regroupSecBlk);

                    findGroupMainSec(side, ALE, frame);
                }
            }
        }

        private static void findGroupMainSec(ThSingleSideBlocks side, Point3d ALE, Polyline frame)
        {
            side.orderReSecBlk();

            var dir = (side.laneSide.Last().Item1.EndPoint - side.laneSide.First().Item1.StartPoint).GetNormal();
            var rotationangle = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
            var mainConnectMatrix = Matrix3d.Displacement(side.reMainBlk.First().GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

            var transSecPt = side.reSecBlk.ToDictionary(x => x, x => x.TransformBy(mainConnectMatrix.Inverse()));
            var transMainPt = side.reMainBlk.ToDictionary(x => x, x => x.TransformBy(mainConnectMatrix.Inverse()));


            //所有散点到所有主点的距离
            var distM = returnValueCalculation.getDistMatrix(side.reMainBlk, side.reSecBlk);

            bool[] visit = new bool[side.reSecBlk.Count];
            visit.ForEach(x => x = false);

            for (int j = 0; j < side.reSecBlk.Count; j++)
            {
                if (visit[j] == false)
                {

                    var ptList = new List<Point3d>();
                    ptList.Add(side.reSecBlk[j]);

                    //找最近的/回头量小的主块，加入图
                    //var mainI = getRootMainBlk(j, distM, side, ALE, ptList);

                    var mainI = getRootMainBlk2(j, distM, side, ALE, ptList);

                    //找到同边散点到这个主块最小的散点list,加入图
                    var secPtIndexList = findCloseSecPtList(distM, mainI);
                    //ptList.AddRange(side.reSecBlk.Where(x => secPtIndexList.Contains(side.reSecBlk.IndexOf(x)) &&
                    //                                         visit[side.reSecBlk.IndexOf(x)] == false &&
                    //                                         transSecPt[x].Y * transSecPt[side.reSecBlk[j]].Y >= 0
                    //                                    ).ToList());

                    ptList.AddRange(side.reSecBlk.Where(x => secPtIndexList.Contains(side.reSecBlk.IndexOf(x)) &&
                                                            visit[side.reSecBlk.IndexOf(x)] == false
                                                       ).ToList());


                    //找到散点的中心,最大最小xy长方形中点
                    var cenPt = findSecPtCenter(ptList);

                    //找到散点的中心前后主点间的点
                    var secPtList = findSecBetweenMain(cenPt.TransformBy(mainConnectMatrix.Inverse()), transMainPt, transSecPt, out var leftMain, out var rightMain);
                    if (secPtList.Count > 0)
                    {
                        //ptList.AddRange(secPtList.Where(x => visit[side.reSecBlk.IndexOf(x)] == false &&
                        //                                      transSecPt[x].Y * transSecPt[side.reSecBlk[j]].Y >= 0 &&
                        //                                      x.DistanceTo(side.reMainBlk[mainI]) < 10000
                        //                                 ).ToList());

                        ptList.AddRange(secPtList.Where(x => visit[side.reSecBlk.IndexOf(x)] == false &&
                                                             x.DistanceTo(side.reMainBlk[mainI]) < EmgConnectCommon.TolConnectSecPtRange
                                                        ).ToList());
                    }

                    //左右最近点的secPt
                    secPtIndexList = findCloseSecPtList(distM, side.reMainBlk.IndexOf(leftMain));
                    ptList.AddRange(side.reSecBlk.Where(x => secPtIndexList.Contains(side.reSecBlk.IndexOf(x)) &&
                                                        visit[side.reSecBlk.IndexOf(x)] == false
                                                   ).ToList());

                    secPtIndexList = findCloseSecPtList(distM, side.reMainBlk.IndexOf(rightMain));
                    ptList.AddRange(side.reSecBlk.Where(x => secPtIndexList.Contains(side.reSecBlk.IndexOf(x)) &&
                                                        visit[side.reSecBlk.IndexOf(x)] == false
                                                   ).ToList());



                    ptList = ptList.Distinct().ToList();

                    //主点和散点找最小生成树
                    buildMST(ptList, side, frame, out var parent);

                    for (int i = 0; i < ptList.Count; i++)
                    {
                        if (parent[i] >= 0)
                        {
                            side.connectPt(ptList[i], ptList[parent[i]]);
                        }
                    }

                    //visit里面mark散点已经遍历过
                    ptList.Where(x => side.reSecBlk.Contains(x)).ForEach(x => visit[side.reSecBlk.IndexOf(x)] = true);

                    //下一个没遍历过的散点
                }

            }

        }

        private static List<Point3d> findSecBetweenMain(Point3d transCenPt, Dictionary<Point3d, Point3d> transMainPt, Dictionary<Point3d, Point3d> transSecPt, out Point3d leftMain, out Point3d rightMain)
        {
            var xMin = double.MaxValue;
            var xMax = double.MaxValue;
            leftMain = new Point3d();
            rightMain = new Point3d();
            var secList = new List<Point3d>();

            foreach (var transMain in transMainPt)
            {
                if (transMain.Value.X <= transCenPt.X && (transCenPt.X - transMain.Value.X) <= xMin)
                {
                    xMin = transCenPt.X - transMain.Value.X;
                    leftMain = transMain.Key;
                }
                if (transMain.Value.X >= transCenPt.X && (transMain.Value.X - transCenPt.X) <= xMax)
                {
                    xMax = transMain.Value.X - transCenPt.X;
                    rightMain = transMain.Key;
                }
            }

            if (leftMain != Point3d.Origin && rightMain != Point3d.Origin)
            {
                var dist = Math.Abs(transMainPt[rightMain].X - transMainPt[leftMain].X) / 5;
                xMin = transMainPt[leftMain].X - dist;
                xMax = transMainPt[rightMain].X + dist;

                secList = transSecPt.Where(x => xMin <= x.Value.X && x.Value.X <= xMax).Select(x => x.Key).ToList();

            }


            return secList;


        }

        private static int getRootMainBlk(int secIndex, List<(int, int, double)> distM, ThSingleSideBlocks side, Point3d ALE, List<Point3d> ptList)
        {

            var mainList = side.reMainBlk;
            Point3d secPt = side.reSecBlk[secIndex];

            var mainDist = distM.Where(x => x.Item2 == secIndex).ToList();

            Dictionary<int, double> returnValueDict = returnValueCalculation.getReturnValueInSide(ALE, mainList, secPt);//key:blockListIndex value:returnValue

            var mainPt = returnValueCalculation.findOptimalConnectionInSide(returnValueDict, mainDist, mainList, secPt, side);

            ptList.Add(mainPt);

            var mainI = side.reMainBlk.IndexOf(mainPt);

            return mainI;

        }

        private static int getRootMainBlk2(int secIndex, List<(int, int, double)> distM, ThSingleSideBlocks side, Point3d ALE, List<Point3d> ptList)
        {
            List<Point3d> blockList = null;
            List<Point3d> thisLaneBlock = null;

            var mainList = side.reMainBlk;
            Point3d secPt = side.reSecBlk[secIndex];
            var secList = new List<Point3d>() { secPt };
    
            var mainToALE = mainList.Select(x => x.DistanceTo(ALE)).Min();
            var secToALE = secPt.DistanceTo(ALE);

            if (mainToALE <= secToALE )
            {
                blockList = mainList;
                thisLaneBlock = secList;
            }
            else
            {
                blockList =secList ;
                thisLaneBlock = mainList;
            }


            Dictionary<int, double> returnValueDict = returnValueCalculation.getReturnValueInGroupAngle(ALE, blockList, thisLaneBlock);//key:blockListIndex value:returnValue

            List<(int, int, double)> closedDists = returnValueCalculation.getDistMatrix(blockList, thisLaneBlock); //(blocklist index, focused side index, distance)

            var connectListTemp = returnValueCalculation.findOptimalConnectionInGroup(returnValueDict, closedDists, blockList, thisLaneBlock, new List<ThSingleSideBlocks> { side });

            var mainPt = connectListTemp[0].Item1.IsEqualTo(secPt, new Tolerance(1, 1)) ? connectListTemp[0].Item2 : connectListTemp[0].Item1;

            ptList.Add(mainPt);

            var mainI = side.reMainBlk.IndexOf(mainPt);

            return mainI;


        }

        private static List<int> findCloseSecPtList(List<(int, int, double)> distM, int mainI)
        {
            List<int> secPtIndexList = new List<int>();
            int secCount = distM.Select(x => x.Item2).Max();

            for (int j = 0; j <= secCount; j++)
            {

                var minMainIndex = distM.Where(x => x.Item2 == j).OrderBy(x => x.Item3).FirstOrDefault().Item1;

                if (minMainIndex == mainI)
                {
                    secPtIndexList.Add(j);
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

        private static void buildMST(List<Point3d> ptList, ThSingleSideBlocks side, Polyline frame, out int[] parent)
        {

            double[,] graph = new double[ptList.Count, ptList.Count];

            for (int i = 0; i < ptList.Count; i++)
            {
                for (int j = i; j < ptList.Count; j++)
                {
                    // if (i == j || intersectWithFrame(ptList[i], ptList[j], frame))
                    if (i == j)
                    {
                        graph[i, j] = 0;
                    }
                    else if (intersectWithFrame(ptList[i], ptList[j], frame))
                    {
                        //如果穿框线，则加点权重。让prim尽量不选穿框线的走法。但是不能直接设置不连通。否则有些case找不到连接方法
                        var dist = ptList[i].DistanceTo(ptList[j]) + EmgConnectCommon.TolConnectSecPrimAddValue;
                        graph[i, j] = dist;
                        graph[j, i] = dist;
                    }
                    else
                    {
                        var dist = ptList[i].DistanceTo(ptList[j]);
                        graph[i, j] = dist;
                        graph[j, i] = dist;
                    }
                }
            }
            int[] connectNo = new int[ptList.Count];
            ptList.ForEach(x => connectNo[ptList.IndexOf(x)] = side.blkConnectNo(x));

            Prim(graph, ptList.Count, connectNo, out parent);

        }

        private static bool intersectWithFrame(Point3d pt1, Point3d pt2, Polyline frame)
        {
            bool bIntersect = false;
            Line l = new Line(pt1, pt2);
            var intersectPt = frame.Intersect(l, Intersect.OnBothOperands);
            if (intersectPt.Count > 0)
            {
                bIntersect = true;
            }
            return bIntersect;
        }

        /// <summary>
        /// prim变体，需要控制连接点数。
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="verticesCount"></param>
        /// <param name="connectNo"></param>
        /// <param name="parent"></param>
        private static void Prim(double[,] graph, int verticesCount, int[] connectNo, out int[] parent)
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
                                connectNo[v] < EmgConnectCommon.TolBlkMaxConnect && connectNo[u] < EmgConnectCommon.TolBlkMaxConnect)
                            {
                                min = graph[u, v];
                                minIndex = v;
                                minParent = u;
                            }
                        }
                    }
                }
                if (minIndex != -1 && minParent != -1)
                {
                    mstSet[minIndex] = true;
                    parent[minIndex] = minParent;
                    connectNo[minIndex] = connectNo[minIndex] + 1;
                    connectNo[minParent] = connectNo[minParent] + 1;
                }
            }
        }
    }
}
