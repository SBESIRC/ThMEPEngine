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
        public Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> LayoutLight(Polyline frame, List<List<Line>> mainLines, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> layoutInfo = new Dictionary<List<Line>, Dictionary<Point3d, Vector3d>>();
            List<Polyline> Layout = new List<Polyline>();

            for (int i = 0; i < mainLines.Count; i++)
            {
                //List<Polyline> LayoutTemp = new List<Polyline>();
                //var lines = l.Select(x => x.Normalize()).ToList();
                var lane = mainLines[i];
                //ParkingLinesService parkingLinesService = new ParkingLinesService();
                //var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //特别短的线跳过
                if (lane[0].Length < TolLightRangeMin )
                {
                    continue;
                }

                //找到构建上可布置面,用第一条车道线的头尾判定
                var filterColmuns = StructureServiceLight.FilterStructure(columns, lane.First(), frame,"c");
                var filterWalls = StructureServiceLight.FilterStructure(walls, lane.First(), frame,"w");

                

                ////获取该车道线上的构建
                //StructureServiceLight structureService = new StructureServiceLight();
                var closeColumn = StructureServiceLight.GetStruct(lane, filterColmuns, TolLane);
                var closeWall = StructureServiceLight.GetStruct(lane, filterWalls, TolLane);

                //破墙
                var brokeWall = StructureLayoutServiceLight.breakWall(closeWall);

                //InsertLightService.ShowGeometry(brokeWall, 20, LineWeight.LineWeight050);

                ////将构建分为上下部分
                var usefulColumns = StructureServiceLight.SeparateColumnsByLine(closeColumn, lane, TolLane);
                var usefulWalls = StructureServiceLight.SeparateColumnsByLine(brokeWall, lane, TolLane);
                ////for debug
                //InsertLightService.ShowGeometry(usefulColumns[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulColumns[1], 11, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[1], 11, LineWeight.LineWeight035);

                if ((usefulColumns == null || usefulColumns .Count==0 ) && (usefulWalls == null || usefulWalls.Count == 0))
                {
                    continue;
                }
                

                ////找出平均的一边. -1:no side 0:left 1:right.
                bool debug = false;

                if (debug == false)
                {

                    int uniformSide = FindUniformDistributionSide(ref usefulColumns, lane, out var columnDistList);

                    if (uniformSide == 0 || uniformSide == 1)
                    {
                        LayoutUniformSide(usefulColumns, uniformSide, lane, columnDistList, out var uniformSideLayout, ref Layout);
                        LayoutOppositeSide(usefulColumns, usefulWalls, uniformSide, lane, columnDistList, uniformSideLayout, ref Layout);
                    }
                    else
                    {
                        LayoutBothNonUniformSide(usefulColumns, usefulWalls, lane, ref Layout);
                    }
                    //Layout.AddRange(LayoutTemp);
                }


            }

            return layoutInfo;
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


        /// <summary>
        /// using dist between columns directly, may has bug if the angle of line in two columns with lines is too large
        /// </summary>
        /// <param name="Columns"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="Layout"></param>
        private void LayoutUniformSide(List<List<Polyline>> Columns, int uniformSide, List<Line> Lines, List<List<double>> distList, out List<Polyline> uniformSideLayout, ref List<Polyline> Layout)
        {
            int LastHasNoLightColumn = 0;
            uniformSideLayout = new List<Polyline>();
            double sum = 0;

            int initial = LayoutFirstUniformSide(Layout, Columns, uniformSide, Lines, ref uniformSideLayout, ref LastHasNoLightColumn, ref sum);

            for (int i = initial; i < Columns[uniformSide].Count; i++)
            {
                if (i < Columns[uniformSide].Count - 1)
                {
                    sum += distList[uniformSide][i];
                    if (sum > TolLightRangeMax)
                    {
                        if (LastHasNoLightColumn != 0)
                        {
                            uniformSideLayout.Add(Columns[uniformSide][i]);
                            LastHasNoLightColumn = 0;
                        }
                        else
                        {
                            LastHasNoLightColumn = 1;
                        }
                        sum = distList[uniformSide][i];
                    }

                    if (distList[uniformSide][i] > TolLightRangeMax)
                    {

                        if (LastHasNoLightColumn != 0)
                        {
                            uniformSideLayout.Add(Columns[uniformSide][i]);
                            uniformSideLayout.Add(Columns[uniformSide][i + 1]);
                            LastHasNoLightColumn = 0;
                            sum = 0;
                        }
                        else
                        {
                            uniformSideLayout.Add(Columns[uniformSide][i + 1]);
                            LastHasNoLightColumn = 0;
                            sum = 0;
                        }


                    }
                }
                else
                {
                    //最后一个点特殊处理
                    if (LastHasNoLightColumn != 0)
                    {
                        uniformSideLayout.Add(Columns[uniformSide][i]);

                    }

                }

            }

            InsertLightService.ShowGeometry(uniformSideLayout, 70, LineWeight.LineWeight050);
            Layout.AddRange(uniformSideLayout.Distinct().ToList());

        }

        private void LayoutOppositeSide(List<List<Polyline>> usefulColumns, List<List<Polyline>> usefulWalls, int uniformSide, List<Line> lines, List<List<double>> columnDistList, List<Polyline> uniformSideLayout, ref List<Polyline> Layout)
        {
            int nonUniformSide = uniformSide == 0 ? 1 : 0;

            //usefulWalls[0] = OrderingColumns(usefulWalls[0], lines);
            //usefulWalls[1] = OrderingColumns(usefulWalls[1], lines);

            List<Polyline> nonUniformSideLayout = new List<Polyline>();
            List<Polyline> usefulSturct = new List<Polyline>();
            usefulSturct.AddRange(usefulWalls[nonUniformSide]);
            usefulSturct.AddRange(usefulColumns[nonUniformSide]);

            usefulSturct = StructureLayoutServiceLight.OrderingColumns(usefulSturct, lines);

            Polyline closestStruct;
            double minDist = 10000;
            Point3d CloestPt;
            Point3d midPt;

            //第一个点
            if (usefulColumns[uniformSide].IndexOf(uniformSideLayout[0]) == usefulColumns[uniformSide].IndexOf(uniformSideLayout[1]) - 1)
            {
                //均匀边每个分布
                StructureLayoutServiceLight.distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[0]), out CloestPt);
                StructureLayoutServiceLight.findClosestStruct(usefulSturct, CloestPt, Layout, out minDist, out closestStruct);
                nonUniformSideLayout.Add(closestStruct);
            }

            //从第二个点开始处理
            for (int i = 1; i < uniformSideLayout.Count; i++)
            {
                if (usefulColumns[uniformSide].IndexOf(uniformSideLayout[i - 1]) != usefulColumns[uniformSide].IndexOf(uniformSideLayout[i]) - 1)
                {
                    //均匀边隔柱分布
                    //distAlongLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i - 1]), StructUtils.GetStructCenter(uniformSideLayout[i]), out midPt);
                    StructureLayoutServiceLight.findMidPointOnLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i - 1]), StructUtils.GetStructCenter(uniformSideLayout[i]), out midPt);

                    //遍历所有对面柱墙,可能会很慢
                    StructureLayoutServiceLight.findClosestStruct(usefulSturct, midPt, Layout, out minDist, out closestStruct);
                    nonUniformSideLayout.Add(closestStruct);


                }
                else
                {
                    //均匀边每个分布
                    StructureLayoutServiceLight.distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i]), out CloestPt);
                    StructureLayoutServiceLight.findClosestStruct(usefulSturct, CloestPt, Layout, out minDist, out closestStruct);
                    nonUniformSideLayout.Add(closestStruct);

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

            if (usefulColumns[uniformSide].IndexOf(uniformSideLayout.Last()) != usefulColumns[uniformSide].Count - 1)
            {
                //最后一点标旗2,找对面点

                StructureLayoutServiceLight.distToLine(lines, StructUtils.GetStructCenter(usefulColumns[uniformSide].Last()), out var LastPrjPtOnLine);
                StructureLayoutServiceLight.findClosestStruct(usefulSturct, LastPrjPtOnLine, Layout, out minDist, out closestStruct);
                if (nonUniformSideLayout.Contains(closestStruct) == false)
                {
                    nonUniformSideLayout.Add(closestStruct);
                }


            }


            InsertLightService.ShowGeometry(nonUniformSideLayout, 210, LineWeight.LineWeight050);
            Layout.AddRange(nonUniformSideLayout.Distinct().ToList());

        }


        private int LayoutFirstUniformSide(List<Polyline> Layout, List<List<Polyline>> Columns, int uniformSide, List<Line> Lines, ref List<Polyline> uniformSideLayout, ref int LastHasNoLightColumn, ref double sum)
        {
            //   A|   |B    |E       [uniform side]
            //-----s[-----------lane----------]e
            //   C|   |D
            //
            //
            //not tested yet
            int nStart = 0;
            int otherSide = uniformSide == 0 ? 1 : 0;
            //  bool added = false;
            if (Layout.Count > 0)
            {
                // //车道线往前做框buffer
                var ExtendLineList = LaneHeadExtend(Lines, TolLightRangeMin);

                var FilteredLayout = StructureServiceLight.GetStruct(ExtendLineList, Layout, TolLane);

                var importLayout = StructureServiceLight.SeparateColumnsByLine(FilteredLayout, ExtendLineList, TolLane);

                importLayout[0] = StructureLayoutServiceLight.OrderingColumns(importLayout[0], ExtendLineList);
                importLayout[1] = StructureLayoutServiceLight.OrderingColumns(importLayout[1], ExtendLineList);

                InsertLightService.ShowGeometry(importLayout[0], 142, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(importLayout[1], 11, LineWeight.LineWeight035);


                //有bug, 前一柱的左边布线也会算,需要建立column的数据结构解决, 暂时不考虑
                var otherSidePoint = importLayout[otherSide].Where(x => x.StartPoint == Columns[otherSide][0].StartPoint ||
                                                                    x.StartPoint == Columns[otherSide][0].EndPoint ||
                                                                    x.EndPoint == Columns[otherSide][0].StartPoint ||
                                                                    x.EndPoint == Columns[otherSide][0].EndPoint).ToList();

                var uniformSidePoint = importLayout[uniformSide].Where(x => x.StartPoint == Columns[uniformSide][0].StartPoint ||
                                                                    x.StartPoint == Columns[uniformSide][0].EndPoint ||
                                                                    x.EndPoint == Columns[uniformSide][0].StartPoint ||
                                                                    x.EndPoint == Columns[uniformSide][0].EndPoint).ToList();
                //在优化:找几种情况里面,line的方向上最近的点
                if (uniformSidePoint.Count > 0)
                {
                    //情况B:
                    uniformSideLayout.Add(importLayout[uniformSide][0]);
                    LastHasNoLightColumn = 0;
                    sum = 0;
                    nStart = 0;

                }
                else if (otherSidePoint.Count > 0)
                {
                    //情况D:
                    uniformSideLayout.Add(Columns[uniformSide][1]);
                    LastHasNoLightColumn = 0;
                    sum = 0;
                    nStart = 1;


                }
                else if (importLayout[uniformSide].Count > 0)
                {
                    //情况A:
                    
                    uniformSideLayout.Add(importLayout[uniformSide][0]);
                    LastHasNoLightColumn = 0;
                    sum = importLayout[uniformSide][0].Distance(Columns[uniformSide][0]);
                    nStart = 0;

                }

                else
                {
                    //情况C:
                    uniformSideLayout.Add(Columns[uniformSide][0]);
                    LastHasNoLightColumn = 0;
                    sum = 0;
                    nStart = 0;
                }

            }
            else
            {
                uniformSideLayout.Add(Columns[uniformSide][0]);
                LastHasNoLightColumn = 0;
                sum = 0;
                nStart = 0;

            }


            return nStart;
        }

        private void LayoutFirstBothNonUniformSide(List<Polyline> Layout, List<List<Polyline>> usefulSturct, List<Line> Lines, ref List<Polyline> ThisLaneLayout, out int currSide, out Point3d ptOnLine)
        {

            var ExtendLineList = LaneHeadExtend(Lines, TolLightRangeMin);

            var FilteredLayout = StructureServiceLight.GetStruct(ExtendLineList, Layout, TolLane);
            var fisrtStruct = usefulSturct[0][0];

            if (FilteredLayout.Count > 0)
            {

                FilteredLayout = StructureLayoutServiceLight.OrderingColumns(FilteredLayout, ExtendLineList);
                fisrtStruct = FilteredLayout.Last();
                StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(fisrtStruct), out ptOnLine);

                var importLayout = StructureServiceLight.SeparateColumnsByLine(FilteredLayout, ExtendLineList, TolLane);
                InsertLightService.ShowGeometry(importLayout[0], 142, LineWeight.LineWeight035);
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

                double distLeft = StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[0][0]), out var ptOnLineLeft);
                double distRight = StructureLayoutServiceLight.distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[1][0]), out var ptOnLineRight);

                distLeft = ptOnLineLeft.DistanceTo(Lines[0].StartPoint);
                distRight = ptOnLineRight.DistanceTo(Lines[0].StartPoint);

                //哪个点的投影点到车道线起点最近 (到车道线最后点最远)
                if (distLeft <= distRight)
                {
                    currSide = 0;
                    ptOnLine = ptOnLineLeft;
                    fisrtStruct = usefulSturct[0][0];
                }
                else
                {
                    currSide = 1;
                    ptOnLine = ptOnLineRight;
                    fisrtStruct = usefulSturct[1][0];
                }
            }


            ThisLaneLayout.Add(fisrtStruct);
            currSide = currSide == 0 ? 1 : 0;



        }


        private void LayoutBothNonUniformSide(List<List<Polyline>> Columns, List<List<Polyline>> Walls, List<Line> Lines, ref List<Polyline> Layout)
        {
            List<List<Polyline>> usefulSturct = new List<List<Polyline>>();
            usefulSturct.Add(new List<Polyline>());
            usefulSturct[0].AddRange(Columns[0]);
            usefulSturct[0].AddRange(Walls[0]);
            usefulSturct.Add(new List<Polyline>());
            usefulSturct[1].AddRange(Columns[1]);
            usefulSturct[1].AddRange(Walls[1]);

            usefulSturct[0] = StructureLayoutServiceLight.OrderingColumns(usefulSturct[0], Lines);
            usefulSturct[1] = StructureLayoutServiceLight.OrderingColumns(usefulSturct[1], Lines);

            List<Polyline> ThisLaneLayout = new List<Polyline>();

            //第一个点
            LayoutFirstBothNonUniformSide(Layout, usefulSturct, Lines, ref ThisLaneLayout, out int currSide, out Point3d ptOnLine);

            bool bEnd = false;
            var moveDir = (Lines[0].EndPoint - Lines[0].StartPoint).GetNormal();
            bool bBothSide = false;
            double TolRangeMaxHalf = TolLightRangeMax / 2;

            while (bEnd == false)
            {
                //判断到车段线末尾距离是否还需要加灯
                //InsertLightService.ShowGeometry(ptOnLine, 221);
                if (StructureLayoutServiceLight.distToLineEnd(Lines, ptOnLine, out var PolylineToEnd) >= TolRangeMaxHalf)
                {
                    // bEnd = true;
                    ////建立当前点距离 tolLightRengeMax/2 前后TolLightRangeMin框
                    //var ExtendLineStart = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf - TolLightRangeMin);
                    //var ExtendLineEnd = ExtendLineStart + moveDir * (TolLightRangeMin * 2);
                    //var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                    //var ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane, 0, TolLane, 0);

                    ////建立当前点距离tolLightRengeMax/2 后TolLightRangeMin框
                    var ExtendLineStart = PolylineToEnd.GetPointAtDist(TolRangeMaxHalf);
                    var ExtendLineEnd = ExtendLineStart + moveDir * TolRangeMaxHalf;
                    var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                    var ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane, 0, TolLane, 0);

                    InsertLightService.ShowGeometry(ExtendPoly, 20);

                    Polyline tempStruct;
                    //找框内对面是否有位置布灯
                    var bAdded = StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);

                    if (bAdded == true)
                    {
                    
                        //框内对面有位置布灯
                        ThisLaneLayout.Add(tempStruct);
                        currSide = currSide == 0 ? 1 : 0;

                        if (bBothSide == true)
                        {
                            StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);
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
                        //debug, not tested yet
                        //框内对面没有位置布灯, 在自己边框内找
                        currSide = currSide == 0 ? 1 : 0;
                        bAdded = StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);

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

                            InsertLightService.ShowGeometry(ExtendPoly, 20);

                            currSide = currSide == 0 ? 1 : 0;
                            //找框内对面是否有位置布灯
                            bAdded = StructureLayoutServiceLight.FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolLightRangeMin, Layout, out tempStruct);

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

    }
}
