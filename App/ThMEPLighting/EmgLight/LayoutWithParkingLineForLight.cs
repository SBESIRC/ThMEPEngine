using System;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Service;

namespace ThMEPLighting.EmgLight
{
    class LayoutWithParkingLineForLight
    {
        //readonly double protectRange = 27000;
        //readonly double oneProtect = 21000;

        //readonly int TolLane = 6000;
        //readonly int TolLengthSide = 400;
        //readonly int TolUniformSideColumnDistVariance = 5000;
        //readonly double TolUniformSideLenth = 0.6;
        //readonly int TolAvgColumnDist = 7900;
        //readonly int TolLightRangeMin = 4000;
        //readonly int TolLightRengeMax = 8500;

        int TolLane = 6000;
        int TolLengthSide = 400;
        int TolUniformSideColumnDistVariance = 5000;
        double TolUniformSideLenth = 0.6;
        int TolAvgColumnDist = 7900;
        int TolLightRangeMin = 4000;
        int TolLightRangeMax = 8500;


        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="mainLines"></param>
        /// <param name="otherLines"></param>
        /// <param name="roomPoly"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<Polyline, (Point3d, Vector3d)> LayoutLight(Polyline frame, List<List<Line>> mainLines, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<Polyline, (Point3d, Vector3d)> layoutPtInfo = new Dictionary<Polyline, (Point3d, Vector3d)>();
            List<Polyline> layoutList = new List<Polyline>();

