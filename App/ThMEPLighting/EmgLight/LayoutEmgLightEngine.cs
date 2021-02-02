using System;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLight.Model;
using Autodesk.AutoCAD.Colors;

namespace ThMEPLighting.EmgLight
{
    class LayoutEmgLightEngine
    {

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="lanes"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<Polyline, (Point3d, Vector3d)> LayoutLight(Polyline frame, List<List<Line>> lanes, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<Polyline, (Point3d, Vector3d)> layoutPtInfo = new Dictionary<Polyline, (Point3d, Vector3d)>();
            List<ThStruct> layoutList = new List<ThStruct>();
            List<ThLane> laneList = new List<ThLane>();

            //跳过完全没有可布点的图
            if (columns.Count == 0 && walls.Count == 0)
            {
                return layoutPtInfo;
            }

            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];

                //特别短的线跳过
                var laneLength = lane.Sum(x => x.Length);
                if (laneLength < EmgLightCommon.TolLightRangeMin)
                {
                    continue;
                }

                var thLane = new ThLane(lane);
                laneList.Add(thLane);
            }

            for (int i = 0; i < laneList.Count; i++)
            {
                var thLane = laneList[i];
                var thlaneService = new ThLaneService(thLane);
                var StructFilterService = new ThStructFilterService(thlaneService, columns, walls);

                StructFilterService.filterStruct(out var filterColumns, out var filterWalls);

                //将构建按车道线方向分成左(0)右(1)两边
                var usefulColumns = StructureService.SeparateColumnsByLine(filterColumns, thLane.geom, EmgLightCommon.TolLane);
                var usefulWalls = StructureService.SeparateColumnsByLine(filterWalls, thLane.geom, EmgLightCommon.TolLane);

                StructureService.removeDuplicateStruct(ref usefulColumns);
                StructureService.removeDuplicateStruct(ref usefulWalls);

                LayoutEmgLightService layoutServer = new LayoutEmgLightService(usefulColumns, usefulWalls, thLane, frame);

                DrawUtils.ShowGeometry(layoutServer.UsefulStruct[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, Color.FromColorIndex(ColorMethod.ByColor, 161), LineWeight.LineWeight035);
                DrawUtils.ShowGeometry(layoutServer.UsefulStruct[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, Color.FromColorIndex(ColorMethod.ByColor, 11), LineWeight.LineWeight035);

                ////滤掉重合部分
                layoutServer.filterOverlapStruc();

                ////滤掉框外边的部分
                layoutServer.getInsideFramePart();

                ////滤掉框后边的部分
                layoutServer.filterStrucBehindFrame();

                DrawUtils.ShowGeometry(layoutServer.UsefulColumns[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 161), LineWeight.LineWeight035);
                DrawUtils.ShowGeometry(layoutServer.UsefulColumns[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 161), LineWeight.LineWeight035);
                DrawUtils.ShowGeometry(layoutServer.UsefulWalls[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 11), LineWeight.LineWeight035);
                DrawUtils.ShowGeometry(layoutServer.UsefulWalls[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 11), LineWeight.LineWeight035);

                if (layoutServer.UsefulColumns[0].Count == 0 && layoutServer.UsefulColumns[1].Count == 0 &&
                    layoutServer.UsefulWalls[0].Count == 0 && layoutServer.UsefulWalls[1].Count == 0)
                {
                    continue;
                }

                var b = false;
                if (b == true)
                {
                    continue;
                }

                ////找出平均的一边. -1:no side 0:left 1:right.
                int uniformSide = FindUniformDistributionSide(layoutServer, out var columnDistList);

                 Dictionary<ThStruct , int> uniformSideLayout = null;
                if (uniformSide == 0 || uniformSide == 1)
                {
                    //有均匀边情况
                    LayoutUniformSide(layoutServer.UsefulColumns, uniformSide, columnDistList, layoutServer, laneList, out  uniformSideLayout, ref layoutList);
                    LayoutOppositeSide(uniformSide,  uniformSideLayout, layoutServer, laneList, ref layoutList);
                }
                else
                {
                    //两边都不均匀情况,找柱多的一边,且柱子数>2 以这一边先布, 否则找构建多的一边先布
                    uniformSide = layoutServer.UsefulColumns[0].Count >= layoutServer.UsefulColumns[1].Count ? 0 : 1;

                    if (layoutServer.UsefulColumns[uniformSide].Count > 2)
                    {
                        LayoutUniformSide(layoutServer.UsefulColumns, uniformSide, columnDistList, layoutServer, laneList, out  uniformSideLayout, ref layoutList);
                    }
                    else
                    {
                        uniformSide = layoutServer.UsefulStruct[0].Count >= layoutServer.UsefulStruct[1].Count ? 0 : 1;

                        columnDistList = new List<List<double>>();
                        columnDistList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulStruct[0]));
                        columnDistList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulStruct[1]));

                        LayoutUniformSide(layoutServer.UsefulStruct, uniformSide, columnDistList, layoutServer, laneList, out  uniformSideLayout, ref layoutList);
                    }
                    LayoutOppositeSide(uniformSide, uniformSideLayout, layoutServer, laneList, ref layoutList);
                }

                layoutServer.AddLayoutStructPt(layoutList, ref layoutPtInfo);
            }

            return layoutPtInfo;
        }

        /// <summary>
        /// 找均匀一边
        /// </summary>
        /// <param name="usefulColumns">沿车道线排序后的</param>
        /// <param name="lines">车道线</param>
        /// <param name="distList">车道线方向坐标系里面的距离差</param>
        /// <returns>-1:两边都不均匀,0:车道线方向左侧均匀,1:右侧均匀</returns>
        private int FindUniformDistributionSide(LayoutEmgLightService layoutServer, out List<List<double>> distList)
        {
            //上下排序
            distList = new List<List<double>>();
            distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulColumns[0]));
            distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulColumns[1]));

            double lineLength = 0;
            layoutServer.thLane.geom.ForEach(l => lineLength += l.Length);
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
        /// 均匀边
        /// </summary>
        /// <param name="usefulStruct"></param>
        /// <param name="uniformSide"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="layout"></param>
        private void LayoutUniformSide(List<List<ThStruct>> usefulStruct, int uniformSide, List<List<double>> distList, LayoutEmgLightService layoutServer, List<ThLane> laneList, out Dictionary<ThStruct, int> uniformSideLayout, ref List<ThStruct> layout)
        {
            int layoutStatus = 0; //0:非布灯状态,1:布灯状态
            uniformSideLayout = new Dictionary<ThStruct, int>(); //int 为不均匀边布灯的标旗: -1: 头部已经layout的,或尾点对面需要布灯的, -1 标旗不计入均匀边最后的layout 0:和前面隔点, 1:对边
            double cumulateDist = 0;

            //找车道线前段的已分布状况,决定第一个点
            int initial = LayoutFirstUniformSide(layout, usefulStruct, uniformSide, layoutServer, laneList, ref uniformSideLayout, ref cumulateDist);

            if (initial < usefulStruct[uniformSide].Count)
            {
                for (int i = initial; i < usefulStruct[uniformSide].Count; i++)
                {
                    if (i < usefulStruct[uniformSide].Count - 1)
                    {
                        cumulateDist += distList[uniformSide][i];
                        if (cumulateDist > EmgLightCommon.TolLightRangeMax)
                        {
                            //累计距离到下个柱距离>tol, 本柱需标记
                            if (layoutStatus != 0)
                            {
                                //如果当前状态为布灯状态,将本柱加入
                                AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], 0, EmgLightCommon.TolLightRangeMin, false, ref uniformSideLayout);
                                layoutStatus = 0;
                            }
                            else
                            {
                                //如果当前状态为非布灯状态,改成布灯状态
                                layoutStatus = 1;
                            }
                            cumulateDist = distList[uniformSide][i];
                        }

                        //本柱到下个柱是否很远
                        if (distList[uniformSide][i] > EmgLightCommon.TolLightRangeMax)
                        {
                            //将自己和下个柱加入,累计距离清零
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], 1, EmgLightCommon.TolLightRangeMin, true, ref uniformSideLayout);
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i + 1], 1, EmgLightCommon.TolLightRangeMin, true, ref uniformSideLayout);
                            layoutStatus = 0;
                            cumulateDist = 0;
                        }
                    }
                    else
                    {
                        //最后一个点特殊处理
                        if (layoutStatus != 0)
                        {
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], 0, EmgLightCommon.TolLightRangeMin, false, ref uniformSideLayout);
                        }
                        else
                        {
                            //如果末尾还有点,标值-1
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], -1, EmgLightCommon.TolLightRangeMin, false, ref uniformSideLayout);
                        }
                    }
                }
            }
            else
            {
                AddToUniformLayoutList(layout, usefulStruct[uniformSide][initial], 0, EmgLightCommon.TolLightRangeMin, false, ref uniformSideLayout);
            }

            DrawUtils.ShowGeometry(uniformSideLayout.Where(x => x.Value == 1 || x.Value == 0).Select(y => y.Key.geom).ToList(), EmgLightCommon.LayerStructLayout, Color.FromColorIndex(ColorMethod.ByColor,3), LineWeight.LineWeight050);
            layout.AddRange(uniformSideLayout.Where(x => x.Value == 1 || x.Value == 0).Select(y => y.Key).ToList());

        }

        /// <summary>
        /// 找车道线前段的已分布状况,决定第一个点
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="usefulStruct"></param>
        /// <param name="uniformSide"></param>
        /// <param name="layoutServer"></param>
        /// <param name="lanes"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="LastHasNoLightColumn"></param>
        /// <param name="sum"></param>
        /// <returns></returns>
        private int LayoutFirstUniformSide(List<ThStruct> layout, List<List<ThStruct>> usefulStruct, int uniformSide, LayoutEmgLightService layoutServer, List<ThLane> laneList, ref Dictionary<ThStruct, int> uniformSideLayout, ref double sum)
        {
            //   A|   |B    |E       [uniform side]
            //-----s[-----------lane----------]e
            //   C|   |D
            //

            int nStart = 0;
            int otherSide = uniformSide == 0 ? 1 : 0;
            List<Polyline> otherSidePoint = new List<Polyline>();
            List<Polyline> uniformSidePoint = new List<Polyline>();
            bool initialSet = false;

            //  bool added = false;
            if (layout.Count > 0)
            {
                ////车道线往前做框buffer,选出车线头部的已布情况
                var importLayout = layoutServer.BuildHeadLayout(layout, EmgLightCommon.TolLightRangeMax, EmgLightCommon.TolLane);

                //情况A:
                var uniformSideHeadLayout = importLayout[uniformSide].Where(x => layoutServer.getCenterInLaneCoor(x).X < 0).ToList();

                //情况B:

                var uniformSideStartLayout = importLayout[uniformSide].Where(x => layoutServer.getCenterInLaneCoor(x).X <= layoutServer.getCenterInLaneCoor(usefulStruct[uniformSide][0]).X && layoutServer.getCenterInLaneCoor(x).X >= 0).ToList();

                //情况D:
                var otherSideStartLayout = importLayout[otherSide].Where(x => layoutServer.getCenterInLaneCoor(x).X >= 0).ToList();


                if (initialSet == false && uniformSideStartLayout.Count > 0)
                {
                    //情况B:
                    uniformSideLayout.Add(uniformSideStartLayout.Last(), 0);
                    sum = 0;
                    nStart = 0;
                    initialSet = true;

                }

                if (initialSet == false && otherSideStartLayout.Count > 0)
                {
                    //情况D:
                    //找D开始距离8.5内最远的作为开始
                    if (usefulStruct[uniformSide].Count > 0)
                    {
                        for (int i = 0; i < usefulStruct[uniformSide].Count; i++)
                        {
                            var dist = layoutServer.getCenterInLaneCoor(usefulStruct[uniformSide][i]).X - layoutServer.getCenterInLaneCoor(otherSideStartLayout.Last()).X;
                            if (Math.Abs(dist) > EmgLightCommon.TolLightRangeMax)
                            {
                                if (i > 0)
                                {
                                    if (CheckIfInLaneHead(usefulStruct[uniformSide][i - 1], laneList) == false)
                                    {
                                        uniformSideLayout.Add(usefulStruct[uniformSide][i - 1], 0);
                                        sum = 0;
                                        nStart = i - 1;
                                        initialSet = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (initialSet == false && uniformSideHeadLayout.Count > 0)
                {
                    //情况A:
                    if (usefulStruct[uniformSide].Count > 0)
                    {
                        uniformSideLayout.Add(importLayout[uniformSide].Last(), -1);
                        sum = importLayout[uniformSide].Last().geom.Distance(usefulStruct[uniformSide][0].geom);
                        nStart = 0;
                    }
                    initialSet = true;
                }
            }

            if (initialSet == false)
            {
                //没有已布, 情况C:插入均匀边第一个点
                if (usefulStruct[uniformSide].Count > 0)
                {
                    for (int i = 0; i < usefulStruct[uniformSide].Count; i++)
                    {
                        if (CheckIfInLaneHead(usefulStruct[uniformSide][i], laneList) == false)
                        {
                            uniformSideLayout.Add(usefulStruct[uniformSide][i], 0);
                            sum = 0;
                            nStart = 0;
                            break;
                        }
                    }
                }
            }

            return nStart;
        }

        /// <summary>
        /// 均匀对边
        /// </summary>
        /// <param name="usefulColumns"></param>
        /// <param name="usefulWalls"></param>
        /// <param name="uniformSide"></param>
        /// <param name="lane"></param>
        /// <param name="columnDistList"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="layout"></param>
        private void LayoutOppositeSide(int uniformSide, Dictionary<ThStruct, int> uniformSideLayout, LayoutEmgLightService layoutServer, List<ThLane> laneList, ref List<ThStruct> layout)
        {
            int nonUniformSide = uniformSide == 0 ? 1 : 0;

            List<ThStruct> nonUniformSideLayout = new List<ThStruct>();
            Point3d CloestPt = new Point3d();
            double distToNext = 0;
            Polyline ExtendPoly;

            if (layoutServer.UsefulStruct[nonUniformSide].Count == 0)
            {
                return;
            }

            if (uniformSideLayout.Count > 0)
            {
                if (uniformSideLayout.Count > 1)
                {
                    //第一个点是头点,不做任何事
                    if (uniformSideLayout.First().Value == -1)
                    {
                    }
                    else if (uniformSideLayout.First().Value == 1)
                    {
                        //均匀边每个分布,第一个点对边找点
                        layoutServer.prjPtToLine(uniformSideLayout.First().Key, out CloestPt);

                        ExtendPoly = StructUtils.CreateExtendPoly(CloestPt, layoutServer.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                        layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out var closestStruct);

                        AddToNonUniformLayoutList(layout, closestStruct, EmgLightCommon.TolLightRangeMin, laneList, ref nonUniformSideLayout);

                    }

                    //从第二个点开始处理
                    for (int i = 1; i < uniformSideLayout.Count; i++)
                    {
                        if (uniformSideLayout.ElementAt(i).Value == 1)
                        {
                            //均匀边每个分布
                            layoutServer.prjPtToLine(uniformSideLayout.ElementAt(i).Key, out CloestPt);
                            ExtendPoly = StructUtils.CreateExtendPoly(CloestPt, layoutServer.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                            layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out var closestStruct);

                            AddToNonUniformLayoutList(layout, closestStruct, EmgLightCommon.TolLightRangeMin, laneList, ref nonUniformSideLayout);
                        }
                        else if (uniformSideLayout.ElementAt(i).Value == 0)
                        {
                            //均匀边隔柱分布
                            layoutServer.findMidPointOnLine(uniformSideLayout.ElementAt(i - 1).Key.centerPt, uniformSideLayout.ElementAt(i).Key.centerPt, out var midPt);
                            ExtendPoly = StructUtils.CreateExtendPoly(midPt, layoutServer.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                            layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], midPt, ExtendPoly, out var closestStruct);

                            AddToNonUniformLayoutList(layout, closestStruct, EmgLightCommon.TolLightRangeMin, laneList, ref nonUniformSideLayout);
                        }
                    }
                }

                //处理最后一个点. 
                ExtendPoly = null;

                if (uniformSideLayout.Last().Value == -1)
                {
                    //对边最后一个点是非布点且对边大于1个点, 此点到前一点距离大于4000,在对面找布点
                    if (uniformSideLayout.Count > 1)
                    {
                        distToNext = uniformSideLayout.Last().Key.centerPt.DistanceTo(uniformSideLayout.ElementAt(uniformSideLayout.Count - 2).Key.centerPt);
                        if (distToNext > EmgLightCommon.TolLightRangeMin)
                        {
                            layoutServer.prjPtToLine(uniformSideLayout.Last().Key, out CloestPt);
                            ExtendPoly = StructUtils.CreateExtendPoly(CloestPt, layoutServer.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                        }
                    }
                }
                else if (uniformSideLayout.Last().Value == 0)
                {
                    //对边最后一个点是已布点,检查车道线后面长度是否需要对面补点
                    layoutServer.prjPtToLineEnd(uniformSideLayout.Last().Key, out var LastPartLines);
                    if (LastPartLines.Length > EmgLightCommon.TolLightRangeMax)
                    {
                        CloestPt = LastPartLines.GetPointAtDist(EmgLightCommon.TolLightRangeMax);
                        ExtendPoly = StructUtils.CreateExtendPoly(CloestPt, layoutServer.thLane.dir, EmgLightCommon.TolLightRangeMin, EmgLightCommon.TolLane);
                    }
                }
                if (ExtendPoly != null)
                {
                    layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out var closestStruct);
                    AddToNonUniformLayoutList(layout, closestStruct, EmgLightCommon.TolLightRangeMin, laneList , ref nonUniformSideLayout);
                }
            }
            else
            {
                //如果均匀边没有布点
                if (layoutServer.UsefulStruct[uniformSide].Count > 0)
                {
                    //均匀边有构建, 加入[均匀边]最后点
                    AddToNonUniformLayoutList(layout, layoutServer.UsefulStruct[uniformSide].Last(), EmgLightCommon.TolLightRangeMin, laneList, ref nonUniformSideLayout);
                }
                else
                {
                    //均匀边没有构建,在非均匀边每隔8500布置一个点
                    AddToNonUniformLayoutList(layout, layoutServer.UsefulStruct[nonUniformSide].First(), EmgLightCommon.TolLightRangeMin, laneList, ref nonUniformSideLayout);
                    var layIndex = 0;
                    for (int i = 1; i < layoutServer.UsefulStruct[nonUniformSide].Count; i++)
                    {
                        var dist = layoutServer.getCenterInLaneCoor(layoutServer.UsefulStruct[nonUniformSide][i]).X - layoutServer.getCenterInLaneCoor(layoutServer.UsefulStruct[nonUniformSide][layIndex]).X;
                        if (dist > EmgLightCommon.TolLightRangeMax)
                        {
                            layIndex = i;
                            AddToNonUniformLayoutList(layout, layoutServer.UsefulStruct[nonUniformSide][i], EmgLightCommon.TolLightRangeMin, laneList, ref nonUniformSideLayout);
                        }
                    }
                }
            }

            DrawUtils.ShowGeometry(nonUniformSideLayout.Select (x=>x.geom).ToList (), EmgLightCommon.LayerStructLayout, Color.FromColorIndex(ColorMethod.ByColor, 210), LineWeight.LineWeight050);
            layout.AddRange(nonUniformSideLayout.Distinct().ToList());
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



        /// <summary>
        /// 检查构建是否在车道线头部附近,防止布点到车道线头的墙
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="laneList"></param>
        /// <returns></returns>
        public bool CheckIfInLaneHead(ThStruct structure, List<ThLane> laneList)
        {
            bool bReturn = false;
            if (structure != null)
            {
                foreach (var l in laneList)
                {
                    if (l.headProtectPoly.Contains(structure.geom) || l.headProtectPoly.Intersects(structure.geom) ||
                      l.endProtectPoly.Contains(structure.geom) || l.endProtectPoly.Intersects(structure.geom))
                    {
                        bReturn = true;
                        break;
                    }
                }
            }

            return bReturn;
        }


        /// <summary>
        /// 构建加入均匀边
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="structure"></param>
        /// <param name="index"></param>
        /// <param name="tol"></param>
        /// <param name="cover"></param>
        /// <param name="uniformSideLayout"></param>
        private static void AddToUniformLayoutList(List<ThStruct> layout, ThStruct structure, int index, double tol, bool cover, ref Dictionary<ThStruct, int> uniformSideLayout)
        {
            var connectLayout = CheckIfInLayout(layout, structure, tol);
            ThStruct temp = null;

            if (connectLayout != null)
            {
                temp = connectLayout;
            }
            else
            {
                temp = structure;
            }

            if (uniformSideLayout.ContainsKey(temp) == false)
            {
                uniformSideLayout.Add(temp, index);
            }
            else
            {
                if (cover == true)
                {
                    uniformSideLayout[temp] = index;
                }
            }
        }

        /// <summary>
        /// 构建加入非均匀边布置
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="structure"></param>
        /// <param name="tol"></param>
        /// <param name="lanes"></param>
        /// <param name="nonUniformSideLayout"></param>
        private void AddToNonUniformLayoutList(List<ThStruct> layout, ThStruct structure, double tol, List<ThLane> laneList, ref List<ThStruct> nonUniformSideLayout)
        {
            var connectLayout = CheckIfInLayout(layout, structure, tol);

            ThStruct temp = null;
            if (connectLayout != null)
            {
                temp = connectLayout;
            }
            else
            {
                temp = structure;
            }

            var bAdd = CheckIfInLaneHead(temp, laneList);
            if (bAdd == false && temp != null)
            {
                nonUniformSideLayout.Add(temp);
            }
        }

        /// <summary>
        /// layout到structure TolRangeMin以内找离structure最近的,没有返回null
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="structure"></param>
        /// <param name="Tol"></param>
        /// <returns></returns>
        private static ThStruct CheckIfInLayout(List<ThStruct> layout, ThStruct structure, double Tol)
        {
            double minDist = Tol + 1;
            ThStruct closestLayout = null;

            if (structure != null)
            {
                for (int i = 0; i < layout.Count; i++)
                {
                    var dist = layout[i].geom.StartPoint.DistanceTo(structure.geom.StartPoint);
                    if (dist <= minDist && dist < Tol)
                    {
                        minDist = dist;
                        closestLayout = layout[i];
                    }
                }
            }
            return closestLayout;
        }
    }
}
