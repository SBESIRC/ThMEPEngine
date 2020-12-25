using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using ThMEPElectrical.EmgLight.Service;
using Linq2Acad;
using ThCADCore.NTS;


namespace ThMEPElectrical.EmgLight
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
                var lines = l.Select(x => x.Normalize()).ToList();
                ThMEPElectrical.Broadcast.ParkingLinesService parkingLinesService = new ThMEPElectrical.Broadcast.ParkingLinesService();
                var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //找到构建上可布置面,用第一条车道线的头尾判定,可能有bug
                CheckService checkService = new CheckService();
                var filterColmuns = checkService.FilterColumns(columns, lines.First(), frame);
                var filterWalls = checkService.FilterWalls(walls, lines.First(), frame);

                //InsertLightService.ShowGeometry(filterColmuns, 30, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(filterWalls, 210, LineWeight.LineWeight035);

                ////获取该车道线上的构建
                StructureServiceLight structureService = new StructureServiceLight();
                var lineColumn = structureService.GetStruct(lines, filterColmuns, TolLane);
                var lineWall = structureService.GetStruct(lines, filterWalls, TolLane);
                //InsertLightService.ShowGeometry(lineColumn, 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(lineWall, 11, LineWeight.LineWeight035);

                ////将构建分为上下部分
                var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, lines, TolLane);
                var usefulWalls = structureService.SeparateColumnsByLine(lineWall, lines, TolLane);

                ////for debug
                InsertLightService.ShowGeometry(usefulColumns[0], 142, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(usefulColumns[1], 11, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(usefulWalls[0], 142, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(usefulWalls[1], 11, LineWeight.LineWeight035);

                if (debug == true)
                {
                    ////找出平均的一边. -1:no side 0:left 1:right.
                    List<List<double>> columnDistList;
                    int uniformSide = FindUniformDistributionSide(ref usefulColumns, lines, out columnDistList);

                    if (uniformSide == 0 || uniformSide == 1)
                    {

                        LayoutUniformSide(usefulColumns[uniformSide], lines, columnDistList[uniformSide], ref LayoutTemp);
                        LayoutOppositeSide(usefulColumns, usefulWalls, uniformSide, lines, columnDistList, ref LayoutTemp);

                    }
                    else
                    {
                        // LayoutBothNonUniformSide();
                    }
                    Layout.AddRange(LayoutTemp);
                    InsertLightService.ShowGeometry(Layout, 10, LineWeight.LineWeight050);
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

            var orderColumns = Columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix).X).ToList();

            return orderColumns;
        }

        /// <summary>
        /// using dist between columns directly, may has bug if the angle of line in two columns with lines is too large
        /// </summary>
        /// <param name="Columns"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="Layout"></param>
        private void LayoutUniformSide(List<Polyline> Columns, List<Line> Lines, List<double> distList, ref List<Polyline> LayoutTemp)
        {
            Polyline FirstLayout = checkIfHasFirstLight(LayoutTemp, Lines);
            if (FirstLayout != null)
            {
                ////车线起始点已有布灯,第一个点顺延

            }

            ////车线起始点没有布灯,从均匀侧布灯
            int LastHasNoLightColumn = 0;
            LayoutTemp.Add(Columns[0]);
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
                            LayoutTemp.Add(Columns[i]);
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
                            LayoutTemp.Add(Columns[i]);
                            LayoutTemp.Add(Columns[i + 1]);
                            LastHasNoLightColumn = 0;
                            sum = 0;
                        }
                        else
                        {
                            LayoutTemp.Add(Columns[i + 1]);
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
                        LayoutTemp.Add(Columns[i]);

                    }

                }

                LayoutTemp = LayoutTemp.Distinct().ToList();
            }
        }

        private void LayoutOppositeSide(List<List<Polyline>> usefulColumns, List<List<Polyline>> usefulWalls, int uniformSide, List<Line> lines, List<List<double>> columnDistList, ref List<Polyline> LayoutTemp)
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
            if (usefulColumns[uniformSide].IndexOf(LayoutTemp[0]) == usefulColumns[uniformSide].IndexOf(LayoutTemp[1]) - 1)
            {
                //均匀边每个分布
                distToLine(lines, StructUtils.GetStructCenter(LayoutTemp[0]), out CloestPt);
                findCloseStruct(usefulSturct, CloestPt, out minDist, out closestStruct);
                outputTemp.Add(closestStruct);
            }

            //从第二个点开始处理
            for (int i = 1; i < LayoutTemp.Count; i++)
            {
                if (usefulColumns[uniformSide].IndexOf(LayoutTemp[i - 1]) != usefulColumns[uniformSide].IndexOf(LayoutTemp[i]) - 1)
                {
                    //均匀边隔柱分布
                    distAlongLine(lines, StructUtils.GetStructCenter(LayoutTemp[i - 1]), StructUtils.GetStructCenter(LayoutTemp[i]), out midPt);

                    //遍历所有对面柱墙,可能会很慢.可在中点做buffer优化
                    findCloseStruct(usefulSturct, midPt, out minDist, out closestStruct);
                    outputTemp.Add(closestStruct);


                }
                else
                {
                    //均匀边每个分布
                    distToLine(lines, StructUtils.GetStructCenter(LayoutTemp[i]), out CloestPt);
                    findCloseStruct(usefulSturct, CloestPt, out minDist, out closestStruct);
                    outputTemp.Add(closestStruct);

                }

            }

            //处理最后一个点.均匀边最后点投影车道线到尾如果大于tol,对面找点,否则不布点
            Polyline LastPartLines;
            double distToLinesEnd = distToLineEnd(lines, StructUtils.GetStructCenter(LayoutTemp.Last()), out LastPartLines);
            if (distToLinesEnd > TolLightRengeMax)
            {
                var LastPrjPtOnLine = LastPartLines.GetPointAtDist(TolLightRengeMax);
                findCloseStruct(usefulSturct, LastPrjPtOnLine, out minDist, out closestStruct);
                outputTemp.Add(closestStruct);
            }



            LayoutTemp.AddRange(outputTemp.Distinct().ToList());
        }
        private void findCloseStruct(List<Polyline> structure, Point3d Pt, out double minDist, out Polyline closestStruct)
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
                //debug : GetClosestPointTo: not project point, if the point out of line, it will use the end-point
                prjPt = l.GetClosestPointTo(pt1, true);


                if (timeToCheck ==0 && l.ToCurve3d().IsOn(prjPt) == true)
                {
                    InsertLightService.ShowGeometry(prjPt, 221);
                    distToEnd = prjPt.DistanceTo(l.EndPoint);
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.EndPoint.ToPoint2D(), 0, 0, 0);
                    timeToCheck += 1;
                }
                else if (timeToCheck>0)
                {
                    distToEnd += l.Length;
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.EndPoint.ToPoint2D(), 0, 0, 0);
                }

            }
            InsertLightService.ShowGeometry(PolylineToEnd, 221,LineWeight.LineWeight040);

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
                //debug : GetClosestPointTo: not project point, if the point out of line, it will use the end-point
                prjPt = l.GetClosestPointTo(pt1, true);
                

                if ( l.ToCurve3d().IsOn (prjPt)==true)
                {
                    distProject = prjPt.DistanceTo(pt1);

                    break;
                }

            }


            return distProject;

        }
        private double distAlongLine(List<Line> lines, Point3d pt1, Point3d pt2, out Point3d prjMidPt)
        {
            double distProject = 0;
            double distPart1 = 0;
            double distPart2 = 0;
            Point3d prjPt1;
            Point3d prjPt2;

            Polyline lineTemp = new Polyline();
            int timeToCheckPt1 =0;
            int timeToCheckPt2 = 0;

            foreach (Line l in lines)
            {
                //debug : GetClosestPointTo: not project point, if the point out of line, it will use the end-point
                prjPt1 = l.GetClosestPointTo(pt1, true);
                prjPt2 = l.GetClosestPointTo(pt2, true);


                if (l.ToCurve3d().IsOn(prjPt1) == true)
                {
                    distProject = prjPt1.DistanceTo(prjPt2);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, prjPt1.ToPoint2D(), 0, 0, 0);
                    lineTemp.AddVertexAt(lineTemp.NumberOfVertices, prjPt2.ToPoint2D(), 0, 0, 0);
             
                    break;
                }
                else if (timeToCheckPt1 ==0 && l.ToCurve3d().IsOn(prjPt1) == true && l.ToCurve3d().IsOn(prjPt2) == false)
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
                else if (timeToCheckPt1 >0 && timeToCheckPt2 >0 )
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

        private Polyline checkIfHasFirstLight(List<Polyline> Layout, List<Line> Lines)
        {
            Polyline first = null;
            if (Layout.Count > 0)
            {

            }
            return first;
        }

        private List<Polyline> everyOtherColumns(List<Polyline> Columns)
        {
            var everyOtherColumns = Columns.Where(x => (Columns.IndexOf(x) % 2) == 0).ToList();

            return everyOtherColumns;
        }



    }
}
