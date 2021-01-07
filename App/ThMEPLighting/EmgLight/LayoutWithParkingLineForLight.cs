using System;
using System.Linq;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
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
                var lanes = mainLines[i];
                //ParkingLinesService parkingLinesService = new ParkingLinesService();
                //var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //特别短的线跳过
                if (lanes[0].Length < TolLightRangeMin )
                {
                    continue;
                }

                //找到构建上可布置面,用第一条车道线的头尾判定
                var filterColmuns = CheckService.FilterColumns(columns, lanes.First(), frame);
                var filterWalls = CheckService.FilterWalls(walls, lanes.First(), frame);

                ////获取该车道线上的构建
                //StructureServiceLight structureService = new StructureServiceLight();
                var lineColumn = StructureServiceLight.GetStruct(lanes, filterColmuns, TolLane);
                var lineWall = StructureServiceLight.GetStruct(lanes, filterWalls, TolLane);


                ////将构建分为上下部分
                var usefulColumns = StructureServiceLight.SeparateColumnsByLine(lineColumn, lanes, TolLane);
                var usefulWalls = StructureServiceLight.SeparateColumnsByLine(lineWall, lanes, TolLane);

                if ((usefulColumns == null || usefulColumns .Count==0 ) && (usefulWalls == null || usefulWalls.Count == 0))
                {
                    continue;
                }
                ////for debug
                //InsertLightService.ShowGeometry(usefulColumns[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulColumns[1], 11, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[1], 11, LineWeight.LineWeight035);


                ////找出平均的一边. -1:no side 0:left 1:right.
                bool debug = false;

                if (debug == false)
                {

                    int uniformSide = FindUniformDistributionSide(ref usefulColumns, lanes, out var columnDistList);

                    if (uniformSide == 0 || uniformSide == 1)
                    {
                        LayoutUniformSide(usefulColumns, uniformSide, lanes, columnDistList, out var uniformSideLayout, ref Layout);
                        LayoutOppositeSide(usefulColumns, usefulWalls, uniformSide, lanes, columnDistList, uniformSideLayout, ref Layout);
                    }
                    else
                    {
                        LayoutBothNonUniformSide(usefulColumns, usefulWalls, lanes, ref Layout);
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
            usefulColumns[0] = OrderingColumns(usefulColumns[0], lines);
            usefulColumns[1] = OrderingColumns(usefulColumns[1], lines);

            distList = new List<List<double>>();
            distList.Add(GetColumnDistList(usefulColumns[0]));
            distList.Add(GetColumnDistList(usefulColumns[1]));

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

        private List<double> GetColumnDistList(List<Polyline> usefulOrderedColumns)
        {
            List<double> distX = new List<double>();
            for (int i = 0; i < usefulOrderedColumns.Count - 1; i++)
            {
                distX.Add((StructUtils.GetStructCenter(usefulOrderedColumns[i]) - StructUtils.GetStructCenter(usefulOrderedColumns[i + 1])).Length);
            }

            return distX;

        }
        private double GetVariance(List<double> distX)
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

        private List<Polyline> OrderingColumns(List<Polyline> Columns, List<Line> Lines)
        {
            Vector3d xDir = (Lines.First().EndPoint - Lines.First().StartPoint).GetNormal();
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
                });

            var orderColumns = Columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix.Inverse()).X).ToList();
            return orderColumns;
        }

        private Point3d TransformPointToLine(Point3d pt, List<Line> Lines)
        {
            //getAngleTo根据右手定则旋转(一般逆时针)
            var rotationangle = Vector3d.XAxis.GetAngleTo((Lines.Last().EndPoint - Lines.First().StartPoint), Vector3d.ZAxis);
            Matrix3d matrix = Matrix3d.Displacement(Lines.First().StartPoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

            var transedPt = pt.TransformBy(matrix.Inverse());

            return transedPt;
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

            usefulSturct = OrderingColumns(usefulSturct, lines);

            Polyline closestStruct;
            double minDist = 10000;
            Point3d CloestPt;
            Point3d midPt;

            //第一个点
            if (usefulColumns[uniformSide].IndexOf(uniformSideLayout[0]) == usefulColumns[uniformSide].IndexOf(uniformSideLayout[1]) - 1)
            {
                //均匀边每个分布
                distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[0]), out CloestPt);
                findClosestStruct(usefulSturct, CloestPt, Layout, out minDist, out closestStruct);
                nonUniformSideLayout.Add(closestStruct);
            }

            //从第二个点开始处理
            for (int i = 1; i < uniformSideLayout.Count; i++)
            {
                if (usefulColumns[uniformSide].IndexOf(uniformSideLayout[i - 1]) != usefulColumns[uniformSide].IndexOf(uniformSideLayout[i]) - 1)
                {
                    //均匀边隔柱分布
                    //distAlongLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i - 1]), StructUtils.GetStructCenter(uniformSideLayout[i]), out midPt);
                    findMidPointOnLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i - 1]), StructUtils.GetStructCenter(uniformSideLayout[i]), out midPt);

                    //遍历所有对面柱墙,可能会很慢.可在中点做buffer优化
                    findClosestStruct(usefulSturct, midPt, Layout, out minDist, out closestStruct);
                    nonUniformSideLayout.Add(closestStruct);


                }
                else
                {
                    //均匀边每个分布
                    distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i]), out CloestPt);
                    findClosestStruct(usefulSturct, CloestPt, Layout, out minDist, out closestStruct);
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

                distToLine(lines, StructUtils.GetStructCenter(usefulColumns[uniformSide].Last()), out var LastPrjPtOnLine);
                findClosestStruct(usefulSturct, LastPrjPtOnLine, Layout, out minDist, out closestStruct);
                if (nonUniformSideLayout.Contains(closestStruct) == false)
                {
                    nonUniformSideLayout.Add(closestStruct);
                }


            }


            InsertLightService.ShowGeometry(nonUniformSideLayout, 210, LineWeight.LineWeight050);
            Layout.AddRange(nonUniformSideLayout.Distinct().ToList());

        }

        private void findClosestStruct(List<Polyline> structure, Point3d Pt, List<Polyline> Layout , out double minDist, out Polyline closestStruct)
        {
            minDist = 10000;
            closestStruct = null;
            foreach (Polyline l in structure)
            {

                var connectLayout = Layout.Where(x => x.StartPoint == l.StartPoint ||
                                    x.StartPoint == l.EndPoint ||
                                    x.EndPoint == l.StartPoint ||
                                    x.EndPoint ==l.EndPoint).ToList();

              

                if  ( l.Distance(Pt) <= minDist)
                {
                    minDist = l.Distance(Pt);
                    if (connectLayout.Count >0)
                    {
                        closestStruct = connectLayout.First();
                    }
                    else {
                        closestStruct = l;
                    }
                   
                }
            }
        }

        /// <summary>
        /// 找到给定点投影到lanes尾的多线段和距离. 如果点在起点外,则返回投影到向前延长线到最末的距离和多线段.如果点在端点外,则返回点到端点的距离(负数)和多线段
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt1"></param>
        /// <param name="PolylineToEnd"></param>
        /// <returns></returns>
        private double distToLineEnd(List<Line> lines, Point3d pt, out Polyline PolylineToEnd)
        {
            double distToEnd = -1;
            Point3d prjPt;
            PolylineToEnd = new Polyline();
            int timeToCheck = 0;
            var ptNew = TransformPointToLine(pt, lines);
            List<Line> transLines = lines.Select(x => new Line(TransformPointToLine(x.StartPoint, lines), TransformPointToLine(x.EndPoint, lines))).ToList();

            if (ptNew.X < transLines.First().StartPoint.X)
            {
                prjPt = lines[0].GetClosestPointTo(pt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                foreach (var l in lines)
                {
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                }
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                distToEnd = PolylineToEnd.Length;
            }
            else if (ptNew.X > transLines.Last().EndPoint.X)
            {
                prjPt = lines.Last().GetClosestPointTo(pt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                distToEnd = -PolylineToEnd.Length;
            }
            else
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (timeToCheck == 0 && transLines[i].StartPoint.X <= ptNew.X && ptNew.X <= transLines[i].EndPoint.X)
                    {
                        prjPt = lines[i].GetClosestPointTo(pt, false);
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                        timeToCheck = 1;
                    }
                    else if (timeToCheck > 0)
                    {
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines[i].StartPoint.ToPoint2D(), 0, 0, 0);
                    }
                }

                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                distToEnd = PolylineToEnd.Length;
            }


            return distToEnd;

        }

        /// <summary>
        /// 找点到线的投影点,如果点在线外,则返回延长线上的投影点
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="pt1"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        private double distToLine(List<Line> lanes, Point3d pt, out Point3d prjPt)
        {
            double distProject = -1;
            var ptNew = TransformPointToLine(pt, lanes);
            prjPt = new Point3d();

            List<Line> transLines = lanes.Select(x => new Line(TransformPointToLine(x.StartPoint, lanes), TransformPointToLine(x.EndPoint, lanes))).ToList();


            if (ptNew.X < transLines.First().StartPoint.X)
            {
                prjPt = lanes[0].GetClosestPointTo(pt, true);

            }
            else if (ptNew.X > transLines.Last().EndPoint.X)
            {
                prjPt = lanes.Last().GetClosestPointTo(pt, true);

            }
            else
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    if (transLines[i].StartPoint.X <= ptNew.X && ptNew.X <= transLines[i].EndPoint.X)

                    {
                        prjPt = lanes[i].GetClosestPointTo(pt, false);
                        break;
                    }


                }
            }

            distProject = prjPt.DistanceTo(pt);
            return distProject;

        }

        private void findMidPointOnLine(List<Line> lines, Point3d pt1, Point3d pt2, out Point3d prjMidPt)
        {

            Point3d midPoint;


            Polyline lineTemp = new Polyline();

            midPoint = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            //InsertLightService.ShowGeometry (midPoint, 40);
            distToLine(lines, midPoint, out prjMidPt);

            // return distProject;

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

                importLayout[0] = OrderingColumns(importLayout[0], ExtendLineList);
                importLayout[1] = OrderingColumns(importLayout[1], ExtendLineList);

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

                FilteredLayout = OrderingColumns(FilteredLayout, ExtendLineList);
                fisrtStruct = FilteredLayout.Last();
                distToLine(Lines, StructUtils.GetStructCenter(fisrtStruct), out ptOnLine);

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

                double distLeft = distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[0][0]), out var ptOnLineLeft);
                double distRight = distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[1][0]), out var ptOnLineRight);

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

            usefulSturct[0] = OrderingColumns(usefulSturct[0], Lines);
            usefulSturct[1] = OrderingColumns(usefulSturct[1], Lines);

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
                if (distToLineEnd(Lines, ptOnLine, out var PolylineToEnd) >= TolRangeMaxHalf)
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
                    var bAdded = FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);

                    if (bAdded == true)
                    {
                    
                        //框内对面有位置布灯
                        ThisLaneLayout.Add(tempStruct);
                        currSide = currSide == 0 ? 1 : 0;

                        if (bBothSide == true)
                        {
                            FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);
                            if (bAdded == true)
                            {
                                //框内对面有位置布灯
                                ThisLaneLayout.Add(tempStruct);
                                currSide = currSide == 0 ? 1 : 0;

                            }

                        }
                       
                            distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
                       


                    }
                    else
                    {
                        //debug, not tested yet
                        //框内对面没有位置布灯, 在自己边框内找
                        currSide = currSide == 0 ? 1 : 0;
                        bAdded = FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolRangeMaxHalf, Layout, out tempStruct);

                        if (bAdded == true)
                        {
                            //框内自己边有位置布灯
                            ThisLaneLayout.Add(tempStruct);
                            currSide = currSide == 0 ? 1 : 0;
                            distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
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
                            bAdded = FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolLightRangeMin, Layout, out tempStruct);

                            if (bAdded == true)
                            {
                                //debug, not tested yet
                                //框内对面有位置布灯
                                ThisLaneLayout.Add(tempStruct);
                                currSide = currSide == 0 ? 1 : 0;
                                distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
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

        private bool FindPolyInExtendPoly(Polyline ExtendPoly, List<Polyline> usefulSturct, Polyline PolylineToEnd, double Tol, List<Polyline> Layout, out Polyline tempStruct)
        {
            bool bReturn = false;
            var inExtendStruct = usefulSturct.Where(x =>
               {
                   return ExtendPoly.Contains(x) || ExtendPoly.Intersects(x);
               }).ToList();

            tempStruct = null;
            if (inExtendStruct.Count > 0)
            {
                //框内对面有位置布灯
                var ExtendLineStart = PolylineToEnd.GetPointAtDist(Tol);
                findClosestStruct(inExtendStruct, ExtendLineStart, Layout, out double minDist, out tempStruct);

            }
            if (tempStruct != null)
            {
                bReturn = true;
            }
            else
            {
                bReturn = false;
            }

            return bReturn;
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
