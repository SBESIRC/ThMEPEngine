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
        int TolLightRengeMax = 8500;


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
            bool debug = true;
            foreach (var l in mainLines)
            {
                List<Polyline> LayoutTemp = new List<Polyline>();
                //var lines = l.Select(x => x.Normalize()).ToList();
                var lines = l;
                //ParkingLinesService parkingLinesService = new ParkingLinesService();
                //var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //找到构建上可布置面,用第一条车道线的头尾判定
                CheckService checkService = new CheckService();
                var filterColmuns = checkService.FilterColumns(columns, lines.First(), frame);
                var filterWalls = checkService.FilterWalls(walls, lines.First(), frame);

                ////获取该车道线上的构建
                StructureServiceLight structureService = new StructureServiceLight();
                var lineColumn = structureService.GetStruct(lines, filterColmuns, TolLane);
                var lineWall = structureService.GetStruct(lines, filterWalls, TolLane);


                ////将构建分为上下部分
                var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, lines, TolLane);
                var usefulWalls = structureService.SeparateColumnsByLine(lineWall, lines, TolLane);

                ////for debug
                //InsertLightService.ShowGeometry(usefulColumns[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulColumns[1], 11, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[0], 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(usefulWalls[1], 11, LineWeight.LineWeight035);


                ////找出平均的一边. -1:no side 0:left 1:right.
                List<List<double>> columnDistList;
                int uniformSide = FindUniformDistributionSide(ref usefulColumns, lines, out columnDistList);

                //if (debug == true)
                //{
                //    uniformSide = -1;
                //}

                if (uniformSide == 0 || uniformSide == 1)
                {

                    LayoutUniformSide(usefulColumns[uniformSide], lines, columnDistList[uniformSide], out var uniformSideLayout, ref LayoutTemp);
                    LayoutOppositeSide(usefulColumns, usefulWalls, uniformSide, lines, columnDistList, uniformSideLayout, ref LayoutTemp);

                }
                else
                {
                    LayoutBothNonUniformSide(usefulColumns, usefulWalls, lines, ref LayoutTemp);
                }
                Layout.AddRange(LayoutTemp);



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

            //长度线
            if ((bLeft == true) && distList[0].Sum() / lineLength < TolUniformSideLenth)
            {
                bLeft = false;
            }

            if ((bRight == true) && distList[1].Sum() / lineLength < TolUniformSideLenth)
            {
                bRight = false;
            }

            //柱数量 > ((车道/平均柱距) * 0.5)
            if ((bLeft == true) && (3 < usefulColumns[0].Count() && distList[0].Count() < (lineLength / TolAvgColumnDist) * 0.5))
            {
                bLeft = false;
            }

            if ((bRight == true) && (3 < usefulColumns[1].Count() && distList[1].Count() < (lineLength / TolAvgColumnDist) * 0.5))
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

            // var orderColumns = Columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix).X).ToList();

            //debug , why it needs inverse??? how to build the matrix?????
            var orderColumns = Columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix.Inverse()).X).ToList();
            return orderColumns;
        }

        /// <summary>
        /// using dist between columns directly, may has bug if the angle of line in two columns with lines is too large
        /// </summary>
        /// <param name="Columns"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="Layout"></param>
        private void LayoutUniformSide(List<Polyline> Columns, List<Line> Lines, List<double> distList, out List<Polyline> uniformSideLayout, ref List<Polyline> LayoutTemp)
        {
            Polyline FirstLayout = getLayoutedStructrue(LayoutTemp, Lines);
            if (FirstLayout != null)
            {
                ////车线起始点已有布灯,第一个点顺延

            }

            ////车线起始点没有布灯,从均匀侧布灯
            int LastHasNoLightColumn = 0;
            List<Polyline> outputTemp = new List<Polyline>();
            outputTemp.Add(Columns[0]);
            double sum = 0;

            for (int i = 0; i < Columns.Count; i++)
            {
                if (i < Columns.Count - 1)
                {


                    sum += distList[i];
                    if (sum > TolLightRengeMax)
                    {
                        if (LastHasNoLightColumn != 0)
                        {
                            outputTemp.Add(Columns[i]);
                            LastHasNoLightColumn = 0;
                        }
                        else
                        {
                            LastHasNoLightColumn = i;
                        }
                        sum = distList[i];
                    }

                    if (distList[i] > TolLightRengeMax)
                    {

                        if (LastHasNoLightColumn != 0)
                        {
                            outputTemp.Add(Columns[i]);
                            outputTemp.Add(Columns[i + 1]);
                            LastHasNoLightColumn = 0;
                            sum = 0;
                        }
                        else
                        {
                            outputTemp.Add(Columns[i + 1]);
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
                        outputTemp.Add(Columns[i]);

                    }

                }

            }

            uniformSideLayout = outputTemp;
            LayoutTemp.AddRange(outputTemp.Distinct().ToList());
            InsertLightService.ShowGeometry(outputTemp, 70, LineWeight.LineWeight050);
        }

        private void LayoutOppositeSide(List<List<Polyline>> usefulColumns, List<List<Polyline>> usefulWalls, int uniformSide, List<Line> lines, List<List<double>> columnDistList, List<Polyline> uniformSideLayout, ref List<Polyline> LayoutTemp)
        {
            int nonuniformSide = uniformSide == 0 ? 1 : 0;

            //usefulWalls[0] = OrderingColumns(usefulWalls[0], lines);
            //usefulWalls[1] = OrderingColumns(usefulWalls[1], lines);

            List<Polyline> outputTemp = new List<Polyline>();
            List<Polyline> usefulSturct = new List<Polyline>();
            usefulSturct.AddRange(usefulWalls[nonuniformSide]);
            usefulSturct.AddRange(usefulColumns[nonuniformSide]);

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
                findClosestStruct(usefulSturct, CloestPt, out minDist, out closestStruct);
                outputTemp.Add(closestStruct);
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
                    findClosestStruct(usefulSturct, midPt, out minDist, out closestStruct);
                    outputTemp.Add(closestStruct);


                }
                else
                {
                    //均匀边每个分布
                    distToLine(lines, StructUtils.GetStructCenter(uniformSideLayout[i]), out CloestPt);
                    findClosestStruct(usefulSturct, CloestPt, out minDist, out closestStruct);
                    outputTemp.Add(closestStruct);

                }

            }

            //处理最后一个点.均匀边最后点投影车道线到尾如果大于tol,对面找点,否则不布点
            Polyline LastPartLines;
            double distToLinesEnd = distToLineEnd(lines, StructUtils.GetStructCenter(uniformSideLayout.Last()), out LastPartLines);
            if (distToLinesEnd > TolLightRengeMax)
            {
                var LastPrjPtOnLine = LastPartLines.GetPointAtDist(TolLightRengeMax);
                findClosestStruct(usefulSturct, LastPrjPtOnLine, out minDist, out closestStruct);
                outputTemp.Add(closestStruct);
            }


            InsertLightService.ShowGeometry(outputTemp, 210, LineWeight.LineWeight050);
            LayoutTemp.AddRange(outputTemp.Distinct().ToList());

        }
        private void findClosestStruct(List<Polyline> structure, Point3d Pt, out double minDist, out Polyline closestStruct)
        {
            minDist = 10000;
            closestStruct = structure[0];
            foreach (Polyline l in structure)
            {
                if (l.Distance(Pt) <= minDist)
                {
                    minDist = l.Distance(Pt);
                    closestStruct = l;
                }
            }
        }

        /// <summary>
        /// 找到给定点到lines尾的多线段和距离
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt1"></param>
        /// <param name="PolylineToEnd"></param>
        /// <returns></returns>
        private double distToLineEnd(List<Line> lines, Point3d pt1, out Polyline PolylineToEnd)
        {
            double distToEnd = 0;
            Point3d prjPt;
            PolylineToEnd = new Polyline();
            int timeToCheck = 0;

            foreach (Line l in lines)
            {
                //debug : if the wall's center point is project out of the lines
                prjPt = l.GetClosestPointTo(pt1, true);

                if (timeToCheck == 0 && l.ToCurve3d().IsOn(prjPt) == true)
                {

                    distToEnd = prjPt.DistanceTo(l.EndPoint);
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.EndPoint.ToPoint2D(), 0, 0, 0);
                    timeToCheck += 1;
                }
                else if (timeToCheck > 0)
                {
                    distToEnd += l.Length;
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.EndPoint.ToPoint2D(), 0, 0, 0);
                }

            }

            return distToEnd;

        }

        /// <summary>
        /// 找点到线的投影点
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt1"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        private double distToLine(List<Line> lines, Point3d pt1, out Point3d prjPt)
        {
            double distProject = 0;

            prjPt = lines[0].GetClosestPointTo(pt1, false);

            foreach (Line l in lines)
            {
                //debug : if the point can project to multiple lines, it will stop at the first time
                prjPt = l.GetClosestPointTo(pt1, true);


                if (l.ToCurve3d().IsOn(prjPt) == true)
                {
                    distProject = prjPt.DistanceTo(pt1);

                    break;
                }

            }


            return distProject;

        }

        /// <summary>
        /// has bug
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="prjMidPt"></param>
        /// <returns></returns>
        private double distAlongLine(List<Line> lines, Point3d pt1, Point3d pt2, out Point3d prjMidPt)
        {
            double distProject = 0;
            double distPart1 = 0;
            double distPart2 = 0;
            Point3d prjPt1;
            Point3d prjPt2;

            Polyline lineTemp = new Polyline();
            int timeToCheckPt1 = 0;
            int timeToCheckPt2 = 0;

            foreach (Line l in lines)
            {

                prjPt1 = l.GetClosestPointTo(pt1, true);
                prjPt2 = l.GetClosestPointTo(pt2, true);


                if (l.ToCurve3d().IsOn(prjPt1) == true)
                {
                    distProject = prjPt1.DistanceTo(prjPt2);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, prjPt1.ToPoint2D(), 0, 0, 0);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, prjPt2.ToPoint2D(), 0, 0, 0);

                    break;
                }
                else if (timeToCheckPt1 == 0 && l.ToCurve3d().IsOn(prjPt1) == true && l.ToCurve3d().IsOn(prjPt2) == false)
                {
                    //debug: not tested
                    distPart1 = prjPt1.DistanceTo(l.EndPoint);
                    distProject += distPart1;
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, prjPt1.ToPoint2D(), 0, 0, 0);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, l.EndPoint.ToPoint2D(), 0, 0, 0);
                    timeToCheckPt1 += 1;

                }
                else if (timeToCheckPt2 == 0 && l.ToCurve3d().IsOn(prjPt1) == false && l.ToCurve3d().IsOn(prjPt2) == true)
                {
                    //debug: not tested
                    distPart2 = prjPt2.DistanceTo(l.StartPoint);
                    distProject += distPart2;
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, prjPt1.ToPoint2D(), 0, 0, 0);
                    timeToCheckPt2 += 1;
                    break;
                }
                else if (timeToCheckPt1 > 0 && timeToCheckPt2 > 0)
                {
                    //debug: not tested


                    distProject += l.Length;
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, l.EndPoint.ToPoint2D(), 0, 0, 0);

                    timeToCheckPt1 += 1;
                    timeToCheckPt2 += 1;

                }
            }

            prjMidPt = lineTemp.GetPointAtDist(distProject / 2);
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

        private Polyline getLayoutedStructrue(List<Polyline> Layout, List<Line> Lines)
        {
            Polyline first = null;
            if (Layout.Count > 0)
            {

            }
            return first;
        }

        private void LayoutBothNonUniformSide(List<List<Polyline>> Columns, List<List<Polyline>> Walls, List<Line> Lines, ref List<Polyline> LayoutTemp)
        {
            Polyline FirstLayout = getLayoutedStructrue(LayoutTemp, Lines);
            if (FirstLayout != null)
            {
                ////车线起始点已有布灯,第一个点顺延

            }
            ////从一边开始
            List<List<Polyline>> usefulSturct = new List<List<Polyline>>();
            usefulSturct.Add(new List<Polyline>());
            usefulSturct[0].AddRange(Columns[0]);
            usefulSturct[0].AddRange(Walls[0]);
            usefulSturct.Add(new List<Polyline>());
            usefulSturct[1].AddRange(Columns[1]);
            usefulSturct[1].AddRange(Walls[1]);

            usefulSturct[0] = OrderingColumns(usefulSturct[0], Lines);
            usefulSturct[1] = OrderingColumns(usefulSturct[1], Lines);

            bool bEnd = false;
            Point3d ptOnLine;
            Point3d ExtendLineStart;
            int currSide = 0;

            List<Polyline> outputTemp = new List<Polyline>();

            //第一个点
            double distLeft = distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[0][0]), out var ptOnLineLeft);
            double distRight = distToLine(Lines, StructUtils.GetStructCenter(usefulSturct[1][0]), out var ptOnLineRight);

            if (distLeft <= distRight)
            {
                currSide = 0;
                ptOnLine = ptOnLineLeft;
            }
            else
            {
                currSide = 1;
                ptOnLine = ptOnLineRight;
            }

            outputTemp.Add(usefulSturct[currSide][0]);
            currSide = currSide == 0 ? 1 : 0;


            var moveDir = (Lines[0].EndPoint - Lines[0].StartPoint).GetNormal();
            bool bBothSide = false;
            while (bEnd == false)
            {
                //判断到车段线末尾距离是否还需要加灯
                //InsertLightService.ShowGeometry(ptOnLine, 221);

                if (distToLineEnd(Lines, ptOnLine, out var PolylineToEnd) >= TolLightRengeMax)
                {

                    //建立当前点距离tolLightRengeMax前后TolLightRangeMin框
                    ExtendLineStart = PolylineToEnd.GetPointAtDist(TolLightRengeMax - TolLightRangeMin);

                    var ExtendLineEnd = ExtendLineStart + moveDir * ((TolLightRengeMax - TolLightRangeMin) * 2);
                    var ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                    var ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane);

                    Polyline tempStruct;
                    //找框内对面是否有位置布灯
                    var bAdded = FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolLightRengeMax, out tempStruct);

                    if (bAdded == true)
                    {
                        //框内对面有位置布灯
                        outputTemp.Add(tempStruct);
                        currSide = currSide == 0 ? 1 : 0;

                        if (bBothSide == true)
                        {
                            FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolLightRengeMax, out tempStruct);
                            if (bAdded == true)
                            {
                                //框内对面有位置布灯
                                outputTemp.Add(tempStruct);
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
                        bAdded = FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolLightRengeMax, out tempStruct);

                        if (bAdded == true)
                        {
                            //框内自己边有位置布灯
                            outputTemp.Add(tempStruct);
                            currSide = currSide == 0 ? 1 : 0;
                            distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
                        }
                        else
                        {
                            //debug, not tested yet
                            //框内自己边没有, 找起点对面TolLightRengeMin内的布灯位置
                            ExtendLineStart = ptOnLine;
                            ExtendLineEnd = ExtendLineStart + moveDir * TolLightRangeMin;
                            ExtendLine = new Line(ExtendLineStart, ExtendLineEnd);
                            ExtendPoly = StructUtils.ExpandLine(ExtendLine, TolLane);
                            currSide = currSide == 0 ? 1 : 0;
                            //找框内对面是否有位置布灯
                            bAdded = FindPolyInExtendPoly(ExtendPoly, usefulSturct[currSide], PolylineToEnd, TolLightRangeMin, out tempStruct);

                            if (bAdded == true)
                            {
                                //debug, not tested yet
                                //框内对面有位置布灯
                                outputTemp.Add(tempStruct);
                                currSide = currSide == 0 ? 1 : 0;
                                distToLine(Lines, StructUtils.GetStructCenter(tempStruct), out ptOnLine);
                                ptOnLine = PolylineToEnd.GetPointAtDist(TolLightRengeMax);
                            }
                            else
                            {
                                //debug, not tested yet
                                //啥都没有
                                ptOnLine = PolylineToEnd.GetPointAtDist(TolLightRengeMax);

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
            InsertLightService.ShowGeometry(outputTemp, 40, LineWeight.LineWeight050);
            LayoutTemp.AddRange(outputTemp.Distinct().ToList());

        }
        private bool FindPolyInExtendPoly(Polyline ExtendPoly, List<Polyline> usefulSturct, Polyline PolylineToEnd, double Tol, out Polyline tempStruct)
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
                findClosestStruct(inExtendStruct, ExtendLineStart, out double minDist, out tempStruct);

                bReturn = true;

            }
            else
            {
                bReturn = false;
            }

            return bReturn;
        }

    }
}
