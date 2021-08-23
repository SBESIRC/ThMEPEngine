using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Linq2Acad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPLighting.EmgLight.Model;

namespace ThMEPLighting.EmgLight.Service
{
    class FindUniformSideService
    {
        /// <summary>
        /// 找均匀一边
        /// </summary>
        /// <param name="usefulColumns">沿车道线排序后的</param>
        /// <param name="lines">车道线</param>
        /// <param name="distList">车道线方向坐标系里面的距离差</param>
        /// <returns>-1:两边都不均匀,0:车道线方向左侧均匀,1:右侧均匀</returns>
        public static int IfHasUniformSide(LayoutService layoutServer, out List<List<double>> distList)
        {
            //上下排序
            distList = new List<List<double>>();
            distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulColumns[0]));
            distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulColumns[1]));

            double lineLength = layoutServer.thLane.length;
            bool bLeft = true;
            bool bRight = true;
            double nVarianceLeft = -1;
            double nVarianceRight = -1;
            int nUniformSide = -1; //-1:no side, 0:left, 1:right

            //柱间距总长度>=车道线总长度的60% 
            if ((bLeft == false) || distList[0].Sum() / lineLength < EmgLightCommon.TolUniformSideLenth)
            {
                bLeft = false;
            }

            if ((bRight == false) || distList[1].Sum() / lineLength < EmgLightCommon.TolUniformSideLenth)
            {
                bRight = false;
            }

            //柱数量 > ((车道/平均柱距) * 0.5) 且 柱数量>=3个
            if (bLeft == false || layoutServer.UsefulColumns[0].Count() < 3 || layoutServer.UsefulColumns[0].Count() < (lineLength / EmgLightCommon.TolAvgColumnDist) * 0.5)
            {
                bLeft = false;
            }

            if (bRight == false || layoutServer.UsefulColumns[1].Count() < 3 || layoutServer.UsefulColumns[1].Count() < (lineLength / EmgLightCommon.TolAvgColumnDist) * 0.5)
            {
                bRight = false;
            }

            //方差
            if (bLeft == true)
            {
                nVarianceLeft = GetVariance(distList[0]);
            }

            if (bRight == true)
            {
                nVarianceRight = GetVariance(distList[1]);
            }

            if (nVarianceLeft >= 0 && (nVarianceLeft <= nVarianceRight || nVarianceRight == -1))
            {
                nUniformSide = 0;

            }
            else if (nVarianceRight >= 0 && (nVarianceRight <= nVarianceLeft || nVarianceLeft == -1))
            {
                nUniformSide = 1;
            }

            return nUniformSide;
        }

        /// <summary>
        /// 有均匀边情况，使用均匀情况。两边都不均匀情况,找柱多的一边,且柱子数>2 以这一边先布, 否则找构建多的一边先布。
        /// </summary>
        /// <param name="layoutServer"></param>
        /// <param name="uniformSideStructsList"></param>
        /// <param name="uniformSide"></param>
        /// <param name="distList"></param>
        public static void DetermineStartSide(LayoutService layoutServer, out List<List<ThStruct>> uniformSideStructsList, ref int uniformSide, ref List<List<double>> distList)
        {

            if (uniformSide == 0 || uniformSide == 1)
            {
                uniformSideStructsList = layoutServer.UsefulColumns;
            }
            else
            {
                uniformSide = layoutServer.UsefulColumns[0].Count >= layoutServer.UsefulColumns[1].Count ? 0 : 1;

                if (layoutServer.UsefulColumns[uniformSide].Count > 2)
                {
                    uniformSideStructsList = layoutServer.UsefulColumns;
                }
                else
                {
                    uniformSide = layoutServer.UsefulStruct[0].Count >= layoutServer.UsefulStruct[1].Count ? 0 : 1;

                    distList = new List<List<double>>();
                    distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulStruct[0]));
                    distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulStruct[1]));
                    uniformSideStructsList = layoutServer.UsefulStruct;
                }
            }
        }

        /// <summary>
        /// 计算方差
        /// </summary>
        /// <param name="distX"></param>
        /// <returns></returns>
        private static double GetVariance(List<double> distX)
        {

            double avg = 0;
            double variance = 0;

            avg = distX.Sum() / distX.Count;

            for (int i = 0; i < distX.Count - 1; i++)
            {
                variance += Math.Pow(distX[i] - avg, 2);
            }

            variance = Math.Sqrt(variance / distX.Count);


            return variance;

        }

        //public static int sideHasEvac(LayoutService layoutServer, Dictionary<BlockReference, BlockReference> evac, out Dictionary<BlockReference, BlockReference> evacInLane)
        //{
        //    int side = -1;
        //    double TolBuffer = 1000;
        //    evacInLane = new Dictionary<BlockReference, BlockReference>();

        //    if (evac.Count > 0)
        //    {
        //        var side0Struc = layoutServer.UsefulStruct[0].Select(x => x.centerPt);
        //        var side1Struc = layoutServer.UsefulStruct[1].Select(x => x.centerPt);

        //        var evacInSide0 = evac.Where(x => side0Struc.Where(s => s.DistanceTo(x.Value.Position) <= TolBuffer).Count() > 0).ToDictionary(x => x.Key, x => x.Value);
        //        var evacInSide1 = evac.Where(x => side1Struc.Where(s => s.DistanceTo(x.Value.Position) <= TolBuffer).Count() > 0).ToDictionary(x => x.Key, x => x.Value);

        //        if (evacInSide0.Count > 0 || evacInSide1.Count > 0)
        //        {
        //            if (evacInSide0.Count >= evacInSide1.Count )
        //            {
        //                side = 0;
        //                evacInLane = evacInSide0;
        //            }
        //            else
        //            {
        //                side = 1;
        //                evacInLane = evacInSide1;
        //            }
        //        }
        //    }

        //    return side;
        //}

        public static int sideHasEvac(LayoutService layoutServer, Dictionary<BlockReference, BlockReference> evac, out Dictionary<Point3d, Point3d> evacInLane)
        {
            int side = -1;
            double TolYBuffer = 6000;
            evacInLane = new Dictionary<Point3d, Point3d>();

            if (evac.Count > 0)
            {
                var evacToLaneDict = evac.ToDictionary(x => x.Value.Position, x => layoutServer.thLane.TransformPointToLine(x.Value.Position));

                var evacPtInSide0 = new Dictionary<Point3d, Point3d>();
                var evacPtInSide1 = new Dictionary<Point3d, Point3d>();

                if (layoutServer.UsefulStruct[0].Count > 0)
                {
                     evacPtInSide0 = evacToLaneDict.Where(x => 0 <= x.Value.Y && x.Value.Y <= TolYBuffer &&
                                                            0 <= x.Value.X && x.Value.X <= layoutServer.getCenterInLaneCoor(layoutServer.UsefulStruct[0].Last()).X)
                                                            .ToDictionary(x => x.Key, x => x.Value);
                }

                if (layoutServer.UsefulStruct[1].Count > 0)
                {
                     evacPtInSide1 = evacToLaneDict.Where(x => -TolYBuffer <= x.Value.Y && x.Value.Y <= 0 &&
                                                            0 <= x.Value.X && x.Value.X <= layoutServer.getCenterInLaneCoor(layoutServer.UsefulStruct[1].Last()).X)
                                                            .ToDictionary(x => x.Key, x => x.Value);
                }

                if (evacPtInSide0.Count > 0 || evacPtInSide1.Count > 0)
                {
                    if (evacPtInSide0.Count >= evacPtInSide1.Count)
                    {
                        side = 0;
                        evacInLane = evacPtInSide0;
                    }
                    else
                    {
                        side = 1;
                        evacInLane = evacPtInSide1;
                    }
                }
            }

            return side;
        }
    }
}
