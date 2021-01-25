using System;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Assistant;
using Autodesk.AutoCAD.Colors;

namespace ThMEPLighting.EmgLight
{
    class LayoutWithParkingLineForLight
    {

        int TolLane = 6000;
        double TolUniformSideLenth = 0.6;
        int TolAvgColumnDist = 7900;
        int TolLightRangeMin = 4000;
        int TolLightRangeMax = 8900;
        int TolLaneProtect = 1000;

        string LayerStruct = "lStruct";
        string LayerStructLayout = "lStructLayout";
        string LayerExtendPoly = "lExtendPoly";

        Dictionary<Line, (Polyline, Polyline)> laneHeadProtectRect = new Dictionary<Line, (Polyline, Polyline)>();

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
            List<Polyline> layoutList = new List<Polyline>();

            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];

                //特别短的线跳过
                var laneLength = lane.Sum(x => x.Length);
                if (laneLength < TolLightRangeMin)
                {
                    continue;
                }
                //跳过完全没有可布点的线
                if (columns.Count == 0 && walls.Count == 0)
                {
                    continue;
                }

                DrawUtils.ShowGeometry(frame, "lFrame", Color.FromRgb(0, 255, 255), LineWeight.LineWeight035);
                
                //获取该车道线上的构建
                var closeColumn = StructureServiceLight.GetStruct(lane, columns, TolLane);
                var closeWall = StructureServiceLight.GetStruct(lane, walls, TolLane);

                //找到构建上可布置面,用第一条车道线的头尾判定
                var filterColmuns = StructureServiceLight.getStructureParallelPart(closeColumn, lane);
                var filterWalls = StructureServiceLight.getStructureParallelPart(closeWall, lane);

                //破墙
                var brokeWall = StructureServiceLight.breakWall(filterWalls);

                //将构建按车道线方向分成左(0)右(1)两边
                var usefulColumns = StructureServiceLight.SeparateColumnsByLine(filterColmuns, lane, TolLane);
                var usefulWalls = StructureServiceLight.SeparateColumnsByLine(brokeWall, lane, TolLane);

                StructureLayoutServiceLight layoutServer = new StructureLayoutServiceLight(usefulColumns, usefulWalls, lane, frame, TolLightRangeMin, TolLightRangeMax);

                //滤掉重合部分
                layoutServer.filterOverlapStruc();
               
                //滤掉框外边的部分
                layoutServer.getInsideFramePart();
                
                //滤掉框后边的部分
                layoutServer.filterStrucBehindFrame();
                DrawUtils.ShowGeometry(layoutServer.UsefulStruct[0], LayerStruct, Color.FromRgb(0, 155, 187), LineWeight.LineWeight035);
                DrawUtils.ShowGeometry(layoutServer.UsefulStruct[1], LayerStruct, Color.FromRgb(247, 129, 144), LineWeight.LineWeight035);

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
                int uniformSide = FindUniformDistributionSide(layoutServer, lane, out var columnDistList);