            for (int i = 0; i < mainLines.Count; i++)
            {
                //List<Polyline> LayoutTemp = new List<Polyline>();
                //var lines = l.Select(x => x.Normalize()).ToList();
                var lane = mainLines[i];
                //ParkingLinesService parkingLinesService = new ParkingLinesService();
                //var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //特别短的线跳过
                var laneLength = lane.Sum(x => x.Length);
                if (laneLength < TolLightRangeMin)
                {
                    continue;
                }

                //找到构建上可布置面,用第一条车道线的头尾判定
                var filterColmuns = StructureServiceLight.FilterStructure(columns, lane.First(), frame, "c");
                var filterWalls = StructureServiceLight.FilterStructure(walls, lane.First(), frame, "w");

                ////获取该车道线上的构建
                //StructureServiceLight structureService = new StructureServiceLight();
                var closeColumn = StructureServiceLight.GetStruct(lane, filterColmuns, TolLane);
                var closeWall = StructureServiceLight.GetStruct(lane, filterWalls, TolLane);

                //破墙
                var brokeWall = StructureLayoutServiceLight.breakWall(closeWall);

                ////将构建分为上下部分
                var usefulColumns = StructureServiceLight.SeparateColumnsByLine(closeColumn, lane, TolLane);
                var usefulWalls = StructureServiceLight.SeparateColumnsByLine(brokeWall, lane, TolLane);

                ////for debug
                //InsertLightService.ShowGeometry(usefulColumns[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulColumns[1], 11, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[1], 11, LineWeight.LineWeight035);

                if (usefulColumns[0].Count == 0 && usefulColumns[1].Count == 0 && usefulWalls[0].Count == 0 && usefulWalls[1].Count == 0)
                {
                    continue;
                }

                ////找出平均的一边. -1:no side 0:left 1:right.
                bool debug = false;

                if (debug == false)
                {

                    int uniformSide = FindUniformDistributionSide(ref usefulColumns, lane, out var columnDistList);

                    //uniformSide = 0;
                    //Polyline test = new Polyline();
                    //test.AddVertexAt(test.NumberOfVertices, new Point2d(455050, 434500), 0, 0, 0);
                    //test.AddVertexAt(test.NumberOfVertices, new Point2d(455550, 434500), 0, 0, 0);
                    //layoutList.Add(test);

                    if (uniformSide == 0 || uniformSide == 1)
                    {
                        LayoutUniformSide(usefulColumns, uniformSide, lane, columnDistList, out var uniformSideLayout, ref layoutList);
                        LayoutOppositeSide(usefulColumns, usefulWalls, uniformSide, lane, uniformSideLayout, ref layoutList);
                    }
                    else
                    {
                        List<List<Polyline>> usefulStruct = new List<List<Polyline>>();
                        usefulStruct.Add(new List<Polyline>());
                        usefulStruct[0].AddRange(usefulColumns[0]);
                        usefulStruct[0].AddRange(usefulWalls[0]);
                        usefulStruct.Add(new List<Polyline>());
                        usefulStruct[1].AddRange(usefulColumns[1]);
                        usefulStruct[1].AddRange(usefulWalls[1]);

                        usefulStruct[0] = StructureLayoutServiceLight.OrderingColumns(usefulStruct[0], lane);
                        usefulStruct[1] = StructureLayoutServiceLight.OrderingColumns(usefulStruct[1], lane);

                        uniformSide = usefulStruct[0].Count >= usefulStruct[1].Count ? 0 : 1;

                        List<List<double>> usefulStructDistList = new List<List<double>>();
                        usefulStructDistList.Add(StructureLayoutServiceLight.GetColumnDistList(usefulStruct[0]));
                        usefulStructDistList.Add(StructureLayoutServiceLight.GetColumnDistList(usefulStruct[1]));

                        InsertLightService.ShowGeometry(usefulStruct[1], 161, LineWeight.LineWeight050);

                        LayoutUniformSide(usefulStruct, uniformSide, lane, usefulStructDistList, out var uniformSideLayout, ref layoutList);
                        LayoutOppositeSide(usefulColumns, usefulWalls, uniformSide, lane, uniformSideLayout, ref layoutList);

                        //LayoutBothNonUniformSide(usefulColumns, usefulWalls, lane, ref layoutList);
                    }

                }

                StructureLayoutServiceLight.AddLayoutStructPt(layoutList, lane, ref layoutPtInfo);

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
        private int FindUniformDistributionSide(ref List<List<Polyline>> usefulColumns, List<Line> lines, out List<List<double>> distList)
        {
            //上下排序
            usefulColumns[0] = StructureLayoutServiceLight.OrderingColumns(usefulColumns[0], lines);
            usefulColumns[1] = StructureLayoutServiceLight.OrderingColumns(usefulColumns[1], lines);

            distList = new List<List<double>>();
            distList.Add(StructureLayoutServiceLight.GetColumnDistList(usefulColumns[0]));
            distList.Add(StructureLayoutServiceLight.GetColumnDistList(usefulColumns[1]));

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

            //柱数量 > ((车道/平均柱距) * 0.5) 且 柱数量>=4个
            if (bLeft == false || usefulColumns[0].Count() < 4 || usefulColumns[0].Count() < (lineLength / TolAvgColumnDist) * 0.5)
            {
                bLeft = false;
            }

            if (bRight == false || usefulColumns[1].Count() < 4 || usefulColumns[1].Count() < (lineLength / TolAvgColumnDist) * 0.5)
            {
                bRight = false;
            }

            //方差
            if (bLeft == true)
            {
                nVarianceLeft = StructureLayoutServiceLight.GetVariance(distList[0]);
            }

            if (bRight == true)
            {
                nVarianceRight = StructureLayoutServiceLight.GetVariance(distList[1]);
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

        private void LayoutFirstBothNonUniformSide(List<Polyline> Layout, List<List<Polyline>> usefulSturct, List<Line> Lines, ref List<Polyline> ThisLaneLayout, out int currSide, out Point3d ptOnLine)
        {

            var ExtendLineList = LaneHeadExtend(Lines, TolLightRangeMin);

            var FilteredLayout = StructureServiceLight.GetStruct(ExtendLineList, Layout, TolLane);
            Polyline fisrtStruct = new Polyline();
            double distLeft = -1;
            double distRight = -1;
            Point3d ptOnLineLeft = new Point3d();
            Point3d ptOnLineRight = new Point3d();
            ptOnLine = new Point3d();
            currSide = -1;

            if (FilteredLayout.Count > 0)
            {

                FilteredLayout = StructureLayoutServiceLight.OrderingColumns(FilteredLayout, ExtendLineList);
                fisrtStruct = FilteredLayout.Last();
                StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(fisrtStruct), out ptOnLine);

                var importLayout = StructureServiceLight.SeparateColumnsByLine(FilteredLayout, ExtendLineList, TolLane);

                if (importLayout[0].Contains(fisrtStruct))
                {
                    currSide = 0;
                }
                else
                {
                    currSide = 1;
                }
                //找排序最后一个layout

            }
            else
            {
                if (usefulSturct[0] != null && usefulSturct[0].Count > 0)
                {
                    StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[0][0]), out ptOnLineLeft);
                    distLeft = ptOnLineLeft.DistanceTo(Lines[0].StartPoint);
                }
                if (usefulSturct[1] != null && usefulSturct[1].Count > 0)
                {
                    StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[1][0]), out ptOnLineRight);
                    distRight = ptOnLineRight.DistanceTo(Lines[0].StartPoint);
                }

                //哪个点的投影点到车道线起点最近 (到车道线最后点最远)
                if (0 <= distLeft && (distLeft <= distRight || distRight < 0))
                {
                    currSide = 0;
                    ptOnLine = ptOnLineLeft;
                    fisrtStruct = usefulSturct[0][0];
                }
                else if (0 <= distRight && (distRight <= distLeft || distLeft < 0))
                {
                    currSide = 1;
                    ptOnLine = ptOnLineRight;
                    fisrtStruct = usefulSturct[1][0];
                }
            }

            if (fisrtStruct.NumberOfVertices > 0)
            {
                ThisLaneLayout.Add(fisrtStruct);
                currSide = currSide == 0 ? 1 : 0;
            }


        }

        private void LayoutBothNonUniformSide(List<List<Polyline>> Columns, List<List<Polyline>> Walls, List<Line> Lines, ref List<Polyline> Layout)
        {
            List<List<Polyline>> usefulStruct = new List<List<Polyline>>();
            usefulStruct.Add(new List<Polyline>());
            usefulStruct[0].AddRange(Columns[0]);
            usefulStruct[0].AddRange(Walls[0]);
            usefulStruct.Add(new List<Polyline>());
            usefulStruct[1].AddRange(Columns[1]);
            usefulStruct[1].AddRange(Walls[1]);

            usefulStruct[0] = StructureLayoutServiceLight.OrderingColumns(usefulStruct[0], Lines);
            usefulStruct[1] = StructureLayoutServiceLight.OrderingColumns(usefulStruct[1], Lines);

            List<Polyline> ThisLaneLayout = new List<Polyline>();

            //第一个点
            LayoutFirstBothNonUniformSide(Layout, usefulStruct, Lines, ref ThisLaneLayout, out int currSide, out Point3d ptOnLine);

            bool bEnd = false;
            var moveDir = (Lines[0].EndPoint - Lines[0].StartPoint).GetNormal();
            bool bBothSide = false;
            double TolRangeMaxHalf = TolLightRangeMax / 2;

            while (bEnd == false && currSide >= 0)
            {
                //判断到车段线末尾距离是否还需要加灯
                if (StructureLayoutServiceLight.distToLineEnd(Lines, ptOnLine, out var PolylineToEnd) >= TolRangeMaxHalf)
                {
                    // bEnd = true;
                    ////建立当前点距离 tolLightRengeMax/2 前后TolLightRangeMin框
                    //var ExtendLineStart = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf - TolLightRangeMin);
                    //var ExtendLineEnd = ExtendLineStart + moveDir * (TolLightRangeMin * 2);
                    //var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                    //var ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane, 0, TolLane, 0);

                    ////建立当前点距离tolLightRengeMax/2 后TolRangeMaxHalf框
                    //var ExtendLineStart = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf);
                    //var ExtendLineEnd = ExtendLineStart + moveDir * TolRangeMaxHalf;
                    //var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                    //var ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane, 0, TolLane, 0);
                    //InsertLightService.ShowGeometry(ExtendPoly, 44);

                    var ExtendLineStart = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf);
                    var ExtendLineEnd = ExtendLineStart + moveDir * TolRangeMaxHalf;
                    var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                    var ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane, 0, TolLane, 0);
                    //InsertLightService.ShowGeometry(ExtendPoly, 44);

                    Polyline tempStruct;
                    //找框内对面是否有位置布灯
                    var bAdded = StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulStruct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);

                    if (bAdded == true)
                    {

                        //框内对面有位置布灯
                        ThisLaneLayout.Add(tempStruct);
                        currSide = currSide == 0 ? 1 : 0;

                        if (bBothSide == true)
                        {
                            StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulStruct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);
                            if (bAdded == true)
                            {
                                //框内对面有位置布灯
                                ThisLaneLayout.Add(tempStruct);
                                currSide = currSide == 0 ? 1 : 0;

                            }

                        }

                        StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);

                    }
                    else
                    {
                        //查看是否线出框
                        if (PolylineToEnd.Length <= TolRangeMaxHalf * 2)
                        {
                            break;
                        }

                        //debug, not tested yet
                        //框内对面没有位置布灯, 在自己边框内找
                        currSide = currSide == 0 ? 1 : 0;
                        bAdded = StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulStruct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);

                        if (bAdded == true)
                        {
                            //框内自己边有位置布灯
                            ThisLaneLayout.Add(tempStruct);
                            currSide = currSide == 0 ? 1 : 0;
                            StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
                        }
                        else
                        {
                            //debug, not tested yet
                            //框内 对边 和 自己边 没有, 找起点对面TolLightRengeMin内的布灯位置
                            ExtendLineStart = ptOnLine;
                            ExtendLineEnd = ExtendLineStart + moveDir * TolLightRangeMin;
                            ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                            ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane, 0, TolLane, 0);

                            currSide = currSide == 0 ? 1 : 0;
                            //找框内对面是否有位置布灯
                            bAdded = StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulStruct[currSide], PolylineToEnd, TolLightRangeMin, Layout, out tempStruct);

                            if (bAdded == true)
                            {
                                //debug, not tested yet
                                //框内对面有位置布灯
                                ThisLaneLayout.Add(tempStruct);
                                currSide = currSide == 0 ? 1 : 0;
                                StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
                                ptOnLine = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf);
                            }
                            else
                            {
                                //debug, not tested yet
                                //啥都没有
                                ptOnLine = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf);

                            }

                            bBothSide = true;

                        }

                    }

                }
                else
                {
                    bEnd = true;
                }

            }
            InsertLightService.ShowGeometry(ThisLaneLayout, 40, LineWeight.LineWeight050);
            Layout.AddRange(ThisLaneLayout.Distinct().ToList());

        }

        /// <summary>
        /// 车道线往前做框buffer
        /// </summary>
        /// <param name="Lines"></param>
        /// <returns></returns>
        private List<Line> LaneHeadExtend(List<Line> Lines, double tol)
        {
            var moveDir = (Lines[0].EndPoint - Lines[0].StartPoint).GetNormal();
            var ExtendLineStart = Lines[0].StartPoint - moveDir * tol;
            var ExtendLineEnd = Lines[0].StartPoint + moveDir * tol;
            var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
            var ExtendLineList = new List<Line>();
            ExtendLineList.Add(ExtendLine);

            return ExtendLineList;
        }

        /// <summary>
        /// 均匀边
        /// </summary>
        /// <param name="usefulColumns"></param>
        /// <param name="uniformSide"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="layout"></param>
        private void LayoutUniformSide(List<List<Polyline>> usefulColumns, int uniformSide, List<Line> Lines, List<List<double>> distList, out List<(Polyline, int)> uniformSideLayout, ref List<Polyline> layout)
        {
            int layoutStatus = 0; //0:非布灯状态,1:布灯状态
            uniformSideLayout = new List<(Polyline, int)>();
            double cumulateDist = 0;

            int initial = LayoutFirstUniformSide(layout, usefulColumns, uniformSide, Lines, ref uniformSideLayout, ref layoutStatus, ref cumulateDist);

            if (initial >= 0)
            {
                for (int i = initial; i < usefulColumns[uniformSide].Count; i++)
                {
                    if (i < usefulColumns[uniformSide].Count - 1)
                    {
                        cumulateDist += distList[uniformSide][i];
                        if (cumulateDist > TolLightRangeMax)
                        {
                            //累计距离到下个柱距离>tol, 本柱需标记
                            if (layoutStatus != 0)
                            {
                                //如果当前状态为布灯状态,将本柱加入
                                StructureLayoutServiceLight.checkIfInLayout(layout, usefulColumns[uniformSide][i], i, ref uniformSideLayout);
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
                            if (layoutStatus != 0)
                            {
                                //如果当前状态为布灯状态,将自己和下个柱加入,累计距离清零
                                StructureLayoutServiceLight.checkIfInLayout(layout, usefulColumns[uniformSide][i], i, ref uniformSideLayout);
                                StructureLayoutServiceLight.checkIfInLayout(layout, usefulColumns[uniformSide][i + 1], i + 1, ref uniformSideLayout);
                                layoutStatus = 0;
                                cumulateDist = 0;
                            }
                            else
                            {
                                //如果当前状态为非布灯状态,将下个柱加入,累计距离清零
                                StructureLayoutServiceLight.checkIfInLayout(layout, usefulColumns[uniformSide][i + 1], i + 1, ref uniformSideLayout);
                                layoutStatus = 0;
                                cumulateDist = 0;
                            }
                        }
                    }
                    else
                    {
                        //最后一个点特殊处理
                        if (layoutStatus != 0)
                        {
                            StructureLayoutServiceLight.checkIfInLayout(layout, usefulColumns[uniformSide][i], i, ref uniformSideLayout);
                        }

                    }

                }
            }

            InsertLightService.ShowGeometry(uniformSideLayout.Select(x => x.Item1).ToList(), 70, LineWeight.LineWeight050);
            layout.AddRange(uniformSideLayout.Select(x => x.Item1).ToList());

        }




        private int LayoutFirstUniformSide(List<Polyline> layout, List<List<Polyline>> usefulStruct, int uniformSide, List<Line> Lines, ref List<(Polyline, int)> uniformSideLayout, ref int LastHasNoLightColumn, ref double sum)
        {
            //   A|   |B    |E       [uniform side]
            //-----s[-----------lane----------]e
            //   C|   |D
            //
            //
            //not tested yet
            int nStart = 0;
            int otherSide = uniformSide == 0 ? 1 : 0;
            List<Polyline> otherSidePoint = new List<Polyline>();
            List<Polyline> uniformSidePoint = new List<Polyline>();

            //  bool added = false;
            if (layout.Count > 0)
            {
                // //车道线往前做框buffer
                var ExtendLineList = LaneHeadExtend(Lines, TolLightRangeMin);

                var FilteredLayout = StructureServiceLight.GetStruct(ExtendLineList, layout, TolLane);

                var importLayout = StructureServiceLight.SeparateColumnsByLine(FilteredLayout, ExtendLineList, TolLane);

                importLayout[0] = StructureLayoutServiceLight.OrderingColumns(importLayout[0], ExtendLineList);
                importLayout[1] = StructureLayoutServiceLight.OrderingColumns(importLayout[1], ExtendLineList);

                //有bug, 前一柱的左边布线也会算,暂时不考虑
                if (usefulStruct[otherSide].Count > 0)
                {
                    otherSidePoint = importLayout[otherSide].Where(x => x.StartPoint == usefulStruct[otherSide][0].StartPoint ||
                                                                   x.StartPoint == usefulStruct[otherSide][0].EndPoint ||
                                                                   x.EndPoint == usefulStruct[otherSide][0].StartPoint ||
                                                                   x.EndPoint == usefulStruct[otherSide][0].EndPoint).ToList();
                }
                if (usefulStruct[uniformSide].Count > 0)
                {
                    uniformSidePoint = importLayout[uniformSide].Where(x => x.StartPoint == usefulStruct[uniformSide][0].StartPoint ||
                                                                    x.StartPoint == usefulStruct[uniformSide][0].EndPoint ||
                                                                    x.EndPoint == usefulStruct[uniformSide][0].StartPoint ||
                                                                    x.EndPoint == usefulStruct[uniformSide][0].EndPoint).ToList();
                }
                if (uniformSidePoint.Count > 0)
                {
                    //情况B:
                    uniformSideLayout.Add((importLayout[uniformSide][0], 0));
                    LastHasNoLightColumn = 0;
                    sum = 0;
                    nStart = 0;

                }
                else if (otherSidePoint.Count > 0)
                {
                    //情况D:
                    if (usefulStruct[uniformSide].Count > 1)
                    {

                        uniformSideLayout.Add((usefulStruct[uniformSide][1], 1));
                        LastHasNoLightColumn = 0;
                        sum = 0;
                        nStart = 1;
                    }

                }
                else if (importLayout[uniformSide].Count > 0)
                {
                    //情况A:
                    if (usefulStruct[uniformSide].Count > 0)
                    {
                        uniformSideLayout.Add((importLayout[uniformSide][0], -1));
                        LastHasNoLightColumn = 0;
                        sum = importLayout[uniformSide][0].Distance(usefulStruct[uniformSide][0]);
                        nStart = 0;
                    }
                }

                else
                {
                    //情况C:
                    if (usefulStruct[uniformSide].Count > 0)
                    {
                        uniformSideLayout.Add((usefulStruct[uniformSide][0], 0));
                        LastHasNoLightColumn = 0;
                        sum = 0;
                        nStart = 0;
                    }
                }

            }
            else
            {
                if (usefulStruct[uniformSide].Count > 0)
                {
                    uniformSideLayout.Add((usefulStruct[uniformSide][0], 0));
                    LastHasNoLightColumn = 0;
                    sum = 0;
                    nStart = 0;
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
        /// <param name="lines"></param>
        /// <param name="columnDistList"></param>
        /// <param name="uniformSideLayout"></param>
        /// <param name="Layout"></param>
        private void LayoutOppositeSide(List<List<Polyline>> usefulColumns, List<List<Polyline>> usefulWalls, int uniformSide, List<Line> lines, List<(Polyline, int)> uniformSideLayout, ref List<Polyline> Layout)
        {
            int nonUniformSide = uniformSide == 0 ? 1 : 0;

            List<Polyline> nonUniformSideLayout = new List<Polyline>();
            List<Polyline> usefulStruct = new List<Polyline>();
            usefulStruct.AddRange(usefulWalls[nonUniformSide]);
            usefulStruct.AddRange(usefulColumns[nonUniformSide]);

            usefulStruct = StructureLayoutServiceLight.OrderingColumns(usefulStruct, lines);

            Polyline closestStruct;
            double minDist = 10000;
            Point3d CloestPt;
            Point3d midPt;
            double distToNext = 0;


            if (uniformSideLayout.Count > 0)
            {
                //有可能中间有很近的柱墙导致对边布点情况的index不连续
                if (uniformSideLayout.Count > 1)
                {
                    distToNext = uniformSideLayout[0].Item1.StartPoint.DistanceTo(uniformSideLayout[1].Item1.StartPoint);

                    //第一个点
                    if (uniformSideLayout[0].Item2 == -1)
                    {

                    }
                    else if (uniformSideLayout[0].Item2 == uniformSideLayout[1].Item2 - 1 || distToNext <= TolLightRangeMax)
                    {

                        //均匀边每个分布
                        StructureLayoutServiceLight.distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[0].Item1), out CloestPt);
                        StructureLayoutServiceLight.findClosestStruct(usefulStruct, CloestPt, Layout, out minDist, out closestStruct);
                        nonUniformSideLayout.Add(closestStruct);
                    }

                    //从第二个点开始处理
                    for (int i = 1; i < uniformSideLayout.Count; i++)
                    {
                        distToNext = uniformSideLayout[i - 1].Item1.StartPoint.DistanceTo(uniformSideLayout[i].Item1.StartPoint);

                        if (uniformSideLayout[i - 1].Item2 == uniformSideLayout[i].Item2 - 1 || distToNext <= TolLightRangeMax)
                        {
                            //均匀边每个分布
                            StructureLayoutServiceLight.distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i].Item1), out CloestPt);
                            StructureLayoutServiceLight.findClosestStruct(usefulStruct, CloestPt, Layout, out minDist, out closestStruct);
                            nonUniformSideLayout.Add(closestStruct);
                        }
                        else
                        {

                            //均匀边隔柱分布
                            StructureLayoutServiceLight.findMidPointOnLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i - 1].Item1), StructUtils.GetStructCenter(uniformSideLayout[i].Item1), out midPt);

                            //遍历所有对面柱墙,可能会很慢
                            StructureLayoutServiceLight.findClosestStruct(usefulStruct, midPt, Layout, out minDist, out closestStruct);
                            nonUniformSideLayout.Add(closestStruct);

                        }

                    }
                }

                //处理最后一个点.
                //Polyline LastPartLines;
                //double distToLinesEnd = distToLineEnd(lines, StructUtils.GetStructCenter(uniformSideLayout.Last()), out LastPartLines);
                ////double localRange = distToLinesEnd > TolLightRangeMax ? TolLightRangeMax : TolLightRangeMin;
                //double localRange = TolLightRangeMax;
                //if (distToLinesEnd > localRange)
                //{
                //    var LastPrjPtOnLine = LastPartLines.GetPointAtDist(localRange);
                //    findClosestStruct(usefulSturct, LastPrjPtOnLine, out minDist, out closestStruct);
                //    if (StructUtils.GetStructCenter(closestStruct).DistanceTo(lines.Last().GetClosestPointTo(StructUtils.GetStructCenter(closestStruct), false)) <= TolLightRangeMin && nonUniformSideLayout.Contains(closestStruct) == false)
                //    {
                //        nonUniformSideLayout.Add(closestStruct);
                //    }

                //}

                if (uniformSideLayout.Last().Item2 != usefulColumns[uniformSide].Count - 1)
                {
                    //最后一点标旗2,找对面点


                    StructureLayoutServiceLight.distToLine(lines, StructUtils.GetStructCenter(usefulColumns[uniformSide].Last()), out var LastPrjPtOnLine);
                    StructureLayoutServiceLight.findClosestStruct(usefulStruct, LastPrjPtOnLine, Layout, out minDist, out closestStruct);

                    nonUniformSideLayout.Add(closestStruct);




                }


                InsertLightService.ShowGeometry(nonUniformSideLayout, 210, LineWeight.LineWeight050);
                Layout.AddRange(nonUniformSideLayout.Distinct().ToList());
            }
        }




    }
}