                Dictionary<Polyline, int> uniformSideLayout = null;
                if (uniformSide == 0 || uniformSide == 1)
                {
                    //有均匀边情况
                    LayoutUniformSide(layoutServer.UsefulColumns, uniformSide, columnDistList, layoutServer, lanes, out uniformSideLayout, ref layoutList);
                    LayoutOppositeSide(uniformSide, lane, uniformSideLayout, layoutServer, lanes, ref layoutList);
                }
                else
                {
                    //两边都不均匀情况,找柱多的一边,且柱子数>2 以这一边先布, 否则找构建多的一边先布
                    uniformSide = layoutServer.UsefulColumns[0].Count >= layoutServer.UsefulColumns[1].Count ? 0 : 1;

                    if (layoutServer.UsefulColumns[uniformSide].Count > 2)
                    {
                        LayoutUniformSide(layoutServer.UsefulColumns, uniformSide, columnDistList, layoutServer, lanes, out uniformSideLayout, ref layoutList);
                    }
                    else
                    {
                        uniformSide = layoutServer.UsefulStruct[0].Count >= layoutServer.UsefulStruct[1].Count ? 0 : 1;

                        columnDistList = new List<List<double>>();
                        columnDistList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulStruct[0]));
                        columnDistList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulStruct[1]));

                        LayoutUniformSide(layoutServer.UsefulStruct, uniformSide, columnDistList, layoutServer, lanes, out uniformSideLayout, ref layoutList);
                    }
                    LayoutOppositeSide(uniformSide, lane, uniformSideLayout, layoutServer, lanes, ref layoutList);
                }

                layoutServer.AddLayoutStructPt(layoutList, lane, ref layoutPtInfo);
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
        private int FindUniformDistributionSide(StructureLayoutServiceLight layoutServer, List<Line> lines, out List<List<double>> distList)
        {
            //上下排序

            distList = new List<List<double>>();
            distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulColumns[0]));
            distList.Add(layoutServer.GetColumnDistList(layoutServer.UsefulColumns[1]));

            double lineLength = 0;
            lines.ForEach(l => lineLength += l.Length);
            bool bLeft = true;
            bool bRight = true;
            double nVarianceLeft = -1;
            double nVarianceRight = -1;
            int nUniformSide = -1; //-1:no side, 0:left, 1:right

            //柱间距总长度>=车道线总长度的60% 
            if ((bLeft == false) || distList[0].Sum() / lineLength < TolUniformSideLenth)
            {
                bLeft = false;
            }

            if ((bRight == false) || distList[1].Sum() / lineLength < TolUniformSideLenth)
            {
                bRight = false;
            }

            //柱数量 > ((车道/平均柱距) * 0.5) 且 柱数量>=3个
            if (bLeft == false || layoutServer.UsefulColumns[0].Count() < 3 || layoutServer.UsefulColumns[0].Count() < (lineLength / TolAvgColumnDist) * 0.5)
            {
                bLeft = false;
            }

            if (bRight == false || layoutServer.UsefulColumns[1].Count() < 3 || layoutServer.UsefulColumns[1].Count() < (lineLength / TolAvgColumnDist) * 0.5)
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
        private void LayoutUniformSide(List<List<Polyline>> usefulStruct, int uniformSide, List<List<double>> distList, StructureLayoutServiceLight layoutServer, List<List<Line>> lanes, out Dictionary<Polyline, int> uniformSideLayout, ref List<Polyline> layout)
        {
            int layoutStatus = 0; //0:非布灯状态,1:布灯状态
            uniformSideLayout = new Dictionary<Polyline, int>(); //int 为不均匀边布灯的标旗: -1: 头部已经layout的,或尾点对面需要布灯的, -1 标旗不计入均匀边最后的layout 0:和前面隔点, 1:对边
            double cumulateDist = 0;

            //找车道线前段的已分布状况,决定第一个点
            int initial = LayoutFirstUniformSide(layout, usefulStruct, uniformSide, layoutServer, lanes, ref uniformSideLayout, ref layoutStatus, ref cumulateDist);

            if (initial < usefulStruct[uniformSide].Count)
            {
                for (int i = initial; i < usefulStruct[uniformSide].Count; i++)
                {
                    if (i < usefulStruct[uniformSide].Count - 1)
                    {
                        cumulateDist += distList[uniformSide][i];
                        if (cumulateDist > TolLightRangeMax)
                        {
                            //累计距离到下个柱距离>tol, 本柱需标记
                            if (layoutStatus != 0)
                            {
                                //如果当前状态为布灯状态,将本柱加入
                                AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], 0, TolLightRangeMin, false, ref uniformSideLayout);
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
                        if (distList[uniformSide][i] > TolLightRangeMax)
                        {
                            //将自己和下个柱加入,累计距离清零
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], 1, TolLightRangeMin, true, ref uniformSideLayout);
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i + 1], 1, TolLightRangeMin, true, ref uniformSideLayout);
                            layoutStatus = 0;
                            cumulateDist = 0;
                        }
                    }
                    else
                    {
                        //最后一个点特殊处理
                        if (layoutStatus != 0)
                        {
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], 0, TolLightRangeMin, false, ref uniformSideLayout);
                        }
                        else
                        {
                            //如果末尾还有点,标值-1
                            AddToUniformLayoutList(layout, usefulStruct[uniformSide][i], -1, TolLightRangeMin, false, ref uniformSideLayout);
                        }
                    }
                }
            }
            else
            {
                AddToUniformLayoutList(layout, usefulStruct[uniformSide][initial], 0, TolLightRangeMin, false, ref uniformSideLayout);
            }

            DrawUtils.ShowGeometry(uniformSideLayout.Where(x => x.Value == 1 || x.Value == 0).Select(y => y.Key).ToList(), LayerStructLayout, Color.FromRgb(0, 255, 0), LineWeight.LineWeight050);
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
        private int LayoutFirstUniformSide(List<Polyline> layout, List<List<Polyline>> usefulStruct, int uniformSide, StructureLayoutServiceLight layoutServer, List<List<Line>> lanes, ref Dictionary<Polyline, int> uniformSideLayout, ref int LastHasNoLightColumn, ref double sum)
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
                var importLayout = layoutServer.BuildHeadLayout(layout, TolLane);

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
                    LastHasNoLightColumn = 0;
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
                            if (Math.Abs(dist) > TolLightRangeMax)
                            {
                                if (i > 0)
                                {
                                    if (CheckIfInLaneHead(usefulStruct[uniformSide][i - 1], lanes) == false)
                                    {
                                        uniformSideLayout.Add(usefulStruct[uniformSide][i - 1], 0);
                                        LastHasNoLightColumn = 0;
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
                        LastHasNoLightColumn = 0;
                        sum = importLayout[uniformSide].Last().Distance(usefulStruct[uniformSide][0]);
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
                        if (CheckIfInLaneHead(usefulStruct[uniformSide][i], lanes) == false)
                        {
                            uniformSideLayout.Add(usefulStruct[uniformSide][i], 0);
                            LastHasNoLightColumn = 0;
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
        private void LayoutOppositeSide(int uniformSide, List<Line> lane, Dictionary<Polyline, int> uniformSideLayout, StructureLayoutServiceLight layoutServer, List<List<Line>> lanes, ref List<Polyline> layout)
        {
            int nonUniformSide = uniformSide == 0 ? 1 : 0;

            List<Polyline> nonUniformSideLayout = new List<Polyline>();

            Polyline closestStruct;
            Point3d CloestPt = new Point3d();
            double distToNext = 0;
            Polyline ExtendPoly;

            if (layoutServer.UsefulStruct[nonUniformSide].Count == 0)
            {
                return;
            }

            var moveDir = lane.Last().EndPoint - lane.First().StartPoint;
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

                        ExtendPoly = CreateExtendPoly(CloestPt, moveDir, TolLightRangeMin, TolLane);
                        layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out closestStruct);

                        AddToNonUniformLayoutList(layout, closestStruct, TolLightRangeMin, lanes, ref nonUniformSideLayout);

                    }

                    //从第二个点开始处理
                    for (int i = 1; i < uniformSideLayout.Count; i++)
                    {
                        if (uniformSideLayout.ElementAt(i).Value == 1)
                        {
                            //均匀边每个分布
                            layoutServer.prjPtToLine(uniformSideLayout.ElementAt(i).Key, out CloestPt);
                            ExtendPoly = CreateExtendPoly(CloestPt, moveDir, TolLightRangeMin, TolLane);
                            layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out closestStruct);

                            AddToNonUniformLayoutList(layout, closestStruct, TolLightRangeMin, lanes, ref nonUniformSideLayout);
                        }
                        else if (uniformSideLayout.ElementAt(i).Value == 0)
                        {
                            //均匀边隔柱分布
                            layoutServer.findMidPointOnLine(layoutServer.getCenter(uniformSideLayout.ElementAt(i - 1).Key), layoutServer.getCenter(uniformSideLayout.ElementAt(i).Key), out var midPt);
                            ExtendPoly = CreateExtendPoly(midPt, moveDir, TolLightRangeMin, TolLane);
                            layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], midPt, ExtendPoly, out closestStruct);

                            AddToNonUniformLayoutList(layout, closestStruct, TolLightRangeMin, lanes, ref nonUniformSideLayout);
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
                        distToNext = layoutServer.getCenter(uniformSideLayout.Last().Key).DistanceTo(layoutServer.getCenter(uniformSideLayout.ElementAt(uniformSideLayout.Count - 2).Key));
                        if (distToNext > TolLightRangeMin)
                        {
                            layoutServer.prjPtToLine(uniformSideLayout.Last().Key, out CloestPt);
                            ExtendPoly = CreateExtendPoly(CloestPt, moveDir, TolLightRangeMin, TolLane);
                        }
                    }
                }
                else if (uniformSideLayout.Last().Value == 0)
                {
                    //对边最后一个点是已布点,检查车道线后面长度是否需要对面补点
                    layoutServer.prjPtToLineEnd(uniformSideLayout.Last().Key, out var LastPartLines);
                    if (LastPartLines.Length > TolLightRangeMax)
                    {
                        CloestPt = LastPartLines.GetPointAtDist(TolLightRangeMax);
                        ExtendPoly = CreateExtendPoly(CloestPt, moveDir, TolLightRangeMin, TolLane);
                    }
                }
                if (ExtendPoly != null)
                {
                    layoutServer.FindClosestStructToPt(layoutServer.UsefulStruct[nonUniformSide], CloestPt, ExtendPoly, out closestStruct);
                    AddToNonUniformLayoutList(layout, closestStruct, TolLightRangeMin, lanes, ref nonUniformSideLayout);
                }
            }
            else
            {
                //如果均匀边没有布点
                if (layoutServer.UsefulStruct[uniformSide].Count > 0)
                {
                    //均匀边有构建, 加入[均匀边]最后点
                    AddToNonUniformLayoutList(layout, layoutServer.UsefulStruct[uniformSide].Last(), TolLightRangeMin, lanes, ref nonUniformSideLayout);
                }
                else
                {
                    //均匀边没有构建,在非均匀边每隔8500布置一个点
                    AddToNonUniformLayoutList(layout, layoutServer.UsefulStruct[nonUniformSide].First(), TolLightRangeMin, lanes, ref nonUniformSideLayout);
                    var layIndex = 0;
                    for (int i = 1; i < layoutServer.UsefulStruct[nonUniformSide].Count; i++)
                    {
                        var dist = layoutServer.getCenterInLaneCoor(layoutServer.UsefulStruct[nonUniformSide][i]).X - layoutServer.getCenterInLaneCoor(layoutServer.UsefulStruct[nonUniformSide][layIndex]).X;
                        if (dist > TolLightRangeMax)
                        {
                            layIndex = i;
                            AddToNonUniformLayoutList(layout, layoutServer.UsefulStruct[nonUniformSide][i], TolLightRangeMin, lanes, ref nonUniformSideLayout);
                        }
                    }
                }
            }

            DrawUtils.ShowGeometry(nonUniformSideLayout, LayerStructLayout, Color.FromRgb(255, 0, 255), LineWeight.LineWeight050);
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
        /// 已pt点为中心做一个方框
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="moveDir"></param>
        /// <param name="tolX"></param>
        /// <param name="tolY"></param>
        /// <returns></returns>
        private Polyline CreateExtendPoly(Point3d pt, Vector3d moveDir, int tolX, int tolY)
        {
            moveDir = moveDir.GetNormal();
            var ExtendPolyStart = pt - moveDir * tolX;
            var ExtendPolyEnd = pt + moveDir * tolX;

            var ExtendLine = new Line(ExtendPolyStart, ExtendPolyEnd);
            var ExtendPoly = StructUtils.ExpandLine(ExtendLine, tolY, 0, tolY, 0);

            DrawUtils.ShowGeometry(ExtendPoly, LayerExtendPoly, Color.FromRgb(141, 118, 12));

            return ExtendPoly;
        }

        /// <summary>
        /// 检查构建是否在车道线头部附近,防止布点到车道线头的墙
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private bool CheckIfInLaneHead(Polyline structure, List<List<Line>> lanes)
        {
            bool bReturn = false;
            if (structure != null)
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    if (laneHeadProtectRect.ContainsKey(lanes[i][0]) == false)
                    {
                        var moveDir = lanes[i].Last().EndPoint - lanes[i].First().StartPoint;
                        var head = CreateExtendPoly(lanes[i].First().StartPoint, moveDir, TolLaneProtect, TolLaneProtect);
                        var end = CreateExtendPoly(lanes[i].Last().EndPoint, moveDir, TolLaneProtect, TolLaneProtect);
                        laneHeadProtectRect.Add(lanes[i][0], (head, end));
                    }
                    if (laneHeadProtectRect[lanes[i][0]].Item1.Contains(structure) || laneHeadProtectRect[lanes[i][0]].Item1.Intersects(structure) ||
                        laneHeadProtectRect[lanes[i][0]].Item2.Contains(structure) || laneHeadProtectRect[lanes[i][0]].Item2.Intersects(structure))
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
        private static void AddToUniformLayoutList(List<Polyline> layout, Polyline structure, int index, double tol, bool cover, ref Dictionary<Polyline, int> uniformSideLayout)
        {
            var connectLayout = CheckIfInLayout(layout, structure, tol);
            Polyline temp = null;
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
        private void AddToNonUniformLayoutList(List<Polyline> layout, Polyline structure, double tol, List<List<Line>> lanes, ref List<Polyline> nonUniformSideLayout)
        {
            var connectLayout = CheckIfInLayout(layout, structure, tol);
            Polyline temp = null;
            if (connectLayout != null)
            {
                temp = connectLayout;
            }
            else
            {
                temp = structure;
            }

            var bAdd = CheckIfInLaneHead(temp, lanes);
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
        private static Polyline CheckIfInLayout(List<Polyline> layout, Polyline structure, double Tol)
        {
            double minDist = Tol + 1;
            Polyline closestLayout = null;

            if (structure != null)
            {
                for (int i = 0; i < layout.Count; i++)
                {
                    var dist = layout[i].StartPoint.DistanceTo(structure.StartPoint);
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
